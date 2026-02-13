using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public static class AggregationDemo {
    public static async Task RunAsync(BlogDbContext context) {
        await BasicAggregationsAsync(context);
        await GroupByAsync(context);
        await GroupByWithHavingAsync(context);
    }

    // [AGRÉGATIONS] Count, Sum, Average, Max, Min → valeurs scalaires côté SQL
    static async Task BasicAggregationsAsync(BlogDbContext context) {
        var totalArticles = await context.Articles.CountAsync();
        var totalAuthors = await context.Authors.CountAsync();

        // À NE PAS FAIRE : calculer la moyenne manuellement en C# (2 requêtes + calcul côté client)
        var avgManual = totalArticles / (double)totalAuthors;
        Debug.Assert(avgManual == 1.5);

        // BONNE PRATIQUE : AverageAsync() délègue le calcul au moteur SQL (fonction AVG)
        // Une seule requête, le calcul est fait côté serveur
        var avgArticlesPerAuthor = await context.Authors
            .Select(a => (double)a.Articles.Count)
            .AverageAsync();

        Debug.Assert(avgArticlesPerAuthor == 1.5);

        // MaxAsync() côté SQL — évite de charger toutes les entités pour trouver le max
        var maxCommentCount = await context.Articles
            .Select(article => article.Comments.Count)
            .MaxAsync();

        Debug.Assert(maxCommentCount == 2);
    }

    // [GROUP BY] Regrouper par critère et agréger
    // SQL: SELECT AuthorName, COUNT(*), SUM(...) FROM Articles GROUP BY AuthorName
    static async Task GroupByAsync(BlogDbContext context) {
        var articlesByAuthor = await context.Articles
            .GroupBy(article => article.Author!.Name)
            .Select(group => new {
                AuthorName = group.Key,
                ArticleCount = group.Count(),
                TotalComments = group.Sum(article => article.Comments.Count)
            })
            .ToListAsync();

        Debug.Assert(articlesByAuthor.Count == 2);

        var alice = articlesByAuthor.First(a => a.AuthorName == "Alice Dupont");
        Debug.Assert(alice.ArticleCount == 2);
        Debug.Assert(alice.TotalComments == 3);
    }

    // [HAVING] Where() APRÈS GroupBy() → filtre les groupes (HAVING en SQL)
    // Where() AVANT GroupBy() → filtre les lignes (WHERE en SQL)
    static async Task GroupByWithHavingAsync(BlogDbContext context) {
        var authorsWithMultipleArticles = await context.Articles
            .GroupBy(article => article.Author!.Name)
            .Where(group => group.Count() > 1)
            .Select(group => new {
                AuthorName = group.Key,
                ArticleCount = group.Count()
            })
            .ToListAsync();

        Debug.Assert(authorsWithMultipleArticles.Count == 1);
        Debug.Assert(authorsWithMultipleArticles.First().AuthorName == "Alice Dupont");
        Debug.Assert(authorsWithMultipleArticles.First().ArticleCount == 2);
    }
}
