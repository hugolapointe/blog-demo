# Cheatsheet - Domain-Driven Design (DDD)

**Projet:** BlogDemo - Application de démonstration pédagogique
**Voir:** `Domain/Article.cs`, `Domain/Author.cs`, `Domain/Comment.cs`

> Référence rapide des concepts DDD avec EF Core

---

## 📋 Table des Matières

- [Introduction](#introduction)
- [Aggregates](#aggregates)
- [Entities vs Value Objects](#entities-vs-value-objects)
- [Factory Methods](#factory-methods)
- [Repositories](#repositories)
- [Ubiquitous Language](#ubiquitous-language)
- [Quand Utiliser DDD](#quand-utiliser-ddd)
- [Checklist](#checklist)

---

## Introduction

### Qu'est-ce que DDD?

**DDD** place la **logique métier** au centre du code, pas la base de données ni l'UI.

**Objectif:** Code qui reflète fidèlement les règles métier et est maintenable.

**Principes:**
- Code = langage métier (pas technique)
- Règles métier dans entités (pas services)
- Domaine indépendant (BD, UI)

### Pourquoi DDD?

| Sans DDD | Avec DDD |
|----------|----------|
| Logique éparpillée | Logique centralisée |
| Code technique | Langage métier |
| Règles contournables | État toujours valide |
| Couplage BD | Indépendance |

### Architecture

```
┌────────────────────────┐
│ Presentation (UI/API)  │
├────────────────────────┤
│ Application (Services) │
├────────────────────────┤
│ Domain ⭐ (Entités)     │
├────────────────────────┤
│ Infrastructure (Data)  │
└────────────────────────┘
```

Domain Layer ne dépend de rien.

---

## Aggregates

### Définition

**Aggregate** = groupe d'objets cohérents
**Aggregate Root** = entité principale qui contrôle l'accès

### Exemple

**Voir:** `Domain/Article.cs`, `Domain/Comment.cs`

```csharp
// Article = Aggregate Root
public class Article {
    public Guid Id { get; protected set; }
    public virtual ICollection<Comment> Comments { get; set; } = [];

    // ✅ Seule façon d'ajouter commentaire
    public Comment AddComment(string content) {
        var comment = Comment.CreateInternal(content, Id);
        Comments.Add(comment);
        return comment;
    }
}

// Comment = Entité enfant
public class Comment {
    protected Comment() { }

    // internal = seul Article peut créer
    internal static Comment CreateInternal(string content, Guid articleId) {
        return new Comment {
            Id = Guid.NewGuid(),
            Content = content,
            ArticleId = articleId
        };
    }
}
```

**Utilisation:**
```csharp
// ✅ Via aggregate root
article.AddComment("Super!");

// ❌ Bypass aggregate
context.Comments.Add(new Comment { ... });
```

### Règles

| Règle | Explication |
|-------|-------------|
| 1 aggregate = 1 transaction | Tout dans SaveChanges() |
| Petits aggregates | Ne pas tout grouper |
| Références par ID | `Guid AuthorId`, pas `Author` objet |
| Cohérence immédiate | Règles garanties |

### Quand l'utiliser?

**✅ Utiliser si:**
- Entités liées avec règles métier
- Cycle de vie commun
- Cohérence à garantir

**❌ Over-engineering si:**
- Simple CRUD sans règles
- Entités indépendantes
- Petit projet (<100 lignes)

---

## Entities vs Value Objects

### Entity

**Définition:** Objet avec identité (ID)

```csharp
public class Article {
    public Guid Id { get; protected set; }
    public string Title { get; set; }
}

// Même ID = même entité
var a1 = new Article { Id = guid1, Title = "A" };
var a2 = new Article { Id = guid1, Title = "B" };
// a1 == a2 (même ID)
```

### Value Object

**Définition:** Objet défini par valeurs, immutable

```csharp
public class Money {
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency) {
        Amount = amount;
        Currency = currency;
    }

    public override bool Equals(object? obj) =>
        obj is Money m &&
        Amount == m.Amount &&
        Currency == m.Currency;

    public override int GetHashCode() =>
        HashCode.Combine(Amount, Currency);
}

// Mêmes valeurs = identiques
var m1 = new Money(10, "CAD");
var m2 = new Money(10, "CAD");
// m1.Equals(m2) == true
```

### Quand utiliser?

| Type | Quand | Exemples |
|------|-------|----------|
| **Entity** | ID, mutable | Article, Author |
| **Value Object** | Pas ID, immutable | Money, Address, Email |

**⚠️ Over-engineering:** Créer value objects partout pour 1-2 propriétés simples

---

## Factory Methods

### Problème et Solution

**Voir:** `Domain/Article.cs` méthode `Create`

**Problème:**
```csharp
// ❌ État invalide possible
var article = new Article();
article.Title = "Test";  // Pas de contenu!
```

**Solution:**
```csharp
public class Article {
    protected Article() { }

    public static Article Create(string title, string content, Guid authorId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Article {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            AuthorId = authorId,
            CreatedAt = DateTime.Now
        };
    }
}

// ✅ Toujours valide
var article = Article.Create("Titre", "Contenu", authorId);
```

### Quand l'utiliser?

**✅ Utiliser si:**
- Règles validation importantes
- Logique initialisation complexe
- Garantir état valide

**❌ Over-engineering si:**
- Simple DTO
- Prototypage rapide
- Aucune règle métier

---

## Repositories

### Problème et Solution

**Problème:** Couplage direct à EF Core
```csharp
// ❌ Partout dans le code
context.Articles.Where(a => a.Status == 2).ToListAsync();
```

**Solution:** Abstraction métier
```csharp
// Interface (Domain Layer)
public interface IArticleRepository {
    Task<Article?> GetByIdAsync(Guid id);
    Task<List<Article>> GetPublishedAsync();
    Task AddAsync(Article article);
}

// Implémentation (Infrastructure Layer)
public class ArticleRepository : IArticleRepository {
    private readonly BlogDbContext _context;

    public async Task<Article?> GetByIdAsync(Guid id) =>
        await _context.Articles
            .Include(a => a.Comments)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Article>> GetPublishedAsync() =>
        await _context.Articles
            .Where(a => a.Status == ArticleStatus.Published)
            .ToListAsync();
}
```

### Avantages

| Avec Repository | Sans |
|-----------------|------|
| `repository.GetPublished()` | `context.Articles.Where(a => a.Status == 2)` |
| Vocabulaire métier | Technique |
| Testable (mock) | Difficile |

### Quand l'utiliser?

**✅ Utiliser si:**
- Plusieurs services
- Tests sans BD
- Équipe

**❌ Over-engineering si:**
- CRUD simple
- Seul sur petit projet

**Alternative:** Injecter `BlogDbContext` directement est OK pour petits projets

---

## Ubiquitous Language

### Principe

**Code = langage client/expert métier**

| Métier dit | Code doit dire | ❌ Pas |
|------------|---------------|--------|
| Publier article | `article.Publish()` | `SetStatus(2)` |
| Archiver | `article.Archive()` | `SetArchived(true)` |
| Ajouter commentaire | `article.AddComment()` | `comment.SetArticleId()` |

### Exemple

```csharp
// ✅ Langage métier
public class Article {
    public void Publish() {
        if (Status == ArticleStatus.Published)
            throw new InvalidOperationException(
                "Article déjà publié");

        Status = ArticleStatus.Published;
        PublishedAt = DateTime.Now;
    }
}

article.Publish();  // Clair

// ❌ Vocabulaire technique
article.SetStatusCode(2);
article.UpdateTimestamp();
```

### En Pratique

**✅ Appliquer:**
- Classes: `Article`, `Author` (pas `Post`, `User`)
- Méthodes: `Publish()`, `Archive()` (pas `SetStatus()`)
- Propriétés: `PublishedAt` (pas `Timestamp`)
- Enums: `ArticleStatus.Published` (pas `Status.Two`)

---

## Checklist

### Aggregate Root
- [ ] `Id`
- [ ] Contrôle entités enfants
- [ ] Méthodes Add/Remove enfants
- [ ] Valide règles métier
- [ ] Références autres aggregates par ID

### Entity
- [ ] `Id`
- [ ] `protected set` sur propriétés immuables
- [ ] Factory Method avec validation
- [ ] Méthodes métier (`Publish`, `Archive`)

### Value Object
- [ ] Pas d'Id
- [ ] `{ get; }` immutable
- [ ] Constructeur initialisation
- [ ] `Equals()` et `GetHashCode()`

---

## Patterns avec EF Core

### 1. Aggregate Root + Enfants

```csharp
public class Article {
    public virtual ICollection<Comment> Comments { get; set; } = [];

    public Comment AddComment(string content) {
        var comment = Comment.CreateInternal(content, Id);
        Comments.Add(comment);
        return comment;
    }
}

// ✅ Via aggregate
article.AddComment("Super!");
```

### 2. Références par ID

```csharp
// ✅ ID + navigation
public class Article {
    public Guid AuthorId { get; protected set; }
    public virtual Author? Author { get; set; }  // EF Core
}

// ❌ Objet uniquement
public class Article {
    public Author Author { get; set; }  // Couplage fort
}
```

### 3. Logique dans Entité

```csharp
// ✅ Entité
public class Article {
    public void Archive() {
        if (Status != ArticleStatus.Published)
            throw new InvalidOperationException(
                "Seuls articles publiés archivables");

        Status = ArticleStatus.Archived;
    }
}

// ❌ Service
public class ArticleService {
    public void Archive(Article article) {
        if (article.Status != ArticleStatus.Published)
            throw new InvalidOperationException("...");

        article.Status = ArticleStatus.Archived;  // ❌
    }
}
```

### 4. Protected Setters

```csharp
public class Article {
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public Guid AuthorId { get; protected set; }

    public string Title { get; set; }  // OK modifier
}
```

---

## Ressources

- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing DDD - Vaughn Vernon](https://vaughnvernon.com/)
- [DDD with EF Core](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
