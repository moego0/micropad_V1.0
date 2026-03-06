using CommunityToolkit.Mvvm.ComponentModel;

namespace Micropad.App.Models;

public partial class SetupJourneyItem : ObservableObject
{
    public string Id { get; init; } = "";

    public string Title { get; init; } = "";

    [ObservableProperty]
    private bool _isComplete;
}
