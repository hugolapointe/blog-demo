# Cheatsheet - Domain-Driven Design (DDD)

**Projet:** BlogDemo - Application de démonstration pédagogique
**Voir:** `Domain/Article.cs`, `Domain/Author.cs`, `Domain/Comment.cs`, `Domain/Tag.cs`, `Data/BlogDbContext.cs`, `Data/BlogSeeder.cs`

> Référence rapide des concepts DDD appliqués avec EF Core

---

## Table des Matières

- [Introduction](#introduction)
- [Bounded Context](#bounded-context)
- [Aggregates](#aggregates)
- [Entities vs Value Objects](#entities-vs-value-objects)
- [Factory Methods](#factory-methods)
- [Domain Services](#domain-services)
- [Repositories](#repositories)
- [Ubiquitous Language](#ubiquitous-language)
- [Patterns avec EF Core](#patterns-avec-ef-core)
- [Quand Utiliser DDD](#quand-utiliser-ddd)
- [Bonnes Pratiques](#bonnes-pratiques)
- [Checklist](#checklist)
- [Ressources](#ressources)

---

## Introduction

### Qu'est-ce que DDD?

**DDD** place la **logique métier** au centre du code, pas la base de données ni l'UI.

**Objectif :** Code qui reflète fidèlement les règles métier et est maintenable.

**Principes :**
- Code = langage métier (pas technique)
- Règles métier dans les entités (pas dans les services)
- Domaine indépendant de l'infrastructure (BD, UI)

### Pourquoi DDD?

| Sans DDD | Avec DDD |
|----------|----------|
| Logique éparpillée | Logique centralisée dans les entités |
| Vocabulaire technique | Langage métier |
| Règles contournables | État toujours valide |
| Couplage à la BD | Indépendance de l'infrastructure |

### Architecture en couches

```
┌────────────────────────┐
│ Presentation (UI/API)  │  ← Contrôleurs, vues
├────────────────────────┤
│ Application (Services) │  ← Orchestration, cas d'usage
├────────────────────────┤
│ Domain (Entités)       │  ← Logique métier (coeur)
├────────────────────────┤
│ Infrastructure (Data)  │  ← EF Core, BD, fichiers
└────────────────────────┘
```

Le Domain Layer ne dépend de rien. Les autres couches dépendent de lui.

**Voir:** `Domain/` (entités) et `Data/` (infrastructure) — séparation dans le projet

---

## Bounded Context

### Définition

Un **Bounded Context** délimite un modèle de domaine. Chaque contexte a son propre vocabulaire et ses propres règles.

### Exemple concret

```
┌─ Contexte Blog ──────────────┐  ┌─ Contexte Facturation ────────┐
│ Article, Author, Comment     │  │ Invoice, Customer, Payment    │
│ "Author" = qui écrit         │  │ "Customer" = qui paie         │
└──────────────────────────────┘  └──────────────────────────────┘
```

Un même concept réel (une personne) peut être modélisé différemment selon le contexte. Dans ce projet, le Bounded Context est le **Blog** : Authors, Articles, Comments, Tags.

### Règle

Chaque Bounded Context a **son propre DbContext**. Ne pas mélanger tous les domaines dans un seul contexte.

---

## Aggregates

### Définition

**Aggregate** = groupe d'objets cohérents traités comme une unité
**Aggregate Root** = entité principale qui contrôle l'accès aux enfants

### Exemple du projet

**Voir:** `Domain/Article.cs` (Aggregate Root), `Domain/Comment.cs` (entité enfant)

```
Article (Aggregate Root)
├── Comment (entité enfant — créé uniquement via Article)
├── Comment
└── [Tags] (référence vers un autre aggregate)

Author (Aggregate Root — indépendant)

Tag (Aggregate Root — partagé entre articles)
```

**Voir:** `Domain/Tag.cs` — Tag est un aggregate séparé car partagé entre plusieurs articles (relation N-N)

### Utilisation

Toujours passer par l'aggregate root. Ne jamais contourner en ajoutant directement via le DbContext.

```csharp
article.AddComment("Super!");
article.AddTag(tag);
```

**Voir:** `Data/BlogSeeder.cs` — utilisation correcte via `article.AddComment()` et `article.AddTag()`

### Règles

| Règle | Explication |
|-------|-------------|
| 1 aggregate = 1 transaction | Tout dans un seul `SaveChanges()` |
| Petits aggregates | Ne pas regrouper trop d'entités |
| Références par ID | `Guid AuthorId`, pas l'objet `Author` |
| Cohérence immédiate | Règles métier garanties à tout moment |

### Quand l'utiliser?

**Utiliser si :** Entités liées avec règles métier, cycle de vie commun, cohérence à garantir

**Over-engineering si :** Simple CRUD sans règles, entités indépendantes

---

## Entities vs Value Objects

### Entity

**Définition :** Objet identifié par un ID unique. Deux entités avec le même ID sont la même entité, même si leurs propriétés diffèrent.

**Voir:** `Domain/Article.cs`, `Domain/Author.cs`

L'`Id` est l'identite de l'entite. Les autres proprietes peuvent changer.

```csharp
public class Article {
    public Guid Id { get; protected set; }
    public string Title { get; set; }
}
```

### Value Object

**Définition :** Objet défini par ses valeurs, immutable, sans identité propre.

L'egalite est basee sur les valeurs (pas la reference). Deux `Money` avec les memes valeurs sont identiques.

```csharp
public class Money {
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency) {
        Amount = amount;
        Currency = currency;
    }

    public override bool Equals(object? obj) =>
        obj is Money m && Amount == m.Amount && Currency == m.Currency;
}
```

### Comparaison

| Type | Identité | Mutable | Exemples |
|------|----------|---------|----------|
| **Entity** | Par ID | Oui | Article, Author, Comment |
| **Value Object** | Par valeurs | Non | Money, Address, Email |

---

## Factory Methods

### Problème et Solution

**Voir:** `Domain/Article.cs` méthode `Create`, `Domain/Author.cs` méthode `Create`

**Problème :** Constructeur public permet un état invalide

**Solution :** Constructeur protégé + Factory Method statique

Le constructeur protege est reserve a EF Core. La Factory Method statique garantit un etat valide.

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
```

### Factory interne (entité enfant)

**Voir:** `Domain/Comment.cs` — `internal` empêche la création directe

`internal` limite l'acces au meme projet/assembly.

```csharp
internal static Comment CreateInternal(string content, Guid articleId) { ... }
```

Le modificateur `internal` enforce la frontière de l'aggregate : seul `Article.AddComment()` peut créer un Comment.

### Quand l'utiliser?

**Utiliser si :** Règles de validation, logique d'initialisation, état valide garanti

**Over-engineering si :** Simple DTO, prototypage rapide, aucune règle métier

---

## Domain Services

### Définition

Logique métier qui **n'appartient à aucune entité** spécifique. Utiliser quand l'opération implique plusieurs aggregates.

### Exemple

Logique qui implique Article ET Author — ni l'un ni l'autre ne devrait la contenir.

```csharp
public class ArticleTransferService {
    public void TransferArticle(Article article, Author newAuthor) {
        if (!newAuthor.CanReceiveArticles)
            throw new InvalidOperationException("Auteur ne peut pas recevoir d'articles");

        article.ChangeAuthor(newAuthor.Id);
    }
}
```

### Quand l'utiliser?

**Utiliser si :** Logique impliquant plusieurs aggregates, calculs complexes cross-entités

**Over-engineering si :** La logique appartient clairement à une seule entité (utiliser une méthode d'entité à la place)

---

## Repositories

### Problème et Solution

**Problème :** Couplage direct à EF Core partout dans le code

**Solution :** Abstraction métier avec un vocabulaire du domaine

L'interface est definie dans le Domain Layer, l'implementation dans l'Infrastructure Layer.

```csharp
public interface IArticleRepository {
    Task<Article?> GetByIdAsync(Guid id);
    Task<List<Article>> GetPublishedAsync();
    Task AddAsync(Article article);
}

public class ArticleRepository(BlogDbContext context) : IArticleRepository {
    public async Task<Article?> GetByIdAsync(Guid id) =>
        await context.Articles.Include(a => a.Comments).FirstOrDefaultAsync(a => a.Id == id);
}
```

### Avantages

| Avec Repository | Sans (DbContext direct) |
|-----------------|-------------------------|
| `repo.GetPublished()` | `context.Articles.Where(a => a.Status == 2)` |
| Vocabulaire métier | Vocabulaire technique |
| Testable (mock) | Difficile à mocker |

### Quand l'utiliser?

**Utiliser si :** Plusieurs services consommateurs, tests sans BD, projet en équipe

**Over-engineering si :** CRUD simple, petit projet solo

**Note :** Dans ce projet, le DbContext est utilisé directement — acceptable pour un projet pédagogique.

---

## Ubiquitous Language

### Principe

Le code utilise le **même vocabulaire** que le domaine métier.

| Métier dit | Code doit dire | Pas |
|------------|---------------|-----|
| Publier article | `article.Publish()` | `SetStatus(2)` |
| Ajouter commentaire | `article.AddComment()` | `comment.SetArticleId()` |
| Archiver | `article.Archive()` | `SetArchived(true)` |

**Voir:** `Domain/Article.cs` — méthodes `AddComment()`, `RemoveComment()`, `AddTag()`, `RemoveTag()`

### En pratique

- **Classes :** `Article`, `Author` (pas `Post`, `User`)
- **Méthodes :** `Publish()`, `Archive()` (pas `SetStatus()`)
- **Propriétés :** `PublishedAt` (pas `Timestamp`)
- **Enums :** `ArticleStatus.Published` (pas `Status.Two`)

---

## Patterns avec EF Core

### 1. DbSet uniquement pour les Aggregate Roots

**Voir:** `Data/BlogDbContext.cs`

Pas de `DbSet<Comment>` — les commentaires sont accessibles uniquement via `Article.Comments`.

```csharp
public DbSet<Author> Authors => Set<Author>();
public DbSet<Article> Articles => Set<Article>();
public DbSet<Tag> Tags => Set<Tag>();
```

### 2. Références par ID + Navigation Property

**Voir:** `Domain/Article.cs` — `AuthorId` (FK) + `Author` (navigation)

`AuthorId` est la reference par ID (DDD), `Author` est la navigation property (EF Core).

```csharp
public Guid AuthorId { get; protected set; }
public virtual Author? Author { get; set; }
```

### 3. Protected Setters pour l'immutabilité

**Voir:** `Domain/Article.cs` — `Id`, `CreatedAt`, `AuthorId`

`protected set` empeche la modification externe. Les proprietes mutables gardent un setter public.

```csharp
public Guid Id { get; protected set; }
public DateTime CreatedAt { get; protected set; }
public Guid AuthorId { get; protected set; }
public string Title { get; set; }
```

### 4. Constructeur protégé (EF Core)

EF Core peut instancier via ce constructeur, mais pas le code externe.

```csharp
protected Article() { }
```

### 5. Logique dans l'entité (pas dans un service)

L'entite controle ses propres regles et le cycle de vie de ses enfants.

```csharp
public Comment AddComment(string content) {
    ArgumentException.ThrowIfNullOrWhiteSpace(content);
    var comment = Comment.CreateInternal(content, Id);
    Comments.Add(comment);
    return comment;
}
```

### 6. Collection expression et virtual

**Voir:** `Domain/Article.cs`

`[]` est une collection expression (C# 12) pour initialiser une collection vide. `virtual` est requis pour le lazy loading EF Core.

```csharp
public virtual ICollection<Comment> Comments { get; set; } = [];
```

---

## Quand Utiliser DDD

### Utiliser si

- Logique métier complexe avec des règles à enforcer
- Plusieurs développeurs travaillent sur le même domaine
- Le domaine évolue fréquemment
- Les règles métier doivent être centralisées et testables

### Over-engineering si

- Simple CRUD sans logique métier
- Petit projet solo (<100 lignes de domaine)
- Prototype ou proof-of-concept
- Application technique sans vrai domaine métier

---

## Bonnes Pratiques

### A Faire

- Créer les entités via Factory Methods (état valide garanti)
- Accéder aux enfants via l'Aggregate Root uniquement
- Utiliser `protected set` sur les propriétés immuables (Id, FK, dates)
- Utiliser `internal` pour protéger les méthodes internes à l'aggregate
- Nommer les méthodes avec le vocabulaire métier
- Référencer les autres aggregates par ID

### A Éviter

- Contourner l'aggregate root (`context.Comments.Add(...)`)
- Mettre la logique métier dans les services au lieu des entités
- Créer des aggregates trop grands (tout regrouper)
- Utiliser des setters publics sur les propriétés immuables
- Nommer avec du vocabulaire technique (`SetStatus`, `UpdateFlag`)
- Over-engineering : DDD partout, même pour du simple CRUD

---

## Checklist

### Aggregate Root

- [ ] Possède un `Id`
- [ ] Contrôle ses entités enfants (Add/Remove)
- [ ] Valide les règles métier
- [ ] Références vers autres aggregates par ID
- [ ] A un DbSet dans le DbContext

### Entity

- [ ] Possède un `Id`
- [ ] `protected set` sur propriétés immuables
- [ ] Factory Method avec validation
- [ ] Méthodes métier (`Publish`, `Archive`, `AddComment`)

### Value Object

- [ ] Pas d'Id
- [ ] Propriétés `{ get; }` uniquement (immutable)
- [ ] Constructeur qui initialise tout
- [ ] `Equals()` et `GetHashCode()` basés sur les valeurs

### Entité enfant

- [ ] Factory Method `internal` (pas public)
- [ ] Constructeur `protected`
- [ ] Créé uniquement via l'Aggregate Root
- [ ] Pas de DbSet dédié

---

## Ressources

- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing DDD - Vaughn Vernon](https://vaughnvernon.com/)
- [DDD with EF Core](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
