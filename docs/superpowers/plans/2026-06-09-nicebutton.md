# NiceButton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a standalone, tappable `NiceButton` control to the NiceEntry MAUI library — icon and/or text, three shapes, theme-aware colors, shadow, press feedback, and command binding, with Material Design Icons bundled as an embedded font.

**Architecture:** `NiceButton` is a `Layout` subclass with a custom `ILayoutManager` (for reliable auto-square in Circle mode) that hosts a single MAUI `Border`. The Border contains a `Grid` with an icon `Label` (MDI glyph font) and a text `Label`. Properties follow NiceEntry's BindableProperty-proxying convention. The MDI font ships as an `EmbeddedResource` and is registered via a new `.UseNiceEntry()` `MauiAppBuilder` extension.

**Tech Stack:** .NET 10, .NET MAUI (`Microsoft.Maui.Controls` 10.0.41), Material Design Icons 7.4.47 webfont, PowerShell (icon enum generator). Verification: `dotnet build` + manual check in `NiceEntryDemoApp` (no test project — matches repo).

**Spec:** `docs/superpowers/specs/2026-06-09-nicebutton-design.md`

---

## Conventions (apply to every file)

- File-scoped namespace `namespace NiceEntry;`, nullable enabled, implicit usings.
- Spaces (not tabs), CRLF line endings.
- BindableProperty field named `{PropertyName}Property`; `propertyChanged` handler `{PropertyName}Changed`; private updater `Update{PropertyName}View()`.
- Never add AI attribution to commits. Conventional commits (`feat:`/`fix:`/`docs:`/`chore:`).
- Work on branch `feature/nicebutton` (already checked out).

---

## Task 1: Layout enums

**Files:**
- Create: `NiceEntry/ButtonShape.cs`
- Create: `NiceEntry/ButtonContentOrientation.cs`
- Create: `NiceEntry/IconPlacement.cs`

- [ ] **Step 1: Create `ButtonShape.cs`**

```csharp
namespace NiceEntry;

/// <summary>Visual shape of a <see cref="NiceButton"/>'s border.</summary>
public enum ButtonShape
{
    /// <summary>Straight corners. <c>CornerRadius</c> is ignored.</summary>
    Rectangle,

    /// <summary>Rounded corners controlled by <c>CornerRadius</c>.</summary>
    Rounded,

    /// <summary>Ellipse; the button is measured square so it renders a perfect circle. <c>CornerRadius</c> is ignored.</summary>
    Circle
}
```

- [ ] **Step 2: Create `ButtonContentOrientation.cs`**

```csharp
namespace NiceEntry;

/// <summary>How icon and text are stacked when both are present on a <see cref="NiceButton"/>.</summary>
public enum ButtonContentOrientation
{
    /// <summary>Icon and text side by side.</summary>
    Horizontal,

    /// <summary>Icon and text stacked vertically.</summary>
    Vertical
}
```

- [ ] **Step 3: Create `IconPlacement.cs`**

```csharp
namespace NiceEntry;

/// <summary>Where the icon sits relative to the text when both are present.</summary>
public enum IconPlacement
{
    /// <summary>Icon before the text (left when horizontal, top when vertical).</summary>
    Start,

    /// <summary>Icon after the text (right when horizontal, bottom when vertical).</summary>
    End
}
```

