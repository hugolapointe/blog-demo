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

    // [TRACKING vs NO TRACKING] AsNoTracking() désactive le suivi → plus rapide, moins de mémoire
    // Utiliser pour les lectures seules (affichage, rapports)
    async Task TrackingVsNoTrackingAsync() {
        // Avec tracking : EF Core conserve un snapshot pour détecter les changements
        var articlesTracked = await Context.Articles.ToListAsync();
        Debug.Assert(articlesTracked.Count > 0);
        Debug.Assert(Context.ChangeTracker.Entries().Any());

        Context.ChangeTracker.Clear();

        // Sans tracking : pas de snapshot, l'entité ne peut pas être modifiée via SaveChanges
        var articlesNoTracking = await Context.Articles.AsNoTracking().ToListAsync();
        Debug.Assert(articlesNoTracking.Count > 0);
        Debug.Assert(!Context.ChangeTracker.Entries().Any());
    }

    // [PROJECTION] Select() charge uniquement les colonnes nécessaires
    // Pas de tracking, agrégations côté SQL, objets légers
    async Task ProjectionAsync() {
        // Sans projection : toutes les colonnes sont chargées
        var fullEntities = await Context.Articles
            .Include(a => a.Author)
            .ToListAsync();
        Debug.Assert(fullEntities.Count > 0);

        Context.ChangeTracker.Clear();

        // Avec projection : seules les colonnes sélectionnées sont retournées
        var projected = await Context.Articles
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

    // [PRODUIT CARTÉSIEN] Include de plusieurs collections → JOIN qui multiplie les lignes
    // Ex: 2 Comments × 2 Tags = 4 lignes retournées pour 1 article (duplication)
    async Task CartesianProductAsync() {
        var articles = await Context.Articles
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .ToListAsync();

        var article = articles.First();
        Debug.Assert(article.Comments.Count >= 0);
        Debug.Assert(article.Tags.Count >= 0);
    }

    // [SPLIT QUERIES] AsSplitQuery() génère des requêtes séparées au lieu d'un seul JOIN
    // Solution au produit cartésien : 1 requête par Include au lieu d'un JOIN unique
    async Task SplitQueriesAsync() {
        Context.ChangeTracker.Clear();

        var articles = await Context.Articles
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .AsSplitQuery()
            .ToListAsync();

        var article = articles.First();
        Debug.Assert(article.Comments.Count >= 0);
        Debug.Assert(article.Tags.Count >= 0);
    }
}
