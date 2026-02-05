namespace BlogDemo.Domain;

public class Tag {
    // Clé primaire avec protected set pour empêcher modification externe
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // required (C# 11) force l'initialisation
    public required string Name { get; set; }

    // virtual requis pour lazy loading proxies d'EF Core
    // [] (C# 12) initialise une collection vide
    public virtual ICollection<Article> Articles { get; set; } = [];

    // Constructeur protected requis par EF Core
    protected Tag() { }

    // Méthode factory pour créer des entités dans un état valide
    public static Tag Create(string name) {
        return new Tag {
            Name = name
        };
    }
}