- [ ] **Step 4: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add NiceEntry/ButtonShape.cs NiceEntry/ButtonContentOrientation.cs NiceEntry/IconPlacement.cs
git commit -m "feat: add NiceButton layout enums"
```

---

## Task 2: MDI font asset + generated `MaterialIcon` enum

**Files:**
- Create: `tools/Generate-MaterialIcon.ps1`
- Create: `NiceEntry/Fonts/materialdesignicons-webfont.ttf` (downloaded)
- Create: `NiceEntry/Icons/MaterialIcon.cs` (generated, ~7400 members)
- Modify: `NiceEntry/NiceEntry.csproj`

> **Why `Fonts/` and not `Resources/Fonts/`:** the MAUI SDK auto-globs `Resources/Fonts/**` as `MauiFont`. We want the file as `EmbeddedResource` instead, so we place it outside that glob to avoid a duplicate-item build error.

- [ ] **Step 1: Create the generator script `tools/Generate-MaterialIcon.ps1`**

```powershell
#requires -Version 7
<#
    Generates NiceEntry/Icons/MaterialIcon.cs from the Material Design Icons
    webfont _variables.scss (icon name -> codepoint map).

    Run this ONLY when intentionally bumping the MDI version — never automatically.
    The URL is pinned to a concrete tag so re-runs are reproducible; bumping means
    editing $MdiVersion here AND re-downloading the matching .ttf (Task 2, Step 2).
#>
param(
    [string]$MdiVersion = "v7.4.47",
    [string]$ScssUrl    = "https://raw.githubusercontent.com/Templarian/MaterialDesign-Webfont/$MdiVersion/scss/_variables.scss",
    [string]$OutputPath = "$PSScriptRoot/../NiceEntry/Icons/MaterialIcon.cs"
)

$ErrorActionPreference = "Stop"

$scss = (Invoke-WebRequest -Uri $ScssUrl -UseBasicParsing).Content

# Matches entries like:  "ab-testing": F01C9,
$regex   = [regex]'"(?<name>[a-z0-9-]+)"\s*:\s*(?<code>[0-9A-Fa-f]{4,6})'
$entries = $regex.Matches($scss)
if ($entries.Count -eq 0) { throw "No icon entries parsed from $ScssUrl" }

$textInfo = (Get-Culture).TextInfo
$seen     = [System.Collections.Generic.HashSet[string]]::new()
$sb       = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("// <auto-generated />")
[void]$sb.AppendLine("// Generated from Material Design Icons webfont (scss/_variables.scss).")
[void]$sb.AppendLine("// Do not edit by hand. Regenerate with tools/Generate-MaterialIcon.ps1.")
[void]$sb.AppendLine()
[void]$sb.AppendLine("namespace NiceEntry;")
[void]$sb.AppendLine()
[void]$sb.AppendLine("/// <summary>Material Design Icons glyphs. The enum value is the font codepoint.</summary>")
[void]$sb.AppendLine("public enum MaterialIcon")
[void]$sb.AppendLine("{")

foreach ($m in $entries) {
    $name = $m.Groups['name'].Value
    $code = $m.Groups['code'].Value

    # kebab-case -> PascalCase, keeping numeric segments as-is
    $pascal = ($name -split '-' | ForEach-Object {
        if ($_ -match '^\d') { $_ } else { $textInfo.ToTitleCase($_) }
    }) -join ''

    # C# identifiers cannot start with a digit
    if ($pascal -match '^\d') { $pascal = "_$pascal" }

    if (-not $seen.Add($pascal)) { continue }  # defensive de-dupe

    [void]$sb.AppendLine("    $pascal = 0x$code,")
}

[void]$sb.AppendLine("}")

$dir = Split-Path -Parent $OutputPath
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
[System.IO.File]::WriteAllText($OutputPath, $sb.ToString())
Write-Host "Wrote $($seen.Count) icons to $OutputPath"
```

- [ ] **Step 2: Download the MDI webfont into `NiceEntry/Fonts/`**

Run (URL pinned to the same tag as the generator's `$MdiVersion`):
```powershell
New-Item -ItemType Directory -Force -Path "NiceEntry/Fonts" | Out-Null
Invoke-WebRequest -UseBasicParsing `
  -Uri "https://raw.githubusercontent.com/Templarian/MaterialDesign-Webfont/v7.4.47/fonts/materialdesignicons-webfont.ttf" `
  -OutFile "NiceEntry/Fonts/materialdesignicons-webfont.ttf"
```
Expected: file `NiceEntry/Fonts/materialdesignicons-webfont.ttf` exists (~1.3 MB). The `.ttf` and the generated enum must come from the **same** tag, or glyphs and codepoints can drift apart.

- [ ] **Step 3: Generate `MaterialIcon.cs`**

Run: `pwsh tools/Generate-MaterialIcon.ps1`
Expected: console prints `Wrote <N> icons to .../NiceEntry/Icons/MaterialIcon.cs` (N ≈ 7400) and the file exists.

- [ ] **Step 4: Spot-check the generated enum**

Run: `Select-String -Path NiceEntry/Icons/MaterialIcon.cs -Pattern 'Pencil =|Cart =|ThumbUp =' | Select-Object -First 5`
Expected: lines like `    Pencil = 0xF03EB,`, `    ThumbUp = 0xF0513,` (exact codepoints may differ by MDI version — just confirm the `Name = 0xF…,` shape).

- [ ] **Step 5: Embed the font in the csproj**

In `NiceEntry/NiceEntry.csproj`, add this `ItemGroup` (next to the other `ItemGroup`s):

```xml
	<ItemGroup>
		<EmbeddedResource Include="Fonts\materialdesignicons-webfont.ttf" />
	</ItemGroup>
```

- [ ] **Step 6: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors. (A ~7400-member enum compiles fine.)

- [ ] **Step 7: Commit**

```bash
git add tools/Generate-MaterialIcon.ps1 NiceEntry/Fonts/materialdesignicons-webfont.ttf NiceEntry/Icons/MaterialIcon.cs NiceEntry/NiceEntry.csproj
git commit -m "feat: bundle Material Design Icons font and generated MaterialIcon enum"
```

---

## Task 3: `.UseNiceEntry()` font registration extension

**Files:**
- Create: `NiceEntry/AppHostBuilderExtensions.cs`
- Modify: `NiceEntryDemoApp/MauiProgram.cs`

- [ ] **Step 1: Create `AppHostBuilderExtensions.cs`**

```csharp
using Microsoft.Maui.Hosting;

namespace NiceEntry;

public static class AppHostBuilderExtensions
{
    /// <summary>Font family alias for the bundled Material Design Icons webfont.</summary>
    public const string MaterialDesignIconsFontFamily = "MaterialDesignIcons";

    /// <summary>
    /// Registers NiceEntry's bundled fonts (Material Design Icons). Call exactly once during
    /// startup. Do NOT also register the <c>MaterialDesignIcons</c> alias manually:
    /// <c>ConfigureFonts</c> appends to a font list, so a duplicate alias adds a duplicate
    /// descriptor and can break font resolution.
    /// </summary>
    public static MauiAppBuilder UseNiceEntry(this MauiAppBuilder builder)
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddEmbeddedResourceFont(
                typeof(AppHostBuilderExtensions).Assembly,
                "materialdesignicons-webfont.ttf",
                MaterialDesignIconsFontFamily);
        });

        return builder;
    }
}
```

- [ ] **Step 2: Call `.UseNiceEntry()` in the demo's `MauiProgram`**

In `NiceEntryDemoApp/MauiProgram.cs`, add `using NiceEntry;` at the top, then chain `.UseNiceEntry()` after `.UseMauiCommunityToolkit()`:

```csharp
        builder.UseMauiApp<App>()
               .UseMauiCommunityToolkit()
               .UseNiceEntry()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
               });
```

- [ ] **Step 3: Build library and demo**

Run: `dotnet build NiceEntry/NiceEntry.csproj` then `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`
Expected: both `Build succeeded`, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add NiceEntry/AppHostBuilderExtensions.cs NiceEntryDemoApp/MauiProgram.cs
git commit -m "feat: add UseNiceEntry() to register the MDI font"
```

---

## Task 4: `NiceButton` core — layout manager + content rendering

**Files:**
- Create: `NiceEntry/NiceButtonLayoutManager.cs`
- Create: `NiceEntry/NiceButton.cs`
- Modify: `NiceEntryDemoApp/MainPage.xaml`

This task establishes the control with content (`Text`, `Icon`, `Orientation`, `IconPlacement`, `Spacing`, `ContentPadding`) and text styling (`FontSize`, `FontFamily`, `FontAttributes`, `IconSize`). Shape, colors, shadow, and command come in later tasks. The button is not yet tappable.

- [ ] **Step 1: Create the layout manager `NiceButtonLayoutManager.cs`**

```csharp
namespace NiceEntry;

/// <summary>
/// Lays out the single child (the Border) of a <see cref="NiceButton"/>. When the button
/// is in Circle shape it returns a square desired size so the ellipse renders as a perfect
/// circle. This avoids both SizeChanged-driven resizing (layout loops on Android) and
/// ContentView.MeasureOverride (unreliable per dotnet/maui#19471).
/// </summary>
internal sealed class NiceButtonLayoutManager : ILayoutManager
{
    private readonly NiceButton _button;

    public NiceButtonLayoutManager(NiceButton button) => _button = button;

    public Size Measure(double widthConstraint, double heightConstraint)
    {
        var desired = new Size();
        foreach (var child in _button)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var size = child.Measure(widthConstraint, heightConstraint);
            desired = new Size(Math.Max(desired.Width, size.Width), Math.Max(desired.Height, size.Height));
        }

        if (_button.ForceSquare)
        {
            var side = Math.Max(desired.Width, desired.Height);
            desired = new Size(side, side);
        }

        return desired;
    }

    public Size ArrangeChildren(Rect bounds)
    {
        foreach (var child in _button)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            child.Arrange(bounds);
        }

        return bounds.Size;
    }
}
```

- [ ] **Step 2: Create `NiceButton.cs` with the class skeleton, fields, constructor, and content properties**

```csharp
namespace NiceEntry;

public class NiceButton : Layout
{
    /// <summary>Default font size for button text (not the field-tuned LabelBase value).</summary>
    public static readonly double DefaultFontSize = 14.0;

    private static readonly Thickness DefaultContentPadding =
        DeviceInfo.Platform == DevicePlatform.iOS ? new Thickness(12, 12) : new Thickness(12, 10);

    private readonly Border _border;
    private readonly Grid _contentHost;
    private readonly Label _iconLabel;
    private readonly Label _textLabel;

    public NiceButton()
    {
        // The Layout root must never paint a fill: a non-transparent Background BRUSH wins
        // over BackgroundColor in rendering, so a transparent brush keeps the root invisible
        // while the inner Border (correctly clipped to the shape) carries all fill. Without
        // this, a set BackgroundColor would paint a rectangle behind rounded/circle corners.
        Background = Brush.Transparent;

        _iconLabel = new Label
        {
            FontFamily = AppHostBuilderExtensions.MaterialDesignIconsFontFamily,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = IconSize
        };

        _textLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            FontSize = FontSize
        };

        _contentHost = new Grid
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _border = new Border
        {
            StrokeThickness = 0,
            Padding = ContentPadding,
            Content = _contentHost,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        Add(_border);
        RebuildContent();
    }

    /// <summary>True when the button must be measured square (Circle shape). Updated in the shape task.</summary>
    internal bool ForceSquare => false;

    protected override ILayoutManager CreateLayoutManager() => new NiceButtonLayoutManager(this);

    // --- Content & text-style bindable properties ---

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NiceButton), string.Empty, propertyChanged: TextChanged);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(MaterialIcon?), typeof(NiceButton), null, propertyChanged: IconChanged);

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(ButtonContentOrientation), typeof(NiceButton),
        ButtonContentOrientation.Horizontal, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty IconPlacementProperty = BindableProperty.Create(
        nameof(IconPlacement), typeof(IconPlacement), typeof(NiceButton),
        IconPlacement.Start, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(NiceButton), 6.0, propertyChanged: LayoutAffectingChanged);

    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(NiceButton), DefaultContentPadding,
        propertyChanged: ContentPaddingChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(NiceButton), DefaultFontSize, propertyChanged: FontSizeChanged);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(NiceButton), null, propertyChanged: FontFamilyChanged);

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(NiceButton),
        FontAttributes.None, propertyChanged: FontAttributesChanged);

    public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
        nameof(IconSize), typeof(double), typeof(NiceButton), 20.0, propertyChanged: IconSizeChanged);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public MaterialIcon? Icon { get => (MaterialIcon?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public ButtonContentOrientation Orientation { get => (ButtonContentOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public IconPlacement IconPlacement { get => (IconPlacement)GetValue(IconPlacementProperty); set => SetValue(IconPlacementProperty, value); }
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public string FontFamily { get => (string)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

    private static void TextChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateTextView();
    private static void IconChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconView();
    private static void LayoutAffectingChanged(BindableObject b, object o, object n) => ((NiceButton)b).RebuildContent();
    private static void ContentPaddingChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateContentPaddingView();
    private static void FontSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontSizeView();
    private static void FontFamilyChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontFamilyView();
    private static void FontAttributesChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateFontAttributesView();
    private static void IconSizeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateIconSizeView();

    private void UpdateTextView()
    {
        _textLabel.Text = Text;
        RebuildContent();
    }

    private void UpdateIconView()
    {
        _iconLabel.Text = Icon.HasValue ? char.ConvertFromUtf32((int)Icon.Value) : null;
        RebuildContent();
    }

    private void UpdateContentPaddingView() => _border.Padding = ContentPadding;
    private void UpdateFontSizeView() => _textLabel.FontSize = FontSize;
    private void UpdateFontFamilyView() => _textLabel.FontFamily = FontFamily;
    private void UpdateFontAttributesView() => _textLabel.FontAttributes = FontAttributes;
    private void UpdateIconSizeView() => _iconLabel.FontSize = IconSize;

    /// <summary>
    /// Rebuilds the icon/text container based on what is set (icon-only, label-only, or both)
    /// and the chosen orientation/placement.
    /// </summary>
    private void RebuildContent()
    {
        _contentHost.Children.Clear();
        _contentHost.RowDefinitions.Clear();
        _contentHost.ColumnDefinitions.Clear();

        var hasIcon = Icon.HasValue;
        var hasText = !string.IsNullOrEmpty(Text);

        _iconLabel.IsVisible = hasIcon;
        _textLabel.IsVisible = hasText;

        if (!hasIcon && !hasText) return;

        if (hasIcon && hasText)
        {
            var iconFirst = IconPlacement == IconPlacement.Start;
            var first = iconFirst ? (View)_iconLabel : _textLabel;
            var second = iconFirst ? (View)_textLabel : _iconLabel;

            if (Orientation == ButtonContentOrientation.Horizontal)
            {
                _contentHost.ColumnSpacing = Spacing;
                _contentHost.RowSpacing = 0;
                _contentHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _contentHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _contentHost.Add(first, 0, 0);
                _contentHost.Add(second, 1, 0);
            }
            else
            {
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
            _contentHost.Add(only, 0, 0);
        }

        InvalidateMeasure();
    }
}
```

- [ ] **Step 3: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Add a NiceButton to the demo page**

In `NiceEntryDemoApp/MainPage.xaml`, add this delimited block inside the `VerticalStackLayout` (e.g. right after the opening `Border` block, before the first `LabeledEntry`). Tasks 5–7 add more buttons **inside** this block; Task 9 removes the whole block atomically by deleting everything between the two markers.

```xml
        <!-- NICEBUTTON-DEV-CHECKS (temporary; removed atomically in Task 9) -->
        <ne:NiceButton Text="Buy now" Icon="Cart" Orientation="Horizontal" IconPlacement="Start" />
        <!-- /NICEBUTTON-DEV-CHECKS -->
```

- [ ] **Step 5: Build the demo**

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Visual check (manual)**

Launch the demo on an Android emulator or iOS simulator. Confirm: a button-shaped area shows a cart glyph followed by "Buy now". (It has no fill color yet — that arrives in Task 6. The glyph must render as an icon, not a box; if it's a box, the font alias/registration is wrong.)

- [ ] **Step 7: Commit**

```bash
git add NiceEntry/NiceButton.cs NiceEntry/NiceButtonLayoutManager.cs NiceEntryDemoApp/MainPage.xaml
git commit -m "feat: add NiceButton control with icon/text content layout"
```

---

## Task 5: Shape and corner radius

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

- [ ] **Step 1: Add the shape `using` and bindable properties**

At the top of `NiceButton.cs`, add:

```csharp
using Microsoft.Maui.Controls.Shapes;
```

Add these properties alongside the others:

```csharp
    public static readonly BindableProperty ButtonShapeProperty = BindableProperty.Create(
        nameof(ButtonShape), typeof(ButtonShape), typeof(NiceButton),
        ButtonShape.Rounded, propertyChanged: ShapeChanged);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(NiceButton), 8.0, propertyChanged: ShapeChanged);

    public ButtonShape ButtonShape { get => (ButtonShape)GetValue(ButtonShapeProperty); set => SetValue(ButtonShapeProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    private static void ShapeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateShapeView();

    private void UpdateShapeView()
    {
        _border.StrokeShape = ButtonShape switch
        {
            ButtonShape.Rectangle => new Rectangle(),
            ButtonShape.Circle => new Ellipse(),
            _ => new RoundRectangle { CornerRadius = new Microsoft.Maui.CornerRadius(CornerRadius) }
        };

        InvalidateMeasure();
    }
```

- [ ] **Step 2: Make `ForceSquare` reflect Circle shape**

Replace the placeholder `ForceSquare` member with:

```csharp
    /// <summary>True when the button must be measured square (Circle shape).</summary>
    internal bool ForceSquare => ButtonShape == ButtonShape.Circle;
```

- [ ] **Step 3: Apply the initial shape in the constructor**

At the end of the `NiceButton()` constructor, after `RebuildContent();`, add:

```csharp
        UpdateShapeView();
```

- [ ] **Step 4: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Verify shapes in the demo**

In `NiceEntryDemoApp/MainPage.xaml`, add these inside the `NICEBUTTON-DEV-CHECKS` block (before the closing `<!-- /NICEBUTTON-DEV-CHECKS -->` marker):

```xml
        <ne:NiceButton Icon="ThumbUp" ButtonShape="Circle" />
        <ne:NiceButton Text="Rounded" ButtonShape="Rounded" CornerRadius="20" />
        <ne:NiceButton Text="Rectangle" ButtonShape="Rectangle" />
```

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj` (Expected: `Build succeeded`), then launch and confirm: the icon-only button is a circle (equal width/height), the rounded one has a 20px radius, the rectangle has square corners.

- [ ] **Step 6: Commit**

```bash
git add NiceEntry/NiceButton.cs NiceEntryDemoApp/MainPage.xaml
git commit -m "feat: add NiceButton shape and corner radius"
```

---

## Task 6: Theme-aware colors and disabled state

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

`BackgroundColor`/`Background` are inherited from `VisualElement` and forwarded to the inner Border (no new BindableProperty — avoids name clash). `TextColor` (shared by icon + text), `BorderColor`, and `BorderWidth` are new.

- [ ] **Step 1: Add color constants**

Add inside `NiceButton` (near the other `static readonly` fields):

```csharp
    private static readonly Color DefaultBackgroundLight = Color.FromArgb("#3B49DF");
    private static readonly Color DefaultBackgroundDark = Color.FromArgb("#5965F2");
    private static readonly Color DefaultForegroundLight = Colors.White;
    private static readonly Color DefaultForegroundDark = Colors.White;
    private static readonly Color DisabledBackgroundLight = Color.FromArgb("#E0E0E0");
    private static readonly Color DisabledBackgroundDark = Color.FromArgb("#3A3A3A");
    private static readonly Color DisabledForegroundLight = Color.FromArgb("#9E9E9E");
    private static readonly Color DisabledForegroundDark = Color.FromArgb("#6E6E6E");

    // Background-brush neutralization (see OnPropertyChanged): _userBackgroundBrush holds the
    // consumer's intended Background brush; the Layout's own Background is forced transparent.
    private bool _suppressBackground;
    private Brush? _userBackgroundBrush;
```

- [ ] **Step 2: Add `TextColor`, `BorderColor`, `BorderWidth` properties**

```csharp
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(NiceButton), null, propertyChanged: ColorChanged);

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(NiceButton), null, propertyChanged: BorderStrokeChanged);

    public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
        nameof(BorderWidth), typeof(double), typeof(NiceButton), 0.0, propertyChanged: BorderStrokeChanged);

    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color BorderColor { get => (Color)GetValue(BorderColorProperty); set => SetValue(BorderColorProperty, value); }
    public double BorderWidth { get => (double)GetValue(BorderWidthProperty); set => SetValue(BorderWidthProperty, value); }

    private static void ColorChanged(BindableObject b, object o, object n) => ((NiceButton)b).ApplyColors();
    private static void BorderStrokeChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateBorderStrokeView();
```

- [ ] **Step 3: Add `ApplyColors`, `SetForeground`, `UpdateBorderStrokeView`, and the `OnPropertyChanged` override**

```csharp
    private void ApplyColors()
    {
        // OnPropertyChanged can fire (e.g. for Background) before the constructor has built
        // the inner views; bail out until they exist.
        if (_border is null) return;

        if (!IsEnabled)
        {
            _border.ClearValue(BackgroundProperty);
            _border.SetAppThemeColor(BackgroundColorProperty, DisabledBackgroundLight, DisabledBackgroundDark);
            SetForeground(DisabledForegroundLight, DisabledForegroundDark, themed: true);
            return;
        }

        if (_userBackgroundBrush is not null)
        {
            _border.Background = _userBackgroundBrush;
        }
        else if (BackgroundColor is not null)
        {
            _border.ClearValue(BackgroundProperty);
            _border.BackgroundColor = BackgroundColor;
        }
        else
        {
            _border.ClearValue(BackgroundProperty);
            _border.SetAppThemeColor(BackgroundColorProperty, DefaultBackgroundLight, DefaultBackgroundDark);
        }

        if (TextColor is not null)
            SetForeground(TextColor, TextColor, themed: false);
        else
            SetForeground(DefaultForegroundLight, DefaultForegroundDark, themed: true);
    }

    private void SetForeground(Color light, Color dark, bool themed)
    {
        if (themed)
        {
            _iconLabel.SetAppThemeColor(Label.TextColorProperty, light, dark);
            _textLabel.SetAppThemeColor(Label.TextColorProperty, light, dark);
        }
        else
        {
            _iconLabel.TextColor = light;
            _textLabel.TextColor = light;
        }
    }

    private void UpdateBorderStrokeView()
    {
        _border.Stroke = BorderColor is null ? null : new SolidColorBrush(BorderColor);
        _border.StrokeThickness = BorderWidth;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (_suppressBackground) return;

        if (propertyName == BackgroundColorProperty.PropertyName
            || propertyName == IsEnabledProperty.PropertyName)
        {
            ApplyColors();
        }
        else if (propertyName == BackgroundProperty.PropertyName)
        {
            // Consumer set a Background brush (e.g. gradient). Capture it for the inner Border,
            // then force the Layout root back to transparent so it never paints a rectangle.
            _userBackgroundBrush = ReferenceEquals(Background, Brush.Transparent) ? null : Background;
            _suppressBackground = true;
            Background = Brush.Transparent;
            _suppressBackground = false;
            ApplyColors();
        }
    }
```

- [ ] **Step 4: Apply colors initially in the constructor**

At the end of the `NiceButton()` constructor (after `UpdateShapeView();`), add:

```csharp
        UpdateBorderStrokeView();
        ApplyColors();
```

- [ ] **Step 5: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Verify colors in the demo**

In `NiceEntryDemoApp/MainPage.xaml`, add these inside the `NICEBUTTON-DEV-CHECKS` block (before the closing marker):

```xml
        <ne:NiceButton Text="Default theme colors" Icon="Check" />
        <ne:NiceButton Text="Custom" Icon="Star" BackgroundColor="DarkGreen" TextColor="White" />
        <ne:NiceButton Text="Disabled" Icon="Lock" IsEnabled="False" />
```

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj` (Expected: `Build succeeded`), then launch and confirm: default button shows the indigo fill with white text/icon; custom shows green/white; disabled shows the muted gray treatment. Toggle the OS to dark mode and confirm the default/disabled buttons adapt.

- [ ] **Step 7: Commit**

```bash
git add NiceEntry/NiceButton.cs NiceEntryDemoApp/MainPage.xaml
git commit -m "feat: add NiceButton theme-aware colors and disabled state"
```

---

## Task 7: Shadow

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

- [ ] **Step 1: Add `HasShadow` and `CustomShadow` properties + updater**

```csharp
    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow), typeof(bool), typeof(NiceButton), false, propertyChanged: ShadowChanged);

    public static readonly BindableProperty CustomShadowProperty = BindableProperty.Create(
        nameof(CustomShadow), typeof(Shadow), typeof(NiceButton), null, propertyChanged: ShadowChanged);

    public bool HasShadow { get => (bool)GetValue(HasShadowProperty); set => SetValue(HasShadowProperty, value); }
    public Shadow CustomShadow { get => (Shadow)GetValue(CustomShadowProperty); set => SetValue(CustomShadowProperty, value); }

    private static void ShadowChanged(BindableObject b, object o, object n) => ((NiceButton)b).UpdateShadowView();

    private void UpdateShadowView()
    {
        if (CustomShadow is not null)
            _border.Shadow = CustomShadow;
        else if (HasShadow)
            _border.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.3f, Radius = 8, Offset = new Point(0, 2) };
        else
            _border.Shadow = null;
    }
```

> Note: `Shadow` here is `Microsoft.Maui.Controls.Shadow`, available via implicit usings. The override property is deliberately named `CustomShadow` to avoid clashing with the inherited `VisualElement.Shadow`.

- [ ] **Step 2: Apply shadow initially in the constructor**

At the end of the `NiceButton()` constructor (after `ApplyColors();`), add:

```csharp
        UpdateShadowView();
```

- [ ] **Step 3: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Verify shadow in the demo**

In `NiceEntryDemoApp/MainPage.xaml`, add these inside the `NICEBUTTON-DEV-CHECKS` block (before the closing marker):

```xml
        <ne:NiceButton Text="Shadow on" Icon="Star" HasShadow="True" />
        <ne:NiceButton Text="No shadow" Icon="Star" />
```

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj` (Expected: `Build succeeded`), then launch and confirm the first button casts a soft drop shadow and the second does not.

- [ ] **Step 5: Commit**

```bash
git add NiceEntry/NiceButton.cs NiceEntryDemoApp/MainPage.xaml
git commit -m "feat: add NiceButton shadow support"
```

---

## Task 8: Command binding, tap feedback, and CanExecute integration

**Files:**
- Modify: `NiceEntry/NiceButton.cs`

- [ ] **Step 1: Add the command `using`**

At the top of `NiceButton.cs`, add:

```csharp
using System.Windows.Input;
```

- [ ] **Step 2: Add `Command` and `CommandParameter` properties + CanExecute wiring**

```csharp
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(NiceButton), null, propertyChanged: CommandChanged);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(NiceButton), null, propertyChanged: CommandParameterChanged);

    public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private static void CommandChanged(BindableObject b, object oldValue, object newValue)
    {
        var btn = (NiceButton)b;
        if (oldValue is ICommand oldCmd) oldCmd.CanExecuteChanged -= btn.OnCanExecuteChanged;
        if (newValue is ICommand newCmd) newCmd.CanExecuteChanged += btn.OnCanExecuteChanged;
        btn.RefreshEnabledFromCommand();
    }

    private static void CommandParameterChanged(BindableObject b, object o, object n)
        => ((NiceButton)b).RefreshEnabledFromCommand();

    private void OnCanExecuteChanged(object? sender, EventArgs e) => RefreshEnabledFromCommand();

    private void RefreshEnabledFromCommand()
    {
        if (Command is { } cmd)
            IsEnabled = cmd.CanExecute(CommandParameter);
    }
```

- [ ] **Step 3: Wire the tap gesture in the constructor**

In the `NiceButton()` constructor, immediately before `Add(_border);`, add:

```csharp
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        _border.GestureRecognizers.Add(tap);
```

- [ ] **Step 4: Add the tap handler with press feedback**

```csharp
    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled) return;
        if (Command is { } cmd && !cmd.CanExecute(CommandParameter)) return;

        await _contentHost.FadeTo(0.3, 100);
        await _contentHost.FadeTo(1, 100);

        if (Command is { } toRun && toRun.CanExecute(CommandParameter))
            toRun.Execute(CommandParameter);
    }
```

- [ ] **Step 5: Build the library**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Build the demo**

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add NiceEntry/NiceButton.cs
git commit -m "feat: add NiceButton command binding and press feedback"
```

---

## Task 9: Demo page — full showcase

**Files:**
- Modify: `NiceEntryDemoApp/MainPage.xaml.cs`
- Modify: `NiceEntryDemoApp/MainPage.xaml`

Replace the ad-hoc demo buttons added in earlier tasks with a single, organized showcase section covering all six layout variants, all three shapes, shadow on/off, disabled, and a working command.

- [ ] **Step 1: Add a demo command and a toggle to `MainViewModel`**

In `NiceEntryDemoApp/MainPage.xaml.cs`, add these members inside the `MainViewModel` class (next to the other `[RelayCommand]`/`[ObservableProperty]` members):

```csharp
    [ObservableProperty] private bool _isButtonEnabled = true;

    [RelayCommand]
    private async Task NiceButtonTapped(string? which)
    {
        var toast = Toast.Make($"NiceButton tapped: {which ?? "(no parameter)"}");
        await toast.Show();
    }
```

- [ ] **Step 2: Remove the temporary demo buttons and add the showcase section**

In `NiceEntryDemoApp/MainPage.xaml`, first delete the entire temporary block — everything from `<!-- NICEBUTTON-DEV-CHECKS ... -->` through `<!-- /NICEBUTTON-DEV-CHECKS -->` inclusive (this removes all dev-check buttons in one atomic edit). Then add this showcase block at the top of the `VerticalStackLayout` (just inside it, before the colored `Border`):

```xml
        <Label Text="NiceButton" FontSize="20" FontAttributes="Bold" />

        <!-- Six layout variants -->
        <ne:NiceButton Text="Label only"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Label only" />

        <ne:NiceButton Icon="ThumbUp" ButtonShape="Circle"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Icon only" />

        <ne:NiceButton Text="Buy now" Icon="Cart" Orientation="Horizontal" IconPlacement="Start"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Icon left" />

        <ne:NiceButton Text="Choose plan" Icon="ArrowRight" Orientation="Horizontal" IconPlacement="End"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Icon right" />

        <ne:NiceButton Text="Read" Icon="Email" Orientation="Vertical" IconPlacement="Start"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Icon top" />

        <ne:NiceButton Text="Undo" Icon="Undo" Orientation="Vertical" IconPlacement="End"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Icon bottom" />

        <!-- Shapes -->
        <ne:NiceButton Text="Rectangle" ButtonShape="Rectangle" BackgroundColor="#3B49DF" TextColor="White" />
        <ne:NiceButton Text="Rounded r=20" ButtonShape="Rounded" CornerRadius="20" BackgroundColor="#3B49DF" TextColor="White" />

        <!-- Shadow + disabled -->
        <ne:NiceButton Text="With shadow" Icon="Star" HasShadow="True" BackgroundColor="#3B49DF" TextColor="White" />
        <ne:NiceButton Text="Default theme colors" Icon="Palette" />
        <ne:NiceButton Text="Bound IsEnabled" Icon="Lock" IsEnabled="{Binding IsButtonEnabled}"
                       BackgroundColor="#3B49DF" TextColor="White"
                       Command="{Binding NiceButtonTappedCommand}" CommandParameter="Bound IsEnabled" />
        <HorizontalStackLayout Spacing="8">
            <Label Text="Enable the button above" VerticalOptions="Center" />
            <Switch IsToggled="{Binding IsButtonEnabled}" />
        </HorizontalStackLayout>
```

> If any icon name (`Cart`, `ArrowRight`, `Email`, `Undo`, `Star`, `Palette`, `ThumbUp`, `Check`, `Lock`) is not present in the generated enum for the pinned MDI version, pick the closest existing name from `NiceEntry/Icons/MaterialIcon.cs` — the demo is illustrative, not contractual.

- [ ] **Step 3: Build the demo**

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Visual verification (manual)**

Launch the demo. Confirm:
- All six layout variants render correctly (label-only, icon-only circle, icon left/right, icon top/bottom).
- Tapping a button dims it briefly (fade) and shows a toast with the parameter.
- The "Default theme colors" button adapts to light/dark mode.
- The shadow button casts a shadow.

- [ ] **Step 5: Commit**

```bash
git add NiceEntryDemoApp/MainPage.xaml NiceEntryDemoApp/MainPage.xaml.cs
git commit -m "feat(demo): showcase NiceButton variants, shapes, shadow, and commands"
```

---

## Task 10: Documentation

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document NiceButton and the font registration requirement**

Add a `## NiceButton` section to `README.md` covering:
- That `NiceButton` is a standalone icon/text button (not part of the labeled-input family).
- **Required setup:** consumers must call `.UseNiceEntry()` in `MauiProgram` so the Material Design Icons font is registered, otherwise `Icon` glyphs render as boxes.
- A minimal XAML example:

```xml
<ne:NiceButton Text="Buy now"
               Icon="Cart"
               BackgroundColor="#3B49DF"
               TextColor="White"
               Command="{Binding BuyCommand}" />
```

- The property table from spec section 8 (Text, Icon, Orientation, IconPlacement, Spacing, ContentPadding, ButtonShape, CornerRadius, BackgroundColor, Background, TextColor, BorderColor, BorderWidth, FontSize, FontFamily, FontAttributes, IconSize, HasShadow, CustomShadow, Command, CommandParameter, IsEnabled).
- A note that icons come from Material Design Icons (pictogrammers.com/library/mdi) and are selected via the `MaterialIcon` enum.

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: document NiceButton usage and UseNiceEntry setup"
```

---

## Spec coverage check

| Spec requirement | Task(s) |
|---|---|
| Standalone control, outside LabelBase | Task 4 |
| Six layout variants (derived from Text/Icon + Orientation + IconPlacement) | Task 4 |
| `Start`/`End` icon placement | Task 1, 4 |
| Three shapes; CornerRadius only for Rounded | Task 5 |
| Circle auto-square via ILayoutManager | Task 4, 5 |
| Theme-aware default colors; shared TextColor | Task 6 |
| Inherited BackgroundColor/Background forwarded | Task 6 |
| BorderColor/BorderWidth | Task 6 |
| Disabled via ApplyColors (not VSM) | Task 6 |
| HasShadow + CustomShadow | Task 7 |
| Command/CommandParameter + CanExecute integration | Task 8 |
| Opacity-fade press feedback | Task 8 |
| IsEnabled + tap guard | Task 6, 8 |
| MaterialIcon enum (codepoint values) + embedded MDI font | Task 2 |
| `.UseNiceEntry()` extension (idempotent) | Task 3 |
| ContentPadding (Border.Padding), default per platform | Task 4 |
| NiceButton.DefaultFontSize = 14.0 | Task 4 |
| Demo of all features | Task 9 |
| Docs | Task 10 |

## Notes for the implementer

- **No test project** — verification is `dotnet build` (library + demo must compile) plus the manual visual checks called out per task. This matches the repo and was an explicit decision.
- **Constructor init order matters:** the constructor ends with `RebuildContent(); UpdateShapeView(); UpdateBorderStrokeView(); ApplyColors(); UpdateShadowView();` after all tasks are applied. Each task appends its own init call — keep them in that order.
- **MDI version** is pinned by whatever `Generate-MaterialIcon.ps1` downloads (currently 7.4.47 on `master`). Re-running the script later may add/rename icons; the generated file is committed so builds are reproducible without network access.
- **Android glyph-as-box** symptom means the font didn't register — check `.UseNiceEntry()` is called and the `EmbeddedResource` include is present.
