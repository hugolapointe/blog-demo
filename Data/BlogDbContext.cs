using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options) {
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
}
