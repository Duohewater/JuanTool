using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using JuanTool.Core;

namespace JuanTool;

public partial class MainWindow : Window
{
    private IReadOnlyList<Bookmark> bookmarks = Array.Empty<Bookmark>();
    private readonly App app;
    private bool settingsOpen;
    private int loadVersion;
    public MainWindow(App app)
    {
        this.app = app;
        InitializeComponent();
        Deactivated += (_, _) => { if (!settingsOpen) Hide(); };
        Closing += OnClosing;
    }
    private void OnClosing(object? sender, CancelEventArgs e) { if (!app.IsExiting) { e.Cancel = true; Hide(); } }
    public void Toggle() { if (IsVisible && IsActive) Hide(); else Reveal(); }
    public void Reveal()
    {
        if (settingsOpen) return;
        Show();
        var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
        var area = screen.WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new Point(area.Left, area.Top));
        var bottomRight = transform.Transform(new Point(area.Right, area.Bottom));
        MaxHeight = bottomRight.Y - topLeft.Y;
        Width = Math.Min(700, bottomRight.X - topLeft.X);
        Left = topLeft.X + (bottomRight.X - topLeft.X - Width) / 2;
        Top = topLeft.Y + Math.Max(0, Math.Min((bottomRight.Y - topLeft.Y) * 0.18, bottomRight.Y - topLeft.Y - ActualHeight));
        Activate();
        Shortcut.Text = app.Settings.ShortcutLabel;
        Dispatcher.BeginInvoke(() => { Query.Focus(); Query.SelectAll(); }, DispatcherPriority.Input);
        _ = RefreshBookmarks();
    }
    private async Task RefreshBookmarks()
    {
        var version = ++loadVersion;
        var root = app.Settings.BookmarkRoot;
        var loaded = await Task.Run(() => Bookmarks.Load(root));
        if (version != loadVersion) return;
        bookmarks = loaded.Items;
        BookmarkCount.Text = $"{bookmarks.Count} saved · {loaded.Profiles} profile{(loaded.Profiles == 1 ? "" : "s")}";
        Status.Text = loaded.FailedProfiles > 0 ? "Some profiles could not be read. Reopen to retry, or check Settings." : "↑ ↓ choose   ·   Enter open   ·   Esc hide";
        Filter();
    }
    private void OnQueryChanged(object sender, TextChangedEventArgs e) { if (Results is not null) Filter(); }
    private void Filter()
    {
        var query = Query.Text;
        Placeholder.Visibility = query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        GoogleButton.IsEnabled = ChatGptButton.IsEnabled = !string.IsNullOrWhiteSpace(query);
        var found = Bookmarks.Search(bookmarks, query);
        Results.ItemsSource = found;
        Results.SelectedIndex = found.Count > 0 ? 0 : -1;
        BookmarkSection.IsExpanded = string.IsNullOrWhiteSpace(query) || found.Count > 0;
        EmptyState.Visibility = found.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = bookmarks.Count == 0 ? "No Chrome bookmarks found yet" : "No matching bookmarks";
        EmptyDetail.Text = bookmarks.Count == 0 ? "Add bookmarks in Chrome, or choose its User Data folder in Settings. You can still search below the bar." : "Try another keyword, or send your text to Google or ChatGPT.";
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
        else if (e.Key is Key.Down or Key.Up)
        {
            if (BookmarkSection.IsExpanded && Results.Items.Count > 0) { Results.SelectedIndex = Math.Clamp(Results.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, Results.Items.Count - 1); Results.ScrollIntoView(Results.SelectedItem); }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) Search(false);
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) Search(true);
            else if (Keyboard.FocusedElement is ButtonBase) return;
            else if (BookmarkSection.IsExpanded && Results.SelectedItem is Bookmark bookmark) OpenBookmark(bookmark);
            else Search(false);
            e.Handled = true;
        }
    }
    private void OpenBookmark(Bookmark bookmark) => RunAction(() => Browser.Open(bookmark.Url, bookmark.Profile, app.Settings.BookmarkRoot));
    private void Search(bool chatGpt)
    {
        var text = Query.Text.Trim();
        if (text.Length == 0) return;
        RunAction(() =>
        {
            if (chatGpt)
            {
                // Long prompts avoid URL length limits. Clipboard is touched only after an explicit ChatGPT action.
                Clipboard.SetText(text);
                Browser.Open(text.Length > 6000 ? "https://chatgpt.com/" : SearchLinks.ChatGpt(text));
                if (text.Length > 6000) app.Notify("Your text is copied. Paste it into ChatGPT with Ctrl+V.");
            }
            else Browser.Open(SearchLinks.Google(text));
        });
    }
    private void RunAction(Action action)
    {
        try { action(); Hide(); }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or System.Runtime.InteropServices.ExternalException or IOException or UnauthorizedAccessException)
        { Status.Text = "Could not open the link: " + e.Message; }
    }
    private void OnResultClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(Results, e.OriginalSource as DependencyObject) is ListBoxItem { DataContext: Bookmark bookmark }) OpenBookmark(bookmark);
    }
    private void OnGoogle(object sender, RoutedEventArgs e) => Search(false);
    private void OnChatGpt(object sender, RoutedEventArgs e) => Search(true);
    private void OnHide(object sender, RoutedEventArgs e) => Hide();
    private void OnSettings(object sender, RoutedEventArgs e) => ShowSettings();
    public void ShowSettings()
    {
        if (settingsOpen) return;
        Reveal();
        settingsOpen = true;
        try { new SettingsWindow(app) { Owner = this }.ShowDialog(); }
        finally { settingsOpen = false; }
        Reveal();
    }
    private void OnDrag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left && e.OriginalSource is not Button) DragMove(); }
}
