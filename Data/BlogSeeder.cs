using BlogDemo.Domain;

namespace BlogDemo.Data;

public static class BlogSeeder {
    public static async Task SeedAsync(BlogDbContext context) {
        var author1 = Author.Create("Alice Dupont");
        var author2 = Author.Create("Bob Martin");
        context.Authors.AddRange(author1, author2);
        await context.SaveChangesAsync();

        var tagCSharp = Tag.Create("C#");
        var tagEFCore = Tag.Create("EF Core");
        var tagDotNet = Tag.Create(".NET");
        context.Tags.AddRange(tagCSharp, tagEFCore, tagDotNet);
        await context.SaveChangesAsync();

        var article1 = Article.Create(
            "Introduction à EF Core",
            "Entity Framework Core est un ORM moderne...",
            author1.Id);

        var article2 = Article.Create(
            "Les relations dans EF Core",
            "Les relations 1-N, 1-1 et N-N sont essentielles...",
            author1.Id);

        var article3 = Article.Create(
            "Code-First avec EF Core",
            "L'approche Code-First permet de définir le modèle en C#...",
            author2.Id);

        context.Articles.AddRange(article1, article2, article3);
        await context.SaveChangesAsync();

        var comment1 = Comment.Create("Excellent article!", article1.Id);
        var comment2 = Comment.Create("Très utile, merci!", article1.Id);
        var comment3 = Comment.Create("Bien expliqué.", article2.Id);
        context.Comments.AddRange(comment1, comment2, comment3);
        await context.SaveChangesAsync();

        article1.Tags.Add(tagEFCore);
        article1.Tags.Add(tagDotNet);
        article2.Tags.Add(tagEFCore);
        article2.Tags.Add(tagCSharp);
        article3.Tags.Add(tagCSharp);
        article3.Tags.Add(tagDotNet);
        await context.SaveChangesAsync();

        Console.WriteLine("✓ Données seed créées");
    }
}
