using BlogDemo.Data;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogDemo.Demos;

public static class LoadingStrategiesDemo {
    public static async Task RunAsync(BlogDbContext context) {
        await EagerLoadingAsync(context);
        await ExplicitLoadingAsync(context);
        await LazyLoadingAsync(context);
        await ProblemN1Async(context);
    }

    // [EAGER LOADING] Stratégie recommandée — Include() charge les relations via JOIN en une seule requête
    // Toujours privilégier Include() quand on sait d'avance quelles relations seront nécessaires
    static async Task EagerLoadingAsync(BlogDbContext context) {
        var articles = await context.Articles
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

    // [EXPLICIT LOADING] Alternative recommandée quand Eager Loading n'est pas approprié
    // Utile pour le chargement conditionnel (ex: charger les commentaires seulement si l'article est publié)
    // Sans Include() ni Lazy Loading, les relations ne sont PAS chargées — il faut les charger explicitement
    // Reference() pour les relations N-1, Collection() pour les relations 1-N
    static async Task ExplicitLoadingAsync(BlogDbContext context) {
        var article = await context.Articles.FirstAsync();

        await context.Entry(article).Reference(a => a.Author).LoadAsync();
        await context.Entry(article).Collection(a => a.Comments).LoadAsync();

        Debug.Assert(article.Author != null);
        Debug.Assert(article.Comments.Count >= 0);
    }

    // [LAZY LOADING] Non recommandé — les requêtes SQL sont déclenchées de façon transparente,
    // ce qui fait perdre le contrôle sur le nombre et le moment des accès à la BD
    // Requiert un opt-in explicite : UseLazyLoadingProxies() + propriétés virtual
    // Préférer Eager ou Explicit Loading pour garder le contrôle
    static async Task LazyLoadingAsync(BlogDbContext context) {
        var article = await context.Articles.FirstAsync();
        Debug.Assert(article.Title != null);

        // Chaque ligne ci-dessous déclenche une requête SQL distincte, de façon invisible
        Debug.Assert(article.Author?.Name != null);
        Debug.Assert(article.Comments.Count >= 0);
        Debug.Assert(article.Tags.Count >= 0);
        // Résultat : 1 requête Article + 3 requêtes relations = 4 requêtes total
    }

    // [PROBLÈME N+1] Anti-pattern causé par le Lazy Loading
    // N articles → N requêtes supplémentaires pour charger Author (1 + N requêtes total)
    // Solution : utiliser Include() (Eager Loading) pour tout charger en une seule requête
    static async Task ProblemN1Async(BlogDbContext context) {
        var articles = await context.Articles.ToListAsync();
        Debug.Assert(articles.Count == 3);

        foreach (var article in articles) {
            // Chaque itération déclenche une requête pour Author (invisible avec Lazy Loading)
            Debug.Assert(article.Author?.Name != null);
        }
        // Résultat : 1 + 3 = 4 requêtes (au lieu de 1 avec Include)
    }
}
