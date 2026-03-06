# Micropad Premium UI — Motion Spec

## Principles

- **Purposeful:** Every animation supports state change or feedback (e.g. layer switch, drop target highlight).
- **Subtle:** Short durations; no childish or distracting motion.
- **Respect Reduce Motion:** When enabled, disable non-essential animations (keep only critical feedback where needed).

---

## Central authority: MotionService

- **ReduceMotion** (bool): When true, most decorative/transitional animations are skipped or reduced to 0 ms.
- **DurationFast:** 120 ms — micro-interactions (hover, press, toggle).
- **DurationNormal:** 160 ms — layer switch, tab change, panel open.
- **DurationSlow:** 200 ms — page transition, modal open, stepper step.
- **EasingOut:** Cubic ease-out (default for enter / “in”).
- **EasingInOut:** Cubic ease-in-out (for state changes that start and end).
- **EasingIn:** Cubic ease-in (for exit / “out” where appropriate).

When **ReduceMotion** is true:

- DurationFast → 0 (or 50 ms max for accessibility feedback).
- DurationNormal → 0.
- DurationSlow → 0 or 50 ms.
- Fade/slide/scale transitions for navigation and panels are disabled or instant.
- Keep: focus ring changes, error shake (can be shortened), and any animation required for accessibility (e.g. focus visibility).

---

## Usage by feature

| Feature | Animation | Duration | Easing | Reduce motion |
|---------|-----------|----------|--------|----------------|
| Layer tab switch | Fade + slide grid content | 120–160 ms | EasingOut | Skip (instant switch) |
| Profile card → editor | Connected transition (header/content) | 160 ms | EasingInOut | Skip |
| Drag action onto key | Key “snap + highlight” | 120 ms | EasingOut | Optional 50 ms |
| Macro block reorder | Drag placeholder + drop | 120 ms | EasingOut | Skip |
| Push to device stepper | Step progress (Preparing → … → Done) | — | — | Keep (informational) |
| Skeleton → content | Shimmer then fade-in | 200 ms fade | EasingOut | Skip shimmer; instant show |
| Connection flow | Stepper state change | 160 ms | EasingOut | Skip (instant state) |
| Hover (cards, buttons) | Slight elevation + highlight | 120 ms | EasingOut | Skip |
| Press (button) | Compress + ripple | 80–120 ms | EasingOut | Shorten to 50 ms |
| Toggle | Thumb slide | 120 ms | EasingOut | Skip |
| Error | Shake + inline message | 200 ms shake | — | Short shake only |
| Confetti (gamification) | Subtle burst | — | — | Disabled when ReduceMotion |

---

## Implementation notes

- WPF: Use `DoubleAnimation`, `ThicknessAnimation` with `EasingFunction` (e.g. `CubicEase`) and duration from MotionService.
- Binding: MotionService exposes properties (e.g. `DurationNormal`) and `ReduceMotion`; views/triggers read them (or a shared ViewModel wrapper) so changing Reduce Motion in Settings applies globally.
- No UI thread blocking: animations run on UI thread but are short; heavy work (sync, load) stays async with skeleton or stepper for feedback.
