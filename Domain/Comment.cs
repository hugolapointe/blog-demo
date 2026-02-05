namespace BlogDemo.Domain;

public class Comment {
    // Clé primaire avec protected set pour empêcher modification externe
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // required (C# 11) force l'initialisation
    public required string Content { get; set; }

    // Clé étrangère avec protected set (modifiée uniquement via factory)
    public Guid ArticleId { get; protected set; }

    // Navigation property virtual pour lazy loading
    public virtual Article? Article { get; set; }

    // Constructeur protected requis par EF Core
    protected Comment() { }

    // Méthode factory garantit que ArticleId est défini à la création
    public static Comment Create(string content, Guid articleId) {
        return new Comment {
            Content = content,
            ArticleId = articleId
        };
    }
}
