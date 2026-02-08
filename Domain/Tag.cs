namespace BlogDemo.Domain;

// [DDD] Aggregate Root - Les tags sont partagés entre plusieurs articles
public class Tag {
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
    protected Tag() { }

    // === FACTORY METHOD ===
    // [Bonne Pratique] Garantit un état valide
    public static Tag Create(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Tag {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}
