using BlogDemo.Data;
using BlogDemo.Demos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Démo EF Core ===\n");

// Configuration du DbContext avec SQLite InMemory et logging SQL
var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
optionsBuilder
    .UseSqlite("DataSource=:memory:")
    .UseLazyLoadingProxies()
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging();

using var context = new BlogDbContext(optionsBuilder.Options);

// Pour SQLite InMemory : ouvrir la connexion et créer la base
await context.Database.OpenConnectionAsync();
await context.Database.EnsureCreatedAsync();

// Seed
Console.WriteLine("--- Seed ---");
await BlogSeeder.SeedAsync(context);

// Nettoyer le change tracker pour réinitialiser l'état des entités
context.ChangeTracker.Clear();

// Démonstrations
await new BasicOperationsDemo(context).RunAsync();
await new LoadingStrategiesDemo(context).RunAsync();
await new PerformanceOptimizationDemo(context).RunAsync();
await new QueryingDemo(context).RunAsync();
await new AggregationDemo(context).RunAsync();

Console.WriteLine("\n=== Fin ===");
