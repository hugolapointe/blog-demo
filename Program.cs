using BlogDemo.Data;
using BlogDemo.Demos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Configuration du DbContext avec SQLite InMemory pour faciliter l'exécution des démos sans installation préalable
var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
optionsBuilder
    .UseSqlite("DataSource=:memory:")
    .UseLazyLoadingProxies() // Active le chargement paresseux via des proxies dynamiques
    .LogTo(Console.WriteLine, LogLevel.Information) // Affiche les requêtes SQL générées dans la console
    .EnableSensitiveDataLogging(); // Inclut les valeurs des paramètres dans les logs (DANGER en production)

using var context = new BlogDbContext(optionsBuilder.Options);

// Crée le schéma de la base de données (nécessaire si aucune migration n'est appliquée)
// Note : Pour SQLite In-Memory, la connexion doit rester ouverte pour que la BD persiste
await context.Database.OpenConnectionAsync();
await context.Database.EnsureCreatedAsync();

// Initialisation des données de test
// Attention : Sur une base de données persistante, ce code dupliquerait les données à chaque exécution
await BlogSeeder.SeedAsync(context);

await RunDemo(BasicOperationsDemo.RunAsync);
await RunDemo(LoadingStrategiesDemo.RunAsync);
await RunDemo(OptimizationDemo.RunAsync);
await RunDemo(QueryingDemo.RunAsync);
await RunDemo(AggregationDemo.RunAsync);

// Démonstrations
// RunDemo() nettoie le ChangeTracker avant chaque démo pour éviter les interférences
async Task RunDemo(Func<BlogDbContext, Task> demo) {
    context.ChangeTracker.Clear();
    await demo(context);
}
