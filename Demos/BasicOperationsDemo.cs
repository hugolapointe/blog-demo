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

    async Task AddEntityAsync() {
        var newAuthor = Author.Create("Charlie Leclerc");
        Context.Authors.Add(newAuthor);
        await Context.SaveChangesAsync();
    }

    async Task UpdateEntityAsync() {

        var author = await Context.Authors.FirstAsync(a => a.Name == "Charlie Leclerc");
        author.Name = "Charles Leclerc";
        await Context.SaveChangesAsync();
    }

    async Task DeleteEntityAsync() {

        var author = await Context.Authors.FirstAsync(a => a.Name == "Charles Leclerc");
        Context.Authors.Remove(author);
        await Context.SaveChangesAsync();
    }

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
    }
}
