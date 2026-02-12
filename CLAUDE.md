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
```

No test project exists currently.

## Architecture

### Control Hierarchy

`LabelBase` is the base control — a Grid containing a Label, required indicator (`*`), a Border wrapping a generic View, and error message labels. Each concrete control inherits from `LabelBase` and composes a native MAUI control inside the Border:

- **LabeledEntry** → wraps `EntryBase` (platform-specific Entry)
- **LabeledPicker** → wraps `Picker`
- **LabeledDatePicker** → wraps `DatePicker`
- **LabeledTimePicker** → wraps `TimePicker`

### Key Pattern: BindableProperty Proxying

Each control proxies BindableProperties from the inner MAUI control to the outer labeled control. The pattern is:

1. Declare a `BindableProperty` with a `propertyChanged` handler
2. The handler casts to the control and calls an `Update*View()` method
3. The update method sets the value on the inner control
4. In the constructor, the inner control binds back via `SetBinding()` with `BindingContext = this`

### Platform-Specific Code

`EntryBase` uses conditional compilation (`#if ANDROID` / `#if IOS`) to select `EntryBaseNative`, which applies platform-specific styling through MAUI handler mappers:
- **Android**: Transparent background on the underlying `AppCompatEditText`
- **iOS**: No border style on the underlying `UITextField`

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
