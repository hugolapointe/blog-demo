namespace BlogDemo.Domain;

// [DDD] Entité enfant de l'aggregate Article (ne peut exister indépendamment)
public class Comment {
    // === IDENTITÉ ===
    // [Bonne Pratique] protected set empêche la modification de l'Id
    public Guid Id { get; protected set; }

    // === PROPRIÉTÉS MÉTIER ===
    public required string Content { get; set; }

    // === CLÉS ÉTRANGÈRES ===
    // [Bonne Pratique] protected set garantit l'immutabilité
    public Guid ArticleId { get; protected set; }

    // === NAVIGATION PROPERTIES ===
    // [EF Core] virtual requis pour le lazy loading
    public virtual Article? Article { get; set; }

    // === CONSTRUCTEUR ===
    // [EF Core] Constructeur sans paramètre requis
    protected Comment() { }

    // === FACTORY METHOD (INTERNE) ===
    // [DDD] Utilisé uniquement par l'aggregate root Article
    internal static Comment CreateInternal(string content, Guid articleId) {
        ArgumentOutOfRangeException.ThrowIfEqual(articleId, Guid.Empty);

        return new Comment {
            Id = Guid.NewGuid(),
            Content = content,
            ArticleId = articleId
        };
    }
}
