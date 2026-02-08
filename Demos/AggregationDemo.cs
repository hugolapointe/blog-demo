using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public class AggregationDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {
        await BasicAggregationsAsync();
        await GroupByAsync();
        await GroupByWithHavingAsync();
    }

    // [AGRÉGATIONS] Count, Sum, Average, Max, Min → valeurs scalaires côté SQL
    async Task BasicAggregationsAsync() {
        var totalArticles = await Context.Articles.CountAsync();
        var totalAuthors = await Context.Authors.CountAsync();
        var avgArticlesPerAuthor = totalArticles / (double)totalAuthors;

        Debug.Assert(totalArticles == 3);
        Debug.Assert(totalAuthors == 2);
        Debug.Assert(avgArticlesPerAuthor == 1.5);

        // Select() + agrégation pour calculs complexes
        var maxCommentCount = await Context.Articles
            .Select(article => article.Comments.Count)
            .MaxAsync();

        Debug.Assert(maxCommentCount == 2);
    }

    // [GROUP BY] Regrouper par critère et agréger
    // SQL: SELECT AuthorName, COUNT(*), SUM(...) FROM Articles GROUP BY AuthorName
    async Task GroupByAsync() {
        var articlesByAuthor = await Context.Articles
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
    async Task GroupByWithHavingAsync() {
        var authorsWithMultipleArticles = await Context.Articles
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
