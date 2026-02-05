using BlogDemo.Data;

namespace BlogDemo.Demos;

public abstract class DemoBase(BlogDbContext context) {
    protected BlogDbContext Context { get; } = context;

    public async Task RunAsync() {
        // Nettoyer le change tracker avant chaque démo
        Context.ChangeTracker.Clear();
        await ExecuteAsync();
    }

    protected abstract Task ExecuteAsync();

    protected static void WriteTitle(string title) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{title}");
        Console.ResetColor();
    }
}
