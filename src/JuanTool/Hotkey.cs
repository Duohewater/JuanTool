using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace JuanTool;

public sealed class Hotkey : IDisposable
{
    private readonly HwndSource source;
    private readonly Action pressed;
    private int activeId;
    private int nextId = 700;
    public Hotkey(Window window, Action pressed)
    {
        this.pressed = pressed;
        source = HwndSource.FromHwnd(new WindowInteropHelper(window).EnsureHandle());
        source.AddHook(Hook);
    }
    public bool Register(uint modifiers, uint key)
    {
        var candidate = ++nextId;
        if (!RegisterHotKey(source.Handle, candidate, modifiers | 0x4000, key)) return false;
        if (activeId != 0) UnregisterHotKey(source.Handle, activeId);
        activeId = candidate;
        return true;
    }
    private IntPtr Hook(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (message == 0x0312 && wparam.ToInt32() == activeId) { handled = true; pressed(); }
        return IntPtr.Zero;
    }
    public static string Label(uint modifiers, uint key)
    {
        var parts = new List<string>();
        if ((modifiers & 2) != 0) parts.Add("Ctrl");
        if ((modifiers & 1) != 0) parts.Add("Alt");
        if ((modifiers & 4) != 0) parts.Add("Shift");
        if ((modifiers & 8) != 0) parts.Add("Win");
        parts.Add(KeyInterop.KeyFromVirtualKey((int)key).ToString());
        return string.Join(" + ", parts);
    }
    public void Dispose() { if (activeId != 0) UnregisterHotKey(source.Handle, activeId); source.RemoveHook(Hook); }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
