using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Micropad.App.Services;

public sealed class CommandPaletteEntry
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public ICommand? Command { get; init; }
    public Action? Action { get; init; }
    public object? CommandParameter { get; init; }
    public string? Category { get; init; }
    public bool IsPinned { get; set; }
}

/// <summary>Registers global commands for Ctrl+K palette. Run action or command when selected.</summary>
public class CommandPaletteService
{
    private readonly List<CommandPaletteEntry> _entries = new();
    private readonly List<string> _recentIds = new();
    private const int MaxRecent = 8;

    public IReadOnlyList<CommandPaletteEntry> AllEntries => _entries;

    public void Register(CommandPaletteEntry entry)
    {
        if (_entries.Any(e => e.Id == entry.Id)) return;
        _entries.Add(entry);
    }

    public void RecordRecent(string id)
    {
        _recentIds.Remove(id);
        _recentIds.Insert(0, id);
        if (_recentIds.Count > MaxRecent)
            _recentIds.RemoveAt(_recentIds.Count - 1);
    }

    public IEnumerable<CommandPaletteEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _entries.OrderByDescending(e => _recentIds.IndexOf(e.Id)).ThenBy(e => e.Title);
        var q = query.Trim().ToLowerInvariant();
        return _entries
            .Where(e => e.Title.ToLowerInvariant().Contains(q) || (e.Subtitle?.ToLowerInvariant().Contains(q) == true))
            .OrderBy(e => e.Title);
    }

    public void Execute(CommandPaletteEntry entry)
    {
        RecordRecent(entry.Id);
        if (entry.Action != null)
        {
            try { entry.Action(); } catch { /* ignore */ }
            return;
        }
        if (entry.Command?.CanExecute(entry.CommandParameter) == true)
        {
            try { entry.Command.Execute(entry.CommandParameter); } catch { /* ignore */ }
        }
    }
}
