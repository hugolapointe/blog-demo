using BlogDemo.Data;

namespace BlogDemo.Demos;

public abstract class DemoBase(BlogDbContext context) {
    protected BlogDbContext Context { get; } = context;

    public async Task ResetAndExecuteAll() {
        // Nettoyer le change tracker avant chaque démo
        Context.ChangeTracker.Clear();
        await ExecuteAll();
    }

    protected abstract Task ExecuteAll();
}
