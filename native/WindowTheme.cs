using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SteamToolkitMRM;

internal static class WindowTheme
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeLegacy = 19;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;

    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyCore(window);
    }

    public static void UpdateCaption(Window window, Color color)
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            int caption = ToColorRef(color);
            DwmSetWindowAttribute(handle, DwmCaptionColor, ref caption, sizeof(int));

            int text = ToColorRef(Color.FromRgb(0xF2, 0xF5, 0xFA));
            DwmSetWindowAttribute(handle, DwmTextColor, ref text, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static void ApplyCore(Window window)
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            int enabled = 1;
            if (DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, DwmUseImmersiveDarkModeLegacy, ref enabled, sizeof(int));

            if (window.FindResource("WindowBrush") is SolidColorBrush background)
            {
                int caption = ToColorRef(background.Color);
                DwmSetWindowAttribute(handle, DwmCaptionColor, ref caption, sizeof(int));

                int text = ToColorRef(Color.FromRgb(0xF2, 0xF5, 0xFA));
                DwmSetWindowAttribute(handle, DwmTextColor, ref text, sizeof(int));
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
