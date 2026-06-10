# NiceButton LineBreakMode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `LineBreakMode?` on `NiceButton` with orientation-aware auto-resolution (Vertical→WordWrap, Horizontal→TailTruncation) and fix the underlying layout so word wrap actually works.

**Architecture:** The nullable property defaults to auto-resolution via `EffectiveLineBreakMode`. `RebuildContent()` sets column `GridLength` (Star or Auto) and `_contentHost.HorizontalOptions` (Fill or Center) **statically** based on `EffectiveLineBreakMode`, and records the result in `internal bool WrapsText`. `NiceButtonLayoutManager.Measure` gates the two-pass logic on `WrapsText` — so icon-only buttons (where text never overflows) skip the extra pass. The non-wrap path is unchanged.

**Tech Stack:** .NET MAUI 10, C# 13, `NiceEntry/NiceButton.cs`, `NiceEntry/NiceButtonLayoutManager.cs`, `README.md`

---

### Task 1: Create feature branch

- [ ] **Create branch from master**

```powershell
git checkout master
git checkout -b feat/nicebutton-linebreak-mode
```

---

### Task 2: Add `LineBreakMode` BindableProperty and `EffectiveLineBreakMode`

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

The property is nullable so `null` means "auto" (orientation-driven). It reuses `LayoutAffectingChanged` as its handler because changing effective mode must rebuild the grid layout — not just flip a label flag.

The hard-coded `LineBreakMode.TailTruncation` in the constructor is intentionally left in place in this task. MAUI's `Label` default is `WordWrap`, so removing it before `UpdateLineBreakModeView()` is wired (Task 3) would change behavior in the intermediate commit. Both are removed in Task 3 as one atomic change.

- [ ] **Add the BindableProperty declaration** (place it with the other text-style properties, after `FontAttributesProperty`):

```csharp
public static readonly BindableProperty LineBreakModeProperty = BindableProperty.Create(
    nameof(LineBreakMode), typeof(Microsoft.Maui.LineBreakMode?), typeof(NiceButton), null,
    propertyChanged: LayoutAffectingChanged);
```

- [ ] **Add the CLR property** (place it with the other text-style CLR accessors, after `FontAttributes`):

```csharp
public Microsoft.Maui.LineBreakMode? LineBreakMode
{
    get => (Microsoft.Maui.LineBreakMode?)GetValue(LineBreakModeProperty);
    set => SetValue(LineBreakModeProperty, value);
}
```

- [ ] **Add `EffectiveLineBreakMode` and `UpdateLineBreakModeView()`** (place them with the other `Update*View()` helpers):

```csharp
internal Microsoft.Maui.LineBreakMode EffectiveLineBreakMode =>
    LineBreakMode ?? (Orientation == ButtonContentOrientation.Vertical
        ? Microsoft.Maui.LineBreakMode.WordWrap
        : Microsoft.Maui.LineBreakMode.TailTruncation);

private void UpdateLineBreakModeView()
    => _textLabel.LineBreakMode = EffectiveLineBreakMode;
```

- [ ] **Build to verify no errors**

