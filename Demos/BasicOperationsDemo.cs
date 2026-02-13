using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public static class BasicOperationsDemo {
    public static async Task RunAsync(BlogDbContext context) {
        await AddEntityAsync(context);
        await UpdateEntityAsync(context);
        await DeleteEntityAsync(context);
        await EntityStatesAsync(context);
    }

    // [CREATE] Ajouter une entité via Factory Method + Add + SaveChanges
    static async Task AddEntityAsync(BlogDbContext context) {
        var newAuthor = Author.Create("Charlie Leclerc");
        context.Authors.Add(newAuthor);
        await context.SaveChangesAsync();

        var found = await context.Authors.FindAsync(newAuthor.Id);
        Debug.Assert(found != null);
        Debug.Assert(found.Name == "Charlie Leclerc");
    }

    // [UPDATE] Modifier une propriété d'une entité trackée → détection automatique
    static async Task UpdateEntityAsync(BlogDbContext context) {
        var author = await context.Authors.FirstAsync(a => a.Name == "Charlie Leclerc");
        author.Name = "Charles Leclerc";
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var updated = await context.Authors.FirstAsync(a => a.Id == author.Id);
        Debug.Assert(updated.Name == "Charles Leclerc");
    }

    // [DELETE] Charger puis supprimer avec Remove + SaveChanges
    static async Task DeleteEntityAsync(BlogDbContext context) {
        var author = await context.Authors.FirstAsync(a => a.Name == "Charles Leclerc");
        context.Authors.Remove(author);
        await context.SaveChangesAsync();

        var exists = await context.Authors.AnyAsync(a => a.Name == "Charles Leclerc");
        Debug.Assert(!exists);
    }

    // [ENTITY STATES] Cycle de vie : Detached → Added → Unchanged → Modified → Deleted
    static async Task EntityStatesAsync(BlogDbContext context) {
        var author = Author.Create("David Martin");
        Debug.Assert(context.Entry(author).State == EntityState.Detached);

        context.Authors.Add(author);
        Debug.Assert(context.Entry(author).State == EntityState.Added);

        await context.SaveChangesAsync();
        Debug.Assert(context.Entry(author).State == EntityState.Unchanged);

        author.Name = "David Dupont";
        Debug.Assert(context.Entry(author).State == EntityState.Modified);

        context.Authors.Remove(author);
        Debug.Assert(context.Entry(author).State == EntityState.Deleted);

        await context.SaveChangesAsync();
    }
}
