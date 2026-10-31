using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SteamToolkitMRM;

internal sealed class ColorPickerDialog : Window
{
    private static readonly string[] Palette =
    [
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#22C55E", "#10B981", "#14B8A6", "#06B6D4", "#0EA5E9",
        "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF", "#EC4899", "#F43F5E", "#FCA5A5", "#FDBA74", "#FCD34D",
        "#FDE047", "#BEF264", "#86EFAC", "#6EE7B7", "#5EEAD4", "#67E8F9", "#7DD3FC", "#93C5FD", "#A5B4FC", "#C4B5FD",
        "#D8B4FE", "#F0ABFC", "#F9A8D4", "#FDA4AF", "#B91C1C", "#C2410C", "#B45309", "#A16207", "#4D7C0F", "#15803D",
        "#047857", "#0F766E", "#0E7490", "#0369A1", "#1D4ED8", "#4338CA", "#6D28D9", "#7E22CE", "#A21CAF", "#BE185D",
        "#111827", "#374151", "#6B7280", "#9CA3AF", "#D1D5DB", "#F3F4F6", "#FFFFFF", "#000000", "#8B5CF6", "#7C3AED"
    ];

    public ColorPickerDialog(string initialColor)
    {
        Title = Localization.Get("Escolher cor");
        Width = 430;
        Height = 390;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(18, 26, 43));
        Foreground = new SolidColorBrush(Color.FromRgb(243, 246, 252));
        FontFamily = new FontFamily("Segoe UI");

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = Localization.Get("Escolhe uma cor"),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var hexInput = new TextBox
        {
            Width = 115,
            MaxLength = 7,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var grid = new UniformGrid { Columns = 10, Rows = 6, Margin = new Thickness(0, 0, 0, 14) };
        foreach (string hex in Palette)
        {
            var swatch = new Button
            {
                Tag = hex,
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(3),
                Background = new SolidColorBrush(ParseColor(hex)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(65, 77, 99)),
                BorderThickness = new Thickness(1),
                ToolTip = hex
            };
            swatch.Click += (_, _) => SelectColor(hex);
            grid.Children.Add(swatch);
        }
        root.Children.Add(grid);

        var selectedColor = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 10, 0)
        };
        hexInput.TextChanged += (_, _) =>
        {
            if (TryParseColor(hexInput.Text, out Color color))
                selectedColor.Background = new SolidColorBrush(color);
        };
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { selectedColor, hexInput }
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 17, 0, 0)
        };
        var cancel = new Button { Content = Localization.Get("Cancelar"), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => DialogResult = false;
        var choose = new Button
        {
            Content = Localization.Get("Escolher"),
            Background = (Brush)Application.Current.Resources["AccentBrush"],
            Foreground = new SolidColorBrush(Color.FromRgb(8, 16, 30))
        };
        choose.Click += (_, _) =>
        {
            if (!TryParseColor(hexInput.Text, out _))
            {
                MessageBox.Show(this, Localization.Get("Indica uma cor hexadecimal válida no formato #RRGGBB."),
                    Localization.Get("Cor inválida"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(choose);
        root.Children.Add(buttons);
        Content = root;

        hexInput.Text = TryParseColor(initialColor, out Color initial)
            ? FormatColor(initial)
            : "#8B5CF6";
        SelectedColor = hexInput.Text;
        hexInput.TextChanged += (_, _) =>
        {
            if (TryParseColor(hexInput.Text, out Color color))
            {
                selectedColor.Background = new SolidColorBrush(color);
                SelectedColor = FormatColor(color);
            }
        };

        void SelectColor(string value)
        {
            hexInput.Text = value;
            SelectedColor = value;
        }
    }

    public string SelectedColor { get; private set; } = "#8B5CF6";

    private static Color ParseColor(string hex)
    {
        TryParseColor(hex, out Color color);
        return color;
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        string hex = value.Trim();
        if (!hex.StartsWith('#'))
            hex = $"#{hex}";
        if (hex.Length != 7 || hex.Skip(1).Any(character => !Uri.IsHexDigit(character)))
            return false;
        color = Color.FromRgb(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
        return true;
    }

    private static string FormatColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
