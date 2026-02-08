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
await new BasicOperationsDemo(context).ResetAndExecuteAll();
await new LoadingStrategiesDemo(context).ResetAndExecuteAll();
await new OptimizationDemo(context).ResetAndExecuteAll();
await new QueryingDemo(context).ResetAndExecuteAll();
await new AggregationDemo(context).ResetAndExecuteAll();
await DeleteBehaviorDemo.Run(context);

Console.WriteLine("\n=== Fin ===");
