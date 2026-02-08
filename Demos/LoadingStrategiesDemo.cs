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

    async Task LazyLoadingAsync() {

        // Lazy Loading charge les relations à la demande
        // Requête initiale charge uniquement l'Article
        var article = await Context.Articles.FirstAsync();
        Debug.Assert(article.Title != null);

        // Chaque accès à une navigation property déclenche une requête SQL
        // Requiert UseLazyLoadingProxies() et propriétés virtuelles
        Debug.Assert(article.Author?.Name != null); // +1 requête
        Debug.Assert(article.Comments.Count >= 0);   // +1 requête
        Debug.Assert(article.Tags.Count >= 0);       // +1 requête

        // Résultat: 1 requête Article + 3 requêtes relations = 4 requêtes total
    }

    async Task ProblemN1Async() {

        // ANTI-PATTERN classique avec Lazy Loading
        var articles = await Context.Articles.ToListAsync();  // 1 requête

        // Chaque itération déclenche une requête pour Author
        // 3 articles → 3 requêtes supplémentaires = 4 requêtes total!
        foreach (var article in articles) {
            Debug.Assert(article.Title != null);
            Debug.Assert(article.Author?.Name != null);
        }

        // Impact majeur sur les performances avec de grandes collections
    }

    async Task EagerLoadingAsync() {

        // SOLUTION au problème N+1 : Eager Loading avec Include()
        // Charge les relations avec JOINs en une seule requête
        var articles = await Context.Articles
            .Include(a => a.Author)
            .Include(a => a.Comments)
            .Include(a => a.Tags)
            .ToListAsync();

        // Toutes les données sont en mémoire, aucune requête supplémentaire
        foreach (var article in articles) {
            Debug.Assert(article.Title != null);
            Debug.Assert(article.Author?.Name != null);
        }

        // Utiliser quand les relations sont toujours nécessaires
    }

    async Task ExplicitLoadingAsync() {

        // Charger l'entité principale
        var article = await Context.Articles.FirstAsync();

        // Explicit Loading : contrôle manuel du chargement
        // Reference() pour relations 1-1 ou N-1, Collection() pour 1-N
        await Context.Entry(article).Reference(a => a.Author).LoadAsync();
        await Context.Entry(article).Collection(a => a.Comments).LoadAsync();

        Debug.Assert(article.Title != null);
        Debug.Assert(article.Author?.Name != null);
        Debug.Assert(article.Comments.Count >= 0);

        // Cas d'usage : chargement conditionnel basé sur la logique métier
    }
}