```powershell
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: 0 errors. `UpdateLineBreakModeView` is not yet wired, so runtime behavior is unchanged (hard-coded TailTruncation still in constructor).

- [ ] **Commit**

```powershell
git add NiceEntry/NiceButton.cs
git commit -m "feat: add LineBreakMode BindableProperty to NiceButton"
```

---

### Task 3: Update `RebuildContent()` to drive layout from effective mode

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

`RebuildContent()` is refactored to (a) compute `wrap` from `EffectiveLineBreakMode` and use it to set column `GridLength` (Star or Auto) and `_contentHost.HorizontalOptions` (Fill or Center) **at build time**, and (b) call `UpdateLineBreakModeView()` at the end. No `_textColumnDef` field, no `SetWrappingLayout()` method.

The hard-coded `LineBreakMode.TailTruncation` is removed in this task because `UpdateLineBreakModeView()` now runs in the constructor via `RebuildContent()`.

**Key behavior:** Star columns under an infinite-width constraint return the same natural (hugging) size as Auto columns — so the static Star/Fill state doesn't affect measurement when the parent gives the button unconstrained width. The two-pass logic in Task 4 handles the constrained case.

- [ ] **Remove the hard-coded `LineBreakMode` from `_textLabel` initialization**

In the constructor block at `NiceButton.cs:101`, remove `LineBreakMode = LineBreakMode.TailTruncation,` from `_textLabel`'s initializer. After the change:

```csharp
_textLabel = new Label
{
    HorizontalTextAlignment = TextAlignment.Center,
    VerticalTextAlignment = TextAlignment.Center,
    HorizontalOptions = LayoutOptions.Center,
    VerticalOptions = LayoutOptions.Center,
    FontSize = FontSize
};
```

- [ ] **Add `WrapsText` field** near the other private fields (after `_tapInFlight`):

```csharp
internal bool WrapsText;
```

- [ ] **Replace the entire `RebuildContent()` method** with the version below:

```csharp
private void RebuildContent()
{
    _contentHost.Children.Clear();
    _contentHost.RowDefinitions.Clear();
    _contentHost.ColumnDefinitions.Clear();

    _iconLabel.Text = Icon.HasValue ? char.ConvertFromUtf32((int)Icon.Value) : null;
    _textLabel.Text = Text;

    var hasIcon = Icon.HasValue;
    var hasText = !string.IsNullOrEmpty(Text);

    _iconLabel.IsVisible = hasIcon;
    _textLabel.IsVisible = hasText;

    WrapsText = hasText && EffectiveLineBreakMode == Microsoft.Maui.LineBreakMode.WordWrap;

    if (!hasIcon && !hasText)
    {
        UpdateLineBreakModeView();
        return;
    }

    var textColumnWidth = WrapsText ? GridLength.Star : GridLength.Auto;
    _contentHost.HorizontalOptions = WrapsText ? LayoutOptions.Fill : LayoutOptions.Center;

    if (hasIcon && hasText)
    {
        var iconFirst = IconPlacement == IconPlacement.Start;
        var first = iconFirst ? (View)_iconLabel : _textLabel;
        var second = iconFirst ? (View)_textLabel : _iconLabel;

        if (Orientation == ButtonContentOrientation.Horizontal)
        {
            _contentHost.ColumnSpacing = Spacing;
            _contentHost.RowSpacing = 0;
            _contentHost.ColumnDefinitions.Add(
                new ColumnDefinition(iconFirst ? GridLength.Auto : textColumnWidth));
            _contentHost.ColumnDefinitions.Add(
                new ColumnDefinition(iconFirst ? textColumnWidth : GridLength.Auto));
            _contentHost.Add(first, 0, 0);
            _contentHost.Add(second, 1, 0);
        }
        else
        {
            // Vertical: single column (Star for wrap, Auto otherwise), two rows
            _contentHost.ColumnDefinitions.Add(new ColumnDefinition(textColumnWidth));
            _contentHost.RowSpacing = Spacing;
            _contentHost.ColumnSpacing = 0;
            _contentHost.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _contentHost.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _contentHost.Add(first, 0, 0);
            _contentHost.Add(second, 0, 1);
        }
    }
    else
    {
        if (hasText)
            _contentHost.ColumnDefinitions.Add(new ColumnDefinition(textColumnWidth));

        var only = hasIcon ? (View)_iconLabel : _textLabel;
        _contentHost.Add(only, 0, 0);
    }

    UpdateLineBreakModeView();
    InvalidateMeasure();
}
```

- [ ] **Build to verify**

```powershell
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: 0 errors.

- [ ] **Commit**

```powershell
git add NiceEntry/NiceButton.cs
git commit -m "feat: drive NiceButton column layout from EffectiveLineBreakMode in RebuildContent"
```

---

