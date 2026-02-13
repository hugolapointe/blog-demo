using BlogDemo.Domain;

namespace BlogDemo.Data;

public static class BlogSeeder {
    public static async Task SeedAsync(BlogDbContext context) {
        var alice = AddAuthor(context, "Alice Dupont");
        var bob = AddAuthor(context, "Bob Martin");
        await context.SaveChangesAsync();

        var csharp = AddTag(context, "C#");
        var efCore = AddTag(context, "EF Core");
        var dotNet = AddTag(context, ".NET");
        await context.SaveChangesAsync();

        AddArticle(context, "Introduction à EF Core",
            "Entity Framework Core est un ORM moderne...",
            alice.Id,
            comments: ["Excellent article!", "Très utile, merci!"],
            tags: [efCore, dotNet]);

        AddArticle(context, "Les relations dans EF Core",
            "Les relations 1-N, 1-1 et N-N sont essentielles...",
            alice.Id,
            comments: ["Bien expliqué."],
            tags: [efCore, csharp]);

        AddArticle(context, "Code-First avec EF Core",
            "L'approche Code-First permet de définir le modèle en C#...",
            bob.Id,
            tags: [csharp, dotNet]);

        await context.SaveChangesAsync();
    }

    static Author AddAuthor(BlogDbContext context, string name) {
        var author = Author.Create(name);
        context.Authors.Add(author);
        return author;
    }

    static Tag AddTag(BlogDbContext context, string name) {
        var tag = Tag.Create(name);
        context.Tags.Add(tag);
        return tag;
    }

    static Article AddArticle(BlogDbContext context, string title, string content, Guid authorId,
        string[]? comments = null, Tag[]? tags = null) {
        var article = Article.Create(title, content, authorId);

        foreach (var comment in comments ?? [])
            article.AddComment(comment);

        foreach (var tag in tags ?? [])
            article.AddTag(tag);

        context.Articles.Add(article);
        return article;
    }
}
