using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options) {
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();

    // [DDD] Les commentaires sont accessibles via l'aggregate root Article (Article.Comments)
    // Pas besoin de DbSet<Comment> au niveau du contexte

    // [FLUENT API] OnModelCreating configure le mapping entités ↔ tables
    // Organisé en méthodes privées par entité pour la lisibilité
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ConfigureAuthor(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureComment(modelBuilder);
        ConfigureTag(modelBuilder);
    }

    private static void ConfigureArticle(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Article>(entity => {
            // [INDEX] Accélère les recherches fréquentes par titre
            entity.HasIndex(e => e.Title);

            // [INDEX] Accélère le tri par date de création
            entity.HasIndex(e => e.CreatedAt);

            // [INDEX COMPOSITE] Optimise les requêtes filtrant par auteur ET triant par date
            // Ex: "tous les articles de l'auteur X, triés par date"
            entity.HasIndex(e => new { e.AuthorId, e.CreatedAt });
        });
    }

    private static void ConfigureComment(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Comment>(entity => {
            // [INDEX] Accélère la recherche des commentaires par article (relation 1-N)
            entity.HasIndex(e => e.ArticleId);
        });
    }

    private static void ConfigureAuthor(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Author>(entity => {
            // [INDEX] Accélère la recherche d'auteurs par nom
            entity.HasIndex(e => e.Name);

            // [RELATION 1-N] Un Author a plusieurs Articles
            // Restrict : empêche la suppression d'un auteur qui a encore des articles
            entity.HasMany(e => e.Articles)
                .WithOne(e => e.Author)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTag(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Tag>(entity => {
            // [INDEX UNIQUE] Empêche les doublons de noms de tags au niveau de la BD
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
