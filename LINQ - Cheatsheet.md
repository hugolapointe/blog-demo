# Cheatsheet - LINQ

**Projet:** BlogDemo - Application de démonstration pédagogique
**Voir:** `Demos/QueryingDemo.cs`, `Demos/AggregationDemo.cs`

> Référence rapide des opérateurs LINQ pour requêtes et agrégations

---

## 📋 Table des Matières

- [Fondamentaux](#fondamentaux)
- [Filtrage](#filtrage)
- [Projection](#projection)
- [Tri](#tri)
- [Agrégation](#agrégation)
- [Regroupement](#regroupement)
- [Pagination](#pagination)
- [Quantification](#quantification)
- [Ensemble](#ensemble)
- [Récupération](#récupération)
- [Exécution](#exécution)

---

## Fondamentaux

### Method Syntax (Recommandé)

```csharp
// ✅ Method Syntax avec lambdas
context.Articles
    .Where(a => a.Title.Contains("LINQ"))
    .OrderByDescending(a => a.CreatedAt)
    .ToListAsync();

// Query Syntax (rarement utilisé avec EF Core)
from a in context.Articles
where a.Title.Contains("LINQ")
orderby a.CreatedAt descending
select a;
```

---

## Filtrage

**Équivalent SQL:** `WHERE`

### Where

```csharp
// Filtre simple
.Where(a => a.Title.Contains("LINQ"))

// Multiple (ET logique)
.Where(a => a.Title.Contains("LINQ"))
.Where(a => a.CreatedAt >= DateTime.Today)

// Équivalent &&
.Where(a => a.Title.Contains("LINQ") &&
           a.CreatedAt >= DateTime.Today)

// OU logique
.Where(a => a.Title.Contains("LINQ") ||
           a.Title.Contains("EF Core"))
```

### OfType

```csharp
// Filtrer par type dérivé
.OfType<SpecialArticle>()
```

---

## Projection

**Équivalent SQL:** `SELECT`

### Select

```csharp
// Une propriété
.Select(a => a.Title)

// Type anonyme
.Select(a => new {
    a.Title,
    AuthorName = a.Author.Name,
    CommentCount = a.Comments.Count
})

// DTO
.Select(a => new ArticleDto {
    Title = a.Title,
    AuthorName = a.Author.Name
})
```

### SelectMany

```csharp
// Aplatir collections
var allComments = context.Articles
    .SelectMany(a => a.Comments)
    .ToListAsync();

// Avec transformation
.SelectMany(
    a => a.Comments,
    (article, comment) => new {
        Article = article.Title,
        Comment = comment.Content
    })
```

---

## Tri

**Équivalent SQL:** `ORDER BY`

### OrderBy / OrderByDescending

```csharp
// Croissant
.OrderBy(a => a.CreatedAt)

// Décroissant
.OrderByDescending(a => a.CreatedAt)
```

### ThenBy / ThenByDescending

```csharp
// Multi-critères
.OrderByDescending(a => a.CreatedAt)
.ThenBy(a => a.Title)
.ThenBy(a => a.Id)
```

**⚠️ Important:**
```csharp
// ❌ OrderBy remplace le tri précédent
.OrderBy(a => a.Date).OrderBy(a => a.Title)  // Date ignoré

// ✅ Utiliser ThenBy
.OrderBy(a => a.Date).ThenBy(a => a.Title)
```

---

## Agrégation

**Équivalent SQL:** `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`

### Count / LongCount

```csharp
// Total
.CountAsync()

// Avec filtre
.CountAsync(a => a.Title.Contains("EF"))

// Grands volumes
.LongCountAsync()
```

### Sum / Average / Min / Max

```csharp
.SumAsync(a => a.ViewCount)
.AverageAsync(a => a.Rating)
.MinAsync(a => a.CreatedAt)
.MaxAsync(a => a.ViewCount)
```

---

## Regroupement

**Équivalent SQL:** `GROUP BY`, `HAVING`

### GroupBy

```csharp
// Grouper et agréger
context.Articles
    .GroupBy(a => a.Author.Name)
    .Select(g => new {
        AuthorName = g.Key,
        ArticleCount = g.Count(),
        TotalComments = g.Sum(a => a.Comments.Count),
        AvgViews = g.Average(a => a.ViewCount)
    })
    .ToListAsync();
```

### Having (Where après GroupBy)

```csharp
// Filtrer les groupes
context.Articles
    .GroupBy(a => a.Author.Name)
    .Where(g => g.Count() > 5)  // HAVING
    .Select(g => new {
        AuthorName = g.Key,
        Count = g.Count()
    })
    .ToListAsync();
```

**Distinction:**
- `Where()` **avant** `GroupBy()` = filtre lignes (WHERE)
- `Where()` **après** `GroupBy()` = filtre groupes (HAVING)

---

## Pagination

**Équivalent SQL:** `OFFSET`, `FETCH`

### Skip / Take

```csharp
// ⚠️ OrderBy OBLIGATOIRE
context.Articles
    .OrderByDescending(a => a.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Avec total
var total = await context.Articles.CountAsync();
var totalPages = (int)Math.Ceiling(total / (double)pageSize);
```

### SkipWhile / TakeWhile

```csharp
// Ignorer tant que condition vraie
.SkipWhile(a => a.CreatedAt < DateTime.Today.AddDays(-7))

// Prendre tant que condition vraie
.TakeWhile(a => a.CreatedAt >= DateTime.Today)
```

---

## Quantification

**Équivalent SQL:** `EXISTS`

### Any

```csharp
// Au moins un élément
.AnyAsync()

// Avec condition
.AnyAsync(a => a.Title.Contains("LINQ"))

// ✅ Plus rapide que Count() > 0
await context.Articles.AnyAsync()         // ✅
await context.Articles.CountAsync() > 0   // ❌
```

### All

```csharp
// Tous satisfont condition
.AllAsync(a => a.IsPublished)
```

### Contains

```csharp
// SQL IN
var ids = new[] { id1, id2, id3 };
.Where(a => ids.Contains(a.Id))
// SQL: WHERE Id IN (@p0, @p1, @p2)
```

---

## Ensemble

### Distinct

```csharp
// Éliminer doublons
.Select(a => a.Author.Name)
.Distinct()
```

### Union / Concat

```csharp
var popular = context.Articles.Where(a => a.ViewCount > 1000);
var recent = context.Articles.Where(a => a.CreatedAt >= DateTime.Today);

// Union (sans doublons)
popular.Union(recent)

// Concat (avec doublons)
popular.Concat(recent)
```

### Intersect / Except

```csharp
// Intersection
popular.Intersect(recent)

// Différence
popular.Except(recent)
```

---

## Récupération

### First / FirstOrDefault

```csharp
// Premier (exception si vide)
.FirstAsync()
.FirstAsync(a => a.Title.Contains("LINQ"))

// Premier ou null
.FirstOrDefaultAsync()
```

### Single / SingleOrDefault

```csharp
// Unique (exception si 0 ou >1)
.SingleAsync(a => a.Id == id)

// Unique ou null (exception si >1)
.SingleOrDefaultAsync()
```

### Last / LastOrDefault

```csharp
// ⚠️ Nécessite OrderBy
.OrderBy(a => a.CreatedAt).LastAsync()

// ✅ Préférer OrderByDescending + First
.OrderByDescending(a => a.CreatedAt).FirstAsync()
```

---

## Exécution

### Différée (Deferred)

```csharp
// ⚠️ Pas encore exécuté
var query = context.Articles
    .Where(a => a.Title.Contains("LINQ"))
    .OrderBy(a => a.CreatedAt);

// ✅ Exécuté ICI
var articles = await query.ToListAsync();
```

**Opérateurs différés:**
`Where`, `Select`, `OrderBy`, `ThenBy`, `Skip`, `Take`, `Join`, `GroupBy`

### Immédiate (Immediate)

```csharp
// Exécution immédiate
.ToListAsync()
.ToArrayAsync()
.FirstAsync()
.SingleAsync()
.CountAsync()
.AnyAsync()
.SumAsync()
```

### Requêtes Dynamiques

```csharp
IQueryable<Article> query = context.Articles;

if (!string.IsNullOrEmpty(searchTerm)) {
    query = query.Where(a => a.Title.Contains(searchTerm));
}

if (authorId.HasValue) {
    query = query.Where(a => a.AuthorId == authorId);
}

// Une seule requête SQL générée
var results = await query.ToListAsync();
```

---

## Conversion

### ToList / ToArray / ToDictionary

```csharp
.ToListAsync()      // List<T>
.ToArrayAsync()     // T[]

// Dictionary
.ToDictionaryAsync(a => a.Id)
.ToDictionaryAsync(a => a.Id, a => a.Title)
```

### AsEnumerable / AsQueryable

```csharp
// Passer en mémoire (LINQ to Objects)
var articles = await context.Articles.ToListAsync();
articles.AsEnumerable()
    .Where(a => ComplexCSharpMethod(a))  // C#, pas SQL
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
        query = query.Where(a =>
            a.Title.Contains(term) ||
            a.Content.Contains(term));

    if (authorId.HasValue)
        query = query.Where(a => a.AuthorId == authorId);

    if (from.HasValue)
        query = query.Where(a => a.CreatedAt >= from);

    return await query
        .OrderByDescending(a => a.CreatedAt)
        .ToListAsync();
}
```

### Pagination Complète

```csharp
public class PagedResult<T> {
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public async Task<PagedResult<Article>> GetPaged(
    int pageNumber = 1, int pageSize = 10) {

    var query = context.Articles
        .OrderByDescending(a => a.CreatedAt);

    var total = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<Article> {
        Items = items,
        TotalCount = total,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

---

## Bonnes Pratiques

### ✅ À Faire

```csharp
// Filtrer en SQL
.Where(a => a.Title.Contains("LINQ")).ToListAsync()

// Any() au lieu de Count() > 0
.AnyAsync()

// OrderBy avant Skip/Take
.OrderBy(a => a.Date).Skip(10).Take(10)

// Projeter colonnes nécessaires
.Select(a => new { a.Title, a.Author.Name })
```

### ❌ À Éviter

```csharp
// ToList puis filtrer en mémoire
var all = await context.Articles.ToListAsync();
var filtered = all.Where(a => ...);  // ❌ C#

// Count() > 0 au lieu de Any()
if (await context.Articles.CountAsync() > 0)  // ❌

// Skip/Take sans OrderBy
.Skip(10).Take(10)  // ❌ Ordre imprévisible

// Charger tout pour compter
var all = await context.Articles.ToListAsync();
var count = all.Count;  // ❌ Utiliser CountAsync()
```

---

## Ressources

- [LINQ Documentation](https://learn.microsoft.com/dotnet/csharp/linq/)
- [101 LINQ Samples](https://learn.microsoft.com/samples/dotnet/try-samples/101-linq-samples/)
- [Query Syntax vs Method Syntax](https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/linq/query-syntax-and-method-syntax-in-linq)
