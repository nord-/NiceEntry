# NiceButton LineBreakMode — Design Spec

**Issue:** #31  
**Date:** 2026-06-10  
**Semver:** minor (new public API + changed default behavior for vertical buttons)

---

## Problem

`NiceButton._textLabel` has a hard-coded `LineBreakMode.TailTruncation`. There is no consumer control over this, and for vertical-orientation buttons the expected behavior is word wrap.

Two issues must be addressed together:

1. **Missing API** — `LineBreakMode` is not exposed as a `BindableProperty`.
2. **Layout blocks wrapping** — `_contentHost` uses `Auto` column definitions, giving the label infinite measurement width. `WordWrap` has no effect without a bounded width constraint. Fixing this requires changes to `NiceButtonLayoutManager`.

---

## Design

### New BindableProperty: `LineBreakMode`

```csharp
public static readonly BindableProperty LineBreakModeProperty = BindableProperty.Create(
    nameof(LineBreakMode), typeof(Microsoft.Maui.LineBreakMode?), typeof(NiceButton), null,
    propertyChanged: LayoutAffectingChanged);

public Microsoft.Maui.LineBreakMode? LineBreakMode
{
    get => (Microsoft.Maui.LineBreakMode?)GetValue(LineBreakModeProperty);
    set => SetValue(LineBreakModeProperty, value);
}
```

Type: `LineBreakMode?` (nullable). Default: `null` — means **auto**, resolved by orientation.

**Auto resolution:**
- `Horizontal` orientation → `TailTruncation`
- `Vertical` orientation → `WordWrap`

**Explicit value:** always wins regardless of orientation.

The property reuses `LayoutAffectingChanged` as its handler (same as `OrientationProperty`), because changing the effective mode requires rebuilding the grid layout — not just setting a flag on the label.

### Effective mode helper

```csharp
internal Microsoft.Maui.LineBreakMode EffectiveLineBreakMode =>
    LineBreakMode ?? (Orientation == ButtonContentOrientation.Vertical
        ? Microsoft.Maui.LineBreakMode.WordWrap
        : Microsoft.Maui.LineBreakMode.TailTruncation);
```

The enum type is fully qualified to avoid the naming conflict with the property `LineBreakMode`.

### `UpdateLineBreakModeView()`

```csharp
private void UpdateLineBreakModeView()
    => _textLabel.LineBreakMode = EffectiveLineBreakMode;
```

Called from `RebuildContent()` (which already runs whenever `LayoutAffectingChanged` fires).

### Constructor change

Remove the hard-coded `LineBreakMode = LineBreakMode.TailTruncation` from `_textLabel` initialization. The value is set via `UpdateLineBreakModeView()` inside `RebuildContent()`.

---

## Layout: two-pass measurement in `NiceButtonLayoutManager`

`NiceButtonLayoutManager` is **in scope**. Setting `Star` columns + `Fill` options on `_contentHost` unconditionally would report the full constraint width as the desired size even for short text, breaking single-line button sizing. A two-pass approach avoids this.

**Pass 1 — natural (unconstrained):**
Measure `_border` with `double.PositiveInfinity` as width. This gives the content's intrinsic width regardless of wrapping.

**Decision:**
- If `natural.Width ≤ widthConstraint` **or** `EffectiveLineBreakMode != WordWrap`: use the natural measurement as desired size (button hugs content, identical to today).
- If `natural.Width > widthConstraint` **and** `EffectiveLineBreakMode == WordWrap`: proceed to pass 2.

**Pass 2 — constrained:**
Before measuring: set `_contentHost.HorizontalOptions = Fill` and the text column definition to `Star` (so the Grid propagates the bounded constraint to the label). Measure `_border` with `widthConstraint`. The label now wraps. Report this as desired size; leave the column in `Star`/`Fill` state so `ArrangeChildren` lays out correctly.

After pass 2, reset `_contentHost.HorizontalOptions = Center` and text column to `Auto` at the end of arrange — or keep a flag on `NiceButton` (`internal bool _wrappingActive`) that `RebuildContent()` and the layout manager both read, to avoid re-measuring on arrange.

`NiceButtonLayoutManager` accesses `_button.EffectiveLineBreakMode` directly (it already accesses `_button.ForceSquare`).

### Text-only case

`RebuildContent()`'s else-branch (icon-only or text-only, no explicit column definitions) must also set `_contentHost.HorizontalOptions`:
- When effective mode is `WordWrap`: `Fill`
- Otherwise: `Center`

Without this, the implicit single-column grid still receives an infinite constraint and wrapping is suppressed.

---

## Scope

| In scope | Out of scope |
|---|---|
| `LineBreakMode` BindableProperty | `MaxLines` property |
| `UpdateLineBreakModeView()` helper | Unrelated layout cleanup |
| `NiceButtonLayoutManager` two-pass measure | Demo app changes (issue validation can be done manually) |
| README `NiceButton` property table — add row for `LineBreakMode` | |

**Note on demo/validation:** Issue #31 requests demo cases (short title, long wrapping title, narrow button). These are not added to the demo app as part of this change but can be validated manually against the acceptance criteria below.

---

## Acceptance criteria

1. `NiceButton` with `Orientation="Vertical"` and long text wraps across multiple lines.
2. `NiceButton` with default `Orientation="Horizontal"` truncates with tail truncation, identical to current behavior.
3. Explicitly setting `LineBreakMode="WordWrap"` on a horizontal button enables wrapping.
4. Explicitly setting `LineBreakMode="TailTruncation"` on a vertical button truncates.
5. Short single-line text on any orientation renders with identical size/alignment to today (two-pass ensures this).
6. Changing `Orientation` at runtime updates the effective line break mode when `LineBreakMode` is `null`.
