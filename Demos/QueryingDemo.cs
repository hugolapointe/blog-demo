using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public static class QueryingDemo {
    public static async Task RunAsync(BlogDbContext context) {
        await FilteringAsync(context);
        await SortingAsync(context);
        await PaginationAsync(context);
        await FindByIdAsync(context);
        await FirstVsSingleAsync(context);
        await AnyVsCountAsync(context);
        await DistinctAsync(context);
        await ContainsAsync(context);
        await AsQueryableAsync(context);
        await CombinedAsync(context);
    }

    // [FILTRAGE] Where() → SQL WHERE
    static async Task FilteringAsync(BlogDbContext context) {
        var articles = await context.Articles
            .Where(a => a.Title.Contains("EF Core"))
            .ToListAsync();

        Debug.Assert(articles.Count >= 1);
        Debug.Assert(articles.All(a => a.Title.Contains("EF Core")));
    }

    // [TRI] OrderBy + ThenBy → SQL ORDER BY
    // Piège : OrderBy() remplace le tri précédent, utiliser ThenBy()
    static async Task SortingAsync(BlogDbContext context) {
        var articles = await context.Articles
            .OrderByDescending(a => a.CreatedAt)
            .ThenBy(a => a.Title)
            .ToListAsync();

        Debug.Assert(articles.Count > 0);
    }

    // [PAGINATION] Skip + Take → SQL OFFSET/FETCH
    // OrderBy est OBLIGATOIRE avant Skip/Take
    static async Task PaginationAsync(BlogDbContext context) {
        int pageSize = 2;
        int pageNumber = 1;

        var articles = await context.Articles
            .OrderBy(a => a.Title)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await context.Articles.CountAsync();

        Debug.Assert(articles.Count <= pageSize);
        Debug.Assert(totalCount == 3);
    }

    // [FIND] FindAsync() recherche par clé primaire — optimisé car vérifie le cache local avant la BD
    // À privilégier quand on cherche par Id, car évite une requête SQL si l'entité est déjà trackée
    static async Task FindByIdAsync(BlogDbContext context) {
        var article = await context.Articles.FirstAsync();
        var articleId = article.Id;

        // FindAsync vérifie d'abord le ChangeTracker → pas de requête SQL si déjà en mémoire
        var cached = await context.Articles.FindAsync(articleId);
        Debug.Assert(cached != null);
        Debug.Assert(ReferenceEquals(article, cached)); // Même instance, pas de requête

        context.ChangeTracker.Clear();

        // Après Clear(), l'entité n'est plus en cache → FindAsync fait une requête SQL
        var fromDb = await context.Articles.FindAsync(articleId);
        Debug.Assert(fromDb != null);
        Debug.Assert(!ReferenceEquals(article, fromDb)); // Nouvelle instance, requête SQL
    }

    // [FIRST vs SINGLE]
    // First() → premier élément (exception si vide)
    // FirstOrDefault() → premier ou null
    // Single() → un seul élément attendu (exception si 0 ou >1)
    static async Task FirstVsSingleAsync(BlogDbContext context) {
        var first = await context.Articles.FirstAsync();
        Debug.Assert(first != null);

        var firstOrNull = await context.Articles
            .FirstOrDefaultAsync(a => a.Title == "Inexistant");
        Debug.Assert(firstOrNull == null);

        var single = await context.Articles
            .SingleAsync(a => a.Title == "Introduction à EF Core");
        Debug.Assert(single.Title == "Introduction à EF Core");
    }

    // [ANY vs COUNT] Pour vérifier l'existence d'un élément
    static async Task AnyVsCountAsync(BlogDbContext context) {
        // À NE PAS FAIRE : Count() parcourt TOUS les résultats pour vérifier l'existence
        var existsViaBadWay = await context.Articles
            .CountAsync(a => a.Title.Contains("EF Core")) > 0;
        Debug.Assert(existsViaBadWay);

        // BONNE PRATIQUE : Any() s'arrête dès le premier résultat trouvé (SQL EXISTS)
        var exists = await context.Articles
            .AnyAsync(a => a.Title.Contains("EF Core"));
        Debug.Assert(exists);
    }

    // [DISTINCT] Éliminer les doublons → SQL DISTINCT
    static async Task DistinctAsync(BlogDbContext context) {
        var authorNames = await context.Articles
            .Select(a => a.Author!.Name)
            .Distinct()
            .ToListAsync();

        Debug.Assert(authorNames.Count == 2);
    }

    // [CONTAINS] Liste.Contains() → SQL IN
    static async Task ContainsAsync(BlogDbContext context) {
        var targetAuthors = new[] { "Alice Dupont", "Bob Martin" };
        var articles = await context.Articles
            .Where(a => targetAuthors.Contains(a.Author!.Name))
            .ToListAsync();

        Debug.Assert(articles.Count == 3);
    }

    // [IQUERYABLE] Construction dynamique de requêtes (exécution différée)
    // Anti-pattern : ToList() puis filtrer en mémoire
    // Bonne pratique : construire la requête IQueryable, puis matérialiser
    static async Task AsQueryableAsync(BlogDbContext context) {
        // Anti-pattern : tout charger puis filtrer en C#
        var allArticles = await context.Articles.ToListAsync();
        var filteredInMemory = allArticles.Where(a => a.Title.Contains("EF Core"));
        Debug.Assert(filteredInMemory.Any());

        context.ChangeTracker.Clear();

        // Bonne pratique : construire la requête SQL dynamiquement
        IQueryable<Article> query = context.Articles;

        string? searchTerm = "EF Core";
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(a => a.Title.Contains(searchTerm));

        var filteredInDb = await query.ToListAsync();
        Debug.Assert(filteredInDb.Count >= 1);
    }

    // [COMBINÉ] Ordre typique : Include → Where → OrderBy → Skip → Take
    static async Task CombinedAsync(BlogDbContext context) {
        var articles = await context.Articles
            .Include(a => a.Author)
            .Where(a => a.Author!.Name.StartsWith("Alice"))
            .OrderByDescending(a => a.CreatedAt)
            .Skip(0)
            .Take(2)
            .ToListAsync();

        Debug.Assert(articles.Count <= 2);
        Debug.Assert(articles.All(a => a.Author!.Name.StartsWith("Alice")));

        var totalCount = await context.Articles
            .Where(a => a.Author!.Name.StartsWith("Alice"))
            .CountAsync();
        Debug.Assert(totalCount == 2);
    }
}
