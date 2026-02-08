using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options) {
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();

    // [DDD] Les commentaires sont accessibles via l'aggregate root Article (Article.Comments)
    // Pas besoin de DbSet<Comment> au niveau du contexte

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ConfigureAuthor(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureComment(modelBuilder);
        ConfigureTag(modelBuilder);
    }

    private void ConfigureArticle(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Article>(entity => {
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.AuthorId, e.CreatedAt });
        });
    }

    private void ConfigureComment(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Comment>(entity => {
            entity.HasIndex(e => e.ArticleId);
        });
    }

    private void ConfigureAuthor(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Author>(entity => {
            entity.HasIndex(e => e.Name);

            entity.HasMany(e => e.Articles)
                .WithOne(e => e.Author)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTag(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Tag>(entity => {
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
