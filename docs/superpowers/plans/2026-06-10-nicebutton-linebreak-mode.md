# NiceButton LineBreakMode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `LineBreakMode?` on `NiceButton` with orientation-aware auto-resolution (Vertical→WordWrap, Horizontal→TailTruncation) and fix the underlying layout so word wrap actually works.

**Architecture:** The nullable property defaults to auto-resolution via `EffectiveLineBreakMode`. `RebuildContent()` tracks an explicit `_textColumnDef` so `SetWrappingLayout()` can toggle between `Auto`/`Center` (hugging) and `Star`/`Fill` (bounded). `NiceButtonLayoutManager` does a two-pass measure to preserve hugging behavior for short text while enabling wrap when content overflows.

**Tech Stack:** .NET MAUI 10, C# 13, `NiceEntry/NiceButton.cs`, `NiceEntry/NiceButtonLayoutManager.cs`, `README.md`

---

### Task 1: Create feature branch

- [ ] **Create branch from master**

```powershell
git checkout master
git checkout -b fix/nicebutton-linebreak-mode
```

---

### Task 2: Add `LineBreakMode` BindableProperty and `EffectiveLineBreakMode`

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

The property is nullable so `null` means "auto" (orientation-driven). It reuses `LayoutAffectingChanged` as its handler because changing effective mode must rebuild the grid layout — not just flip a label flag.

- [ ] **Remove the hard-coded `LineBreakMode` from `_textLabel` initialization**

In the constructor block starting at line 95, remove `LineBreakMode = LineBreakMode.TailTruncation,` from `_textLabel`'s initializer. After the change the initializer should be:

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

Expected: build succeeds (0 errors). `UpdateLineBreakModeView` is not yet called anywhere, so behavior is unchanged at this point.

- [ ] **Commit**

```powershell
git add NiceEntry/NiceButton.cs
git commit -m "feat: add LineBreakMode BindableProperty to NiceButton"
```

---

### Task 3: Update `RebuildContent()` and add `SetWrappingLayout()`

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

`RebuildContent()` must (a) always create an explicit text `ColumnDefinition` stored in `_textColumnDef` so `SetWrappingLayout()` can toggle its `Width`, and (b) call `UpdateLineBreakModeView()` at the end. `SetWrappingLayout()` flips `_contentHost.HorizontalOptions` and the column width; it is called by `NiceButtonLayoutManager` during measure.

- [ ] **Add `_textColumnDef` field** near the other private fields (after `_tapInFlight`):

```csharp
private ColumnDefinition? _textColumnDef;
```

- [ ] **Replace the entire `RebuildContent()` method** with the version below.

Key changes vs today:
- icon+text horizontal: text `ColumnDefinition` is stored in `_textColumnDef`
- icon+text vertical: add an explicit `ColumnDefinition(Auto)` (one column, two rows) stored in `_textColumnDef` — this lets `SetWrappingLayout` make it `Star` for wrapping
- text-only: add an explicit `ColumnDefinition(Auto)` stored in `_textColumnDef`
- icon-only: `_textColumnDef = null` (no text to wrap)
- `_contentHost.HorizontalOptions` is set based on effective line break mode
- `UpdateLineBreakModeView()` is called at the end

