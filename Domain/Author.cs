namespace BlogDemo.Domain;

// [DDD] Aggregate Root - Un auteur peut publier plusieurs articles
public class Author {
    // === IDENTITÉ ===
    // [Bonne Pratique] protected set empêche la modification de l'Id
    public Guid Id { get; protected set; }

    // === PROPRIÉTÉS MÉTIER ===
    public required string Name { get; set; }

    // === NAVIGATION PROPERTIES ===
    // [EF Core] virtual requis pour le lazy loading
    public virtual ICollection<Article> Articles { get; set; } = [];

    // === CONSTRUCTEUR ===
    // [EF Core] Constructeur sans paramètre requis
    protected Author() { }

    // === FACTORY METHOD ===
    // [Bonne Pratique] Garantit un état valide
    public static Author Create(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Author {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}
