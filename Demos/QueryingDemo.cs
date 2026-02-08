using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class QueryingDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {
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

        // SQL: WHERE Title LIKE '%EF Core%'
        var articles = await Context.Articles
            .Where(article => article.Title.Contains("EF Core"))
            .ToListAsync();
    }

    async Task SortingAsync() {

        // SQL: ORDER BY CreatedAt DESC, Title ASC
        var articles = await Context.Articles
            .OrderByDescending(article => article.CreatedAt)
            .ThenBy(article => article.Title)
            .ToListAsync();

        // PIÈGE: OrderBy() remplace le tri précédent, utiliser ThenBy()
    }

    async Task PaginationAsync() {

        int pageSize = 2;
        int pageNumber = 1; // 0-indexed

        // Sans OrderBy, l'ordre n'est pas garanti entre les requêtes
        // SQL: ORDER BY Title OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
        var articles = await Context.Articles
            .OrderBy(article => article.Title)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await Context.Articles.CountAsync();
    }

    async Task FirstVsSingleAsync() {

        // First() - Retourne le premier, exception si aucun
        var first = await Context.Articles.FirstAsync();

        // FirstOrDefault() - Retourne le premier ou null
        var firstOrNull = await Context.Articles.FirstOrDefaultAsync(article => article.Title == "Inexistant");

        // Single() - Un seul élément attendu, exception si 0 ou >1
        var single = await Context.Articles.SingleAsync(article => article.Title == "Introduction à EF Core");
    }

    async Task AnyVsCountAsync() {

        // Count() - Compte tous les éléments
        var count = await Context.Articles.CountAsync(article => article.Title.Contains("EF Core"));
        Console.WriteLine($"Count: {count}");

        // Any() vérifie l'existence sans compter (plus rapide)
        var exists = await Context.Articles.AnyAsync(article => article.Title.Contains("EF Core"));

        // Bonne pratique : utiliser Any() au lieu de Count() > 0
        // Any() s'arrête au premier résultat, Count() doit tout parcourir
    }

    async Task DistinctAsync() {

        // Distinct() élimine les doublons
        var authorNames = await Context.Articles
            .Select(article => article.Author!.Name)
            .Distinct()
            .ToListAsync();
    }

    async Task ContainsAsync() {

        // Contains() avec liste → SQL IN
        var targetAuthors = new[] { "Alice Dupont", "Bob Martin" };
        var articles = await Context.Articles
            .Where(article => targetAuthors.Contains(article.Author!.Name))
            .ToListAsync();
    }

    async Task AsQueryableAsync() {

        // ANTI-PATTERN: ToList() matérialise, puis filtre en mémoire (C#)
        var allArticles = await Context.Articles.ToListAsync();
        var filteredInMemory = allArticles.Where(article => article.Title.Contains("EF Core"));

        Context.ChangeTracker.Clear();

        // BONNE PRATIQUE: IQueryable permet de construire la requête SQL
        IQueryable<Article> query = Context.Articles; // .AsQueryable() est implicite

        // Construire dynamiquement la requête (exécution différée)
        string? searchTerm = "EF Core";
        if (!string.IsNullOrEmpty(searchTerm)) {
            query = query.Where(article => article.Title.Contains(searchTerm));
        }

        var filteredInDb = await query.ToListAsync();
    }

    async Task CombinedAsync() {

        // Ordre: Include → Where → OrderBy → Skip → Take
        var query = Context.Articles
            .Include(article => article.Author)
            .Where(article => article.Author!.Name.StartsWith("Alice"))
            .OrderByDescending(article => article.CreatedAt)
            .Skip(0)
            .Take(2);

        var articles = await query.ToListAsync();

        var totalCount = await Context.Articles
            .Where(article => article.Author!.Name.StartsWith("Alice"))
            .CountAsync();
    }
}
