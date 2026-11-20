using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SteamToolkitMRM;

internal sealed class ColorPickerDialog : Window
{
    private const double SpectrumWidth = 268;
    private const double SpectrumHeight = 196;
    private const double HueWidth = 18;
    private const double HueGap = 12;

    private static readonly string[] Palette =
    [
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#22C55E", "#10B981", "#14B8A6",
        "#06B6D4", "#0EA5E9", "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF", "#EC4899",
        "#F43F5E", "#FCA5A5", "#FDBA74", "#FCD34D", "#FDE047", "#BEF264", "#86EFAC", "#6EE7B7",
        "#5EEAD4", "#67E8F9", "#7DD3FC", "#93C5FD", "#A5B4FC", "#C4B5FD", "#D8B4FE", "#F0ABFC",
        "#F9A8D4", "#FDA4AF", "#B91C1C", "#C2410C", "#B45309", "#A16207", "#4D7C0F", "#15803D",
        "#047857", "#0F766E", "#0E7490", "#0369A1", "#1D4ED8", "#4338CA", "#6D28D9", "#7E22CE",
        "#A21CAF", "#BE185D", "#111827", "#374151", "#6B7280", "#9CA3AF", "#D1D5DB", "#F3F4F6",
        "#FFFFFF", "#000000", "#F8FAFC", "#A8A29E", "#57534E", "#292524", "#8B5CF6", "#7C3AED"
    ];

    private static readonly Brush SwatchBorderBrush = new SolidColorBrush(Color.FromRgb(65, 77, 99));
    private static readonly Brush SelectedSwatchBrush = new SolidColorBrush(Color.FromRgb(242, 245, 250));

    private readonly TextBox _hexInput;
    private readonly TextBox[] _rgbInputs = new TextBox[3];
    private readonly Border _preview;
    private readonly List<Button> _swatches = [];
    private readonly Canvas _spectrum;
    private readonly Canvas _hueTrack;
    private readonly Grid _spectrumThumb;
    private readonly Border _hueThumb;

    private double _hue;
    private double _saturation;
    private double _value = 1;
    private bool _syncing;

    public ColorPickerDialog(string initialColor)
    {
        WindowTheme.Apply(this);
        Title = Localization.Get("Escolher cor");
        Width = 640;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.Resources["WindowBrush"];
        Foreground = (Brush)Application.Current.Resources["TextBrush"];
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

        var root = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        root.Children.Add(new TextBlock
        {
            Text = Localization.Get("Escolhe uma cor"),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });
        root.Children.Add(new TextBlock
        {
            Text = Localization.Get("Escolhe uma cor na grelha ou escreve o seu código hexadecimal."),
            Foreground = (Brush)Application.Current.Resources["MutedBrush"],
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 18)
        });

        // Color palette (left) and exact color picker (right).
        var columns = new Grid();
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var palettePanel = new StackPanel { Margin = new Thickness(0, 0, 28, 0) };
        palettePanel.Children.Add(new TextBlock
        {
            Text = Localization.Get("Cores predefinidas"),
            Foreground = (Brush)Application.Current.Resources["MutedBrush"],
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        });
        var grid = new UniformGrid { Columns = 8, Rows = 8 };
        foreach (string hex in Palette)
        {
            var swatch = new Button
            {
                Tag = hex,
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(3),
                Background = new SolidColorBrush(ParseColor(hex)),
                BorderBrush = SwatchBorderBrush,
                BorderThickness = new Thickness(1),
                ToolTip = hex
            };
            AutomationProperties.SetName(swatch, hex);
            swatch.Click += (_, _) => SelectColor(hex);
            _swatches.Add(swatch);
            grid.Children.Add(swatch);
        }
        palettePanel.Children.Add(grid);
        Grid.SetColumn(palettePanel, 0);
        columns.Children.Add(palettePanel);

