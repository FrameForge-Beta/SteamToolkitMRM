using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SteamToolkitMRM;

internal sealed class RenameDialog : Window
{
    private readonly TextBox _nameInput;

    public RenameDialog(string currentName)
    {
        Title = Localization.Get("Renomear backup");
        Width = 430;
        Height = 190;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(18, 26, 43));
        Foreground = new SolidColorBrush(Color.FromRgb(243, 246, 252));
        FontFamily = new FontFamily("Segoe UI");

        var content = new StackPanel { Margin = new Thickness(22) };
        content.Children.Add(new TextBlock
        {
            Text = Localization.Get("Nome do backup"),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        _nameInput = new TextBox { Text = currentName, VerticalContentAlignment = VerticalAlignment.Center };
        content.Children.Add(_nameInput);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var cancel = new Button { Content = Localization.Get("Cancelar"), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button
        {
            Content = Localization.Get("Guardar"),
            Background = (Brush)Application.Current.Resources["AccentBrush"],
            Foreground = new SolidColorBrush(Color.FromRgb(8, 16, 30))
        };
        save.Click += (_, _) => Confirm();
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        content.Children.Add(buttons);
        Content = content;

        _nameInput.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
                Confirm();
            else if (args.Key == Key.Escape)
                DialogResult = false;
        };
        Loaded += (_, _) =>
        {
            _nameInput.Focus();
            _nameInput.SelectAll();
        };
    }

    public string BackupName => _nameInput.Text.Trim();

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(BackupName))
        {
            MessageBox.Show(this, Localization.Get("O nome não pode ficar vazio."),
                Localization.Get("Nome inválido"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