```csharp
private void RebuildContent()
{
    _contentHost.Children.Clear();
    _contentHost.RowDefinitions.Clear();
    _contentHost.ColumnDefinitions.Clear();
    _textColumnDef = null;

    _iconLabel.Text = Icon.HasValue ? char.ConvertFromUtf32((int)Icon.Value) : null;
    _textLabel.Text = Text;

    var hasIcon = Icon.HasValue;
    var hasText = !string.IsNullOrEmpty(Text);

    _iconLabel.IsVisible = hasIcon;
    _textLabel.IsVisible = hasText;

    if (!hasIcon && !hasText)
    {
        UpdateLineBreakModeView();
        return;
    }

    if (hasIcon && hasText)
    {
        var iconFirst = IconPlacement == IconPlacement.Start;
        var first = iconFirst ? (View)_iconLabel : _textLabel;
        var second = iconFirst ? (View)_textLabel : _iconLabel;

        if (Orientation == ButtonContentOrientation.Horizontal)
        {
            var textCol = new ColumnDefinition(GridLength.Auto);
            _textColumnDef = textCol;

            _contentHost.ColumnSpacing = Spacing;
            _contentHost.RowSpacing = 0;
            _contentHost.ColumnDefinitions.Add(
                iconFirst ? new ColumnDefinition(GridLength.Auto) : textCol);
            _contentHost.ColumnDefinitions.Add(
                iconFirst ? textCol : new ColumnDefinition(GridLength.Auto));
            _contentHost.Add(first, 0, 0);
            _contentHost.Add(second, 1, 0);
        }
        else
        {
            // Vertical: one column (tracked for wrapping), two rows
            var col = new ColumnDefinition(GridLength.Auto);
            _textColumnDef = col;

            _contentHost.ColumnDefinitions.Add(col);
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
        var only = hasIcon ? (View)_iconLabel : _textLabel;

        if (hasText)
        {
            // Text-only: explicit column so SetWrappingLayout can toggle it
            var col = new ColumnDefinition(GridLength.Auto);
            _textColumnDef = col;
            _contentHost.ColumnDefinitions.Add(col);
        }

        _contentHost.Add(only, 0, 0);
    }

    // Apply wrapping layout state based on effective line break mode
    SetWrappingLayout(EffectiveLineBreakMode == Microsoft.Maui.LineBreakMode.WordWrap);
    UpdateLineBreakModeView();
    InvalidateMeasure();
}
```

- [ ] **Add `SetWrappingLayout()`** in the `Update*View()` section:

```csharp
internal void SetWrappingLayout(bool wrapping)
{
    _contentHost.HorizontalOptions = wrapping ? LayoutOptions.Fill : LayoutOptions.Center;
    if (_textColumnDef is not null)
        _textColumnDef.Width = wrapping ? GridLength.Star : GridLength.Auto;
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
git commit -m "refactor: track text column def and drive wrapping layout from RebuildContent"
```

---

### Task 4: Two-pass measurement in `NiceButtonLayoutManager`

**Files:**
- Modify: `NiceEntry/NiceButtonLayoutManager.cs`

The layout manager already accesses `_button.ForceSquare`; extend it to use `_button.EffectiveLineBreakMode` and `_button.SetWrappingLayout()`.

**Why two passes:** `SetWrappingLayout(true)` gives the label a bounded width (Star column + Fill contentHost), causing the label to report wrapping height. But for short text that fits in the available width, we want the button to hug its content — which requires keeping `Auto`/`Center`. A single pass can't do both. So:
1. Pass 1 (natural): reset to `Auto`/`Center`, measure with `double.PositiveInfinity`. Gets intrinsic width.
2. If the natural width overflows the constraint AND effective mode is WordWrap → Pass 2: switch to `Star`/`Fill`, measure with the real `widthConstraint`. Gets wrapping height.

- [ ] **Replace `Measure` in `NiceButtonLayoutManager.cs`**:

```csharp
public Size Measure(double widthConstraint, double heightConstraint)
{
    var desired = new Size();
    foreach (var child in _button)
    {
        if (child.Visibility == Visibility.Collapsed) continue;

        // Pass 1: natural (unconstrained) measurement
        _button.SetWrappingLayout(false);
        var natural = child.Measure(double.PositiveInfinity, heightConstraint);

        Size size;
        if (_button.EffectiveLineBreakMode == Microsoft.Maui.LineBreakMode.WordWrap
            && !double.IsPositiveInfinity(widthConstraint)
            && natural.Width > widthConstraint)
        {
            // Pass 2: bounded measurement — text will wrap
            _button.SetWrappingLayout(true);
            size = child.Measure(widthConstraint, heightConstraint);
        }
        else
        {
            size = natural;
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
git commit -m "fix: two-pass measure in NiceButtonLayoutManager to enable WordWrap without breaking short-text hug"
```

---

### Task 5: Update README property table

**Files:**
- Modify: `README.md`

Add a row for `LineBreakMode` in the NiceButton property table (line ~213, after the `FontAttributes` row):

- [ ] **Add row to the property table**

Find this line in the table:

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

Expected: 0 errors, 0 warnings about new code.

- [ ] **Pack the library**

```powershell
dotnet pack NiceEntry/NiceEntry.csproj
```

Expected: package created under `./nupkgs/`.

- [ ] **Manual smoke test checklist (run demo app on Android or iOS simulator)**

Verify each scenario in the demo app or a temporary XAML snippet:

