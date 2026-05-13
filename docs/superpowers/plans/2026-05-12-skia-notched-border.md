# SkiaSharp Notched Border Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current label-overlay mask (which assumes parent background = pure White/Black) with a custom-drawn outlined border that has a real gap where the floating label sits — eliminating the page-background coupling that issue #12 surfaced.

**Architecture:** Introduce SkiaSharp via `SkiaSharp.Views.Maui.Controls` and add a new internal `NotchedBorder` control that renders the rounded-rectangle stroke with a gap segment centered around the label's measured bounds. `LabelBase.xaml` swaps its `<Border x:Name="BorderLabel">` for the new control; `LabelContainer` becomes `BackgroundColor="Transparent"` and the existing `LabelBackgroundColor` shortcut from PR #13 is either deprecated to a no-op or kept as escape hatch for non-SKIA fallbacks.

**Tech Stack:** .NET MAUI 10, SkiaSharp.Views.Maui.Controls, C# 12, MAUI BindableProperty pattern.

---

## Execution context (resolved 2026-05-13)

The pre-flight decisions are now settled:

- **PR #13 is the vehicle.** Continue on branch `fix/label-background-transparent`. The Skia work replaces the `LabelBackgroundColor` approach inside the same PR; the PR's scope grows but its target version stays `1.5.1` (label is `patch`).
- **`LabelBackgroundColor` is removed entirely.** No "obsolete no-op" — the property is deleted, along with the `UpdateLabelContainerBackground()` coordinator that PR #13 added. The notched border makes it unnecessary.
- **README:** keep the `LabeledAutoCompleteEntry` section (it's retroactive 1.5.0 doc that was missing). Delete the "Custom page backgrounds" section (no longer relevant). Add the SkiaSharp setup note (Task 6).
- **Version stays 1.5.1.** Release workflow handles the bump via PR's `patch` label on merge.
- **Scope:** still only `LabelBase`'s outer border. `LabeledAutoCompleteEntry`'s dropdown stays a plain `Border`.

---

## File Structure

**New files:**
- `NiceEntry/Drawing/NotchedBorder.cs` — `ContentView` subclass that hosts an `SKCanvasView` and exposes bindable `StrokeColor`, `StrokeThickness`, `CornerRadius`, `NotchStart`, `NotchEnd`, `NotchPadding`. One responsibility: draw a rounded rectangle with one gap on the top edge.
- `NiceEntry/Drawing/NotchedBorderDrawing.cs` — pure-function helper that builds an `SKPath` from the bindable inputs. Separated from the view so it's testable without a renderer.

**Modified:**
- `NiceEntry/NiceEntry.csproj` — add `SkiaSharp.Views.Maui.Controls` package reference.
- `NiceEntry/Base/LabelBase.xaml` — replace `<Border x:Name="BorderLabel">` with `<drawing:NotchedBorder x:Name="BorderLabel">`. Remove `LabelContainer.BackgroundColor` AppThemeBinding (set to `Transparent`). Remove the `BaseBorderBasic` style or repoint it.
- `NiceEntry/Base/LabelBase.xaml.cs` — rewire `BorderLabel` access; add wiring so the notch width follows `LabelContainer`'s actual measured width on `SizeChanged`. Update `ChangeBorderColor()` to set `NotchedBorder.StrokeColor` instead of `Border.Stroke`. Make `LabelBackgroundColor` from PR #13 a no-op (kept for source compat) or delete.
- `NiceEntry/MauiProgramExtensions.cs` (NEW or extend existing) — register `UseSkiaSharp()` if SkiaSharp requires it. Document for consumers.
- `README.md` — document the SkiaSharp dependency and the `UseSkiaSharp()` builder call if needed.

---

## Task 1: Add SkiaSharp package and verify it builds on both targets

**Files:**
- Modify: `NiceEntry/NiceEntry.csproj`

- [ ] **Step 1: Add the package reference**

```xml
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="3.119.0" />
```

(Verify the latest stable 3.x at https://www.nuget.org/packages/SkiaSharp.Views.Maui.Controls before pinning.)

- [ ] **Step 2: Restore and build both targets**

```bash
dotnet restore NiceEntry/NiceEntry.csproj
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: builds for `net10.0-android` and `net10.0-ios` with 0 errors. SkiaSharp ships native bins per RID — confirm both targets resolve them.

- [ ] **Step 3: Commit**

```bash
git add NiceEntry/NiceEntry.csproj
git commit -m "chore: add SkiaSharp.Views.Maui.Controls dependency"
```

---

## Task 2: Pure path-building helper

**Files:**
- Create: `NiceEntry/Drawing/NotchedBorderDrawing.cs`

This is a static method that takes geometry + notch bounds and returns an `SKPath`. No UI, no MAUI types. Easier to reason about and (eventually) unit-testable.

- [ ] **Step 1: Implement the path builder**

```csharp
using SkiaSharp;

namespace NiceEntry.Drawing;

internal static class NotchedBorderDrawing
{
    /// <summary>
    /// Builds a rounded-rectangle stroke path with a gap on the top edge
    /// between <paramref name="notchStart"/> and <paramref name="notchEnd"/>
    /// (both in pixel coordinates relative to the canvas). If the notch span
    /// is zero or outside the rectangle, returns a plain rounded rectangle.
    /// </summary>
    public static SKPath BuildPath(
        float width,
        float height,
        float cornerRadius,
        float strokeThickness,
        float notchStart,
        float notchEnd)
    {
        // Inset by half the stroke so the stroke renders inside the bounds.
        var inset = strokeThickness / 2f;
        var left = inset;
        var top = inset;
        var right = width - inset;
        var bottom = height - inset;
        var r = Math.Max(0, cornerRadius - inset);

        var path = new SKPath();

        var notchActive = notchEnd > notchStart
            && notchStart > left + r
            && notchEnd < right - r;

        // Top-left corner → start of notch (or right side of top arc if no notch)
        path.MoveTo(left, top + r);
        path.ArcTo(new SKRect(left, top, left + 2 * r, top + 2 * r), 180, 90, false);

        if (notchActive)
        {
            path.LineTo(notchStart, top);
            // Gap — move (don't draw) across the notch.
            path.MoveTo(notchEnd, top);
            path.LineTo(right - r, top);
        }
        else
        {
            path.LineTo(right - r, top);
        }

        // Top-right corner
        path.ArcTo(new SKRect(right - 2 * r, top, right, top + 2 * r), 270, 90, false);
        // Right edge
        path.LineTo(right, bottom - r);
        // Bottom-right corner
        path.ArcTo(new SKRect(right - 2 * r, bottom - 2 * r, right, bottom), 0, 90, false);
        // Bottom edge
        path.LineTo(left + r, bottom);
        // Bottom-left corner
        path.ArcTo(new SKRect(left, bottom - 2 * r, left + 2 * r, bottom), 90, 90, false);
        // Left edge
        path.LineTo(left, top + r);

        return path;
    }
}
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: 0 errors. (No call sites yet; only static method.)

- [ ] **Step 3: Commit**

```bash
git add NiceEntry/Drawing/NotchedBorderDrawing.cs
git commit -m "feat: add NotchedBorderDrawing path builder"
```

---

## Task 3: NotchedBorder control (no notch yet)

**Files:**
- Create: `NiceEntry/Drawing/NotchedBorder.cs`

First iteration: solid rounded-rect stroke, no gap, no content slot. Verifies the SKCanvasView plumbing.

- [ ] **Step 1: Implement minimal control**

```csharp
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NiceEntry.Drawing;

public class NotchedBorder : ContentView
{
    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor), typeof(Color), typeof(NotchedBorder),
        defaultValue: Colors.Gray,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._canvas.InvalidateSurface());

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(NotchedBorder),
        defaultValue: 1.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._canvas.InvalidateSurface());

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(NotchedBorder),
        defaultValue: 8.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._canvas.InvalidateSurface());

    public static readonly BindableProperty NotchStartProperty = BindableProperty.Create(
        nameof(NotchStart), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._canvas.InvalidateSurface());

    public static readonly BindableProperty NotchEndProperty = BindableProperty.Create(
        nameof(NotchEnd), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._canvas.InvalidateSurface());

    public Color StrokeColor { get => (Color)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double NotchStart { get => (double)GetValue(NotchStartProperty); set => SetValue(NotchStartProperty, value); }
    public double NotchEnd { get => (double)GetValue(NotchEndProperty); set => SetValue(NotchEndProperty, value); }

    private readonly SKCanvasView _canvas;
    private readonly Grid _root;

    public NotchedBorder()
    {
        _canvas = new SKCanvasView { InputTransparent = true };
        _canvas.PaintSurface += OnPaint;
        _root = new Grid();
        _root.Add(_canvas);
        Content = _root;
    }

    public new View Content
    {
        get => _root.Children.Count > 1 ? (View)_root.Children[1] : null!;
        set
        {
            while (_root.Children.Count > 1) _root.RemoveAt(1);
            if (value is not null) _root.Add(value);
        }
    }

    private void OnPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var scale = (float)(e.Info.Width / Math.Max(_canvas.Width, 1));
        var stroke = (float)(StrokeThickness * scale);
        var radius = (float)(CornerRadius * scale);
        var notchStart = (float)(NotchStart * scale);
        var notchEnd = (float)(NotchEnd * scale);

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = StrokeColor.ToSKColor(),
            StrokeWidth = stroke,
            IsAntialias = true
        };

        using var path = NotchedBorderDrawing.BuildPath(
            e.Info.Width, e.Info.Height, radius, stroke, notchStart, notchEnd);

        canvas.DrawPath(path, paint);
    }
}
```

**Note on the `new Content` shadow:** `ContentView.Content` is the standard slot; we need to host both the canvas (drawing layer) AND consumer content (the input). Hiding the property and routing to an inner `Grid` is the simplest composition. Verify this doesn't break XAML content syntax — if it does, switch to a custom `ChildContent` property instead.

- [ ] **Step 2: Build**

```bash
dotnet build NiceEntry/NiceEntry.csproj
```

Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add NiceEntry/Drawing/NotchedBorder.cs
git commit -m "feat: add NotchedBorder control (no notch wiring yet)"
```

---

## Task 4: Wire NotchedBorder into LabelBase

**Files:**
- Modify: `NiceEntry/Base/LabelBase.xaml`
- Modify: `NiceEntry/Base/LabelBase.xaml.cs`

- [ ] **Step 1: Replace the Border in LabelBase.xaml**

Replace:

```xml
<Border x:Name="BorderLabel" Style="{StaticResource BaseBorderBasic}" Grid.Row="0" />
```

with:

```xml
<drawing:NotchedBorder x:Name="BorderLabel"
                       Grid.Row="0"
                       CornerRadius="8"
                       StrokeThickness="1"
                       StrokeColor="{AppThemeBinding Light=#212121, Dark=#E1E1E1}" />
```

And add the xmlns at the top of the file:

```xml
xmlns:drawing="clr-namespace:NiceEntry.Drawing"
```

Also set `LabelContainer` background to Transparent (remove the `BackgroundColor` setter from the `LabelContainer` style on lines 47-59).

- [ ] **Step 2: Update LabelBase.xaml.cs to drive NotchStart/NotchEnd from LabelContainer's measured bounds**

Add to constructor (after `InitializeComponent()`):

```csharp
LabelContainer.SizeChanged += (_, _) => UpdateNotchBounds();
LabelContainer.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(VisualElement.X) || e.PropertyName == nameof(VisualElement.Width))
        UpdateNotchBounds();
};
```

Add method:

```csharp
private void UpdateNotchBounds()
{
    if (LabelContainer.Width <= 0 || !LabelLabel.IsVisible)
    {
        BorderLabel.NotchStart = 0;
        BorderLabel.NotchEnd = 0;
        return;
    }
    // The label sits over the top edge of the border. Translate its X-range
    // into BorderLabel-local coordinates and pad by 4px on each side so the
    // text doesn't kiss the stroke ends.
    const double pad = 4;
    BorderLabel.NotchStart = LabelContainer.X - pad;
    BorderLabel.NotchEnd = LabelContainer.X + LabelContainer.Width + pad;
}
```

Also call `UpdateNotchBounds()` from `UpdateLabelView()` so the notch disappears when `Label = ""`.

- [ ] **Step 3: Repoint ChangeBorderColor**

Replace:

```csharp
private void ChangeBorderColor()
{
    if (Error is not null && Error.Count > 0)
    {
        BorderLabel.Stroke = Colors.Red;
    }
    else
    {
        BorderLabel.ClearValue(Border.StrokeProperty);
    }
}
```

with:

```csharp
private void ChangeBorderColor()
{
    if (Error is not null && Error.Count > 0)
    {
        BorderLabel.StrokeColor = Colors.Red;
    }
    else
    {
        BorderLabel.ClearValue(NotchedBorder.StrokeColorProperty);
    }
}
```

- [ ] **Step 4: Remove `LabelBackgroundColor` entirely**

Delete from `NiceEntry/Base/LabelBase.xaml.cs`:
- `LabelBackgroundColorProperty` bindable (lines ~80-83)
- `LabelBackgroundColor` CLR property (line ~96)
- `LabelBackgroundColorChanged` callback (line ~108)
- `UpdateLabelContainerBackground()` method
- The `BackgroundColor`-handling branch in `OnPropertyChanged`

The notched border makes this property obsolete — the consumer no longer needs to mask anything. The existing `BackgroundColor`-shortcut on `LabelBase` (which propagated to `LabelContainer`) also goes away; consumers who set `BackgroundColor` on the control get a normal `VisualElement` background, nothing more.

- [ ] **Step 5: Build and run the demo app**

```bash
dotnet build NiceEntry/NiceEntry.csproj
dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj
```

Then deploy demo to Android emulator and iOS simulator. Inspect MainPage:
- Each labeled control shows a continuous rounded border with a real gap where the label is.
- Error state turns stroke red (trigger via the form).
- Theme switch updates stroke color.
- Notch width matches label width (no over/under-cut).
- No black/white rectangle visible behind any label, regardless of page background.

- [ ] **Step 6: Commit**

```bash
git add NiceEntry/Base/LabelBase.xaml NiceEntry/Base/LabelBase.xaml.cs
git commit -m "feat: render NotchedBorder in LabelBase, drop label mask"
```

---

## Task 5: Demo a non-standard page background

**Files:**
- Modify: `NiceEntryDemoApp/MainPage.xaml`

Add a section near the top with `BackgroundColor="DarkSlateBlue"` (or similar non-black dark surface) on a wrapping `Border` containing one `LabeledEntry`. Confirms the notch works against any page color.

- [ ] **Step 1: Add the visual smoke test**

```xml
<Border BackgroundColor="DarkSlateBlue" Padding="16" StrokeThickness="0">
    <ne:LabeledEntry Label="On a colored surface"
                     Placeholder="Notch should be cut, no rectangle behind label" />
