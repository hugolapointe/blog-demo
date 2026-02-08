# Cheatsheet - Entity Framework Core

**Projet:** BlogDemo - Application de démonstration pédagogique
**Voir:** `Data/BlogDbContext.cs`, `Data/BlogSeeder.cs`, `Demos/BasicOperationsDemo.cs`, `Demos/LoadingStrategiesDemo.cs`, `Demos/OptimizationDemo.cs`, `Demos/DeleteBehaviorDemo.cs`

> Référence rapide pour EF Core - Architecture, configuration, CRUD, chargement, optimisations

---

## Table des Matières

- [Introduction](#introduction)
- [Fournisseurs](#fournisseurs)
- [Approches](#approches)
- [Configuration](#configuration)
- [Opérations CRUD](#opérations-crud)
- [États d'Entités](#états-dentités)
- [Stratégies de Chargement](#stratégies-de-chargement)
- [Optimisations](#optimisations)
- [Fluent API](#fluent-api)
- [Migrations](#migrations)
- [Seeding](#seeding)
- [Change Tracker](#change-tracker)
- [Débogage](#débogage)
- [Bonnes Pratiques](#bonnes-pratiques)
- [Ressources](#ressources)

---

## Introduction

### Qu'est-ce qu'un ORM?

Un **ORM** (Object-Relational Mapper) fait le pont entre les objets C# et les tables de la base de données. EF Core traduit les requêtes LINQ en SQL et mappe les résultats vers des objets.

### Architecture

```
Code C#                    EF Core                      Base de données
──────────                 ──────────                   ──────────
Entités (classes)    →     DbContext / DbSet       →    Tables
Propriétés           →     Change Tracker          →    Colonnes
LINQ (.Where, etc.)  →     Traduction SQL          →    Requêtes SQL
SaveChangesAsync()   →     Détection changements   →    INSERT/UPDATE/DELETE
```

### Composants clés

| Composant | Rôle |
|-----------|------|
| **DbContext** | Point d'entrée principal, gère la connexion et le suivi |
| **DbSet\<T>** | Représente une table, permet les requêtes LINQ |
| **Change Tracker** | Détecte les modifications sur les entités |
| **Fluent API** | Configure le mapping entités ↔ tables |
| **Migrations** | Gère l'évolution du schéma de la BD |

**Voir:** `Data/BlogDbContext.cs` — DbContext du projet avec DbSet et configuration Fluent API

---

## Fournisseurs

EF Core supporte plusieurs bases de données via des **providers** (packages NuGet).

| Fournisseur | Package NuGet | Usage |
|-------------|---------------|-------|
| **SQLite** | `Microsoft.EntityFrameworkCore.Sqlite` | Développement, mobile |
| **SQL Server** | `Microsoft.EntityFrameworkCore.SqlServer` | Production Windows |
| **PostgreSQL** | `Npgsql.EntityFrameworkCore.PostgreSQL` | Production Linux |
| **MySQL** | `Pomelo.EntityFrameworkCore.MySql` | Alternative open-source |
| **InMemory** | `Microsoft.EntityFrameworkCore.InMemory` | Tests unitaires |

Pour changer de fournisseur, il suffit de remplacer le package NuGet et l'appel de configuration (`UseSqlite` → `UseSqlServer`, etc.). Le reste du code reste identique.

**Voir:** `BlogDemo.csproj` — packages Sqlite et SqlServer installés

---

## Approches

### Code-First (Recommandé)

On écrit les classes C# d'abord, puis on génère la base de données via les migrations.

```
Classes C# → Migrations → Base de données
```

C'est l'approche utilisée dans ce projet. Les entités dans `Domain/` définissent le modèle.

### Database-First

La base de données existe déjà, on génère les classes C# à partir du schéma.

```bash
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer
```

Utile pour intégrer une BD existante ou héritée (legacy).

### Model-First (Déconseillé)

Approche visuelle avec un designer graphique → génère BD et classes. Abandonnée dans EF Core, ne pas utiliser.

---

## Configuration

### DbContext minimal

**Voir:** `Data/BlogDbContext.cs`

Pas de `DbSet<Comment>` — les commentaires sont accessibles via `Article.Comments` (pattern DDD).

```csharp
public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options) {
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Article> Articles => Set<Article>();
}
```

### SQL Server

```csharp
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### SQLite (fichier)

```csharp
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite("Data Source=blog.db"));
```

### SQLite InMemory

**Voir:** `Program.cs`

La connexion doit rester ouverte tant que le contexte est utilisé. `EnsureCreatedAsync()` crée le schéma sans migrations.

```csharp
var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
optionsBuilder.UseSqlite("DataSource=:memory:");

using var context = new BlogDbContext(optionsBuilder.Options);
await context.Database.OpenConnectionAsync();
await context.Database.EnsureCreatedAsync();
```

### InMemory (tests unitaires)

Provider sans base de données réelle. Ne supporte pas les contraintes relationnelles (FK, index). À utiliser uniquement pour des tests simples.

```csharp
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

### Options courantes

| Option | Usage |
|--------|-------|
| `UseLazyLoadingProxies()` | Active le lazy loading automatique |
| `LogTo(Console.WriteLine)` | Affiche le SQL généré |
| `EnableSensitiveDataLogging()` | Affiche les valeurs des paramètres (dev seulement) |
| `EnableDetailedErrors()` | Messages d'erreur détaillés |

---

## Opérations CRUD

**Voir:** `Demos/BasicOperationsDemo.cs`

### Create

```csharp
context.Authors.Add(author);
await context.SaveChangesAsync();

context.Authors.AddRange(author1, author2);
await context.SaveChangesAsync();
```

### Read

`FindAsync` est optimise pour les cles primaires et verifie le cache local avant de requeter la BD.

```csharp
var authors = await context.Authors.ToListAsync();
var author = await context.Authors.FindAsync(id);
var alice = await context.Authors.FirstOrDefaultAsync(a => a.Name == "Alice");
```

### Update

Entite trackee (recommande) — la detection des changements est automatique. Pour une entite non-trackee, utiliser `Update()`.

```csharp
var author = await context.Authors.FindAsync(id);
author.Name = "Bob";
await context.SaveChangesAsync();

context.Authors.Update(author);
await context.SaveChangesAsync();
```

### Delete

```csharp
var author = await context.Authors.FindAsync(id);
context.Authors.Remove(author);
await context.SaveChangesAsync();
```

---

## États d'Entités

**Voir:** `Demos/BasicOperationsDemo.cs` méthode `EntityStatesAsync`

| État | Description | SaveChanges() |
|------|-------------|---------------|
| `Detached` | Non trackée par EF Core | Rien |
| `Unchanged` | Trackée, pas modifiée | Rien |
| `Added` | Nouvelle entité | INSERT |
| `Modified` | Propriété modifiée | UPDATE |
| `Deleted` | Marquée pour suppression | DELETE |

```csharp
var state = context.Entry(author).State;
context.Entry(author).State = EntityState.Modified;
```

Cycle de vie typique : `Detached` → `Added` → `Unchanged` → `Modified` → `Unchanged`

---

## Stratégies de Chargement

**Voir:** `Demos/LoadingStrategiesDemo.cs`

### Comparaison

| Stratégie | Quand | Requêtes | Prérequis |
|-----------|-------|----------|-----------|
| **Lazy Loading** | Accès occasionnel | N+1 | `UseLazyLoadingProxies()` + `virtual` |
| **Eager Loading** | Données toujours nécessaires | 1 (JOIN) | `Include()` |
| **Explicit Loading** | Chargement conditionnel | Sur demande | `Entry().LoadAsync()` |

### Lazy Loading

Requiert des proprietes `virtual` et `UseLazyLoadingProxies()`. L'acces a une navigation property declenche automatiquement une requete SQL.

```csharp
var article = await context.Articles.FirstAsync();
var name = article.Author.Name;
```

**Problème N+1** — Voir: `Demos/LoadingStrategiesDemo.cs` méthode `ProblemN1Async`

Chaque iteration declenche une requete supplementaire pour Author (1 + N requetes au total).

```csharp
var articles = await context.Articles.ToListAsync();
foreach (var a in articles)
    Console.WriteLine(a.Author.Name);
```

### Eager Loading (Include)

Une relation directe avec `Include`, ou imbriquee avec `ThenInclude`.

```csharp
.Include(a => a.Author)
.Include(a => a.Comments).ThenInclude(c => c.Article)
```

### Explicit Loading

`Reference()` pour les relations 1-1 ou N-1, `Collection()` pour les relations 1-N.

```csharp
var article = await context.Articles.FirstAsync();
await context.Entry(article).Reference(a => a.Author).LoadAsync();
await context.Entry(article).Collection(a => a.Comments).LoadAsync();
```

---

## Optimisations

**Voir:** `Demos/OptimizationDemo.cs`

### AsNoTracking

Pour les lectures seules : plus rapide, moins de memoire.

```csharp
var articles = await context.Articles.AsNoTracking().ToListAsync();
```

### Projection (Select)

Charge uniquement les colonnes necessaires. Pas de tracking automatique.

```csharp
var summaries = await context.Articles
    .Select(a => new { a.Title, AuthorName = a.Author.Name, CommentCount = a.Comments.Count })
    .ToListAsync();
```

### Split Queries

**Voir:** `Demos/OptimizationDemo.cs` méthode `SplitQueriesAsync`

Évite le produit cartésien (N*M lignes) en générant des requêtes séparées au lieu d'un seul JOIN.

```csharp
await context.Articles
    .Include(a => a.Comments)
    .Include(a => a.Tags)
    .AsSplitQuery()
    .ToListAsync();
```

### Batching

EF Core regroupe automatiquement les operations en un seul INSERT.

```csharp
context.Authors.AddRange(author1, author2, author3);
await context.SaveChangesAsync();
```

**Voir aussi:** `LINQ - Cheatsheet.md` pour les détails sur Select, projection et pagination

---

## Fluent API

**Voir:** `Data/BlogDbContext.cs` méthode `OnModelCreating`

### Propriétés

```csharp
entity.HasKey(e => e.Id);
entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
```

### Relations

**One-to-Many (1-N)** — Voir: `BlogDbContext.cs` ConfigureAuthor

```csharp
entity.HasMany(a => a.Articles)
    .WithOne(a => a.Author)
    .OnDelete(DeleteBehavior.Restrict);
```

**Many-to-Many (N-N)** — configure par convention (Article ↔ Tag)

```csharp
entity.HasMany(a => a.Tags).WithMany(t => t.Articles);
```

**One-to-One (1-1)**

```csharp
entity.HasOne(u => u.Profile).WithOne(p => p.User).HasForeignKey<Profile>(p => p.UserId);
```

### Index

**Voir:** `Data/BlogDbContext.cs` — index sur Title, CreatedAt, composite AuthorId+CreatedAt, unique sur Tag.Name

```csharp
entity.HasIndex(e => e.Title);
entity.HasIndex(e => new { e.AuthorId, e.CreatedAt });
entity.HasIndex(e => e.Name).IsUnique();
```

Index simple, composite (plusieurs colonnes) et unique.

### Delete Behaviors

| DeleteBehavior | Effet |
|----------------|-------|
| `Cascade` | Supprime les enfants automatiquement |
| `Restrict` | Bloque la suppression si dépendances existent |
| `SetNull` | Met la FK à NULL |
| `NoAction` | Aucune action (erreur BD possible) |

**Voir:** `Data/BlogDbContext.cs` — `DeleteBehavior.Restrict` sur Author → Articles
**Voir:** `Demos/DeleteBehaviorDemo.cs` — démonstration Restrict et Cascade

---

## Migrations

### EnsureCreated vs Migrate

| Méthode | Usage |
|---------|-------|
| `EnsureCreatedAsync()` | Crée la BD sans migrations (dev, tests) |
| `MigrateAsync()` | Applique les migrations (production) |

### Commandes CLI

Créer une migration :

```bash
dotnet ef migrations add NomMigration
```

Appliquer les migrations :

```bash
dotnet ef database update
```

### Solution multi-projets

Quand le DbContext est dans un projet séparé (ex: `Data`), spécifier le projet contenant le contexte et le projet de démarrage :

```bash
dotnet ef migrations add NomMigration --project Data --startup-project WebApp --context BlogDbContext
dotnet ef database update --project Data --startup-project WebApp --context BlogDbContext
```

| Option | Rôle |
|--------|------|
| `--project` | Projet contenant le DbContext et les migrations |
| `--startup-project` | Projet exécutable (pour la configuration) |
| `--context` | Classe DbContext à utiliser (si plusieurs contextes) |

---

## Seeding

**Voir:** `Data/BlogSeeder.cs`

Verifier avec `AnyAsync()` avant de seeder pour eviter les doublons.

```csharp
public static async Task SeedAsync(BlogDbContext context) {
    if (await context.Authors.AnyAsync()) return;

    var alice = Author.Create("Alice");
    context.Authors.Add(alice);
    await context.SaveChangesAsync();
}
```

Appelé dans `Program.cs` après la création de la BD.

---

## Change Tracker

```csharp
context.Entry(author).State
context.Entry(author).State = EntityState.Modified
context.ChangeTracker.Clear()
```

Verifier l'etat, forcer un etat manuellement, ou vider le cache du tracker.

**Voir:** `Demos/DemoBase.cs` — `ChangeTracker.Clear()` utilisé entre chaque démo

---

## Débogage

### Voir le SQL généré

```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
```

**Voir:** `Program.cs` — logging SQL activé pour toutes les démos

### Afficher les valeurs des paramètres

Dev seulement, jamais en production.

```csharp
optionsBuilder.EnableSensitiveDataLogging();
```

### Vérifier les requêtes

Activer le logging SQL et surveiller :
- Le nombre de requêtes (détecter N+1)
- Les colonnes chargées (détecter les SELECT * inutiles)
- Les JOINs (détecter les produits cartésiens)

---

## Bonnes Pratiques

### A Faire

- `AsNoTracking()` pour les lectures seules
- `Include()` pour éviter le problème N+1
- `Select()` pour charger uniquement les colonnes nécessaires
- `AsSplitQuery()` avec multiples Include sur des collections
- `async/await` pour toutes les opérations BD
- `Any()` au lieu de `Count() > 0`
- Configurer les index sur les colonnes fréquemment filtrées

### A Éviter

- Lazy loading dans une boucle (N+1)
- `ToList()` puis filtrer en mémoire
- Méthodes synchrones (`ToList()` au lieu de `ToListAsync()`)
- Créer un DbContext par itération dans une boucle
- `EnableSensitiveDataLogging()` en production

---

## Ressources

- [Documentation EF Core](https://learn.microsoft.com/ef/core/)
- [Performance Best Practices](https://learn.microsoft.com/ef/core/performance/)
- [EF Core CLI Tools](https://learn.microsoft.com/ef/core/cli/dotnet)
