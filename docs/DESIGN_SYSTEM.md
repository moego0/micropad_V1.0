# Micropad Design System

## Design tokens

Defined in `Micropad.App/DesignSystem/DesignTokens.xaml` and merged into `App.xaml`.

### Spacing (Thickness)

| Key        | Use                    |
|-----------|------------------------|
| Spacing4  | Tight inline (4px)     |
| Spacing8  | Inline (8px)           |
| Spacing12 | Default gap (12px)     |
| Spacing16 | Section gap (16px)     |
| Spacing24 | Page margin (24px)     |
| Padding4–24 | Same values as padding |

### Corner radius

| Key      | Use              |
|----------|-------------------|
| Radius4  | Buttons, inputs   |
| Radius8  | Cards, panels      |
| Radius10 | Standard (dialogs)|
| Radius12 | Key grid, large cards |

### Typography (styles)

| Style        | FontSize | Use           |
|-------------|----------|---------------|
| CaptionText | 11       | Captions, hints |
| BodyText    | 14       | Body copy     |
| SubtitleText| 16      | Section titles|
| TitleText   | 20       | Page titles   |

Use: `Style="{StaticResource TitleText}"` and set `Foreground` in view (e.g. `TextPrimary`).

### Shadows

- **ShadowCard** – default card elevation  
- **ShadowCardHover** – hover state  
- **ShadowFocusRing** – accent focus ring  

---

## Motion (MotionService)

- **DurationFastMs** – 120 ms (0 when Reduce Motion)  
- **DurationNormalMs** – 160 ms (0 when Reduce Motion)  
- **DurationSlowMs** – 200 ms (0 when Reduce Motion)  
- **DurationMinimalMs** – 50 ms (accessibility feedback when Reduce Motion is on)  
- **ReduceMotion** – persisted in settings; when true, disable or shorten non-essential animations.

Use these values for `DoubleAnimation`/`Storyboard` durations and respect **ReduceMotion** in triggers.

---

## Theme (ThemeService)

- **IsDark** – `true` = Dark, `false` = Light.  
- Persisted in settings; applies Wpf.Ui `ApplicationThemeManager` so Fluent controls follow theme.

---

## Accent (AccentService)

- **AccentColor** / **AccentBrush** – used for focus rings, selected key, primary actions.  
- Persisted as hex in settings.

---

## Icons (IconService)

- `IconService.GetSymbol(name, sizePx, foreground)` – returns a `TextBlock` with Segoe Fluent Icons.  
- Names: `Devices`, `Profiles`, `Templates`, `Macros`, `Stats`, `Settings`, `Search`, `Add`, `Key`, `Play`, `Stop`, etc.  
- Sizes: 16, 20, 24.

---

## How to add new actions / commands

1. **Command palette:** In `MainWindow.RegisterCommandPalette()` add:
   ```csharp
   _commandPalette.Register(new CommandPaletteEntry {
       Id = "my-action",
       Title = "My action",
       Subtitle = "Optional description",
       Action = () => { ... }
   });
   ```
2. **Macros / presets:** Add new step types or preset definitions in the respective ViewModels and storage (see `REQUIREMENTS_SYNC.md` and protocol spec).
