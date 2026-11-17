namespace RelationsTab.Tests;

static class ModRoot
{
    public static readonly string Path = Find();

    static string Find() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(System.IO.Path.Combine(dir.FullName, "rimhunter-runner.json")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("rimhunter-runner.json not found above the test binary");
    }
}
