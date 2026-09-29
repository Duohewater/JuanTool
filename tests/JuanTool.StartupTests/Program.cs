using System.IO;
using JuanTool;
using Microsoft.Win32;

var testRoot = @"Software\JuanTool\StartupTests\" + Guid.NewGuid().ToString("N");
var runPath = testRoot + @"\Run";
var approvedPath = testRoot + @"\StartupApproved\Run";
var registration = new StartupRegistration(runPath, approvedPath, "JuanToolTest", @"C:\Program Files\JuanTool\JuanTool.exe");
var passed = 0;

void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

try
{
    Check(!registration.IsEnabled(), "No startup entry is unchecked");

    registration.SetEnabled(true);
    using (var run = Registry.CurrentUser.OpenSubKey(runPath))
        Check(registration.IsEnabled() && run?.GetValue("JuanToolTest") as string == "\"C:\\Program Files\\JuanTool\\JuanTool.exe\" --background",
            "Enabling creates the Windows Run command");

    var disabled = new byte[] { 3, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 };
    using (var approved = Registry.CurrentUser.CreateSubKey(approvedPath))
        approved.SetValue("JuanToolTest", disabled, RegistryValueKind.Binary);
    Check(!registration.IsEnabled(), "Windows-disabled startup is unchecked");

    disabled[0] = 7;
    using (var approved = Registry.CurrentUser.CreateSubKey(approvedPath))
        approved.SetValue("JuanToolTest", disabled, RegistryValueKind.Binary);
    Check(!registration.IsEnabled(), "Alternate Windows-disabled state is unchecked");

    var before = registration.Capture();
    registration.SetEnabled(true);
    using (var approved = Registry.CurrentUser.OpenSubKey(approvedPath))
        Check(registration.IsEnabled() && approved?.GetValue("JuanToolTest") is null,
            "Explicitly enabling clears Windows-disabled state");

    registration.Restore(before);
    using (var approved = Registry.CurrentUser.OpenSubKey(approvedPath))
        Check(!registration.IsEnabled() && (approved?.GetValue("JuanToolTest") as byte[])?.SequenceEqual(disabled) == true,
            "Rollback restores the original startup state");

    registration.SetEnabled(false);
    using (var run = Registry.CurrentUser.OpenSubKey(runPath))
        Check(!registration.IsEnabled() && run?.GetValue("JuanToolTest") is null,
            "Disabling removes the Windows Run command");

    try
    {
        new StartupRegistration(runPath, approvedPath, "JuanToolTest", @"C:\Program Files\dotnet\dotnet.exe").SetEnabled(true);
        throw new Exception("FAIL: Non-JuanTool executable accepted");
    }
    catch (IOException) { Check(true, "A runtime host cannot be registered as JuanTool"); }
}
finally
{
    Registry.CurrentUser.DeleteSubKeyTree(testRoot, throwOnMissingSubKey: false);
}

Console.WriteLine($"All {passed} startup tests passed.");
