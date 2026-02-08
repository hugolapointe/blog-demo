# Cheatsheet - LINQ

**Projet:** BlogDemo - Application de démonstration pédagogique
**Voir:** `Demos/QueryingDemo.cs`, `Demos/AggregationDemo.cs`, `Demos/OptimizationDemo.cs`

> Référence rapide des opérateurs LINQ pour requêtes et agrégations

---

## Table des Matières

- [Fondamentaux](#fondamentaux)
- [Filtrage](#filtrage)
- [Projection](#projection)
- [Tri](#tri)
- [Pagination](#pagination)
- [Récupération](#récupération)
- [Quantification](#quantification)
- [Ensemble](#ensemble)
- [Agrégation](#agrégation)
- [Regroupement](#regroupement)
- [Exécution](#exécution)
- [Conversion](#conversion)
- [Patterns Courants](#patterns-courants)
- [Bonnes Pratiques](#bonnes-pratiques)
- [Ressources](#ressources)

---

## Fondamentaux

### Method Syntax (Recommandé)

Method Syntax avec lambdas, utilise dans tout le projet.

```csharp
context.Articles
    .Where(a => a.Title.Contains("LINQ"))
    .OrderByDescending(a => a.CreatedAt)
    .ToListAsync();
```

### Query Syntax

Rarement utilise avec EF Core.

```csharp
from a in context.Articles
where a.Title.Contains("LINQ")
orderby a.CreatedAt descending
select a;
```

### IQueryable vs IEnumerable

| Type | Exécution | Traduction |
|------|-----------|------------|
| `IQueryable<T>` | Côté BD (SQL) | LINQ → SQL |
| `IEnumerable<T>` | Côté mémoire (C#) | LINQ to Objects |

Toujours travailler avec `IQueryable` le plus longtemps possible avant de matérialiser.

**Voir:** `Demos/QueryingDemo.cs` méthode `AsQueryableAsync`

---

## Filtrage

**Equivalent SQL:** `WHERE`
**Voir:** `Demos/QueryingDemo.cs` méthode `FilteringAsync`

### Where

Filtre simple, multiple (ET logique en chainant), ou OU logique dans le meme lambda.

```csharp
.Where(a => a.Title.Contains("LINQ"))

.Where(a => a.Title.Contains("LINQ"))
.Where(a => a.CreatedAt >= DateTime.Today)

.Where(a => a.Title.Contains("LINQ") || a.Title.Contains("EF Core"))
```

### OfType

Filtrer par type derive (heritage).

```csharp
.OfType<SpecialArticle>()
```

---

## Projection

**Equivalent SQL:** `SELECT`
**Voir:** `Demos/OptimizationDemo.cs` méthode `ProjectionAsync`

### Select

Une propriete, un type anonyme (charge uniquement les colonnes necessaires), ou un DTO.

```csharp
.Select(a => a.Title)

.Select(a => new {
    a.Title,
    AuthorName = a.Author.Name,
    CommentCount = a.Comments.Count
})

.Select(a => new ArticleDto { Title = a.Title, AuthorName = a.Author.Name })
```

### SelectMany

Aplatir des collections imbriquees en une seule sequence.

```csharp
var allComments = context.Articles
    .SelectMany(a => a.Comments)
    .ToListAsync();
```

---

## Tri

**Equivalent SQL:** `ORDER BY`
**Voir:** `Demos/QueryingDemo.cs` méthode `SortingAsync`

### OrderBy / OrderByDescending

Croissant ou decroissant.

```csharp
.OrderBy(a => a.CreatedAt)
.OrderByDescending(a => a.CreatedAt)
```

### ThenBy / ThenByDescending

Multi-criteres de tri.

```csharp
.OrderByDescending(a => a.CreatedAt)
.ThenBy(a => a.Title)
```

**Piège courant:**

`OrderBy` remplace le tri precedent. Utiliser `ThenBy` pour un second critere.

```csharp
.OrderBy(a => a.Date).OrderBy(a => a.Title)

.OrderBy(a => a.Date).ThenBy(a => a.Title)
```

---

## Pagination

**Equivalent SQL:** `OFFSET`, `FETCH`
**Voir:** `Demos/QueryingDemo.cs` méthode `PaginationAsync`

### Skip / Take

`OrderBy` est obligatoire avant `Skip/Take`, sinon l'ordre est imprevisible.

```csharp
context.Articles
    .OrderByDescending(a => a.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### Avec total

```csharp
var total = await context.Articles.CountAsync();
var totalPages = (int)Math.Ceiling(total / (double)pageSize);
```

---

## Récupération

**Voir:** `Demos/QueryingDemo.cs` méthode `FirstVsSingleAsync`

### First / FirstOrDefault

`First` retourne le premier element (exception si vide). `FirstOrDefault` retourne `null` si vide.

```csharp
.FirstAsync()
.FirstAsync(a => a.Title.Contains("LINQ"))
.FirstOrDefaultAsync()
```

### Single / SingleOrDefault

`Single` attend exactement un element (exception si 0 ou >1). `SingleOrDefault` retourne `null` si 0 (exception si >1).

```csharp
.SingleAsync(a => a.Id == id)
.SingleOrDefaultAsync()
```

### FindAsync (EF Core)

Optimise pour les cles primaires. Verifie le cache local avant de requeter la BD.

```csharp
await context.Articles.FindAsync(id);
```

**Voir:** `Demos/BasicOperationsDemo.cs` — utilisation de FindAsync pour Read/Update/Delete

### Last / LastOrDefault

Necessite `OrderBy`. Preferer `OrderByDescending` + `First`.

```csharp
.OrderByDescending(a => a.CreatedAt).FirstAsync()
```

---

## Quantification

**Equivalent SQL:** `EXISTS`
**Voir:** `Demos/QueryingDemo.cs` méthode `AnyVsCountAsync`

### Any

`Any()` s'arrete au premier resultat, `Count()` parcourt tout. Toujours preferer `Any()` pour verifier l'existence.

```csharp
.AnyAsync()
.AnyAsync(a => a.Title.Contains("LINQ"))

await context.Articles.AnyAsync()
await context.Articles.CountAsync() > 0
```

### All

Verifie que tous les elements satisfont la condition.

```csharp
.AllAsync(a => a.IsPublished)
```

### Contains

**Voir:** `Demos/QueryingDemo.cs` méthode `ContainsAsync`

Traduit en SQL `IN` : `WHERE Id IN (@p0, @p1, @p2)`.

```csharp
var ids = new[] { id1, id2, id3 };
.Where(a => ids.Contains(a.Id))
```

---

## Ensemble

**Voir:** `Demos/QueryingDemo.cs` méthode `DistinctAsync`

### Distinct

Eliminer les doublons.

```csharp
.Select(a => a.Author.Name).Distinct()
```

### Union / Concat

`Union` sans doublons, `Concat` avec doublons.

```csharp
var popular = context.Articles.Where(a => a.ViewCount > 1000);
var recent = context.Articles.Where(a => a.CreatedAt >= DateTime.Today);

popular.Union(recent)
popular.Concat(recent)
```

### Intersect / Except

`Intersect` retourne les elements communs, `Except` retourne ceux du premier ensemble absents du second.

```csharp
popular.Intersect(recent)
popular.Except(recent)
```

---

## Agrégation

**Equivalent SQL:** `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`
**Voir:** `Demos/AggregationDemo.cs` méthode `BasicAggregationsAsync`

### Count / LongCount

Total, avec filtre, ou `LongCount` pour les grands volumes (retourne `long`).

```csharp
.CountAsync()
.CountAsync(a => a.Title.Contains("EF"))
.LongCountAsync()
```

### Sum / Average / Min / Max

```csharp
.SumAsync(a => a.ViewCount)
.AverageAsync(a => a.Rating)
.MinAsync(a => a.CreatedAt)
.MaxAsync(a => a.ViewCount)
```

### Agrégation avec Select

Combiner `Select` et agregation pour des calculs complexes.

```csharp
var maxComments = await context.Articles
    .Select(a => a.Comments.Count)
    .MaxAsync();
```

---

## Regroupement

**Equivalent SQL:** `GROUP BY`, `HAVING`
**Voir:** `Demos/AggregationDemo.cs` méthodes `GroupByAsync` et `GroupByWithHavingAsync`

### GroupBy

```csharp
context.Articles
    .GroupBy(a => a.Author.Name)
    .Select(g => new {
        AuthorName = g.Key,
        ArticleCount = g.Count(),
        TotalComments = g.Sum(a => a.Comments.Count)
    })
    .ToListAsync();
```

### Having (Where après GroupBy)

```csharp
context.Articles
    .GroupBy(a => a.Author.Name)
    .Where(g => g.Count() > 1)       // HAVING COUNT(*) > 1
    .Select(g => new {
        AuthorName = g.Key,
        Count = g.Count()
    })
    .ToListAsync();
```

**Distinction importante :**
- `Where()` **avant** `GroupBy()` = filtre les lignes (WHERE en SQL)
- `Where()` **après** `GroupBy()` = filtre les groupes (HAVING en SQL)

---

## Exécution

### Différée (Deferred)

La requete n'est pas encore executee — elle construit l'arbre d'expression. L'execution a lieu a la materialisation (`ToListAsync`).

```csharp
var query = context.Articles
    .Where(a => a.Title.Contains("LINQ"))
    .OrderBy(a => a.CreatedAt);

var articles = await query.ToListAsync();
```

**Opérateurs différés :**
`Where`, `Select`, `OrderBy`, `ThenBy`, `Skip`, `Take`, `GroupBy`, `Include`

### Immédiate (Immediate)

```csharp
.ToListAsync()       .ToArrayAsync()
.FirstAsync()        .SingleAsync()
.CountAsync()        .AnyAsync()
.SumAsync()          .MaxAsync()
```

### Requêtes Dynamiques

**Voir:** `Demos/QueryingDemo.cs` méthode `AsQueryableAsync`

Construire la requete conditionnellement. Une seule requete SQL est generee avec tous les filtres appliques.

```csharp
IQueryable<Article> query = context.Articles;

if (!string.IsNullOrEmpty(searchTerm))
    query = query.Where(a => a.Title.Contains(searchTerm));

if (authorId.HasValue)
    query = query.Where(a => a.AuthorId == authorId);

var results = await query.ToListAsync();
```

---

## Conversion

### ToList / ToArray / ToDictionary

```csharp
.ToListAsync()
.ToArrayAsync()
.ToDictionaryAsync(a => a.Id)
.ToDictionaryAsync(a => a.Id, a => a.Title)
```

Retournent respectivement `List<T>`, `T[]`, `Dictionary<TKey, T>` et `Dictionary<TKey, TValue>`.

### AsEnumerable (passer en mémoire)

Materialiser puis filtrer en C# pour de la logique non-traduisible en SQL.

```csharp
var articles = await context.Articles.ToListAsync();
articles.AsEnumerable()
    .Where(a => ComplexCSharpMethod(a))
    .ToList();
```

---

## Patterns Courants

### Recherche Multi-Critères

```csharp
public async Task<List<Article>> Search(
    string? term, Guid? authorId, DateTime? from) {

    var query = context.Articles.AsQueryable();

    if (!string.IsNullOrEmpty(term))
        query = query.Where(a => a.Title.Contains(term) || a.Content.Contains(term));
    if (authorId.HasValue)
        query = query.Where(a => a.AuthorId == authorId);
    if (from.HasValue)
        query = query.Where(a => a.CreatedAt >= from);

    return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
}
```

### Pagination Complète

```csharp
public class PagedResult<T> {
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

**Voir:** `Demos/QueryingDemo.cs` méthode `CombinedAsync` — exemple complet Include + Where + OrderBy + Skip + Take

### Ordre typique des opérateurs

```
Include → Where → OrderBy/ThenBy → Skip → Take → Select → ToListAsync
```

---

## Bonnes Pratiques

### A Faire

- Filtrer en SQL avec `Where()` (pas après `ToList()`)
- `Any()` au lieu de `Count() > 0`
- `OrderBy` obligatoire avant `Skip/Take`
- `Select()` pour charger uniquement les colonnes nécessaires
- Construire les requêtes avec `IQueryable` avant de matérialiser

### A Éviter

- `ToList()` puis filtrer en mémoire (ramène tout de la BD)
- `Count() > 0` au lieu de `Any()`
- `Skip/Take` sans `OrderBy` (ordre imprévisible)
- Charger tout pour compter (`ToList()` puis `.Count`)
- Multiples `OrderBy` au lieu de `OrderBy` + `ThenBy`

---

## Ressources

- [LINQ Documentation](https://learn.microsoft.com/dotnet/csharp/linq/)
- [101 LINQ Samples](https://learn.microsoft.com/samples/dotnet/try-samples/101-linq-samples/)
- [EF Core Querying](https://learn.microsoft.com/ef/core/querying/)
