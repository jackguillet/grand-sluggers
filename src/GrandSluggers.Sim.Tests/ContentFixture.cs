using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A throwaway copy of the shipped data root that a test may break on purpose, so a validator's
/// refusal is proved against the real catalog rather than a hand-built stub that has drifted from it.
/// Lifted out of <see cref="ContentValidationTests"/> by #807 so more than one suite can name it.
/// </summary>
sealed class ContentFixture : IDisposable
{
    static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public ContentFixture()
    {
        Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grand-sluggers-content-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(ContentCatalog.Load().Root.Shipped, Root);
    }

    public string Root { get; }

    public string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public string Copy(string from, string to)
    {
        var destination = Path(to);
        File.Copy(Path(from), destination);
        return destination;
    }

    public void ChangeObject(string relative, Action<JsonObject> change)
    {
        var path = Path(relative);
        var json = JsonNode.Parse(File.ReadAllText(path), documentOptions: DataJson.Document)!.AsObject();
        change(json);
        File.WriteAllText(path, json.ToJsonString(WriteOptions));
    }

    public void ChangeArray(string relative, Action<JsonArray> change)
    {
        var path = Path(relative);
        var json = JsonNode.Parse(File.ReadAllText(path), documentOptions: DataJson.Document)!.AsArray();
        change(json);
        File.WriteAllText(path, json.ToJsonString(WriteOptions));
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(System.IO.Path.Combine(destination, System.IO.Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, System.IO.Path.Combine(destination, System.IO.Path.GetRelativePath(source, file)));
    }
}
