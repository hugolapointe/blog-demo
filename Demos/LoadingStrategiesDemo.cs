using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public class LoadingStrategiesDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAll() {
        await LazyLoadingAsync();
        await ProblemN1Async();
        await EagerLoadingAsync();
        await ExplicitLoadingAsync();
    }

    // [LAZY LOADING] Chaque accès à une navigation property déclenche une requête SQL
    // Requiert UseLazyLoadingProxies() et propriétés virtual
    async Task LazyLoadingAsync() {
        var article = await Context.Articles.FirstAsync();
        Debug.Assert(article.Title != null);

        // Chaque ligne ci-dessous déclenche une requête SQL distincte
        Debug.Assert(article.Author?.Name != null);
        Debug.Assert(article.Comments.Count >= 0);
        Debug.Assert(article.Tags.Count >= 0);
        // Résultat : 1 requête Article + 3 requêtes relations = 4 requêtes total
    }

    // [PROBLÈME N+1] Anti-pattern classique avec Lazy Loading
    // N articles → N requêtes supplémentaires pour charger Author
    async Task ProblemN1Async() {
        var articles = await Context.Articles.ToListAsync();
        Debug.Assert(articles.Count == 3);

        foreach (var article in articles) {
            // Chaque itération déclenche une requête pour Author
            Debug.Assert(article.Author?.Name != null);
        }
        // Résultat : 1 + 3 = 4 requêtes (au lieu de 1 avec Include)
    }

    // [EAGER LOADING] Include() charge les relations en une seule requête (JOIN)
    // Solution au problème N+1
    async Task EagerLoadingAsync() {
        var articles = await Context.Articles
            .Include(a => a.Author)
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .ToListAsync();

        // Toutes les données sont en mémoire, aucune requête supplémentaire
        foreach (var article in articles) {
            Debug.Assert(article.Author != null);
            Debug.Assert(article.Comments != null);
        }
        Debug.Assert(articles.Count == 3);
    }

    // [EXPLICIT LOADING] Contrôle manuel : Reference() pour N-1, Collection() pour 1-N
    // Utile pour le chargement conditionnel basé sur la logique métier
    async Task ExplicitLoadingAsync() {
        var article = await Context.Articles.FirstAsync();

        await Context.Entry(article).Reference(a => a.Author).LoadAsync();
        await Context.Entry(article).Collection(a => a.Comments).LoadAsync();

        Debug.Assert(article.Author != null);
        Debug.Assert(article.Comments.Count >= 0);
    }
}
