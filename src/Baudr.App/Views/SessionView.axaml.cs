using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Baudr.App.Controls;

namespace Baudr.App.Views;

public partial class SessionView : UserControl
{
    public SessionView()
    {
        InitializeComponent();

        var term = this.FindControl<TerminalControl>("TermControl");
        var scroll = this.FindControl<ScrollBar>("TermScrollBar");

        if (term != null && scroll != null)
        {
            term.AttachScrollBar(scroll);
        }
    }
}

