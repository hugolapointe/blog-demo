using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class AggregationDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAsync() {
        WriteTitle("=== AGGREGATION ===");

        await BasicAggregationsAsync();
        await GroupByAsync();
        await GroupByWithHavingAsync();
    }

    async Task BasicAggregationsAsync() {
        WriteTitle("--- Agrégations de base ---");

        // Count, Sum, Average, Max, Min s'exécutent côté SQL
        var totalArticles = await Context.Articles.CountAsync();
        var totalComments = await Context.Comments.CountAsync();
        var avgCommentsPerArticle = totalComments / (double)totalArticles;

        Console.WriteLine($"Total articles: {totalArticles}");
        Console.WriteLine($"Total comments: {totalComments}");
        Console.WriteLine($"Moyenne comments/article: {avgCommentsPerArticle:F1}");

        // Avec Select() pour calculer en SQL
        var maxCommentCount = await Context.Articles
            .Select(article => article.Comments.Count)
            .MaxAsync();

        Console.WriteLine($"Max comments sur un article: {maxCommentCount}");
    }

    async Task GroupByAsync() {
        WriteTitle("--- GroupBy (Regroupement) ---");

        // GroupBy() regroupe les données et permet des agrégations par groupe
        // SQL: SELECT AuthorId, COUNT(*) FROM Articles GROUP BY AuthorId
        var articlesByAuthor = await Context.Articles
            .GroupBy(article => article.Author!.Name)
            .Select(group => new {
                AuthorName = group.Key,
                ArticleCount = group.Count(),
                TotalComments = group.Sum(article => article.Comments.Count)
            })
            .ToListAsync();

        Console.WriteLine("Articles par auteur:");
        foreach (var stats in articlesByAuthor) {
            Console.WriteLine($"  {stats.AuthorName}: {stats.ArticleCount} articles, {stats.TotalComments} comments");
        }
    }

    async Task GroupByWithHavingAsync() {
        WriteTitle("--- GroupBy + Having (Filtrer les groupes) ---");

        // Where() avant GroupBy() = filtrer les lignes
        // Where() après GroupBy() = filtrer les groupes (HAVING en SQL)
        var authorsWithMultipleArticles = await Context.Articles
            .GroupBy(article => article.Author!.Name)
            .Where(group => group.Count() > 1)  // HAVING COUNT(*) > 1
            .Select(group => new {
                AuthorName = group.Key,
                ArticleCount = group.Count()
            })
            .ToListAsync();

        Console.WriteLine("Auteurs avec plusieurs articles:");
        foreach (var stats in authorsWithMultipleArticles) {
            Console.WriteLine($"  {stats.AuthorName}: {stats.ArticleCount} articles");
        }
    }
}
