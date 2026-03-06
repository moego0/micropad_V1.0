using Micropad.Core.Models;

namespace Micropad.App.Models;

/// <summary>One entry in the Action Library (search, categories, drag onto key).</summary>
public class ActionLibraryItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public ActionType ActionType { get; set; }
    /// <summary>e.g. "Pro", "Requires App"</summary>
    public string? Badge { get; set; }
    /// <summary>For Macro entries: macro asset id.</summary>
    public string? MacroId { get; set; }
    public bool IsFavorite { get; set; }
}
