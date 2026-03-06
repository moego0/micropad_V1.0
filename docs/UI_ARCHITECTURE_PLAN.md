# Micropad Premium UI — Architecture Plan

## Approach

- **Extend, don’t rewrite.** Keep existing MVVM, services, and BLE/protocol logic. Add a design system and new UI layers on top.
- **UI stack:** WPF (.NET 8) + **Wpf.Ui** (Fluent controls, navigation, snackbar) + custom DesignSystem (tokens, theme, motion).
- **Performance:** No UI-thread blocking; async + debounced search; virtualized lists where applicable.

---

## Views & ViewModels (extended)

| Area | Current | Add/Change |
|------|---------|------------|
| **Shell** | MainWindow + Frame + sidebar ListBox | Use Wpf.Ui `FluentWindow` + `NavigationView` (or keep Frame, restyle with design tokens). Add theme toggle, command palette host. |
| **Devices** | DevicesView / DevicesViewModel | Add connection stepper (Scan → Found → Pairing → Ready), skeleton loading, inline error banners. |
| **Profiles** | ProfilesView / ProfilesViewModel | Add **Action Library** left panel (search, categories, favorites, drag-drop). Layer tabs + Fn indicator. **Combo Builder** sub-view or panel. **Encoder Mapper** panel. Profile conflict resolution modal. Undo/redo for key/layer/profile edits. |
| **Templates** | PresetsView / PresetsViewModel | Gallery cards, empty state, preset marketplace (local export/import). |
| **Macros** | MacrosView / MacrosViewModel | Replace/ augment with **Macro Timeline Editor** (blocks, delays, loops, variables, inspector). Record, Play/Stop preview. Undo/redo. |
| **Stats** | StatsView / StatsViewModel | Optional heatmap, “time saved” estimate, gamification hooks. |
| **Settings** | SettingsView / SettingsViewModel | Add Reduce Motion, Background (Solid/Acrylic/Mica), Gamification toggles. Accent picker. |
| **Dialogs** | ActionEditWindow | Restyle with tokens; optional **Command Palette** (Ctrl+K) for “Find action”, “Push to device”, etc. |

---

## Services (new / extended)

| Service | Role |
|---------|------|
| **ThemeService** | Light/Dark theme; applies Wpf.Ui theme + our DesignTokens; persists (e.g. SettingsStorage). |
| **AccentService** | Accent color; persists; notifies for focus rings / selected states. |
| **IconService** | Central Fluent/Segoe icon mapping (16/20/24); consistent stroke. |
| **MotionService** | Durations (120/160/200 ms), easings, **ReduceMotion** flag; used by animations and transitions. |
| **CommandPaletteService** | Registers commands (Create Profile, Push to Device, Find Macro…); opens overlay; keyboard nav. |
| **UndoRedoService** | Command stack (IUndoableCommand); Ctrl+Z / Ctrl+Y; toasts “Undo: …”. |
| **ConflictResolutionService** | Detects PC vs device profile conflicts; provides data for diff modal (Pull / Push / Keep both). |
| **SetupJourneyService** | Tracks “Connect device”, “First profile”, “First macro”, etc.; raises events for XP/badges/confetti. |
| **GamificationService** | XP, levels, badges, streaks; optional; respects Settings toggles. |

Existing: BleConnection, ProtocolHandler, ProfileSyncService, LocalProfileStorage, LocalMacroStorage, SettingsStorage, TrayService, HudService, ForegroundMonitor, DiagnosticsService, MacroRecorder — **unchanged**; call from new UI/ViewModels as needed.

---

## Design System (DesignSystem folder in App)

- **DesignTokens.xaml** — Spacing (4/8/12/16/24), radius (4/8/10/12), typography (Title/Subtitle/Body/Caption), shadows.
- **ThemeService** — Light/Dark; sync with Wpf.Ui ApplicationTheme.
- **AccentService** — Accent brush; used in styles.
- **IconService** — Symbol/name → DrawingImage or path; sizes 16/20/24.
- **MotionService** — DurationFast/Normal/Slow, EasingOut/EasingInOut, ReduceMotion; events for binding.

All new styles and controls reference these; existing brushes (BgPrimary, TextPrimary, etc.) stay, extended with light theme and token-based spacing/radius.

---

## Data flow (unchanged)

- ViewModels get services via DI (existing + new ThemeService, MotionService, UndoRedoService, etc.).
- ProtocolHandler / ProfileSyncService / LocalMacroStorage remain source of truth for device and profiles/macros.
- New panels (Action Library, Combo Builder, Encoder Mapper) read/write through existing ViewModels or small extensions (e.g. ProfilesViewModel exposes ComboList, Encoder list; Macro timeline writes to MacroAsset/Steps).

---

## Navigation (screen map)

See **SCREEN_MAP.md**. Shell remains single-window; sidebar items: Devices, Profiles, Templates, Macros, Stats, Settings. Profiles page gains sub-areas: Key grid + Action Library, Layer tabs, Combo Builder, Encoder Mapper. Command palette overlays on top for global actions.

---

## Deliverables (implementation order)

1. **Foundation:** DesignTokens, ThemeService, AccentService, IconService, MotionService; merge into App; theme toggle in Settings + shell.
2. **Wpf.Ui integration:** Fluent styles where beneficial; snackbar for toasts; optional NavigationView.
3. **Pro editors:** Action Library, Macro Timeline, Layer tabs + Fn, Combo Builder, Encoder Mapper.
4. **UX patterns:** Command palette, empty states, undo/redo, conflict resolution UI.
5. **Premium effects:** Mica/Acrylic option, shadows, focus rings, rounded corners.
6. **Motion:** State journey, skeleton, push stepper, micro-interactions (all via MotionService).
7. **Gamification:** Setup journey, XP/badges, heatmap, toggles.
