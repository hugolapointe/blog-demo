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

        // [DDD] Créer les aggregates complets avant de sauvegarder
        var article1 = Article.Create(
            "Introduction à EF Core",
            "Entity Framework Core est un ORM moderne...",
            author1.Id);
        article1.AddComment("Excellent article!");
        article1.AddComment("Très utile, merci!");
        article1.AddTag(tagEFCore);
        article1.AddTag(tagDotNet);

        var article2 = Article.Create(
            "Les relations dans EF Core",
            "Les relations 1-N, 1-1 et N-N sont essentielles...",
            author1.Id);
        article2.AddComment("Bien expliqué.");
        article2.AddTag(tagEFCore);
        article2.AddTag(tagCSharp);

        var article3 = Article.Create(
            "Code-First avec EF Core",
            "L'approche Code-First permet de définir le modèle en C#...",
            author2.Id);
        article3.AddTag(tagCSharp);
        article3.AddTag(tagDotNet);

        context.Articles.AddRange(article1, article2, article3);
        await context.SaveChangesAsync();
    }
}
