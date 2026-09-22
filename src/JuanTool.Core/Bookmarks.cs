using System.Text.Json;

namespace JuanTool.Core;

public sealed record Bookmark(string Title, string Url, string Folder, string Profile)
{
    public string Detail => $"{new Uri(Url).Host}  ·  {Folder}  ·  {Profile}";
    public string Initial => string.IsNullOrEmpty(Title) ? "↗" : Title[..1].ToUpperInvariant();
}

public sealed record BookmarkLoadResult(IReadOnlyList<Bookmark> Items, int Profiles, int FailedProfiles);

public static class Bookmarks
{
    public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");

    public static BookmarkLoadResult Load(string root)
    {
        var items = new List<Bookmark>();
        int profiles = 0, failed = 0;
        if (!Directory.Exists(root)) return new(items, 0, 0);
        string[] directories;
        try { directories = Directory.GetDirectories(root); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return new(items, 0, 1); }
        foreach (var directory in directories.Order(StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(directory, "Bookmarks");
            if (!File.Exists(path)) continue;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var json = JsonDocument.Parse(stream);
                var parsed = Parse(json.RootElement, Path.GetFileName(directory));
                items.AddRange(parsed);
                profiles++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { failed++; }
        }
        return new(items.DistinctBy(b => (b.Url, b.Profile)).ToArray(), profiles, failed);
    }

    public static IReadOnlyList<Bookmark> Parse(JsonElement document, string profile)
    {
        var items = new List<Bookmark>();
        if (document.ValueKind != JsonValueKind.Object || !document.TryGetProperty("roots", out var roots) || roots.ValueKind != JsonValueKind.Object)
            throw new JsonException("Missing bookmark roots.");
        foreach (var root in roots.EnumerateObject()) Walk(root.Value, "", profile, items);
        return items;
    }

    private static string Value(JsonElement node, string key) => node.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static void Walk(JsonElement node, string folder, string profile, List<Bookmark> items)
    {
        if (node.ValueKind != JsonValueKind.Object) return;
        var name = Value(node, "name");
        var url = Value(node, "url");
        if (Value(node, "type") == "url" && SearchLinks.IsWebUrl(url))
            items.Add(new(string.IsNullOrWhiteSpace(name) ? url : name, url, folder, profile));
        if (!node.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array) return;
        var next = string.IsNullOrEmpty(folder) ? name : $"{folder} / {name}";
        foreach (var child in children.EnumerateArray()) Walk(child, next, profile, items);
    }

    public static IReadOnlyList<Bookmark> Search(IEnumerable<Bookmark> items, string query)
    {
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return items.Select(b => (Item: b, Score: Score(b, terms, query.Trim())))
            .Where(x => x.Score >= 0).OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase).Select(x => x.Item).ToArray();
    }

    private static int Score(Bookmark b, string[] terms, string query)
    {
        var haystack = $"{b.Title} {b.Url} {b.Folder} {b.Profile}";
        if (terms.Any(t => !haystack.Contains(t, StringComparison.OrdinalIgnoreCase))) return -1;
        if (terms.Length == 0) return 0;
        return (b.Title.Equals(query, StringComparison.OrdinalIgnoreCase) ? 100 : 0)
            + (b.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 40 : 0)
            + terms.Count(t => b.Title.Contains(t, StringComparison.OrdinalIgnoreCase)) * 10;
    }
}
