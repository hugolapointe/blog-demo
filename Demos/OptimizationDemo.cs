using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public class OptimizationDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {

        await TrackingVsNoTrackingAsync();
        await ProjectionAsync();
        await CartesianProductAsync();
        await SplitQueriesAsync();
    }

    async Task TrackingVsNoTrackingAsync() {

        // Coût en mémoire et performance
        var articlesTracked = await Context.Articles.ToListAsync();
        Debug.Assert(articlesTracked.Count > 0);

        Context.ChangeTracker.Clear();

        // AsNoTracking() : pas de snapshot, plus rapide, moins de mémoire
        var articlesNoTracking = await Context.Articles.AsNoTracking().ToListAsync();
    }

    async Task ProjectionAsync() {

        // Sans projection: toutes les colonnes chargées (inefficace)
        var fullEntities = await Context.Articles
            .Include(article => article.Author)
            .ToListAsync();
        Debug.Assert(fullEntities.Count > 0);

        Context.ChangeTracker.Clear();

        // Avantages : pas de tracking, agrégations en SQL, objets légers
        var projected = await Context.Articles
            .Select(article => new {
                article.Title,
                AuthorName = article.Author!.Name,
                CommentCount = article.Comments.Count
            })
            .ToListAsync();
    }

    async Task CartesianProductAsync() {

        // PROBLÈME : Include de plusieurs collections → produit cartésien
        // SQL génère un JOIN qui multiplie les lignes
        // 2 Comments × 3 Tags = 6 lignes retournées (duplication de données)
        var articles = await Context.Articles
            .Include(article => article.Comments)
            .Include(article => article.Tags)
            .ToListAsync();

        var article = articles.First();
    }

    async Task SplitQueriesAsync() {

        Context.ChangeTracker.Clear();

        // SOLUTION au produit cartésien : AsSplitQuery()
        // Génère 3 requêtes séparées au lieu d'un seul JOIN
        // 1. Articles, 2. Comments, 3. Tags
        var articles = await Context.Articles
            .Include(article => article.Comments)
            .Include(article => article.Tags)
            .AsSplitQuery()
            .ToListAsync();

        var article = articles.First();
    }
}
