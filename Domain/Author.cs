namespace BlogDemo.Domain;

public class Author {
    // Clé primaire avec protected set pour empêcher modification externe
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // required (C# 11) force l'initialisation de cette propriété
    public required string Name { get; set; }

    // virtual requis pour lazy loading proxies d'EF Core
    // [] (C# 12) initialise une collection vide (équivalent à new List<Article>())
    public virtual ICollection<Article> Articles { get; set; } = [];

    // Constructeur protected requis par EF Core pour créer les entités
    protected Author() { }

    // Méthode factory pour créer des entités dans un état valide
    public static Author Create(string name) {
        return new Author {
            Name = name
        };
    }
}
