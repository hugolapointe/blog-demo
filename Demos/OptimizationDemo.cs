using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public static class OptimizationDemo {
    public static async Task RunAsync(BlogDbContext context) {
        await TrackingVsNoTrackingAsync(context);
        await ProjectionAsync(context);
        await CartesianProductAsync(context);
    }

    // [TRACKING vs NO TRACKING] AsNoTracking() désactive le suivi → plus rapide, moins de mémoire
    // Utiliser pour les lectures seules (affichage, rapports)
    static async Task TrackingVsNoTrackingAsync(BlogDbContext context) {
        // Avec tracking : EF Core conserve un snapshot pour détecter les changements
        var articlesTracked = await context.Articles.ToListAsync();
        Debug.Assert(articlesTracked.Count > 0);
        Debug.Assert(context.ChangeTracker.Entries().Any());

        context.ChangeTracker.Clear();

        // Sans tracking : pas de snapshot, l'entité ne peut pas être modifiée via SaveChanges
        var articlesNoTracking = await context.Articles.AsNoTracking().ToListAsync();
        Debug.Assert(articlesNoTracking.Count > 0);
        Debug.Assert(!context.ChangeTracker.Entries().Any());
    }

    // [PROJECTION] Select() charge uniquement les colonnes nécessaires
    // Pas de tracking, agrégations côté SQL, objets légers
    static async Task ProjectionAsync(BlogDbContext context) {
        // Sans projection : toutes les colonnes sont chargées
        var fullEntities = await context.Articles
            .Include(a => a.Author)
            .ToListAsync();
        Debug.Assert(fullEntities.Count > 0);

        context.ChangeTracker.Clear();

        // Avec projection : seules les colonnes sélectionnées sont retournées
        var projected = await context.Articles
            .Select(a => new {
                a.Title,
                AuthorName = a.Author!.Name,
                CommentCount = a.Comments.Count
            })
            .ToListAsync();

        Debug.Assert(projected.Count > 0);
        Debug.Assert(projected.First().Title != null);
        Debug.Assert(projected.First().CommentCount >= 0);
    }

    // [PRODUIT CARTÉSIEN vs SPLIT QUERIES]
    // Include de plusieurs collections génère un JOIN qui multiplie les lignes
    // Ex: 2 Comments × 2 Tags = 4 lignes retournées pour 1 article (duplication)
    static async Task CartesianProductAsync(BlogDbContext context) {
        // À NE PAS FAIRE avec plusieurs collections : un seul JOIN crée un produit cartésien
        var articles = await context.Articles
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .ToListAsync();

        var article = articles.First();
        Debug.Assert(article.Comments.Count >= 0);
        Debug.Assert(article.Tags.Count >= 0);

        context.ChangeTracker.Clear();

        // BONNE PRATIQUE : AsSplitQuery() génère des requêtes séparées, évite la duplication
        var articlesSplit = await context.Articles
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .AsSplitQuery()
            .ToListAsync();

        var articleSplit = articlesSplit.First();
        Debug.Assert(articleSplit.Comments.Count >= 0);
        Debug.Assert(articleSplit.Tags.Count >= 0);
    }
}
