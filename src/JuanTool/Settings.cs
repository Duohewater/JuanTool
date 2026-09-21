using System.Text.Json;

namespace JuanTool;

public sealed record Settings
{
    public uint Modifiers { get; init; } = 1;
    public uint Key { get; init; } = 0x20;
    public string BookmarkRoot { get; init; } = JuanTool.Core.Bookmarks.DefaultRoot;
    public bool StartWithWindows { get; init; }
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JuanTool", "settings.json");
    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, FilePath, true);
    }
    public string ShortcutLabel => Hotkey.Label(Modifiers, Key);
}
