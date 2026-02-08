# Cheatsheet - Entity Framework Core

**Projet:** BlogDemo - Application de démonstration pédagogique
**Fichier:** `Data/BlogDbContext.cs`

> Référence rapide pour EF Core - Configuration, CRUD, chargement, optimisations

---

## 📋 Table des Matières

- [Configuration](#configuration)
- [Opérations CRUD](#opérations-crud)
- [Stratégies de Chargement](#stratégies-de-chargement)
- [Optimisations](#optimisations)
- [Fluent API](#fluent-api)
- [Migrations](#migrations)
- [Seeding](#seeding)
- [Change Tracker](#change-tracker)
- [Débogage](#débogage)

---

## Configuration

### ASP.NET Core (Program.cs)

```csharp
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### SQLite (Développement)

```csharp
options.UseSqlite("Data Source=blog.db");
```

**Package:** `Microsoft.EntityFrameworkCore.Sqlite`

### SQL Server (Production)

```csharp
options.UseSqlServer("Server=localhost;Database=BlogDb;Trusted_Connection=True;");
```

**Package:** `Microsoft.EntityFrameworkCore.SqlServer`

### InMemory (Tests)

```csharp
options.UseInMemoryDatabase("BlogTestDb");
```

**Package:** `Microsoft.EntityFrameworkCore.InMemory`

### Options Courantes

```csharp
options
    .UseLazyLoadingProxies() // Utilsier laxy-loading automatiquement
    .LogTo(Console.WriteLine, LogLevel.Information) // Afficher les opérations réalisées
    .EnableSensitiveDataLogging()  // Debug uniquement
    .EnableDetailedErrors();
```

---

## Opérations CRUD

### Create

```csharp
var author = Author.Create("Alice");
context.Authors.Add(author);
await context.SaveChangesAsync();

// Multiple
context.Authors.AddRange(author1, author2);
await context.SaveChangesAsync();
```

### Read

```csharp
// Toutes les entités
var authors = await context.Authors.ToListAsync();

// Par ID (Find = optimisé pour PK)
var author = await context.Authors.FindAsync(id);

// Avec filtre (voir CHEATSHEET_LINQ.md)
var alice = await context.Authors
    .Where(a => a.Name == "Alice")
    .FirstOrDefaultAsync();
```

### Update

```csharp
// Entité trackée (recommandé)
var author = await context.Authors.FindAsync(id);
author.Name = "Bob";
await context.SaveChangesAsync();  // Détecte le changement

// Entité non-trackée
context.Authors.Update(author);
await context.SaveChangesAsync();
```

### Delete

```csharp
// Charger puis supprimer
var author = await context.Authors.FindAsync(id);
context.Authors.Remove(author);
await context.SaveChangesAsync();

// Supprimer sans charger
var author = new Author { Id = id };
context.Attach(author);
context.Remove(author);
await context.SaveChangesAsync();
```

### États d'Entités

| État | Description | SaveChanges() |
|------|-------------|---------------|
| `Detached` | Non trackée | Rien |
| `Unchanged` | Trackée, pas modifiée | Rien |
| `Added` | Nouvelle | INSERT |
| `Modified` | Modifiée | UPDATE |
| `Deleted` | À supprimer | DELETE |

---

## Stratégies de Chargement

**Voir:** `Demos/LoadingStrategiesDemo.cs`

### Comparaison

| Stratégie | Quand | Requêtes |
|-----------|-------|----------|
| **Lazy Loading** | Accès occasionnel | N+1 ⚠️ |
| **Eager Loading** | Données toujours nécessaires | 1 |
| **Explicit Loading** | Chargement conditionnel | Sur demande |

### Lazy Loading

```csharp
// Nécessite: UseLazyLoadingProxies() + virtual
public virtual Author? Author { get; set; }

var article = await context.Articles.FirstAsync();
var name = article.Author.Name;  // Requête auto
```

**⚠️ Problème N+1:**
```csharp
// ❌ 1 + N requêtes
var articles = await context.Articles.ToListAsync();
foreach (var a in articles) {
    Console.WriteLine(a.Author.Name);  // N requêtes!
}

// ✅ 1 requête
var articles = await context.Articles
    .Include(a => a.Author)
    .ToListAsync();
```

### Eager Loading (Include)

```csharp
// Une relation
.Include(a => a.Author)

// Plusieurs relations
.Include(a => a.Author)
.Include(a => a.Comments)
.Include(a => a.Tags)

// Relation imbriquée
.Include(a => a.Comments)
    .ThenInclude(c => c.Author)
```

### Explicit Loading

```csharp
var article = await context.Articles.FirstAsync();

// Charger une référence (1-1, N-1)
await context.Entry(article)
    .Reference(a => a.Author)
    .LoadAsync();

// Charger une collection (1-N)
await context.Entry(article)
    .Collection(a => a.Comments)
    .LoadAsync();

// Avec filtre
await context.Entry(article)
    .Collection(a => a.Comments)
    .Query()
    .Where(c => c.Content.Contains("excellent"))
    .LoadAsync();
```

### Split Queries

```csharp
// Évite produit cartésien (N×M lignes)
var articles = await context.Articles
    .Include(a => a.Comments)
    .Include(a => a.Tags)
    .AsSplitQuery()  // 3 requêtes séparées
    .ToListAsync();
```

---

## Optimisations

**Voir:** `Demos/OptimizationDemo.cs`

### AsNoTracking

```csharp
// Lecture seule = plus rapide, moins de mémoire
var articles = await context.Articles
    .AsNoTracking()
    .ToListAsync();
```

**Utiliser pour:** Affichage, rapports, pas de modifications

### Projection (Select)

```csharp
// ✅ Optimal: colonnes spécifiques
var summaries = await context.Articles
    .Select(a => new {
        a.Title,
        AuthorName = a.Author.Name,
        CommentCount = a.Comments.Count
    })
    .ToListAsync();
```

**Voir CHEATSHEET_LINQ.md pour plus de détails**

### Batching

```csharp
// EF Core batch automatiquement
context.Authors.AddRange(author1, author2, author3);
await context.SaveChangesAsync();  // 1 requête INSERT
```

**Voir CHEATSHEET_LINQ.md pour pagination**

---

## Fluent API

**Voir:** `Data/BlogDbContext.cs` méthode `OnModelCreating`

### Propriétés

```csharp
modelBuilder.Entity<Article>(entity => {
    entity.HasKey(e => e.Id);

    entity.Property(e => e.Title)
        .IsRequired()
        .HasMaxLength(500);

    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("datetime('now')");  // SQLite
});
```

### Relations

```csharp
// One-to-Many (1-N)
entity.HasMany(a => a.Comments)
    .WithOne(c => c.Article)
    .HasForeignKey(c => c.ArticleId)
    .OnDelete(DeleteBehavior.Cascade);

// Many-to-Many (N-N)
entity.HasMany(a => a.Tags)
    .WithMany(t => t.Articles)
    .UsingEntity(j => j.ToTable("ArticleTag"));

// One-to-One (1-1)
entity.HasOne(u => u.Profile)
    .WithOne(p => p.User)
    .HasForeignKey<Profile>(p => p.UserId);
```

### Index

```csharp
// Simple
entity.HasIndex(e => e.Title);

// Composite
entity.HasIndex(e => new { e.AuthorId, e.CreatedAt });

// Unique
entity.HasIndex(e => e.Email).IsUnique();
```

### Delete Behaviors

| DeleteBehavior | Effet |
|----------------|-------|
| `Cascade` | Supprime les enfants |
| `Restrict` | Bloque si dépendances |
| `SetNull` | FK à NULL |
| `NoAction` | Rien (erreur BD) |

---

## Migrations

### Commandes CLI

```bash
# Créer migration
dotnet ef migrations add NomMigration

# Appliquer
dotnet ef database update

# Voir SQL
dotnet ef migrations script

# Annuler dernière
dotnet ef migrations remove

# Supprimer BD
dotnet ef database drop
```

---

## Seeding

**Voir:** `Data/BlogSeeder.cs`

### Méthode Recommandée

```csharp
public static async Task SeedAsync(BlogDbContext context) {
    if (await context.Authors.AnyAsync()) return;

    var alice = Author.Create("Alice");
    context.Authors.Add(alice);
    await context.SaveChangesAsync();

    var article = Article.Create("Titre", "Contenu", alice.Id);
    article.AddComment("Super!");
    context.Articles.Add(article);
    await context.SaveChangesAsync();
}
```

### Appel

```csharp
// Program.cs
await context.Database.MigrateAsync();
await BlogSeeder.SeedAsync(context);
```

---

## Change Tracker

### États

```csharp
// Vérifier
var state = context.Entry(author).State;

// Modifier
context.Entry(author).State = EntityState.Modified;

// Détacher
context.Entry(author).State = EntityState.Detached;

// Vider
context.ChangeTracker.Clear();
```

---

## Bonnes Pratiques

### ✅ À Faire

```csharp
// AsNoTracking pour lecture seule
.AsNoTracking()

// Projeter données nécessaires
.Select(a => new { a.Title, a.Author.Name })

// Include pour éviter N+1
.Include(a => a.Author)

// Any() au lieu de Count() > 0
.AnyAsync()

// Async/await
await context.SaveChangesAsync();
```

### ❌ À Éviter

```csharp
// Lazy loading causant N+1
foreach (var a in articles) {
    Console.WriteLine(a.Author.Name);  // N requêtes
}

// ToList puis filtrer en mémoire
var all = await context.Articles.ToListAsync();
var filtered = all.Where(a => ...);  // Filtrage C#

// Sync au lieu d'async
context.Articles.ToList();  // Bloque thread

// DbContext par requête dans boucle
foreach (var id in ids) {
    using var ctx = new BlogDbContext();  // ❌
}
```

---

## Ressources

- [Documentation EF Core](https://learn.microsoft.com/ef/core/)
- [Performance Best Practices](https://learn.microsoft.com/ef/core/performance/)
- [EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet)
