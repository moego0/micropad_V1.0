using CommunityToolkit.Mvvm.ComponentModel;

namespace Micropad.Core.Models;

/// <summary>One slot on the Micropad grid (key K1–K12 or encoder E1/E2).</summary>
public partial class MicroSlot : ObservableObject
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsEncoder { get; set; }

    [ObservableProperty]
    private string _sequence = string.Empty;

    partial void OnSequenceChanged(string value) => OnPropertyChanged(nameof(DisplaySequence));

    public string DisplaySequence => string.IsNullOrEmpty(Sequence) ? "Drop or click" : (Sequence.Length > 24 ? Sequence[..24] + "…" : Sequence);
}
