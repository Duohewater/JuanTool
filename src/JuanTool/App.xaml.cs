using System.Threading;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace JuanTool;

public partial class App : Application
{
    private Mutex? singleton;
    private EventWaitHandle? wake;
    private RegisteredWaitHandle? wakeRegistration;
    private Forms.NotifyIcon? tray;
    private Hotkey? hotkey;
    private MainWindow? launcher;
    public Settings Settings { get; private set; } = Settings.Load();
    public bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        singleton = new Mutex(true, @"Local\JuanTool.Singleton", out var first);
        if (!first)
        {
            try { using var existing = EventWaitHandle.OpenExisting(@"Local\JuanTool.Show"); existing.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            Shutdown(); return;
        }
        wake = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\JuanTool.Show");
        launcher = new MainWindow(this);
        MainWindow = launcher;
        hotkey = new Hotkey(launcher, launcher.Toggle);
        var registered = hotkey.Register(Settings.Modifiers, Settings.Key);
        tray = new Forms.NotifyIcon { Text = "JuanTool · " + Settings.ShortcutLabel, Icon = System.Drawing.SystemIcons.Application, Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open JuanTool", null, (_, _) => launcher.Reveal());
        menu.Items.Add("Settings", null, (_, _) => launcher.ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit JuanTool", null, (_, _) => { IsExiting = true; Shutdown(); });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => launcher.Reveal();
        wakeRegistration = ThreadPool.RegisterWaitForSingleObject(wake, (_, _) => Dispatcher.BeginInvoke(launcher.Reveal), null, Timeout.Infinite, false);
        if (!e.Args.Contains("--background")) launcher.Reveal();
        if (!registered)
        {
            launcher.Reveal();
            Notify("The shortcut is already in use. Choose another shortcut in Settings.");
            Dispatcher.BeginInvoke(launcher.ShowSettings, DispatcherPriority.ApplicationIdle);
        }
    }
    public string? ApplySettings(Settings candidate)
    {
        var previous = Settings;
        var changed = candidate.Key != previous.Key || candidate.Modifiers != previous.Modifiers;
        if (changed && hotkey?.Register(candidate.Modifiers, candidate.Key) != true) return "That shortcut is unavailable. Try Ctrl+Shift+Space or another combination.";
        try
        {
            SetStartup(candidate.StartWithWindows);
            candidate.Save();
            Settings = candidate;
            if (tray is not null) tray.Text = "JuanTool · " + candidate.ShortcutLabel;
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            if (changed) hotkey?.Register(previous.Modifiers, previous.Key);
            try { SetStartup(previous.StartWithWindows); } catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
            return "Could not save settings: " + ex.Message;
        }
    }
    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("JuanTool", $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue("JuanTool", false);
    }
    public void Notify(string text) => tray?.ShowBalloonTip(5000, "JuanTool", text, Forms.ToolTipIcon.Info);
    protected override void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        wakeRegistration?.Unregister(null); wake?.Dispose();
        hotkey?.Dispose(); tray?.Dispose(); singleton?.Dispose();
        base.OnExit(e);
    }
}
