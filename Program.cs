using BlogDemo.Data;
using BlogDemo.Demos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