        var pickerPanel = new StackPanel();
        pickerPanel.Children.Add(new TextBlock
        {
            Text = Localization.Get("Ajusta o tom e a intensidade"),
            Foreground = (Brush)Application.Current.Resources["MutedBrush"],
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var spectrumRow = new StackPanel { Orientation = Orientation.Horizontal };
        _spectrum = new Canvas
        {
            Width = SpectrumWidth,
            Height = SpectrumHeight,
            Cursor = Cursors.Cross,
            Background = Brushes.Transparent
        };
        var hueLayer = new Rectangle
        {
            Width = SpectrumWidth,
            Height = SpectrumHeight,
            Fill = BuildHueBrush(vertical: false),
            IsHitTestVisible = false
        };
        var saturationLayer = new Rectangle
        {
            Width = SpectrumWidth,
            Height = SpectrumHeight,
            Fill = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), 0),
            IsHitTestVisible = false
        };
        var valueLayer = new Rectangle
        {
            Width = SpectrumWidth,
            Height = SpectrumHeight,
            Fill = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, new Point(0, 0), new Point(0, 1)),
            IsHitTestVisible = false
        };
        _spectrumThumb = CreateRingThumb();
        _spectrum.Children.Add(hueLayer);
        _spectrum.Children.Add(saturationLayer);
        _spectrum.Children.Add(valueLayer);
        _spectrum.Children.Add(_spectrumThumb);
        _spectrum.MouseLeftButtonDown += (_, e) =>
        {
            _spectrum.CaptureMouse();
            PickFromSpectrum(e.GetPosition(_spectrum));
            e.Handled = true;
        };
        _spectrum.MouseMove += (_, e) =>
        {
            if (_spectrum.IsMouseCaptured)
                PickFromSpectrum(e.GetPosition(_spectrum));
        };
        _spectrum.MouseLeftButtonUp += (_, _) => _spectrum.ReleaseMouseCapture();

        var spectrumHost = new Border
        {
            Width = SpectrumWidth,
            Height = SpectrumHeight,
            BorderBrush = (Brush)Application.Current.Resources["BorderStrongBrush"],
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true,
            Child = _spectrum
        };
        spectrumRow.Children.Add(spectrumHost);

        _hueTrack = new Canvas
        {
            Width = HueWidth + 8,
            Height = SpectrumHeight,
            Margin = new Thickness(HueGap, 0, 0, 0),
            Cursor = Cursors.Hand,
            Background = Brushes.Transparent
        };
        var hueBar = new Border
        {
            Width = HueWidth,
            Height = SpectrumHeight,
            CornerRadius = new CornerRadius(3),
            Background = BuildHueBrush(vertical: true),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(hueBar, 4);
        _hueThumb = new Border
        {
            Width = HueWidth + 8,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(10, 15, 25)),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        _hueTrack.Children.Add(hueBar);
        _hueTrack.Children.Add(_hueThumb);
        _hueTrack.MouseLeftButtonDown += (_, e) =>
        {
            _hueTrack.CaptureMouse();
            PickHue(e.GetPosition(_hueTrack));
            e.Handled = true;
        };
        _hueTrack.MouseMove += (_, e) =>
        {
            if (_hueTrack.IsMouseCaptured)
                PickHue(e.GetPosition(_hueTrack));
        };
        _hueTrack.MouseLeftButtonUp += (_, _) => _hueTrack.ReleaseMouseCapture();
        spectrumRow.Children.Add(_hueTrack);
        pickerPanel.Children.Add(spectrumRow);
        Grid.SetColumn(pickerPanel, 1);
        columns.Children.Add(pickerPanel);
        root.Children.Add(columns);

        _preview = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(10),
            BorderBrush = SwatchBorderBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        _hexInput = new TextBox
        {
            Width = 112,
            MinHeight = 44,
            Padding = new Thickness(11, 13, 11, 13),
            MaxLength = 9,
            FontFamily = new FontFamily("Consolas"),
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var fieldsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 18, 0, 0)
        };
        fieldsRow.Children.Add(_preview);

        var hexColumn = new StackPanel { Margin = new Thickness(0, 0, 18, 0), VerticalAlignment = VerticalAlignment.Bottom };
        hexColumn.Children.Add(new TextBlock
        {
            Text = "Hex",
            Foreground = (Brush)Application.Current.Resources["MutedBrush"],
            FontSize = 10.5,
            Margin = new Thickness(2, 0, 0, 5)
        });
        hexColumn.Children.Add(_hexInput);
        fieldsRow.Children.Add(hexColumn);

        string[] rgbKeys = ["Vermelho", "Verde", "Azul"];
        string[] rgbLabels = ["R", "G", "B"];
        for (int index = 0; index < 3; index++)
        {
            var field = new TextBox
            {
                Width = 52,
                MinHeight = 44,
                Padding = new Thickness(6, 13, 6, 13),
                MaxLength = 3,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = Localization.Get(rgbKeys[index])
            };
            AutomationProperties.SetName(field, Localization.Get(rgbKeys[index]));
            field.TextChanged += (_, _) => ApplyRgbInputs();
            _rgbInputs[index] = field;

            var column = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            column.Children.Add(new TextBlock
            {
                Text = rgbLabels[index],
                Foreground = (Brush)Application.Current.Resources["MutedBrush"],
                FontSize = 10.5,
                Margin = new Thickness(2, 0, 0, 5)
            });
            column.Children.Add(field);
            fieldsRow.Children.Add(column);
        }
        root.Children.Add(fieldsRow);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0)
        };
        var cancel = new Button
        {
            Content = Localization.Get("Cancelar"),
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 96,
            IsCancel = true,
            Style = (Style)Application.Current.Resources["SecondaryButtonStyle"]
        };
        var choose = new Button
        {
            Content = Localization.Get("Escolher"),
            MinWidth = 96,
            IsDefault = true,
            Style = (Style)Application.Current.Resources["PrimaryButtonStyle"]
        };
        choose.Click += (_, _) =>
        {
            if (!TryParseColor(_hexInput.Text, out Color color))
            {
                MessageBox.Show(this, Localization.Get("Indica uma cor hexadecimal válida no formato #RRGGBB."),
                    Localization.Get("Cor inválida"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedColor = FormatColor(color);
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(choose);
        root.Children.Add(buttons);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
        Content = scroll;
        MaxHeight = Math.Max(430, SystemParameters.WorkArea.Height - 60);

        _hexInput.TextChanged += (_, _) =>
        {
            if (_syncing)
                return;
            if (TryParseColor(_hexInput.Text, out Color color))
                ApplyColor(color, updateHsv: true, updateHexText: false);
        };
        _hexInput.LostFocus += (_, _) =>
        {
            if (TryParseColor(_hexInput.Text, out Color color))
            {
                string formatted = FormatColor(color);
                if (!string.Equals(_hexInput.Text, formatted, StringComparison.OrdinalIgnoreCase))
                    _hexInput.Text = formatted;
            }
        };

        ApplyColor(
            TryParseColor(initialColor, out Color initial) ? initial : ParseColor("#8B5CF6"),
            updateHsv: true,
            updateHexText: true);
    }

    public string SelectedColor { get; private set; } = "#8B5CF6";

    private void SelectColor(string value)
    {
        if (!TryParseColor(value, out Color color))
            return;
        ApplyColor(color, updateHsv: true, updateHexText: true);
        if (!_hexInput.IsKeyboardFocused)
            _hexInput.CaretIndex = _hexInput.Text.Length;
    }

    private void PickFromSpectrum(Point point)
    {
        _saturation = Math.Clamp(point.X / SpectrumWidth, 0, 1);
        _value = Math.Clamp(1 - point.Y / SpectrumHeight, 0, 1);
        ApplyColor(HsvToRgb(_hue, _saturation, _value), updateHsv: false, updateHexText: true);
    }

    private void PickHue(Point point)
    {
        _hue = Math.Clamp(point.Y / SpectrumHeight, 0, 1) * 360;
        if (_saturation < 0.02)
            _saturation = 1;
        ApplyColor(HsvToRgb(_hue, _saturation, _value), updateHsv: false, updateHexText: true);
    }

    private void ApplyRgbInputs()
    {
        if (_syncing)
            return;
        if (!TryReadByte(_rgbInputs[0].Text, out byte red)
            || !TryReadByte(_rgbInputs[1].Text, out byte green)
            || !TryReadByte(_rgbInputs[2].Text, out byte blue))
            return;
        ApplyColor(Color.FromRgb(red, green, blue), updateHsv: true, updateHexText: true);
    }

    private void ApplyColor(Color color, bool updateHsv, bool updateHexText)
    {
        if (updateHsv)
            (_hue, _saturation, _value) = RgbToHsv(color);

        SelectedColor = FormatColor(color);
        _syncing = true;
        _preview.Background = new SolidColorBrush(color);
        if (updateHexText && !_hexInput.IsKeyboardFocused)
            _hexInput.Text = SelectedColor;
        if (!_rgbInputs[0].IsKeyboardFocused)
            _rgbInputs[0].Text = color.R.ToString();
        if (!_rgbInputs[1].IsKeyboardFocused)
            _rgbInputs[1].Text = color.G.ToString();
        if (!_rgbInputs[2].IsKeyboardFocused)
            _rgbInputs[2].Text = color.B.ToString();
        HighlightSwatch(SelectedColor);
        PositionThumbs();
        _syncing = false;
    }

    private void HighlightSwatch(string value)
    {
        foreach (Button swatch in _swatches)
        {
            bool selected = string.Equals(swatch.Tag as string, value, StringComparison.OrdinalIgnoreCase);
            swatch.BorderBrush = selected ? SelectedSwatchBrush : SwatchBorderBrush;
            swatch.BorderThickness = new Thickness(selected ? 2 : 1);
        }
    }

    private void PositionThumbs()
    {
        Canvas.SetLeft(_spectrumThumb, Math.Clamp(_saturation * SpectrumWidth, 0, SpectrumWidth) - 8);
        Canvas.SetTop(_spectrumThumb, Math.Clamp((1 - _value) * SpectrumHeight, 0, SpectrumHeight) - 8);
        Canvas.SetTop(_hueThumb, Math.Clamp(_hue / 360 * SpectrumHeight, 0, SpectrumHeight) - 2);
    }

    private static Grid CreateRingThumb()
    {
        var thumb = new Grid { Width = 16, Height = 16, IsHitTestVisible = false };
        thumb.Children.Add(new Ellipse
        {
            Width = 16,
            Height = 16,
            Stroke = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
            StrokeThickness = 3
        });
        thumb.Children.Add(new Ellipse
        {
            Width = 14,
            Height = 14,
            Stroke = Brushes.White,
            StrokeThickness = 2
        });
        return thumb;
    }

    private static LinearGradientBrush BuildHueBrush(bool vertical)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = vertical ? new Point(0, 1) : new Point(1, 0),
            GradientStops =
            {
                new GradientStop(Colors.Red, 0),
                new GradientStop(Color.FromRgb(255, 255, 0), 1.0 / 6),
                new GradientStop(Color.FromRgb(0, 255, 0), 2.0 / 6),
                new GradientStop(Color.FromRgb(0, 255, 255), 3.0 / 6),
                new GradientStop(Color.FromRgb(0, 0, 255), 4.0 / 6),
                new GradientStop(Color.FromRgb(255, 0, 255), 5.0 / 6),
                new GradientStop(Colors.Red, 1)
            }
        };
        return brush;
    }

    private static bool TryReadByte(string? value, out byte result) =>
        byte.TryParse(value?.Trim(), out result);

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
        if (hex.StartsWith('#'))
            hex = hex[1..];
        if (hex.Length == 3)
            hex = string.Concat(hex.Select(character => new string(character, 2)));
        else if (hex.Length == 8)
            hex = hex[2..];

        if (hex.Length != 6 || hex.Any(character => !Uri.IsHexDigit(character)))
            return false;

        color = Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
        return true;
    }

    private static (double Hue, double Saturation, double Value) RgbToHsv(Color color)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;
        double max = Math.Max(red, Math.Max(green, blue));
        double min = Math.Min(red, Math.Min(green, blue));
        double delta = max - min;

        double hue = 0;
        if (delta > 0)
        {
            if (max == red)
                hue = 60 * (((green - blue) / delta) % 6);
            else if (max == green)
                hue = 60 * ((blue - red) / delta + 2);
            else
                hue = 60 * ((red - green) / delta + 4);
        }
        if (hue < 0)
            hue += 360;

        double saturation = max <= 0 ? 0 : delta / max;
        return (hue, saturation, max);
    }

    private static Color HsvToRgb(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        double chroma = value * saturation;
        double x = chroma * (1 - Math.Abs(hue / 60.0 % 2 - 1));
        double match = value - chroma;

        (double red, double green, double blue) = hue switch
        {
            < 60 => (chroma, x, 0.0),
            < 120 => (x, chroma, 0.0),
            < 180 => (0.0, chroma, x),
            < 240 => (0.0, x, chroma),
            < 300 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };

        return Color.FromRgb(
            (byte)Math.Round((red + match) * 255),
            (byte)Math.Round((green + match) * 255),
            (byte)Math.Round((blue + match) * 255));
    }

    private static string FormatColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
