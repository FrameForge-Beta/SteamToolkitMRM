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
        WindowTheme.Apply(this);
        Title = Localization.Get("Renomear backup");
        Width = 440;
        Height = 214;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.Resources["WindowBrush"];
        Foreground = (Brush)Application.Current.Resources["TextBrush"];
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

        var content = new StackPanel { Margin = new Thickness(22, 20, 22, 18) };
        content.Children.Add(new TextBlock
        {
            Text = Localization.Get("Nome do backup"),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        _nameInput = new TextBox
        {
            Text = currentName,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = 38
        };
        content.Children.Add(_nameInput);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        var cancel = new Button
        {
            Content = Localization.Get("Cancelar"),
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)Application.Current.Resources["SecondaryButtonStyle"]
        };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button
        {
            Content = Localization.Get("Guardar"),
            Style = (Style)Application.Current.Resources["PrimaryButtonStyle"]
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