</Border>
```

- [ ] **Step 2: Run demo, screenshot, attach to PR**

Visual confirmation = test. No assertions to write since we have no UI test harness.

- [ ] **Step 3: Commit**

```bash
git add NiceEntryDemoApp/MainPage.xaml
git commit -m "test: add colored-surface case to demo"
```

---

## Task 6: README + MauiProgram requirements

**Files:**
- Modify: `README.md`
- Possibly modify: consumer's `MauiProgram.cs` (document, don't change theirs)

- [ ] **Step 1: Check whether `UseSkiaSharp()` is needed**

Look at `SkiaSharp.Views.Maui.Controls` 3.x release notes. If it auto-registers via source generator, skip. If not, NiceEntry consumers must call `.UseSkiaSharp()` in their `MauiProgram.CreateMauiApp()`.

- [ ] **Step 2: Document in README**

Add a section under Installation:

```markdown
## Setup

NiceEntry uses SkiaSharp to render the floating-label border. Register it in your `MauiProgram.cs`:

\`\`\`csharp
builder
    .UseMauiApp<App>()
    .UseSkiaSharp();
\`\`\`

(If you already use SkiaSharp elsewhere, calling `UseSkiaSharp()` more than once is safe.)
```

Delete the "Custom page backgrounds" section that PR #13 added to README — no longer relevant since the border has a real notch. Delete the `LabelBackgroundColor` row from the Common Properties table. Keep the `LabeledAutoCompleteEntry` section that PR #13 added (it's retroactive 1.5.0 documentation).

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: document SkiaSharp setup and notched border behavior"
```

---

## Task 7: Cross-target smoke check

- [ ] **Step 1: Build and pack both targets**

```bash
dotnet pack NiceEntry/NiceEntry.csproj -c Release
```

Expected: produces `NiceEntry.X.Y.Z.nupkg` in `./nupkgs/`. Inspect that SkiaSharp is listed as a dependency in the `.nuspec` inside the package.

- [ ] **Step 2: Deploy demo to physical iOS device (or simulator) and physical Android (or emulator)**

Confirm: notch renders cleanly on both, no rendering tearing during scroll, theme switch repaints, no perf regression vs. old Border (the SKCanvasView paints rarely — only on size/state change).

- [ ] **Step 3: Verify PR label**

PR #13 already has the `patch` label. `release.yml` will read it on merge and bump `v1.5.0` → `v1.5.1`. No tag is created manually — the workflow does it via `gh release create` (`.github/workflows/release.yml:64-71`). Do not push tags manually before merge or it will collide with the workflow.

---

## Open risks / things to verify during execution

1. **SKCanvasView scaling on iOS Retina / Android XXHDPI.** The `e.Info.Width / _canvas.Width` ratio gives pixel scale, but verify the math doesn't produce sub-pixel rounding artifacts on the stroke. If yes, snap stroke positions to pixel grid in `BuildPath`.
2. **Label measurement timing.** `LabelContainer.Width` may be 0 on first layout pass. The `SizeChanged` hook should catch the eventual real value, but watch for an initial unpainted frame where the notch is missing. Workaround: paint without a notch initially, repaint when width arrives.
3. **iOS bitmap caching.** Older MAUI/SkiaSharp combos had issues with `SKCanvasView` not redrawing after backgrounding. Test app suspend/resume.
4. **MAUI 10 + SkiaSharp 3 compatibility.** Confirm the SkiaSharp.Views.Maui.Controls 3.x line targets net10.0 explicitly. If only net9.0, may need a preview build.
5. **Package size impact.** SkiaSharp brings ~5MB of native bins per RID. For a control library this is significant. Document it in README.
6. **AOT compatibility.** SkiaSharp generally works under iOS AOT but the source-generated initializer matters — verify `UseSkiaSharp()` (or equivalent) is called before any control instantiates.

---

## Self-review summary

- Spec coverage: notch rendering ✓, error state color ✓, theme switching ✓, custom page background ✓, label-less mode (notch disappears) ✓, PR #13 reconciliation ✓.
- Placeholders: none — all code blocks are concrete.
- Type consistency: `NotchedBorder` properties (`StrokeColor`, `StrokeThickness`, `CornerRadius`, `NotchStart`, `NotchEnd`) match between Task 3 (definition) and Task 4 (consumption).
- Known gaps to resolve at execution: SkiaSharp 3.x package version (Task 1 Step 1), whether `UseSkiaSharp()` is required (Task 6 Step 1), final version-bump strategy (decision 2 in pre-flight).
