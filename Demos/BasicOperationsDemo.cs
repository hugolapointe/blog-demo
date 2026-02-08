using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public class BasicOperationsDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {
        await AddEntityAsync();
        await UpdateEntityAsync();
        await DeleteEntityAsync();
        await EntityStatesAsync();
    }

    // [CREATE] Ajouter une entité via Factory Method + Add + SaveChanges
    async Task AddEntityAsync() {
        var newAuthor = Author.Create("Charlie Leclerc");
        Context.Authors.Add(newAuthor);
        await Context.SaveChangesAsync();

        var found = await Context.Authors.FindAsync(newAuthor.Id);
        Debug.Assert(found != null);
        Debug.Assert(found.Name == "Charlie Leclerc");
    }

    // [UPDATE] Modifier une propriété d'une entité trackée → détection automatique
    async Task UpdateEntityAsync() {
        var author = await Context.Authors.FirstAsync(a => a.Name == "Charlie Leclerc");
        author.Name = "Charles Leclerc";
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        var updated = await Context.Authors.FirstAsync(a => a.Id == author.Id);
        Debug.Assert(updated.Name == "Charles Leclerc");
    }

    // [DELETE] Charger puis supprimer avec Remove + SaveChanges
    async Task DeleteEntityAsync() {
        var author = await Context.Authors.FirstAsync(a => a.Name == "Charles Leclerc");
        Context.Authors.Remove(author);
        await Context.SaveChangesAsync();

        var exists = await Context.Authors.AnyAsync(a => a.Name == "Charles Leclerc");
        Debug.Assert(!exists);
    }

    // [ENTITY STATES] Cycle de vie : Detached → Added → Unchanged → Modified → Deleted
    async Task EntityStatesAsync() {
        var author = Author.Create("David Martin");
        Debug.Assert(Context.Entry(author).State == EntityState.Detached);

        Context.Authors.Add(author);
        Debug.Assert(Context.Entry(author).State == EntityState.Added);

        await Context.SaveChangesAsync();
        Debug.Assert(Context.Entry(author).State == EntityState.Unchanged);

        author.Name = "David Dupont";
        Debug.Assert(Context.Entry(author).State == EntityState.Modified);

        Context.Authors.Remove(author);
        Debug.Assert(Context.Entry(author).State == EntityState.Deleted);

        await Context.SaveChangesAsync();
    }
}
