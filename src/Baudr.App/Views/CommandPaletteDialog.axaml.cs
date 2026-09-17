using Avalonia.Controls;
using Avalonia.Input;
using Baudr.App.ViewModels;

namespace Baudr.App.Views;

public partial class CommandPaletteDialog : UserControl
{
    public CommandPaletteDialog()
    {
        InitializeComponent();

        var box = this.FindControl<TextBox>("SearchBox");
        if (box != null)
        {
            AttachedToVisualTree += (s, e) => box.Focus();
            box.KeyDown += OnSearchBoxKeyDown;
        }
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel vm)
        {
            if (e.Key == Key.Enter)
            {
                vm.ExecuteSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.Close();
                e.Handled = true;
            }
        }
    }
}

