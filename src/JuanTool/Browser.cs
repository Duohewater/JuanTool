using System.Diagnostics;
using Microsoft.Win32;
using JuanTool.Core;

namespace JuanTool;

public static class Browser
{
    public static void Open(string url, string? profile = null, string? userDataRoot = null)
    {
        if (!SearchLinks.IsWebUrl(url)) throw new InvalidOperationException("Only http and https links can be opened.");
        var chrome = FindChrome();
        if (chrome is null) { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); return; }
        var start = new ProcessStartInfo(chrome) { UseShellExecute = false };
        if (profile is not null)
        {
            if (userDataRoot is not null && !string.Equals(Path.GetFullPath(userDataRoot).TrimEnd('\\'), Path.GetFullPath(Bookmarks.DefaultRoot).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add("--user-data-dir=" + userDataRoot);
            start.ArgumentList.Add("--profile-directory=" + profile);
        }
        start.ArgumentList.Add(url);
        Process.Start(start);
    }
    private static string? FindChrome()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
            if (key?.GetValue(null) is string path && File.Exists(path.Trim('"'))) return path.Trim('"');
        }
        return new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) }
            .Select(root => Path.Combine(root, "Google", "Chrome", "Application", "chrome.exe")).FirstOrDefault(File.Exists);
    }
}
