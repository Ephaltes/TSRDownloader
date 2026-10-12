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
    private const int ClipboardUpdateMessage = 0x031D; // WM_CLIPBOARDUPDATE
    private const uint UnicodeTextFormat = 13; // CF_UNICODETEXT

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr newOwnerWindowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memoryHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr memoryHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr memoryHandle);

    private HwndSource? _windowSource;
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
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowProcedure);

        if (!AddClipboardFormatListener(handle))
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register clipboard listener. Error: {error}");
        }
    }

    private IntPtr WindowProcedure(IntPtr windowHandle, int message, IntPtr wordParameter, IntPtr longParameter, ref bool handled)
    {
        if (message == ClipboardUpdateMessage)
        {
            OnClipboardUpdate();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnClipboardUpdate()
    {
        // Ignored while paused, so copying the same link again after resuming still counts.
        if (!_mainViewModel.IsMonitoring)
            return;

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
            IntPtr dataHandle = GetClipboardData(UnicodeTextFormat);
            if (dataHandle == IntPtr.Zero)
                return null;

            IntPtr textPointer = GlobalLock(dataHandle);
            if (textPointer == IntPtr.Zero)
                return null;

            try
            {
                // The buffer is NUL-terminated but may be larger than the text; stop at the terminator.
                int characterCapacity = (int)Math.Min((ulong)GlobalSize(dataHandle) / 2, int.MaxValue);
                string text = Marshal.PtrToStringUni(textPointer, characterCapacity);
                int terminator = text.IndexOf('\0');
                return terminator >= 0 ? text[..terminator] : text;
            }
            finally
            {
                GlobalUnlock(dataHandle);
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
        if (_windowSource is not null)
        {
            IntPtr handle = _windowSource.Handle;
            RemoveClipboardFormatListener(handle);
            _windowSource.RemoveHook(WindowProcedure);
            _windowSource.Dispose();
            // Disposed again by the DI container on shutdown; make that a no-op.
            _windowSource = null;
        }
    }
}