```xml
<!-- Scenario 1: Horizontal default — short text, should hug width as today -->
<nice:NiceButton Text="Buy now" />

<!-- Scenario 2: Horizontal default — long text, should tail-truncate -->
<nice:NiceButton Text="This is a very long button label that should truncate"
                 WidthRequest="150" />

<!-- Scenario 3: Vertical default — long text, should wrap (WordWrap auto) -->
<nice:NiceButton Text="This is a very long button label that should wrap"
                 Orientation="Vertical"
                 WidthRequest="150" />

<!-- Scenario 4: Vertical explicit TailTruncation — should truncate despite vertical -->
<nice:NiceButton Text="This should truncate even in vertical"
                 Orientation="Vertical"
                 LineBreakMode="TailTruncation"
                 WidthRequest="150" />

<!-- Scenario 5: Horizontal explicit WordWrap — should wrap despite horizontal -->
<nice:NiceButton Text="This is a long text that should wrap on a horizontal button"
                 LineBreakMode="WordWrap"
                 WidthRequest="150" />

<!-- Scenario 6: Vertical short text — button must hug content, NOT stretch full width -->
<nice:NiceButton Text="Short"
                 Orientation="Vertical"
                 HorizontalOptions="Center" />
```

Expected results:
- Scenario 1: button width matches text width (hugging) ✓
- Scenario 2: text ends with "…" at 150px width ✓
- Scenario 3: text wraps across lines, button taller than scenario 2 ✓
- Scenario 4: text truncates even though orientation is Vertical ✓
- Scenario 5: text wraps across lines on a horizontal button ✓
- Scenario 6: button hugs "Short" text — NOT 150px wide ✓

- [ ] **Commit if any last-minute fixes were needed; otherwise the branch is ready**

---

### Task 7: Open pull request

- [ ] **Push branch**

```powershell
git push -u origin fix/nicebutton-linebreak-mode
```

- [ ] **Create PR**

```powershell
gh pr create `
  --title "fix: add LineBreakMode to NiceButton with orientation-aware wrapping" `
  --body "Closes #31

## Changes
- New nullable \`LineBreakMode\` BindableProperty on \`NiceButton\` (\`null\` = auto, resolved by \`Orientation\`)
- \`Vertical\` orientation defaults to \`WordWrap\`; \`Horizontal\` retains \`TailTruncation\`
- Explicit value always wins regardless of orientation
- Fixed underlying layout: \`RebuildContent()\` tracks the text \`ColumnDefinition\` and \`SetWrappingLayout()\` toggles between \`Auto\`/\`Center\` (hugging) and \`Star\`/\`Fill\` (bounded)
- \`NiceButtonLayoutManager.Measure\` uses two-pass strategy: natural measure first, bounded re-measure only when content overflows and \`WordWrap\` is active — preserves button hugging for short text
- README property table updated

## Labels
\`minor\` (new public API + changed default for vertical buttons)" `
  --label "enhancement,minor"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Task |
|---|---|
| `LineBreakMode?` BindableProperty, default `null` | Task 2 |
| `EffectiveLineBreakMode` helper (qualified enum names) | Task 2 |
| `LayoutAffectingChanged` as handler (triggers `RebuildContent`) | Task 2 |
| `UpdateLineBreakModeView()` called from `RebuildContent()` | Task 3 |
| Constructor hard-coded `TailTruncation` removed | Task 2 |
| Track `_textColumnDef` for all cases (icon+text H, icon+text V, text-only) | Task 3 |
| `SetWrappingLayout(bool)` toggles `_contentHost.HorizontalOptions` + column width | Task 3 |
| Text-only case: explicit column def so wrapping can be toggled | Task 3 |
| `NiceButtonLayoutManager` two-pass: natural first, bounded re-measure on overflow+WordWrap | Task 4 |
| README property table row | Task 5 |
| Manual smoke test covering all 6 acceptance criteria | Task 6 |

**No gaps found.** All spec requirements are covered.

**Placeholder scan:** No TBD, no "similar to", no missing code blocks. ✓

**Type consistency:**
- `EffectiveLineBreakMode` returns `Microsoft.Maui.LineBreakMode` (non-nullable) — used by `NiceButtonLayoutManager` as `Microsoft.Maui.LineBreakMode.WordWrap` comparison ✓
- `SetWrappingLayout(bool)` signature matches all call sites ✓
- `_textColumnDef` is `ColumnDefinition?` — `null`-guarded in `SetWrappingLayout` ✓
