namespace JuanTool.Core;

public static class SearchLinks
{
    public static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);

    public static string? DirectUrl(string input)
    {
        var text = input.Trim();
        if (text.Length == 0 || text.Any(char.IsWhiteSpace)) return null;
        if (IsWebUrl(text)) return new Uri(text).AbsoluteUri;

        // Accept pasted domains without a scheme, but never reinterpret another scheme as a web link.
        if (text.Contains("://") || text.Contains('@') || text.Contains('\\')) return null;
        if (!Uri.TryCreate("https://" + text, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.HostNameType == UriHostNameType.Unknown
            || (uri.HostNameType == UriHostNameType.Dns
                && !uri.Host.Contains('.')
                && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))) return null;

        return uri.AbsoluteUri;
    }

    public static string Google(string query) => "https://www.google.com/search?q=" + Uri.EscapeDataString(query.Trim());
    // A browser handoff, not an API contract. The clipboard fallback also works if this parameter changes.
    public static string ChatGpt(string query) => "https://chatgpt.com/?q=" + Uri.EscapeDataString(query.Trim());
}
