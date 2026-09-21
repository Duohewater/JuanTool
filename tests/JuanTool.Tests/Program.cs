using System.Text.Json;
using JuanTool.Core;

int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }

const string fixture = """
{"roots":{"bookmark_bar":{"type":"folder","name":"Bookmarks bar","children":[
 {"type":"url","name":"GitHub","url":"https://github.com/"},
 {"type":"folder","name":"Work","children":[
  {"type":"url","name":"Build notes","url":"https://example.com/notes?q=a&lang=en"},
  {"type":"url","name":"你好","url":"https://example.org/中文"},
  {"type":"url","name":"Unsafe","url":"javascript:alert(1)"},
  {"type":"url","name":"File","url":"file:///C:/secret.txt"},
  {"type":"url","name":"Invalid","url":"not a URL"}]}]},
 "other":{"type":"folder","name":"Other bookmarks","children":[
 {"type":"url","name":"GitHub docs","url":"https://docs.github.com/"}]}}}
""";
using var document = JsonDocument.Parse(fixture);
var items = Bookmarks.Parse(document.RootElement, "Default");
Check(items.Count == 4, "Nested bookmarks loaded; unsafe URLs excluded");
Check(items.Single(b => b.Title == "Build notes").Folder == "Bookmarks bar / Work", "Folder ancestry preserved");
Check(Bookmarks.Search(items, "GITHUB")[0].Title == "GitHub", "Case-insensitive exact title ranked first");
Check(Bookmarks.Search(items, "work notes").Single().Title == "Build notes", "Multiple words match title and folder");
Check(Bookmarks.Search(items, "example.org").Single().Title == "你好", "Host names searchable");
Check(Bookmarks.Search(items, "你好").Count == 1, "Unicode search");
Check(Bookmarks.Search(items, "missing").Count == 0, "No-match query");
Check(Bookmarks.Search(items, "   ", 2).Count == 2, "Empty query respects result limit");
Check(Bookmarks.Search(items, "default").Count == 4, "Profile searchable");
var query = "C++ & C# / 你好?\nnext line";
Check(Uri.UnescapeDataString(SearchLinks.Google(query).Split("?q=")[1]) == query, "Google URL preserves symbols, newlines and Unicode");
Check(Uri.UnescapeDataString(SearchLinks.ChatGpt(query).Split("?q=")[1]) == query, "ChatGPT URL preserves pasted text");
Check(!SearchLinks.IsWebUrl("data:text/html,test") && !SearchLinks.IsWebUrl("chrome://settings") && SearchLinks.IsWebUrl("https://example.com"), "Only web navigation accepted");

var root = Path.Combine(Path.GetTempPath(), "JuanTool-tests-" + Guid.NewGuid());
try
{
    Directory.CreateDirectory(Path.Combine(root, "Default"));
    Directory.CreateDirectory(Path.Combine(root, "Profile 1"));
    Directory.CreateDirectory(Path.Combine(root, "Profile 2"));
    File.WriteAllText(Path.Combine(root, "Default", "Bookmarks"), fixture);
    File.WriteAllText(Path.Combine(root, "Profile 1", "Bookmarks"), fixture);
    File.WriteAllText(Path.Combine(root, "Profile 2", "Bookmarks"), "{unfinished");
    var loaded = Bookmarks.Load(root);
    Check(loaded.Items.Count == 8 && loaded.Profiles == 2 && loaded.FailedProfiles == 1, "Broken profile does not block other profiles");
    Check(loaded.Items.Select(b => b.Profile).Distinct().Count() == 2, "Same URL in different profiles retained");
    File.WriteAllText(Path.Combine(root, "Default", "Bookmarks"), fixture.Replace("GitHub docs", "Changed title"));
    Check(Bookmarks.Load(root).Items.Any(b => b.Title == "Changed title"), "Reload sees changed bookmark files");
    Check(Bookmarks.Load(Path.Combine(root, "missing")).Items.Count == 0, "Missing Chrome installation handled");
    File.WriteAllText(Path.Combine(root, "Profile 2", "Bookmarks"), "{}");
    Check(Bookmarks.Load(root).FailedProfiles == 1, "Malformed schema handled");
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"All {passed} tests passed.");
