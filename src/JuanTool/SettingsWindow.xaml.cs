namespace JuanTool;

public partial class SettingsWindow : Window
{
    private readonly App app;
    private uint modifiers;
    private uint key;
    public SettingsWindow(App app)
    {
        this.app = app;
        InitializeComponent();
        modifiers = app.Settings.Modifiers; key = app.Settings.Key;
        ShortcutBox.Text = app.Settings.ShortcutLabel;
        RootBox.Text = app.Settings.BookmarkRoot;
        StartupBox.IsChecked = app.Settings.StartWithWindows;
    }
    private void CaptureShortcut(object sender, KeyEventArgs e)
    {
        var pressed = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        if (pressed == System.Windows.Input.Key.Tab) return;
        e.Handled = true;
        if (pressed is System.Windows.Input.Key.LeftAlt or System.Windows.Input.Key.RightAlt or System.Windows.Input.Key.LeftCtrl or System.Windows.Input.Key.RightCtrl or System.Windows.Input.Key.LeftShift or System.Windows.Input.Key.RightShift or System.Windows.Input.Key.LWin or System.Windows.Input.Key.RWin) return;
        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None && (pressed < System.Windows.Input.Key.F1 || pressed > System.Windows.Input.Key.F11)) { Error.Text = "Use a modifier (Ctrl, Alt, Shift, Win), or a function key F1–F11."; return; }
        modifiers = (uint)mods; // WPF and RegisterHotKey share modifier flag values.
        key = (uint)KeyInterop.VirtualKeyFromKey(pressed);
        ShortcutBox.Text = Hotkey.Label(modifiers, key);
        Error.Text = "";
    }
    private void Browse(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Select Chrome's User Data folder", InitialDirectory = Directory.Exists(RootBox.Text) ? RootBox.Text : Core.Bookmarks.DefaultRoot };
        if (picker.ShowDialog(this) == true) RootBox.Text = picker.FolderName;
    }
    private void Save(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(RootBox.Text.Trim())) { Error.Text = "Choose an existing Chrome User Data folder."; return; }
        var candidate = app.Settings with { Modifiers = modifiers, Key = key, BookmarkRoot = RootBox.Text.Trim(), StartWithWindows = StartupBox.IsChecked == true };
        var error = app.ApplySettings(candidate);
        if (error is not null) { Error.Text = error; return; }
        DialogResult = true;
    }
}
