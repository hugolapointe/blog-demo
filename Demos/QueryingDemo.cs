using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

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

    // [FILTRAGE] Where() → SQL WHERE
    async Task FilteringAsync() {
        var articles = await Context.Articles
            .Where(a => a.Title.Contains("EF Core"))
            .ToListAsync();

        Debug.Assert(articles.Count >= 1);
        Debug.Assert(articles.All(a => a.Title.Contains("EF Core")));
    }

    // [TRI] OrderBy + ThenBy → SQL ORDER BY
    // Piège : OrderBy() remplace le tri précédent, utiliser ThenBy()
    async Task SortingAsync() {
        var articles = await Context.Articles
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Title)
            .ToListAsync();

        Debug.Assert(articles.Count > 0);
    }

    // [PAGINATION] Skip + Take → SQL OFFSET/FETCH
    // OrderBy est OBLIGATOIRE avant Skip/Take
    async Task PaginationAsync() {
        int pageSize = 2;
        int pageNumber = 1;

        var articles = await Context.Articles
            .OrderBy(a => a.Title)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await Context.Articles.CountAsync();

        Debug.Assert(articles.Count <= pageSize);
        Debug.Assert(totalCount == 3);
    }

    // [FIRST vs SINGLE]
    // First() → premier élément (exception si vide)
    // FirstOrDefault() → premier ou null
    // Single() → un seul élément attendu (exception si 0 ou >1)
    async Task FirstVsSingleAsync() {
        var first = await Context.Articles.FirstAsync();
        Debug.Assert(first != null);

        var firstOrNull = await Context.Articles
            .FirstOrDefaultAsync(a => a.Title == "Inexistant");
        Debug.Assert(firstOrNull == null);

        var single = await Context.Articles
            .SingleAsync(a => a.Title == "Introduction à EF Core");
        Debug.Assert(single.Title == "Introduction à EF Core");
    }

    // [ANY vs COUNT]
    // Any() s'arrête au premier résultat (rapide)
    // Count() parcourt tout (plus lent)
    // Bonne pratique : utiliser Any() au lieu de Count() > 0
    async Task AnyVsCountAsync() {
        var count = await Context.Articles
            .CountAsync(a => a.Title.Contains("EF Core"));
        Debug.Assert(count >= 1);

        var exists = await Context.Articles
            .AnyAsync(a => a.Title.Contains("EF Core"));
        Debug.Assert(exists);
    }

    // [DISTINCT] Éliminer les doublons → SQL DISTINCT
    async Task DistinctAsync() {
        var authorNames = await Context.Articles
            .Select(a => a.Author!.Name)
            .Distinct()
            .ToListAsync();

        Debug.Assert(authorNames.Count == 2);
    }

    // [CONTAINS] Liste.Contains() → SQL IN
    async Task ContainsAsync() {
        var targetAuthors = new[] { "Alice Dupont", "Bob Martin" };
        var articles = await Context.Articles
            .Where(a => targetAuthors.Contains(a.Author!.Name))
            .ToListAsync();

        Debug.Assert(articles.Count == 3);
    }

    // [IQUERYABLE] Construction dynamique de requêtes (exécution différée)
    // Anti-pattern : ToList() puis filtrer en mémoire
    // Bonne pratique : construire la requête IQueryable, puis matérialiser
    async Task AsQueryableAsync() {
        // Anti-pattern : tout charger puis filtrer en C#
        var allArticles = await Context.Articles.ToListAsync();
        var filteredInMemory = allArticles.Where(a => a.Title.Contains("EF Core"));
        Debug.Assert(filteredInMemory.Any());

        Context.ChangeTracker.Clear();

        // Bonne pratique : construire la requête SQL dynamiquement
        IQueryable<Article> query = Context.Articles;

        string? searchTerm = "EF Core";
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(a => a.Title.Contains(searchTerm));

        var filteredInDb = await query.ToListAsync();
        Debug.Assert(filteredInDb.Count >= 1);
    }

    // [COMBINÉ] Ordre typique : Include → Where → OrderBy → Skip → Take
    async Task CombinedAsync() {
        var articles = await Context.Articles
            .Include(a => a.Author)
            .Where(a => a.Author!.Name.StartsWith("Alice"))
            .OrderByDescending(a => a.CreatedAt)
            .Skip(0)
            .Take(2)
            .ToListAsync();

        Debug.Assert(articles.Count <= 2);
        Debug.Assert(articles.All(a => a.Author!.Name.StartsWith("Alice")));

        var totalCount = await Context.Articles
            .Where(a => a.Author!.Name.StartsWith("Alice"))
            .CountAsync();
        Debug.Assert(totalCount == 2);
    }
}
