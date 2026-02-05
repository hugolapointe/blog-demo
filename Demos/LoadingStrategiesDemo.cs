using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class LoadingStrategiesDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAsync() {
        WriteTitle("=== LOADING STRATEGIES ===");

        await LazyLoadingAsync();
        await ProblemN1Async();
        await EagerLoadingAsync();
        await ExplicitLoadingAsync();
    }

    async Task LazyLoadingAsync() {
        WriteTitle("--- Lazy Loading ---");

        // Requête initiale charge uniquement l'Article
        var article = await Context.Articles.FirstAsync();
        Console.WriteLine($"Article: {article.Title}");

        // Chaque accès à une navigation property déclenche une requête SQL séparée
        Console.WriteLine($"Author: {article.Author?.Name}");      // +1 requête
        Console.WriteLine($"Comments: {article.Comments.Count}");  // +1 requête
        Console.WriteLine($"Tags: {article.Tags.Count}");          // +1 requête

        // Résultat: 1 requête Article + 3 requêtes relations = 4 requêtes total
    }

    async Task ProblemN1Async() {
        WriteTitle("--- Problème N+1 ---");

        // PROBLÈME: Lazy loading avec plusieurs entités
        var articles = await Context.Articles.ToListAsync();  // 1 requête

        // Chaque accès à Author dans la boucle = 1 requête SQL
        // 3 articles → 3 requêtes supplémentaires = 4 requêtes total!
        foreach (var article in articles) {
            Console.WriteLine($"{article.Title} par {article.Author?.Name}");
        }

        Console.WriteLine($"→ 1 requête articles + {articles.Count} requêtes authors = {articles.Count + 1} requêtes!");
    }

    async Task EagerLoadingAsync() {
        WriteTitle("--- Eager Loading (Solution N+1) ---");

        // SOLUTION: Include() charge les relations avec JOINs en une seule requête
        var articles = await Context.Articles
            .Include(a => a.Author)
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .ToListAsync();

        // Toutes les données sont en mémoire, aucune requête supplémentaire
        foreach (var article in articles) {
            Console.WriteLine($"{article.Title} par {article.Author?.Name}");
        }
        Console.WriteLine($"→ 1 seule requête pour tout charger");
    }

    async Task ExplicitLoadingAsync() {
        WriteTitle("--- Explicit Loading ---");

        // Charger l'entité principale
        var article = await Context.Articles.FirstAsync();

        // Charger manuellement les relations spécifiques
        // Reference() pour relations 1-1 ou N-1, Collection() pour 1-N
        await Context.Entry(article).Reference(a => a.Author).LoadAsync();
        await Context.Entry(article).Collection(a => a.Comments).LoadAsync();

        Console.WriteLine($"{article.Title} par {article.Author?.Name}");
        Console.WriteLine($"Comments: {article.Comments.Count}");

        // Utile pour logique conditionnelle (charger seulement si nécessaire)
    }
}
