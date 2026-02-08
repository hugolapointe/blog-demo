using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class AggregationDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {
        await BasicAggregationsAsync();
        await GroupByAsync();
        await GroupByWithHavingAsync();
    }

    async Task BasicAggregationsAsync() {

        // Agrégations exécutées côté SQL (pas en mémoire)
        // Count, Sum, Average, Max, Min retournent une valeur scalaire
        var totalArticles = await Context.Articles.CountAsync();
        var totalAuthors = await Context.Authors.CountAsync();
        var avgArticlesPerAuthor = totalArticles / (double)totalAuthors;

        // Combiner Select() et agrégations pour calculs complexes
        var maxCommentCount = await Context.Articles
            .Select(article => article.Comments.Count)
            .MaxAsync();
    }

    async Task GroupByAsync() {

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
    }

    async Task GroupByWithHavingAsync() {

        // Distinction importante :
        // Where() AVANT GroupBy() → filtre les lignes (WHERE en SQL)
        // Where() APRÈS GroupBy() → filtre les groupes (HAVING en SQL)
        var authorsWithMultipleArticles = await Context.Articles
            .GroupBy(article => article.Author!.Name)
            .Where(group => group.Count() > 1)  // HAVING COUNT(*) > 1
            .Select(group => new {
                AuthorName = group.Key,
                ArticleCount = group.Count()
            })
            .ToListAsync();
    }
}
