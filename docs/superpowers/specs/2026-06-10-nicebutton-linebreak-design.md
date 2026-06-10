# NiceButton LineBreakMode — Design Spec

**Issue:** #31  
**Date:** 2026-06-10  
**Label:** patch

---

## Problem

`NiceButton._textLabel` has a hard-coded `LineBreakMode.TailTruncation`. There is no way for consumers to change this, and for vertical-orientation buttons (icon above/below text) the expected behavior is word wrap — not truncation.

Additionally, `LineBreakMode.WordWrap` has no effect unless the label is measured against a bounded width. The current `_contentHost` grid uses `HorizontalOptions = Center` (auto-size) and `Auto` column definitions, which gives the label infinite measurement width. This must be addressed alongside the property addition.

---

## Design

### New BindableProperty: `LineBreakMode`

```csharp
public static readonly BindableProperty LineBreakModeProperty = BindableProperty.Create(
    nameof(LineBreakMode), typeof(LineBreakMode?), typeof(NiceButton), null,
    propertyChanged: LineBreakModeChanged);

public LineBreakMode? LineBreakMode { get; set; }
```

Type: `LineBreakMode?` (nullable).  
Default: `null` — means **auto**, resolved by orientation at render time.

**Auto resolution:**
- `Horizontal` orientation → `TailTruncation`
- `Vertical` orientation → `WordWrap`

**Explicit value:** always wins regardless of orientation.

### Effective mode helper

A private helper computes the resolved value used internally:

```csharp
private LineBreakMode EffectiveLineBreakMode =>
    LineBreakMode ?? (Orientation == ButtonContentOrientation.Vertical
        ? LineBreakMode.WordWrap
        : LineBreakMode.TailTruncation);
```

### Layout changes in `RebuildContent()`

`WordWrap` only works when the label has a bounded width. `RebuildContent()` must set layout options dynamically based on `EffectiveLineBreakMode`:

**When `EffectiveLineBreakMode == WordWrap`:**
- `_contentHost.HorizontalOptions = LayoutOptions.Fill`
- Text column/row definition: `GridLength.Star`

**Otherwise (TailTruncation or other truncating modes):**
- `_contentHost.HorizontalOptions = LayoutOptions.Center`
- Text column/row definition: `GridLength.Auto`

The icon column/row always remains `Auto`.

### Update method

```csharp
private void UpdateLineBreakModeView()
{
    _textLabel.LineBreakMode = EffectiveLineBreakMode;
}
```

Called from:
- `LineBreakModeChanged` handler
- `RebuildContent()` (because orientation affects effective mode)
- Constructor (replaces the hard-coded `TailTruncation`)

### Constructor change

Remove the hard-coded `LineBreakMode = LineBreakMode.TailTruncation` from `_textLabel` initialization. Call `UpdateLineBreakModeView()` after `RebuildContent()` in the constructor (or inside `RebuildContent()` itself).

### `OrientationProperty` change handler

`OrientationProperty` already triggers `RebuildContent()` via `LayoutAffectingChanged`. Since `RebuildContent()` will call `UpdateLineBreakModeView()`, no extra wiring is needed for orientation changes.

---

## Acceptance criteria

- `NiceButton` with `Orientation="Vertical"` and long text wraps across multiple lines.
- `NiceButton` with `Orientation="Horizontal"` (default) truncates with tail truncation as before.
- Explicitly setting `LineBreakMode="WordWrap"` on a horizontal button enables wrapping.
- Explicitly setting `LineBreakMode="TailTruncation"` on a vertical button truncates.
- Short single-line text on any orientation renders identically to before.
- Layout/alignment remains visually stable.

---

## Out of scope

- `MaxLines` property (not in issue #31).
- Changes to `NiceButtonLayoutManager`.
- Demo app additions (validation done manually or in existing demo page).
