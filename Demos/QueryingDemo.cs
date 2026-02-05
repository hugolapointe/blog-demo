using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class QueryingDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAsync() {
        WriteTitle("=== QUERYING ===");

        await FilteringAsync();
        await SortingAsync();
        await PaginationAsync();
        await FirstVsSingleAsync();
        await AnyVsCountAsync();
        await DistinctAsync();
        await ContainsAsync();
        await AsQueryableAsync();
        await CombinedAsync();
    }

    async Task FilteringAsync() {
        WriteTitle("--- Filtrage (Where) ---");

        // Where() traduit en clause SQL WHERE (filtrage côté base de données)
        var articles = await Context.Articles
            .Where(article => article.Title.Contains("EF Core"))
            .ToListAsync();

        Console.WriteLine($"Articles contenant 'EF Core': {articles.Count}");
        foreach (var article in articles) {
            Console.WriteLine($"  - {article.Title}");
        }

        // Peut chaîner plusieurs Where() → combinés avec AND en SQL
    }

    async Task SortingAsync() {
        WriteTitle("--- Tri (OrderBy) ---");

        // OrderBy() = critère principal, ThenBy() = critères secondaires
        // SQL: ORDER BY Date DESC, Title ASC
        var articles = await Context.Articles
            .OrderByDescending(article => article.Date)
            .ThenBy(article => article.Title)
            .ToListAsync();

        Console.WriteLine("Articles triés par date (desc) puis titre:");
        foreach (var article in articles) {
            Console.WriteLine($"  - {article.Date:yyyy-MM-dd} - {article.Title}");
        }

        // PIÈGE: OrderBy() remplace le tri précédent, utiliser ThenBy()
    }

    async Task PaginationAsync() {
        WriteTitle("--- Pagination (Skip/Take) ---");

        int pageSize = 2;
        int pageNumber = 1; // 0-indexed

        // OrderBy() OBLIGATOIRE avant Skip() pour pagination cohérente
        // SQL: ORDER BY Title OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
        var articles = await Context.Articles
            .OrderBy(article => article.Title)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await Context.Articles.CountAsync();

        Console.WriteLine($"Page {pageNumber + 1} (taille: {pageSize}, total: {totalCount}):");
        foreach (var article in articles) {
            Console.WriteLine($"  - {article.Title}");
        }
    }

    async Task FirstVsSingleAsync() {
        WriteTitle("--- First vs Single vs FirstOrDefault ---");

        // First() - Retourne le premier, exception si aucun
        var first = await Context.Articles.FirstAsync();
        Console.WriteLine($"First: {first.Title}");

        // FirstOrDefault() - Retourne le premier ou null
        var firstOrNull = await Context.Articles.FirstOrDefaultAsync(article => article.Title == "Inexistant");
        Console.WriteLine($"FirstOrDefault: {firstOrNull?.Title ?? "null"}");

        // Single() - Un seul élément attendu, exception si 0 ou >1
        var single = await Context.Articles.SingleAsync(article => article.Title == "Introduction à EF Core");
        Console.WriteLine($"Single: {single.Title}");

        // Utiliser First pour "donnes-moi un", Single pour "doit être unique"
    }

    async Task AnyVsCountAsync() {
        WriteTitle("--- Any vs Count (Existence) ---");

        // Count() - Compte tous les éléments
        var count = await Context.Articles.CountAsync(article => article.Title.Contains("EF Core"));
        Console.WriteLine($"Count: {count}");

        // Any() - Vérifie si au moins un existe (plus rapide)
        var exists = await Context.Articles.AnyAsync(article => article.Title.Contains("EF Core"));
        Console.WriteLine($"Any: {exists}");

        // Any() s'arrête au premier résultat, Count() compte tout
    }

    async Task DistinctAsync() {
        WriteTitle("--- Distinct (Éliminer doublons) ---");

        // Distinct() élimine les doublons
        var authorNames = await Context.Articles
            .Select(article => article.Author!.Name)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"Auteurs uniques ({authorNames.Count}):");
        foreach (var name in authorNames) {
            Console.WriteLine($"  - {name}");
        }
    }

    async Task ContainsAsync() {
        WriteTitle("--- Contains (IN clause) ---");

        // Contains() avec liste → SQL IN
        var targetAuthors = new[] { "Alice Dupont", "Bob Martin" };
        var articles = await Context.Articles
            .Where(article => targetAuthors.Contains(article.Author!.Name))
            .ToListAsync();

        Console.WriteLine($"Articles de Alice ou Bob: {articles.Count}");
        foreach (var article in articles) {
            Console.WriteLine($"  - {article.Title}");
        }
    }

    async Task AsQueryableAsync() {
        WriteTitle("--- AsQueryable (Requêtes dynamiques) ---");

        // PROBLÈME: ToList() charge tout en mémoire, puis filtre en mémoire
        var allArticles = await Context.Articles.ToListAsync();
        var filteredInMemory = allArticles.Where(article => article.Title.Contains("EF Core"));
        Console.WriteLine($"ToList puis Where: {filteredInMemory.Count()} (filtré en mémoire)");

        Context.ChangeTracker.Clear();

        // SOLUTION: AsQueryable() permet de continuer à construire la requête SQL
        IQueryable<Article> query = Context.Articles;

        // Construire dynamiquement la requête
        string? searchTerm = "EF Core";
        if (!string.IsNullOrEmpty(searchTerm)) {
            query = query.Where(article => article.Title.Contains(searchTerm));
        }

        var filteredInDb = await query.ToListAsync();
        Console.WriteLine($"AsQueryable puis Where: {filteredInDb.Count} (filtré en SQL)");

        // Cas d'usage: filtres conditionnels, requêtes dynamiques, repository pattern
    }

    async Task CombinedAsync() {
        WriteTitle("--- Exemple Combiné ---");

        // Ordre: Include → Where → OrderBy → Skip → Take
        var query = Context.Articles
            .Include(article => article.Author)
            .Where(article => article.Author!.Name.StartsWith("Alice"))
            .OrderByDescending(article => article.Date)
            .Skip(0)
            .Take(2);

        var articles = await query.ToListAsync();

        var totalCount = await Context.Articles
            .Where(article => article.Author!.Name.StartsWith("Alice"))
            .CountAsync();

        Console.WriteLine($"Articles de 'Alice*' (page 1, total: {totalCount}):");
        foreach (var article in articles) {
            Console.WriteLine($"  - {article.Title}");
        }
    }
}
