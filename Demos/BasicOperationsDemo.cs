using BlogDemo.Data;
using BlogDemo.Domain;

using Microsoft.EntityFrameworkCore;

namespace BlogDemo.Demos;

public class BasicOperationsDemo(BlogDbContext context) : DemoBase(context) {
    protected override async Task ExecuteAsync() {
        WriteTitle("=== BASIC OPERATIONS (CRUD) ===");

        await AddEntityAsync();
        await UpdateEntityAsync();
        await DeleteEntityAsync();
        await EntityStatesAsync();
    }

    async Task AddEntityAsync() {
        WriteTitle("--- Add (Ajouter) ---");

        // Créer une nouvelle entité
        var newAuthor = Author.Create("Charlie Leclerc");

        // Add() marque l'entité comme Added
        Context.Authors.Add(newAuthor);

        // SaveChanges() génère INSERT et change l'état à Unchanged
        await Context.SaveChangesAsync();

        Console.WriteLine($"Auteur ajouté: {newAuthor.Name} (Id: {newAuthor.Id})");
    }

    async Task UpdateEntityAsync() {
        WriteTitle("--- Update (Modifier) ---");

        // Charger une entité existante (état: Unchanged)
        var author = await Context.Authors.FirstAsync(a => a.Name == "Charlie Leclerc");

        // Modification détectée automatiquement → état: Modified
        author.Name = "Charles Leclerc";

        // SaveChanges() génère UPDATE
        await Context.SaveChangesAsync();

        Console.WriteLine($"Auteur modifié: {author.Name}");
    }

    async Task DeleteEntityAsync() {
        WriteTitle("--- Delete (Supprimer) ---");

        // Charger l'entité à supprimer
        var author = await Context.Authors.FirstAsync(a => a.Name == "Charles Leclerc");

        // Remove() marque l'entité comme Deleted
        Context.Authors.Remove(author);

        // SaveChanges() génère DELETE
        await Context.SaveChangesAsync();

        Console.WriteLine($"Auteur supprimé: {author.Name}");
    }

    async Task EntityStatesAsync() {
        WriteTitle("--- États d'entités ---");

        // Créer une nouvelle entité (pas trackée)
        var author = Author.Create("David Martin");
        Console.WriteLine($"Avant Add: {Context.Entry(author).State}");  // Detached

        // Add() → Added
        Context.Authors.Add(author);
        Console.WriteLine($"Après Add: {Context.Entry(author).State}");  // Added

        // SaveChanges() → Unchanged
        await Context.SaveChangesAsync();
        Console.WriteLine($"Après Save: {Context.Entry(author).State}"); // Unchanged

        // Modifier → Modified
        author.Name = "David Dupont";
        Console.WriteLine($"Après modification: {Context.Entry(author).State}"); // Modified

        // États: Detached, Unchanged, Added, Modified, Deleted
    }
}