### Task 4: Two-pass measurement in `NiceButtonLayoutManager`

**Files:**
- Modify: `NiceEntry/NiceButtonLayoutManager.cs`

The layout manager adds a two-pass measure only for the WordWrap path. No layout properties are mutated during measure — the column state was set statically by `RebuildContent()`.

**Why two passes (`WrapsText` only):**
- Pass 1 with `double.PositiveInfinity`: Star columns behave like Auto under infinite constraint → returns the natural hugging size. If this fits within `widthConstraint`, the button hugs its content identically to today.
- Pass 2 with `widthConstraint` (only when pass 1 overflows): Star column apportions the real available width → label wraps and reports a taller desired size.

`WrapsText` is false for icon-only buttons, so they never enter the two-pass path regardless of orientation.

**Non-wrap path:** unchanged — measures with `widthConstraint` as before.

- [ ] **Replace `Measure` in `NiceButtonLayoutManager.cs`**:

```csharp
public Size Measure(double widthConstraint, double heightConstraint)
{
    var desired = new Size();
    foreach (var child in _button)
    {
        if (child.Visibility == Visibility.Collapsed) continue;

        Size size;
        if (_button.WrapsText
            && !double.IsPositiveInfinity(widthConstraint))
        {
            // Pass 1: natural size — Star columns act like Auto at infinite width
            var natural = child.Measure(double.PositiveInfinity, heightConstraint);
            // Pass 2 only when content overflows the real constraint
            size = natural.Width <= widthConstraint
                ? natural
                : child.Measure(widthConstraint, heightConstraint);
        }
        else
        {
            // Non-wrap: unchanged — measure with real constraint
            size = child.Measure(widthConstraint, heightConstraint);
        }

        desired = new Size(Math.Max(desired.Width, size.Width), Math.Max(desired.Height, size.Height));
    }

    if (_button.ForceSquare)
    {
        var side = Math.Max(desired.Width, desired.Height);
        desired = new Size(side, side);
    }

    return desired;
}
```

- [ ] **Build to verify**

```powershell
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: 0 errors.

- [ ] **Build the demo app too**

```powershell
dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj
```

Expected: 0 errors.

- [ ] **Commit**

```powershell
git add NiceEntry/NiceButtonLayoutManager.cs
git commit -m "feat: two-pass measure for WordWrap in NiceButtonLayoutManager"
```

---

### Task 5: Update README property table

**Files:**
- Modify: `README.md`

- [ ] **Add row to the NiceButton property table** (after the `FontAttributes` row at ~line 215):

Find:
```markdown
| Text | `FontAttributes` | `FontAttributes` | `None` |
```

Add after it:
```markdown
| Text | `LineBreakMode` | `LineBreakMode?` | `null` (auto: `WordWrap` for Vertical, `TailTruncation` for Horizontal) |
```

- [ ] **Commit**

```powershell
git add README.md
git commit -m "docs: document LineBreakMode property in NiceButton table"
```

---

### Task 6: Build verification and manual smoke test

- [ ] **Full build — both projects**

```powershell
dotnet build NiceEntry/NiceEntry.csproj && dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj
```

Expected: 0 errors.

- [ ] **Pack the library**

```powershell
dotnet pack NiceEntry/NiceEntry.csproj
```

Expected: package created under `./nupkgs/`.

- [ ] **Manual smoke test checklist (run demo app on Android or iOS simulator)**

Add a temporary XAML snippet or use the demo page:

```xml
<!-- Scenario 1: Horizontal default — short text, button must hug width -->
<nice:NiceButton Text="Buy now" HorizontalOptions="Center" />

<!-- Scenario 2: Horizontal default — long text at narrow width, must tail-truncate -->
<nice:NiceButton Text="This is a very long button label that should truncate"
                 WidthRequest="150" />

<!-- Scenario 3: Vertical default — long text at narrow width, must wrap -->
<nice:NiceButton Text="This is a very long button label that should wrap"
                 Orientation="Vertical"
                 WidthRequest="150" />

