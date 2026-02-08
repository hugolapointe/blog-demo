namespace BlogDemo.Domain;

// [DDD] Aggregate Root - Gère le cycle de vie de ses entités enfants (Comment)
public class Article {
    // === IDENTITÉ ===
    // [Bonne Pratique] protected set empêche la modification de l'Id
    public Guid Id { get; protected set; }

    // === PROPRIÉTÉS MÉTIER ===
    public required string Title { get; set; }
    public required string Content { get; set; }

    // [Bonne Pratique] protected set empêche la modification de la date
    public DateTime CreatedAt { get; protected set; }

    // === CLÉS ÉTRANGÈRES ===
    // [DDD] Référence l'aggregate Author par ID (pas par objet)
    // [Bonne Pratique] protected set garantit l'immutabilité
    public Guid AuthorId { get; protected set; }

    // === NAVIGATION PROPERTIES ===
    // [EF Core] virtual requis pour le lazy loading
    public virtual Author? Author { get; set; }

    // [DDD] Collection d'entités enfants gérée par l'aggregate root
    // [EF Core] virtual requis pour le lazy loading
    public virtual ICollection<Comment> Comments { get; set; } = [];

    // [DDD] Relation N-N avec l'aggregate Tag (indépendants)
    // [EF Core] virtual requis pour le lazy loading
    public virtual ICollection<Tag> Tags { get; set; } = [];

    // === CONSTRUCTEUR ===
    // [EF Core] Constructeur sans paramètre requis
    protected Article() { }

    // === FACTORY METHOD ===
    // [Bonne Pratique] Garantit un état valide
    public static Article Create(string title, string content, Guid authorId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentOutOfRangeException.ThrowIfEqual(authorId, Guid.Empty);

        return new Article {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            AuthorId = authorId,
            CreatedAt = DateTime.Now
        };
    }

    // === MÉTHODES DE DOMAINE ===
    // [DDD] L'aggregate root contrôle la création de ses entités enfants

    public Comment AddComment(string content) {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var comment = Comment.CreateInternal(content, Id);
        Comments.Add(comment);

        return comment;
    }

    public void RemoveComment(Comment comment) {
        ArgumentNullException.ThrowIfNull(comment);
        Comments.Remove(comment);
    }

    // [DDD] Associe/dissocie des tags (aggregates séparés)

    public void AddTag(Tag tag) {
        ArgumentNullException.ThrowIfNull(tag);

        if (!Tags.Contains(tag))
            Tags.Add(tag);
    }

    public void RemoveTag(Tag tag) {
        ArgumentNullException.ThrowIfNull(tag);
        Tags.Remove(tag);
    }
}
