# BlogDemo - Projet de Démonstration

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0.2-512BD4)
![Status](https://img.shields.io/badge/Status-Demo-success)

> Projet pédagogique couvrant les concepts fondamentaux d'EF Core à travers des démonstrations pratiques et progressives.

## Table des matières

- [Aperçu](#aperçu)
- [Modèle de Domaine](#modèle-de-domaine)
- [Parcours pédagogique](#parcours-pédagogique)
- [Démonstrations](#démonstrations)
- [Cheatsheets](#cheatsheets)

---

## Aperçu

Ce projet démontre les concepts essentiels d'Entity Framework Core à travers un domaine simple de blog (Authors, Articles, Comments, Tags). Chaque démonstration illustre un concept spécifique avec son problème et sa solution.

**Objectifs pédagogiques :**
- Comprendre les différentes stratégies de chargement de données
- Identifier et résoudre les problèmes de performance courants
- Maîtriser les opérations de requêtage et d'agrégation
- Appliquer les bonnes pratiques EF Core
- Découvrir les patterns DDD appliqués avec EF Core

---

## Modèle de Domaine

Ce projet utilise un modèle de domaine simple mais riche en relations pour illustrer les concepts.

| Entité | Description | Relations |
|--------|-------------|-----------|
| **Author** | L'auteur des articles. Aggregate Root. | 1-N avec **Article** |
| **Article** | Le contenu principal du blog. Aggregate Root. | N-1 avec **Author**, 1-N avec **Comment**, N-N avec **Tag** |
| **Comment** | Les commentaires des lecteurs. Entité enfant. | N-1 avec **Article** |
| **Tag** | Les étiquettes pour catégoriser les articles. Indépendant. | N-N avec **Article** |

---
## Parcours pédagogique

### Progression recommandée

Le projet est organisé pour un apprentissage progressif :

```
1. Basic Operations       → Fondations (CRUD)
2. Loading Strategies     → Chargement des données
3. Performance Optimization → Optimisations essentielles
4. Querying              → Requêtes courantes
5. Aggregation           → Opérations avancées
6. Delete Behavior       → Comportements de suppression
```

**Conseil :** Suivez l'ordre des démonstrations, car chaque section s'appuie sur les concepts précédents.

---

## Démonstrations

### 1. Basic Operations - Fondations du CRUD

**Fichier :** `Demos/BasicOperationsDemo.cs`

**Concepts démontrés :**
- **Add** - Insérer de nouvelles entités (état Added → Unchanged)
- **Update** - Modifier des entités existantes (détection automatique)
- **Delete** - Supprimer des entités (état Deleted)
- **Entity States** - Cycle de vie (Detached, Unchanged, Added, Modified, Deleted)

---

### 2. Loading Strategies - Chargement des données

**Fichier :** `Demos/LoadingStrategiesDemo.cs`

**Concepts démontrés :**
- **Lazy Loading** - Chargement automatique à l'accès
- **N+1 Problem** - Le piège classique (1 + N requêtes)
- **Eager Loading** - `Include()` charge tout en une requête
- **Explicit Loading** - Chargement manuel avec `Entry().Reference/Collection().LoadAsync()`

---

### 3. Performance Optimization - Optimisations essentielles

**Fichier :** `Demos/OptimizationDemo.cs`

**Concepts démontrés :**
- **Tracking vs NoTracking** - `AsNoTracking()` pour lectures seules
- **Projection** - `Select()` charge uniquement les colonnes nécessaires
- **Cartesian Product** - Problème des JOINs multiples sur collections
- **Split Queries** - `AsSplitQuery()` évite le produit cartésien

---

### 4. Querying - Requêtes courantes

**Fichier :** `Demos/QueryingDemo.cs`

**Concepts démontrés :**
- **Filtering** - `Where()` (SQL WHERE)
- **Sorting** - `OrderBy()` et `ThenBy()`
- **Pagination** - `Skip()` et `Take()` (OrderBy obligatoire)
- **First vs Single** - Récupération d'éléments
- **Any vs Count** - Vérifier l'existence efficacement
- **Distinct** - Éliminer les doublons
- **Contains** - Clause SQL IN
- **AsQueryable** - Construction dynamique de requêtes

---

### 5. Aggregation - Opérations avancées

**Fichier :** `Demos/AggregationDemo.cs`

**Concepts démontrés :**
- **Basic Aggregations** - Count, Sum, Average, Max, Min
- **GroupBy** - Regrouper par critère et agréger
- **Having** - Filtrer les groupes après agrégation

---

---

## Cheatsheets

Trois références rapides pour consulter les concepts en parallèle au code :

| Cheatsheet | Contenu |
|------------|---------|
| [EF Core](EF%20Core%20-%20Cheatsheet.md) | Architecture, fournisseurs, approches, configuration, CRUD, chargement, optimisations, Fluent API, migrations |
| [LINQ](LINQ%20-%20Cheatsheet.md) | Filtrage, projection, tri, pagination, agrégation, regroupement, exécution différée, patterns courants |
| [DDD](DDD%20-%20Cheatsheet.md) | Aggregates, entities vs value objects, factory methods, repositories, ubiquitous language, patterns EF Core |

---

## Ressources

- [Documentation officielle EF Core](https://learn.microsoft.com/ef/core/)
- [Performance Best Practices](https://learn.microsoft.com/ef/core/performance/)
- [Querying Data](https://learn.microsoft.com/ef/core/querying/)

---

**Version :** 2.0
**Dernière mise à jour :** Février 2026
