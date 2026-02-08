# Entity Framework Core - Projet de Démonstration

> Projet pédagogique couvrant les concepts fondamentaux d'EF Core à travers des démonstrations pratiques et progressives.

## 📋 Table des matières

- [Aperçu](#aperçu)
- [Parcours pédagogique](#parcours-pédagogique)
- [Démonstrations](#démonstrations)
- [Cheatsheet](#cheatsheet---référence-rapide)

---

## 🎯 Aperçu

Ce projet démontre les concepts essentiels d'Entity Framework Core à travers un domaine simple de blog (Authors, Articles, Comments, Tags). Chaque démonstration illustre un concept spécifique avec son problème et sa solution.

**Objectifs pédagogiques :**
- Comprendre les différentes stratégies de chargement de données
- Identifier et résoudre les problèmes de performance courants
- Maîtriser les opérations de requêtage et d'agrégation
- Appliquer les bonnes pratiques EF Core

---

## 📚 Parcours pédagogique

### Progression recommandée

Le projet est organisé pour un apprentissage progressif :

```
1. Basic Operations       → Fondations (CRUD)
2. Loading Strategies     → Chargement des données
3. Performance Optimization → Optimisations essentielles
4. Querying              → Requêtes courantes
5. Aggregation           → Opérations avancées
```

**Conseil :** Suivez l'ordre des démonstrations, car chaque section s'appuie sur les concepts précédents.

---

## 🎓 Démonstrations

### 1. Basic Operations - Fondations du CRUD

**Fichier :** `Demos/BasicOperationsDemo.cs`

**Objectif :** Comprendre comment EF Core gère les opérations de base et le cycle de vie des entités.

**Concepts démontrés :**
- **Add** - Insérer de nouvelles entités (état Added → Unchanged)
- **Update** - Modifier des entités existantes (détection automatique)
- **Delete** - Supprimer des entités (état Deleted)
- **Entity States** - Cycle de vie (Detached, Unchanged, Added, Modified, Deleted)

**Raisonnement pédagogique :** Avant d'optimiser, il faut comprendre les fondations. Ces opérations forment la base de toute interaction avec EF Core.

---

### 2. Loading Strategies - Chargement des données

**Fichier :** `Demos/LoadingStrategiesDemo.cs`

**Objectif :** Comprendre quand et comment charger les données relationnelles.

**Concepts démontrés :**
- **Lazy Loading** - Chargement automatique à l'accès (simplicité vs requêtes multiples)
- **N+1 Problem** - Le piège classique (1 requête + N requêtes = problème de performance)
- **Eager Loading** - `Include()` charge tout en une requête (solution au N+1)
- **Explicit Loading** - Chargement manuel avec `Entry().Reference/Collection().LoadAsync()`

**Raisonnement pédagogique :** La stratégie de chargement a un impact direct sur le nombre de requêtes SQL. Comprendre le problème N+1 est essentiel pour toute application performante.

---

### 3. Performance Optimization - Optimisations essentielles

**Fichier :** `Demos/PerformanceOptimizationDemo.cs`

**Objectif :** Identifier et appliquer les optimisations courantes.

**Concepts démontrés :**
- **Tracking vs NoTracking** - `AsNoTracking()` pour lectures seules (moins de mémoire)
- **Projection** - `Select()` charge uniquement les colonnes nécessaires
- **Cartesian Product** - Problème des JOINs multiples sur collections
- **Split Queries** - `AsSplitQuery()` évite le produit cartésien

**Raisonnement pédagogique :** Ces optimisations suivent des patterns établis et sont applicables immédiatement dans tout projet.

---

### 4. Querying - Requêtes courantes

**Fichier :** `Demos/QueryingDemo.cs`

**Objectif :** Maîtriser les opérations de requêtage essentielles.

**Concepts démontrés :**
- **Filtering** - `Where()` pour filtrer (SQL WHERE)
- **Sorting** - `OrderBy()` et `ThenBy()` (attention : OrderBy remplace le tri précédent)
- **Pagination** - `Skip()` et `Take()` (OrderBy OBLIGATOIRE avant)
- **First vs Single** - Récupération d'éléments (`First` = "donnes-moi un", `Single` = "unique")
- **Any vs Count** - Vérifier l'existence (`Any()` plus rapide que `Count() > 0`)
- **Distinct** - Éliminer les doublons
- **Contains** - Clause SQL IN
- **AsQueryable** - Construction dynamique de requêtes
- **Combined** - Exemple réaliste complet

**Raisonnement pédagogique :** Ces opérations couvrent 90% des besoins courants. Les maîtriser permet de construire des requêtes efficaces rapidement.

---

### 5. Aggregation - Opérations avancées

**Fichier :** `Demos/AggregationDemo.cs`

**Objectif :** Effectuer des calculs et regroupements côté base de données.

**Concepts démontrés :**
- **Basic Aggregations** - Count, Sum, Average, Max, Min (exécutés en SQL)
- **GroupBy** - Regrouper par critère et agréger
- **Having** - Filtrer les groupes après agrégation

**Raisonnement pédagogique :** Les agrégations permettent d'effectuer des calculs complexes directement en base de données, évitant de charger toutes les données en mémoire.

---

## 📝 Cheatsheet - Référence Rapide

### Patterns des Entités EF Core

```csharp
public class Entity {
    // [EF Core] Clé primaire - protected set empêche modification externe
    public Guid Id { get; protected set; }

    // required (C# 11) - force initialisation à la création
    public required string Property { get; set; }

    // Clé étrangère - protected set (immutable après création)
    public Guid ForeignKeyId { get; protected set; }

    // [EF Core] Navigation property - virtual REQUIS pour lazy loading proxies
    // ? indique nullable (avant chargement)
    public virtual RelatedEntity? Related { get; set; }

    // Collection - [] (C# 12) équivalent à new List<>()
    public virtual ICollection<Entity> Collection { get; set; } = [];

    // [EF Core] Constructeur sans paramètre REQUIS pour la création depuis la BD
    protected Entity() { }

    // Factory method - garantit état valide à la création
    public static Entity Create(string property, Guid foreignKeyId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentOutOfRangeException.ThrowIfEqual(foreignKeyId, Guid.Empty);

        return new Entity {
            Id = Guid.NewGuid(),
            Property = property,
            ForeignKeyId = foreignKeyId
        };
    }
}
```

### Association d'Entités

**Règle:** Préférer les FK par défaut, utiliser les navigation properties si l'entité est déjà chargée.

```csharp
// ✅ BON: Par FK (pas de chargement inutile)
var comment = Comment.Create(content, articleId);
context.Comments.Add(comment);

// ✅ BON: Par navigation si déjà chargé
var article = await context.Articles.Include(a => a.Comments).FirstAsync();
article.AddComment(comment);  // Méthode de domaine
```

| Approche | Quand utiliser | Avantage |
|----------|----------------|----------|
| **Par FK** | Création, API endpoints | Pas de requête pour charger l'entité parente |
| **Par Navigation** | Entité déjà en mémoire | Évite requête supplémentaire |
| **Relations N-N** | Tags, catégories | EF Core gère la table de jointure automatiquement |

### Stratégies de Chargement

| Stratégie | Syntaxe | Quand utiliser |
|-----------|---------|----------------|
| **Lazy Loading** | `article.Author.Name` | Accès occasionnel aux relations |
| **Eager Loading** | `.Include(x => x.Author)` | Relations toujours nécessaires (évite N+1) |
| **Explicit Loading** | `Entry(x).Reference(r => r.Author).LoadAsync()` | Chargement conditionnel |
| **Projection** | `.Select(x => new { x.Title })` | Lecture seule, colonnes spécifiques |

### Optimisations Performance

| Technique | Code | Gain |
|-----------|------|------|
| **No Tracking** | `.AsNoTracking()` | Moins de mémoire, plus rapide (lecture seule) |
| **Projection** | `.Select(x => new { ... })` | Moins de données transférées |
| **Split Query** | `.AsSplitQuery()` | Évite produit cartésien (collections multiples) |
| **AsQueryable** | `IQueryable<T> query = ...` | Construction dynamique de requêtes |

### Opérations de Requêtage

```csharp
// Filtrage
.Where(article => article.Title.Contains("text"))

// Tri
.OrderBy(article => article.Date)          // Critère principal
.ThenBy(article => article.Title)          // Critère secondaire

// Pagination (OrderBy OBLIGATOIRE avant)
.Skip(pageNumber * pageSize)
.Take(pageSize)

// Récupération
.First()           // Premier élément (exception si vide)
.FirstOrDefault()  // Premier ou null
.Single()          // Unique élément (exception si 0 ou >1)

// Existence
.Any()            // Au moins un existe (rapide)
.Count()          // Nombre total (plus lent)

// Transformation
.Distinct()       // Élimine doublons
.Contains(list)   // SQL IN clause
```

### Agrégations

```csharp
// Agrégations simples
.Count()
.Sum(article => article.ViewCount)
.Average(article => article.Rating)
.Max(article => article.Date)
.Min(article => article.Price)

// Regroupement
.GroupBy(article => article.Author.Name)
.Select(group => new {
    AuthorName = group.Key,
    ArticleCount = group.Count(),
    TotalViews = group.Sum(article => article.ViewCount)
})

// Filtrer les groupes (HAVING)
.Where(group => group.Count() > 1)  // Après GroupBy
```

### États des Entités

| État | Signification | Résultat SaveChanges() |
|------|---------------|------------------------|
| **Detached** | Non trackée par EF Core | Aucune action |
| **Unchanged** | Trackée, pas de modification | Aucune action |
| **Added** | Nouvelle entité | INSERT |
| **Modified** | Entité modifiée | UPDATE |
| **Deleted** | Marquée pour suppression | DELETE |

### Opérateurs Null

```csharp
article.Author?.Name     // Null-conditional: retourne null si Author est null
article.Author!.Name     // Null-forgiving: assure qu'Author n'est PAS null
```

**Quand utiliser `!`** : Après `Include()` car on sait que la relation est chargée

---

## 📚 Ressources additionnelles

- [Documentation officielle EF Core](https://learn.microsoft.com/ef/core/)
- [Performance Best Practices](https://learn.microsoft.com/ef/core/performance/)
- [Querying Data](https://learn.microsoft.com/ef/core/querying/)

---

**Version :** 1.0
**Dernière mise à jour :** Février 2026
