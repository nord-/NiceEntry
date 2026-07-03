# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NiceEntry is a .NET MAUI control library providing labeled input controls (Entry, Picker, DatePicker, TimePicker) with built-in validation error display, required field indicators, and light/dark theme support. Targets Android 21+ and iOS 15+ on .NET 10.

## Build Commands

```bash
# Build the library
dotnet build NiceEntry/NiceEntry.csproj

# Build the demo app
dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj

# Create NuGet package (outputs to ./nupkgs/)
dotnet pack NiceEntry/NiceEntry.csproj

# Run tests (xunit.v3, plain net10.0 — no MAUI workload needed)
dotnet test NiceEntry.Tests/NiceEntry.Tests.csproj
```

**NiceEntry.Tests** covers the pure path geometry in `Drawing/NotchedBorderDrawing` by linking the source file directly — it tests without referencing the MAUI library. UI behavior is verified manually via the demo app.

## Architecture

### Control Hierarchy

`LabelBase` is the base control — a Grid containing a label (with required indicator `*`) floating over a `NotchedBorder`, plus example and error message labels below. The label sits over the top edge of the border; `LabelBase` computes the notch coordinates so the stroke leaves a gap behind the label text. Each concrete control inherits from `LabelBase` and composes a native MAUI control inside the border:

- **LabeledEntry** → wraps `EntryBase` (platform-specific Entry)
- **LabeledPicker** → wraps `PickerBase`
- **LabeledDatePicker** → wraps `DatePickerBase`
- **LabeledTimePicker** → wraps `TimePickerBase`

`LabeledAutoCompleteEntry` is a separate `ContentView` that composes a `LabeledEntry` plus a suggestion dropdown (`CollectionView` in a `Border`), forwarding a subset of `LabeledEntry`'s properties.

### Drawing

`Drawing/NotchedBorder` is an internal-by-convention control (public but `[EditorBrowsable(Never)]`) that renders the outlined border via `Microsoft.Maui.Graphics` (`GraphicsView` + `IDrawable`). The pure path geometry lives in `Drawing/NotchedBorderDrawing.BuildPath`.

### Key Pattern: BindableProperty Proxying

Each control proxies BindableProperties from the inner MAUI control to the outer labeled control. The pattern is:

1. Declare a `BindableProperty` with a `propertyChanged` handler
2. The handler casts to the control and calls an `Update*View()` method
3. The update method sets the value on the inner control
4. In the constructor, the inner control binds back via `SetBinding()` with `BindingContext = this`

### Platform-Specific Code

`EntryBase` uses conditional compilation (`#if ANDROID` / `#if IOS`) to select `EntryBaseNative`; `PickerBase`/`DatePickerBase`/`TimePickerBase` use inline `#if` blocks. All apply platform-specific styling through MAUI handler mappers, filtered on the NiceEntry view type so consumer controls are unaffected:
- **Android**: Transparent background on the underlying `AppCompatEditText`
- **iOS**: No border style on the underlying `UITextField`; the picker-style controls also override `MeasureOverride` to match the height of a borderless `UITextField` at the current font size (shared cache in `Base/NativeEntryHeight`)

### Validation

The `Error` property on `LabelBase` accepts `IReadOnlyCollection<string>`. When errors are present, the border turns red and error messages display below the control. The demo app shows integration with `CommunityToolkit.Mvvm`'s `ObservableValidator` via a custom `ValidatableViewModel` base class.

## Solution Structure

- **NiceEntry/** — The packable control library (namespace: `NiceEntry`)
- **NiceEntryDemoApp/** — Demo app showing all controls with MVVM validation

## Conventions

- File-scoped namespaces (`namespace NiceEntry;`)
- Nullable reference types enabled
- Implicit usings enabled
- BindableProperty fields named `{PropertyName}Property`
- Private update methods named `Update{Property}View()`
- Extension method `SetVisualElementBinding()` wires up `IsEnabled`/`IsVisible` on inner controls
- CRLF line endings

## CI/CD

NuGet-publicering sker automatiskt via GitHub Actions:

1. PR mergas till master → `release.yml` läser PR-labels (`major`/`minor`/`patch`, default `minor`) och skapar en GitHub release med bumpat versionsnummer
2. Release publiceras → `publish.yml` bygger, packar och pushar till nuget.org

Version i csproj är för lokala byggen — CI överskriver med `/p:Version` från git-taggen.

Kräver repository secret `NUGET_API_KEY`.

## Git & PR Policy

- Never add "Generated with Claude Code" or "Co-Authored-By: Claude" (or similar attribution) to commit messages, PR descriptions, or PR comments.
