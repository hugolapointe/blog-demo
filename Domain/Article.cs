namespace BlogDemo.Domain;

public class Article {
    // Clé primaire avec protected set pour empêcher modification externe
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // required (C# 11) force l'initialisation lors de la création
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    // Clé étrangère avec protected set (modifiée uniquement via factory)
    public Guid AuthorId { get; protected set; }

    // Navigation properties virtual pour lazy loading
    // ? indique que Author peut être null (avant chargement)
    public virtual Author? Author { get; set; }
    public virtual ICollection<Comment> Comments { get; set; } = [];
    public virtual ICollection<Tag> Tags { get; set; } = [];

    // Constructeur protected requis par EF Core
    protected Article() { }

    // Méthode factory garantit que AuthorId est défini à la création
    public static Article Create(string title, string content, Guid authorId) {
        return new Article {
            Title = title,
            Content = content,
            AuthorId = authorId
        };
    }
}
