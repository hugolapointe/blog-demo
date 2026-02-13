using BlogDemo.Domain;

namespace BlogDemo.Data;

public static class BlogSeeder {
    public static async Task SeedAsync(BlogDbContext context) {
        var (alice, bob) = CreateAuthors();
        context.Authors.AddRange(alice, bob);
        await context.SaveChangesAsync();

        var (csharp, efCore, dotNet) = CreateTags();
        context.Tags.AddRange(csharp, efCore, dotNet);
        await context.SaveChangesAsync();

        context.Articles.AddRange(
            CreateArticle("Introduction à EF Core",
                "Entity Framework Core est un ORM moderne...",
                alice.Id,
                comments: ["Excellent article!", "Très utile, merci!"],
                tags: [efCore, dotNet]),

            CreateArticle("Les relations dans EF Core",
                "Les relations 1-N, 1-1 et N-N sont essentielles...",
                alice.Id,
                comments: ["Bien expliqué."],
                tags: [efCore, csharp]),

            CreateArticle("Code-First avec EF Core",
                "L'approche Code-First permet de définir le modèle en C#...",
                bob.Id,
                tags: [csharp, dotNet]));

        await context.SaveChangesAsync();
    }

    private static (Author alice, Author bob) CreateAuthors() =>
        (Author.Create("Alice Dupont"), Author.Create("Bob Martin"));

    private static (Tag csharp, Tag efCore, Tag dotNet) CreateTags() =>
        (Tag.Create("C#"), Tag.Create("EF Core"), Tag.Create(".NET"));

    private static Article CreateArticle(string title, string content, Guid authorId,
        string[]? comments = null, Tag[]? tags = null) {
        var article = Article.Create(title, content, authorId);

        foreach (var comment in comments ?? [])
            article.AddComment(comment);

        foreach (var tag in tags ?? [])
            article.AddTag(tag);

        return article;
    }
}
