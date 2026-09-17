using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Baudr.App.ViewModels;

public record PaletteAction(
    string Title,
    string Category,
    string? Subtitle = null,
    string? Shortcut = null,
    Action? Execute = null);

public partial class CommandPaletteViewModel : ViewModelBase
{
    private readonly List<PaletteAction> _allActions = [];

    [ObservableProperty]
    private string _filterQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PaletteAction> _filteredActions = [];

    [ObservableProperty]
    private PaletteAction? _selectedAction;

    [ObservableProperty]
    private bool _isOpen;

    public event Action? Closed;

    public CommandPaletteViewModel()
    {
    }

    public void RegisterActions(IEnumerable<PaletteAction> actions)
    {
        _allActions.Clear();
        _allActions.AddRange(actions);
        FilterActions();
    }

    partial void OnFilterQueryChanged(string value)
    {
        FilterActions();
    }

    private void FilterActions()
    {
        if (string.IsNullOrWhiteSpace(FilterQuery))
        {
            FilteredActions = new ObservableCollection<PaletteAction>(_allActions);
        }
        else
        {
            var q = FilterQuery.Trim();
            var matches = _allActions
                .Where(a => a.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (a.Subtitle != null && a.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                            a.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredActions = new ObservableCollection<PaletteAction>(matches);
        }

        SelectedAction = FilteredActions.FirstOrDefault();
    }

    [RelayCommand]
    public void ExecuteSelected()
    {
        var action = SelectedAction;
        Close();
        action?.Execute?.Invoke();
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
        FilterQuery = string.Empty;
        Closed?.Invoke();
    }
}

