using BlogDemo.Data;
using BlogDemo.Domain;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

// [DELETE BEHAVIOR] Démonstration des comportements de suppression configurés dans le DbContext
// Restrict : empêche la suppression d'un parent ayant des enfants
// Cascade : supprime automatiquement les enfants (comportement par défaut)
public static class DeleteBehaviorDemo {
    public static async Task Run(BlogDbContext context) {
        context.ChangeTracker.Clear();

        await RestrictDeleteAsync(context);
        await CascadeDeleteAsync(context);
    }

    // [RESTRICT] OnDelete(DeleteBehavior.Restrict) sur Author → Articles
    // Supprimer un Author qui a des Articles lève une DbUpdateException
    static async Task RestrictDeleteAsync(BlogDbContext context) {
        // Ne PAS inclure les Articles pour que l'erreur vienne de la BD (pas du ChangeTracker)
        var author = await context.Authors
            .FirstAsync(a => a.Name == "Alice Dupont");

        context.Authors.Remove(author);

        var restrictThrew = false;
        try {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) {
            restrictThrew = true;
        }

        Debug.Assert(restrictThrew);

        context.ChangeTracker.Clear();

        var authorStillExists = await context.Authors.AnyAsync(a => a.Name == "Alice Dupont");
        Debug.Assert(authorStillExists);
    }

    // [CASCADE] Comportement par défaut sur Article → Comments
    // Supprimer un Article supprime automatiquement ses Comments
    static async Task CascadeDeleteAsync(BlogDbContext context) {
        var article = await context.Articles
            .Include(a => a.Comments)
            .FirstAsync(a => a.Title == "Introduction à EF Core");

        Debug.Assert(article.Comments.Count == 2);

        context.Articles.Remove(article);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var commentsRemaining = await context.Set<Comment>()
            .CountAsync(c => c.ArticleId == article.Id);
        Debug.Assert(commentsRemaining == 0);

        var articleExists = await context.Articles.AnyAsync(a => a.Title == "Introduction à EF Core");
        Debug.Assert(!articleExists);
    }
}
