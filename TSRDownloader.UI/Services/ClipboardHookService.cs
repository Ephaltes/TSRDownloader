using System.Runtime.InteropServices;
using System.Windows.Interop;
using TSRDownloader.UI.ViewModels;

namespace TSRDownloader.UI.Services;

/// <summary>
/// Windows API clipboard monitoring hook.
/// Uses AddClipboardFormatListener to receive WM_CLIPBOARDUPDATE messages
/// instead of polling. This is the recommended approach for clipboard monitoring.
/// </summary>
public class ClipboardHookService : IDisposable
{
    private const int WM_CLIPBOARUPDATE = 0x031D;
    private const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    private HwndSource? _hwndSource;
    private readonly MainViewModel _mainViewModel;
    private string _lastClipboardText = string.Empty;

    public ClipboardHookService(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    /// <summary>
    /// Attaches the clipboard listener to the specified window.
    /// </summary>
    public void Attach(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        if (!AddClipboardFormatListener(handle))
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register clipboard listener. Error: {error}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARUPDATE)
        {
            OnClipboardUpdate();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnClipboardUpdate()
    {
        string? clipboardText = TryReadClipboardText();
        if (clipboardText is null)
        {
            // Another app may still hold the clipboard; WPF's reader retries internally.
            try { clipboardText = Clipboard.GetText(); } catch { /* ignore */ }
        }

        if (string.IsNullOrEmpty(clipboardText) || clipboardText == _lastClipboardText)
            return;

        _lastClipboardText = clipboardText;
        _mainViewModel.OnClipboardContentChanged(clipboardText);
    }

    /// <summary>
    /// Reads CF_UNICODETEXT via the Win32 API. Returns null if the clipboard could not be opened
    /// or holds no text. The clipboard is always closed again — leaving it open would block
    /// every other application's copy and paste.
    /// </summary>
    private static string? TryReadClipboardText()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return null;

        try
        {
            IntPtr hData = GetClipboardData(CF_UNICODETEXT);
            if (hData == IntPtr.Zero)
                return null;

            IntPtr hGlobal = GlobalLock(hData);
            if (hGlobal == IntPtr.Zero)
                return null;

            try
            {
                // The buffer is NUL-terminated but may be larger than the text; stop at the terminator.
                int maxChars = (int)Math.Min((ulong)GlobalSize(hData) / 2, int.MaxValue);
                string text = Marshal.PtrToStringUni(hGlobal, maxChars);
                int terminator = text.IndexOf('\0');
                return terminator >= 0 ? text[..terminator] : text;
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            CloseClipboard();
        }
    }

    public void Dispose()
    {
        if (_hwndSource is not null)
        {
            IntPtr handle = _hwndSource.Handle;
            RemoveClipboardFormatListener(handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            // Disposed again by the DI container on shutdown; make that a no-op.
            _hwndSource = null;
        }
    }
}
