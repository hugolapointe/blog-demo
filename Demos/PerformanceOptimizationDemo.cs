using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class PerformanceOptimizationDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAsync() {
        WriteTitle("=== PERFORMANCE OPTIMIZATION ===");

        await TrackingVsNoTrackingAsync();
        await ProjectionAsync();
        await CartesianProductAsync();
        await SplitQueriesAsync();
    }

    async Task TrackingVsNoTrackingAsync() {
        WriteTitle("--- Tracking vs NoTracking ---");

        // Avec tracking (par défaut): EF Core crée un snapshot pour détecter les changements
        var articlesTracked = await Context.Articles.ToListAsync();
        Console.WriteLine($"Avec tracking: {articlesTracked.Count} articles");

        Context.ChangeTracker.Clear();

        // Sans tracking: pas de snapshot, plus rapide, moins de mémoire
        var articlesNoTracking = await Context.Articles.AsNoTracking().ToListAsync();
        Console.WriteLine($"Sans tracking: {articlesNoTracking.Count} articles");

        // Utiliser AsNoTracking() pour lectures seules (affichage, rapports)
    }

    async Task ProjectionAsync() {
        WriteTitle("--- Projection (Select) ---");

        // Sans projection: toutes les colonnes chargées
        var fullEntities = await Context.Articles
            .Include(article => article.Author)
            .ToListAsync();
        Console.WriteLine($"Sans projection: {fullEntities.Count} articles complets");

        Context.ChangeTracker.Clear();

        // Avec projection: uniquement les colonnes nécessaires
        // Pas de tracking, agrégations en SQL
        var projected = await Context.Articles
            .Select(article => new {
                article.Title,
                AuthorName = article.Author!.Name,
                CommentCount = article.Comments.Count
            })
            .ToListAsync();

        Console.WriteLine($"Avec projection: {projected.Count} objets légers");
        foreach (var summary in projected) {
            Console.WriteLine($"  {summary.Title} - {summary.AuthorName} ({summary.CommentCount} comments)");
        }
    }

    async Task CartesianProductAsync() {
        WriteTitle("--- Cartesian Product (Problème) ---");

        // Include de plusieurs collections → produit cartésien
        // 2 Comments × 3 Tags = 6 lignes retournées par SQL
        var articles = await Context.Articles
            .Include(article => article.Comments)
            .Include(article => article.Tags)
            .ToListAsync();

        var article = articles.First();
        Console.WriteLine($"{article.Title}:");
        Console.WriteLine($"  Comments: {article.Comments.Count}");
        Console.WriteLine($"  Tags: {article.Tags.Count}");
        Console.WriteLine($"  Lignes SQL: {article.Comments.Count} × {article.Tags.Count} = {article.Comments.Count * article.Tags.Count}");
    }

    async Task SplitQueriesAsync() {
        WriteTitle("--- Split Queries (Solution) ---");

        Context.ChangeTracker.Clear();

        // AsSplitQuery() génère 3 requêtes séparées
        // 1. Articles, 2. Comments, 3. Tags
        var articles = await Context.Articles
            .Include(article => article.Comments)
            .Include(article => article.Tags)
            .AsSplitQuery()
            .ToListAsync();

        var article = articles.First();
        Console.WriteLine($"{article.Title}:");
        Console.WriteLine($"  3 requêtes séparées");
        Console.WriteLine($"  Lignes totales: 1 + {article.Comments.Count} + {article.Tags.Count} = {1 + article.Comments.Count + article.Tags.Count}");
    }
}
