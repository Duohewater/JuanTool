namespace JuanTool.Core;

public static class SearchLinks
{
    public static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);
    public static string Google(string query) => "https://www.google.com/search?q=" + Uri.EscapeDataString(query.Trim());
    // A browser handoff, not an API contract. The clipboard fallback also works if this parameter changes.
    public static string ChatGpt(string query) => "https://chatgpt.com/?q=" + Uri.EscapeDataString(query.Trim());
}
