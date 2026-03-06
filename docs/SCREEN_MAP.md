# Micropad Premium UI — Screen Map (Navigation)

## Shell (single window)

```
┌─────────────────────────────────────────────────────────────────┐
│ [Logo] MICROPAD                    [Theme] [Ctrl+K]              │
├──────────────┬──────────────────────────────────────────────────┤
│              │                                                    │
│  Devices     │  Content area (Frame / NavigationView content)     │
│  Profiles    │                                                    │
│  Templates   │  • One main view per nav item                      │
│  Macros      │  • Profiles has sub-areas (grid, library, combos,   │
│  Stats       │    encoders) in one scrollable or tabbed layout     │
│  Settings    │                                                    │
│              │                                                    │
├──────────────┴──────────────────────────────────────────────────┤
│ Status: … | Last sync: …  [Connection] [Battery] v1.0.0          │
└─────────────────────────────────────────────────────────────────┘
```

- **Overlay:** Command palette (Ctrl+K) appears on top of content; non-modal; Esc to close.
- **Snackbar/Toasts:** Bottom or top-right (Wpf.Ui Snackbar); for “Undo: …”, “Profile pushed”, errors.

---

## Screen breakdown

| Nav item | Main content | Key sub-areas / modals |
|----------|--------------|--------------------------|
| **Devices** | Device list, scan, connect | Connection stepper (Scan → Found → Pairing → Ready); diagnostics export; empty: “Connect your device to sync profiles” |
| **Profiles** | Profile list + editor | **Left:** Action Library (search, categories, favorites, drag-drop). **Center:** Key grid with Layer 0/1/2 tabs, Fn indicator. **Right (or below):** Combo Builder, Encoder Mapper. Modals: Action edit, Conflict resolution (PC vs device) |
| **Templates** | Preset cards (VS Code, etc.) | “Use this preset”; gallery view; empty: “Use a template to get started”; Preset marketplace (local export/import) |
| **Macros** | Macro list + editor | **Macro Timeline Editor:** blocks (KeyDown, Text, Delay, Loop, etc.), inspector, Play/Stop; empty: “Create your first macro” |
| **Stats** | Counts, uptime, encoders | Optional heatmap, “time saved”; gamification hooks |
| **Settings** | Options list | Theme, Accent, Reduce Motion, Background (Solid/Acrylic/Mica), Gamification toggles; process–profile rules; default profile; debounce |

---

## Empty states (teaching)

- **Devices:** “Connect your device to sync profiles” + [Scan for devices].
- **Profiles (no profiles):** “Create your first profile” + [Create profile].
- **Profiles (no keys assigned):** “Drag an action onto a key to begin” (+ Action Library visible).
- **Action Library (no search results):** “Try searching for ‘volume’ or ‘copy’”.
- **Macros (no macros):** “Create your first macro” or “Click + to add a step, or record a macro.”
- **Combos:** “Combos let two keys trigger a new action.” + [Record Combo].
- **Templates:** “Use a template to get started” + preset cards.

---

## Command palette (Ctrl+K)

- Actions: “Create Profile”, “Push to Device”, “Find Macro…”, “Switch Profile…”, “Open Settings”, “Scan for devices”, etc.
- Recent + pinned; keyboard Up/Down + Enter.
- Integrates with ProfileSyncService, ProtocolHandler, LocalMacroStorage (search), etc.

---

## Profile conflict resolution

- When sync detects same ProfileId, different version/hash: open **modal**.
- Left: PC version (name, version, modified); Right: Device version.
- Buttons: **Pull** (device wins), **Push** (PC wins), **Keep both** (duplicate with new id).
- Clear copy on what will happen.

---

## HUD overlay (existing, enhanced)

- Top-right; topmost; ~1.2 s.
- Text e.g. “Premiere • Layer 2 • Encoder: Jog”.
- Optional click-through; non-focus-stealing.