<!-- Scenario 4: Vertical + explicit TailTruncation — must truncate despite vertical -->
<nice:NiceButton Text="This should truncate even in vertical"
                 Orientation="Vertical"
                 LineBreakMode="TailTruncation"
                 WidthRequest="150" />

<!-- Scenario 5: Horizontal + explicit WordWrap — must wrap despite horizontal -->
<nice:NiceButton Text="This is a long text that should wrap on a horizontal button"
                 LineBreakMode="WordWrap"
                 WidthRequest="150" />

<!-- Scenario 6: Vertical short text — button must hug, NOT stretch full width -->
<nice:NiceButton Text="Short"
                 Orientation="Vertical"
                 HorizontalOptions="Center" />
```

Expected:
- Scenario 1: button width = text width (hugging), NOT full-width ✓
- Scenario 2: text truncated with "…" at 150 px ✓
- Scenario 3: text wraps, button taller than S2 ✓
- Scenario 4: text truncated even on vertical button ✓
- Scenario 5: text wraps on horizontal button ✓
- Scenario 6: button width = "Short" width, NOT 150 px ✓

- [ ] **Commit any fixes needed; otherwise branch is ready**

---

### Task 7: Open pull request

- [ ] **Push branch**

```powershell
git push -u origin feat/nicebutton-linebreak-mode
```

- [ ] **Update issue label from `patch` to `minor` before creating PR**

```powershell
gh issue edit 31 --remove-label "patch" --add-label "minor"
```

- [ ] **Create PR**

```powershell
gh pr create `
  --title "feat: add LineBreakMode to NiceButton with orientation-aware wrapping" `
  --body "Closes #31

## Changes
- New nullable \`LineBreakMode\` BindableProperty (\`null\` = auto: \`WordWrap\` for Vertical, \`TailTruncation\` for Horizontal)
- Explicit value always wins regardless of \`Orientation\`
- \`RebuildContent()\` sets column \`GridLength\` (Star/Auto) and \`_contentHost.HorizontalOptions\` (Fill/Center) statically from \`EffectiveLineBreakMode\` — no layout mutations during measure
- \`NiceButtonLayoutManager.Measure\` uses two-pass strategy for WordWrap: natural measure first (Star acts as Auto at infinity → button hugs short text), bounded re-measure only when content overflows
- Non-wrap measurement path unchanged
- README property table updated" `
  --label "enhancement,minor"
```

---

## Self-Review

**Spec coverage:**

| Spec requirement | Task |
|---|---|
| `LineBreakMode?` BindableProperty, default `null` | Task 2 |
| `EffectiveLineBreakMode` qualified enum names | Task 2 |
| `LayoutAffectingChanged` as handler | Task 2 |
| `UpdateLineBreakModeView()` called from `RebuildContent()` | Task 3 |
| Hard-coded `TailTruncation` removed (same commit as wiring) | Task 3 |
| `WrapsText` field set in `RebuildContent()` | Task 3 |
| Column `GridLength` (`textColumnWidth`) and `HorizontalOptions` set from `WrapsText` | Task 3 |
| Text-only and vertical icon+text cases have explicit column def | Task 3 |
| Icon-only: `WrapsText = false` → HorizontalOptions stays Center | Task 3 |
| `NiceButtonLayoutManager` two-pass gated on `WrapsText` (not re-computed) | Task 4 |
| Non-wrap path unchanged | Task 4 |
| README row | Task 5 |
| Smoke test all 6 acceptance criteria incl. hug for short text | Task 6 |

**Placeholder scan:** None found. ✓

**Type consistency:**
- `EffectiveLineBreakMode` → `Microsoft.Maui.LineBreakMode` (non-nullable) used in both NiceButton and NiceButtonLayoutManager ✓
- No `SetWrappingLayout()` call sites (method removed from design) ✓
- No `_textColumnDef` references (field removed from design) ✓
