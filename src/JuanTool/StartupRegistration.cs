using Microsoft.Win32;

namespace JuanTool;

public sealed class StartupRegistration
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static StartupRegistration Current { get; } = new(RunPath, ApprovedPath, "JuanTool", Environment.ProcessPath);

    private readonly string runPath;
    private readonly string approvedPath;
    private readonly string valueName;
    private readonly string? executablePath;

    public StartupRegistration(string runPath, string approvedPath, string valueName, string? executablePath)
    {
        this.runPath = runPath;
        this.approvedPath = approvedPath;
        this.valueName = valueName;
        this.executablePath = executablePath;
    }

    public bool IsEnabled()
    {
        using var run = Registry.CurrentUser.OpenSubKey(runPath);
        if (run?.GetValue(valueName) is not string command || string.IsNullOrWhiteSpace(command)) return false;

        using var approved = Registry.CurrentUser.OpenSubKey(approvedPath);
        return approved?.GetValue(valueName) is not byte[] state || state.Length == 0 || state[0] is not (3 or 7);
    }

    public State Capture()
    {
        using var run = Registry.CurrentUser.OpenSubKey(runPath);
        using var approved = Registry.CurrentUser.OpenSubKey(approvedPath);
        return new State(run?.GetValue(valueName) as string, approved?.GetValue(valueName) as byte[]);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!string.Equals(Path.GetFileName(executablePath), "JuanTool.exe", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Start JuanTool from its executable before enabling Windows startup.");
            var command = $"\"{executablePath}\" --background";
            if (command.Length > 260) throw new IOException("The JuanTool executable path is too long for Windows startup.");

            using (var run = Registry.CurrentUser.CreateSubKey(runPath))
                run.SetValue(valueName, command, RegistryValueKind.String);

            // Windows can retain a disabled state even while the Run entry still exists.
            using var approved = Registry.CurrentUser.OpenSubKey(approvedPath, writable: true);
            approved?.DeleteValue(valueName, throwOnMissingValue: false);
        }
        else
        {
            using var run = Registry.CurrentUser.OpenSubKey(runPath, writable: true);
            run?.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    public void Restore(State state)
    {
        using (var run = Registry.CurrentUser.OpenSubKey(runPath, writable: true))
        {
            if (state.Command is null) run?.DeleteValue(valueName, throwOnMissingValue: false);
            else if (run is not null) run.SetValue(valueName, state.Command, RegistryValueKind.String);
            else
            {
                using var restoredRun = Registry.CurrentUser.CreateSubKey(runPath);
                restoredRun.SetValue(valueName, state.Command, RegistryValueKind.String);
            }
        }

        using (var approved = Registry.CurrentUser.OpenSubKey(approvedPath, writable: true))
        {
            if (state.Approval is null) approved?.DeleteValue(valueName, throwOnMissingValue: false);
            else if (approved is not null) approved.SetValue(valueName, state.Approval, RegistryValueKind.Binary);
            else
            {
                using var restoredApproval = Registry.CurrentUser.CreateSubKey(approvedPath);
                restoredApproval.SetValue(valueName, state.Approval, RegistryValueKind.Binary);
            }
        }
    }

    public sealed record State(string? Command, byte[]? Approval);
}
