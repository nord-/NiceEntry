# Nullable Pickers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gör `LabeledDatePicker.Date` och `LabeledTimePicker.Time` nullable (tomt fält vid null) och lägg till opt-in clear-knapp (`ShowClearButton`).

**Architecture:** MAUI 10 har redan nativt nullable-stöd i `DatePicker.Date` (`DateTime?`) och `TimePicker.Time` (`TimeSpan?`) inkl. blank rendering — de yttre proxy-propertyerna byter bara typ, den befintliga TwoWay-bindningen behålls. Clear-knappen är en ✕-Label i en Grid-wrapper runt inre pickern i respektive kontrolls XAML.

**Tech Stack:** .NET MAUI 10 (Microsoft.Maui.Controls 10.0.41), net10.0-android + net10.0-ios. Inget testprojekt finns — verifiering via build + demo-app (spec:ens avsnitt "Demo & verifiering").

**Spec:** `docs/superpowers/specs/2026-07-03-nullable-pickers-design.md`

## Global Constraints

- TargetFrameworks: `net10.0-android;net10.0-ios` — build ska lyckas för båda
- Breaking change: PR:en ska märkas med label `major` (styr CI-versionering)
- CRLF-radslut, spaces (inte tabbar)
- Filscopade namespaces (`namespace NiceEntry;`), nullable reference types på
- BindableProperty-fält heter `{PropertyName}Property`; privata update-metoder heter `Update{Property}View()`
- Ingen AI-attribution i commits
- Byggkommando: `dotnet build NiceEntry/NiceEntry.csproj` resp. `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`; förväntat: "Build succeeded"

---

### Task 1: Nullable Date/Time på yttre kontrollerna

**Files:**
- Modify: `NiceEntry/LabeledDatePicker.xaml.cs:16-25`
- Modify: `NiceEntry/LabeledTimePicker.xaml.cs:16-23`

**Interfaces:**
- Consumes: MAUI:s `DatePicker.DateProperty` (`DateTime?`) och `TimePicker.TimeProperty` (`TimeSpan?`) — nullable sedan MAUI 10.
- Produces: `LabeledDatePicker.Date` som `DateTime?` (default `null`), `LabeledTimePicker.Time` som `TimeSpan?` (default `null`). Task 2 och 3 hänger på dessa typer.

- [ ] **Step 1: Byt typ på Date**

I `NiceEntry/LabeledDatePicker.xaml.cs`, ersätt property-deklarationen (rad 16) och CLR-propertyn (rad 21–25):

```csharp
public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime?), typeof(LabeledDatePicker), defaultBindingMode: BindingMode.TwoWay);
```

```csharp
public DateTime? Date
{
    get => (DateTime?)GetValue(DateProperty);
    set => SetValue(DateProperty, value);
}
```

Observera: `defaultValueCreator: static _ => DateTime.Today` tas bort — default är nu `null`. Konstruktorns `Element.SetBinding(DatePicker.DateProperty, nameof(Date), BindingMode.TwoWay)` lämnas orörd (typerna matchar nu: `DateTime?` ↔ `DateTime?`).

- [ ] **Step 2: Byt typ på Time**

I `NiceEntry/LabeledTimePicker.xaml.cs`, ersätt property-deklarationen (rad 16) och CLR-propertyn (rad 19–23):

```csharp
public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(TimeSpan?), typeof(LabeledTimePicker), defaultBindingMode: BindingMode.TwoWay);
```

```csharp
public TimeSpan? Time
{
    get => (TimeSpan?)GetValue(TimeProperty);
    set => SetValue(TimeProperty, value);
}
```

- [ ] **Step 3: Bygg biblioteket**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: Build succeeded, 0 errors (varningar om LF/CRLF från git är OK).

- [ ] **Step 4: Commit**

```bash
git add NiceEntry/LabeledDatePicker.xaml.cs NiceEntry/LabeledTimePicker.xaml.cs
git commit -m "feat!: make LabeledDatePicker.Date and LabeledTimePicker.Time nullable"
```

---

### Task 2: ShowClearButton på LabeledDatePicker

**Files:**
- Modify: `NiceEntry/LabeledDatePicker.xaml`
- Modify: `NiceEntry/LabeledDatePicker.xaml.cs`

**Interfaces:**
- Consumes: `Date` som `DateTime?` (Task 1); `LabelBase.Label`-propertyn (ärvd); `Element.BindingContext = this`-mönstret.
- Produces: `LabeledDatePicker.ShowClearButton` (`bool` BindableProperty, default `false`); XAML-element `ClearButton` (Label) och eventhandler `OnClearTapped`. Task 3 speglar exakt samma mönster; Task 4 använder `ShowClearButton` i demo-appen.

- [ ] **Step 1: Wrappa Element i en Grid med ✕ i XAML**

Ersätt hela innehållet i `NiceEntry/LabeledDatePicker.xaml` med:

```xml
<ne:LabelBase xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
              xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
              xmlns:ne="clr-namespace:NiceEntry"
              x:Class="NiceEntry.LabeledDatePicker">
    
    <ne:LabelBase.View>
        <Grid ColumnDefinitions="*,Auto">
            <ne:DatePickerBase x:Name="Element" />
            <Label x:Name="ClearButton"
                   Grid.Column="1"
                   Text="&#x2715;"
                   IsVisible="False"
                   FontSize="16"
                   Opacity="0.6"
                   VerticalOptions="Fill"
                   MinimumWidthRequest="44"
                   HorizontalTextAlignment="Center"
                   VerticalTextAlignment="Center"
                   TextColor="{AppThemeBinding Light={StaticResource Gray900}, Dark={StaticResource Gray100}}"
                   SemanticProperties.Description="Clear">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnClearTapped" />
                </Label.GestureRecognizers>
            </Label>
        </Grid>
    </ne:LabelBase.View>
    
</ne:LabelBase>
```

Designnoter (från spec:en):
- `MinimumWidthRequest="44"` + `VerticalOptions="Fill"` ger ≥44 pt bred träffyta över hela innehållshöjden utan att blåsa upp kontrollens höjd (därför INGEN `MinimumHeightRequest`).
- `StaticResource Gray900/Gray100` följer samma mönster som `LabelBase.xaml` (nycklarna kommer från konsumentappens MAUI-mall-resurser).
- `Opacity="0.6"` matchar `Unit`-etikettens dämpning.

- [ ] **Step 2: Lägg till ShowClearButton + synlighetslogik i code-behind**

Ersätt hela innehållet i `NiceEntry/LabeledDatePicker.xaml.cs` med:

```csharp
namespace NiceEntry;

public partial class LabeledDatePicker
{
    public LabeledDatePicker()
    {
        InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(DatePicker.DateProperty, nameof(Date), BindingMode.TwoWay);
        // LabelBase sätter semantikbeskrivningen på View, som numera är Grid-wrappern —
        // vidarebefordra etiketten till själva pickern så skärmläsare annonserar den där.
        Element.SetBinding(SemanticProperties.DescriptionProperty, nameof(Label));
        Element.BindingContext = this;

        // SetVisualElementBinding kopplar IsEnabled till Element, inte till ✕:et —
        // lyssna själv så clear-knappen döljs när kontrollen disablas.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsEnabled))
                UpdateClearButtonView();
        };

        UpdateFontSizeView();
        UpdateClearButtonView();
    }
    
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime?), typeof(LabeledDatePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: DateChanged);
    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(nameof(MinimumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MinValue, propertyChanged: MinimumDateChanged);
    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(nameof(MaximumDate), typeof(DateTime), typeof(LabeledDatePicker), DateTime.MaxValue, propertyChanged: MaximumDateChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledDatePicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);
    public static readonly BindableProperty ShowClearButtonProperty = BindableProperty.Create(nameof(ShowClearButton), typeof(bool), typeof(LabeledDatePicker), false, propertyChanged: ShowClearButtonChanged);
    
    public DateTime? Date
    {
        get => (DateTime?)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public DateTime MinimumDate
    {
        get => (DateTime)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public DateTime MaximumDate
    {
        get => (DateTime)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }
    
    private static void DateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateClearButtonView();
    private static void MinimumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMinimumDateView();
    private static void MaximumDateChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateMaximumDateView();
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateFontSizeView();
    private static void ShowClearButtonChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledDatePicker)bindable).UpdateClearButtonView();
    
    private void UpdateMinimumDateView() => Element.MinimumDate = MinimumDate;
    private void UpdateMaximumDateView() => Element.MaximumDate = MaximumDate;
    private void UpdateFontSizeView() => Element.FontSize = FontSize;
    private void UpdateClearButtonView() => ClearButton.IsVisible = ShowClearButton && Date is not null && IsEnabled;

    private void OnClearTapped(object? sender, TappedEventArgs e) => Date = null;
}
```

- [ ] **Step 3: Bygg biblioteket**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add NiceEntry/LabeledDatePicker.xaml NiceEntry/LabeledDatePicker.xaml.cs
git commit -m "feat: add ShowClearButton to LabeledDatePicker"
```

---

### Task 3: ShowClearButton på LabeledTimePicker

**Files:**
- Modify: `NiceEntry/LabeledTimePicker.xaml`
- Modify: `NiceEntry/LabeledTimePicker.xaml.cs`

**Interfaces:**
- Consumes: `Time` som `TimeSpan?` (Task 1). Samma mönster som Task 2 men för TimePicker.
- Produces: `LabeledTimePicker.ShowClearButton` (`bool` BindableProperty, default `false`). Task 4 använder den i demo-appen.

- [ ] **Step 1: Wrappa Element i en Grid med ✕ i XAML**

Ersätt hela innehållet i `NiceEntry/LabeledTimePicker.xaml` med:

```xml
<ne:LabelBase xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
              xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
              xmlns:ne="clr-namespace:NiceEntry"
              x:Class="NiceEntry.LabeledTimePicker">
    
    <ne:LabelBase.View>
        <Grid ColumnDefinitions="*,Auto">
            <ne:TimePickerBase x:Name="Element" />
            <Label x:Name="ClearButton"
                   Grid.Column="1"
                   Text="&#x2715;"
                   IsVisible="False"
                   FontSize="16"
                   Opacity="0.6"
                   VerticalOptions="Fill"
                   MinimumWidthRequest="44"
                   HorizontalTextAlignment="Center"
                   VerticalTextAlignment="Center"
                   TextColor="{AppThemeBinding Light={StaticResource Gray900}, Dark={StaticResource Gray100}}"
                   SemanticProperties.Description="Clear">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnClearTapped" />
                </Label.GestureRecognizers>
            </Label>
        </Grid>
    </ne:LabelBase.View>
    
</ne:LabelBase>
```

- [ ] **Step 2: Lägg till ShowClearButton + synlighetslogik i code-behind**

Ersätt hela innehållet i `NiceEntry/LabeledTimePicker.xaml.cs` med:

```csharp
namespace NiceEntry;

public partial class LabeledTimePicker
{
    public LabeledTimePicker()
    {
        InitializeComponent();
        
        Element.SetVisualElementBinding();
        Element.SetBinding(TimePicker.TimeProperty, nameof(Time), BindingMode.TwoWay);
        // LabelBase sätter semantikbeskrivningen på View, som numera är Grid-wrappern —
        // vidarebefordra etiketten till själva pickern så skärmläsare annonserar den där.
        Element.SetBinding(SemanticProperties.DescriptionProperty, nameof(Label));
        Element.BindingContext = this;

        // SetVisualElementBinding kopplar IsEnabled till Element, inte till ✕:et —
        // lyssna själv så clear-knappen döljs när kontrollen disablas.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsEnabled))
                UpdateClearButtonView();
        };

        UpdateFontSizeView();
        UpdateClearButtonView();
    }
    
    public static readonly BindableProperty TimeProperty = BindableProperty.Create(nameof(Time), typeof(TimeSpan?), typeof(LabeledTimePicker), defaultBindingMode: BindingMode.TwoWay, propertyChanged: TimeChanged);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabeledTimePicker), LabelBase.DefaultFontSize, propertyChanged: FontSizeChanged);
    public static readonly BindableProperty ShowClearButtonProperty = BindableProperty.Create(nameof(ShowClearButton), typeof(bool), typeof(LabeledTimePicker), false, propertyChanged: ShowClearButtonChanged);
    
    public TimeSpan? Time
    {
        get => (TimeSpan?)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }
    
    private static void TimeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateClearButtonView();
    private static void FontSizeChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateFontSizeView();
    private static void ShowClearButtonChanged(BindableObject bindable, object oldValue, object newValue) => ((LabeledTimePicker)bindable).UpdateClearButtonView();

    private void UpdateFontSizeView() => Element.FontSize = FontSize;
    private void UpdateClearButtonView() => ClearButton.IsVisible = ShowClearButton && Time is not null && IsEnabled;

    private void OnClearTapped(object? sender, TappedEventArgs e) => Time = null;
}
```

Notera: medveten duplicering av clear-knappslogiken mellan Task 2 och 3 — en generisk bas för två fall vore över-abstraktion (beslut från spec-reviewen).

- [ ] **Step 3: Bygg biblioteket**

Run: `dotnet build NiceEntry/NiceEntry.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add NiceEntry/LabeledTimePicker.xaml NiceEntry/LabeledTimePicker.xaml.cs
git commit -m "feat: add ShowClearButton to LabeledTimePicker"
```

---

### Task 4: Demo-appen visar nullable + clear-knapp

**Files:**
- Modify: `NiceEntryDemoApp/MainPage.xaml:54-64`
- Modify: `NiceEntryDemoApp/MainPage.xaml.cs:27-28`

**Interfaces:**
- Consumes: `Date` (`DateTime?`), `Time` (`TimeSpan?`), `ShowClearButton` (Task 1–3).
- Produces: inget — demonstration/verifieringsyta.

- [ ] **Step 1: Uppdatera picker-exemplen i MainPage.xaml**

Ersätt rad 54–64 i `NiceEntryDemoApp/MainPage.xaml` med:

```xml
        <ne:LabeledDatePicker Label="Select a date"
                              Date="{Binding DateSelected}"
                              MinimumDate="2025-05-01"
                              ShowClearButton="True"
                              Example="Starts empty — pick a date, clear with the cross"
                              Error="{Binding ValidationErrors[DateSelected]}" />

        <ne:LabeledTimePicker Label="Select a time"
                              Time="{Binding TimeSelected}"
                              IsRequired="True"
                              ShowClearButton="True"
                              Error="{Binding ValidationErrors[TimeSelected]}"
                              />
```

- [ ] **Step 2: Låt VM:et starta tomt och validera null**

I `NiceEntryDemoApp/MainPage.xaml.cs`, ersätt rad 27–28:

```csharp
    [ObservableProperty] private DateTime? _dateSelected;
    [ObservableProperty,NotifyDataErrorInfo,Required(ErrorMessage = "You have to pick a time")] private TimeSpan? _timeSelected;
```

(Startvärdena `DateTime.Today`/`DateTime.Now.TimeOfDay` tas bort så fälten demonstrerar tomt startläge; `Required` på tiden demonstrerar `IsRequired` + null-validering. Toast-formatsträngen på rad 71 hanterar null utan ändring.)

- [ ] **Step 3: Bygg demo-appen**

Run: `dotnet build NiceEntryDemoApp/NiceEntryDemoApp.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add NiceEntryDemoApp/MainPage.xaml NiceEntryDemoApp/MainPage.xaml.cs
git commit -m "feat: demo nullable pickers and clear button in demo app"
```

---

### Task 5: README — nullable-dokumentation och breaking change-notis

**Files:**
- Modify: `README.md:101-110` (avsnittet "### Date and time pickers")

**Interfaces:**
- Consumes: API:t från Task 1–3.
- Produces: inget — dokumentation.

- [ ] **Step 1: Uppdatera picker-avsnittet**

Ersätt avsnittet "### Date and time pickers" (rad 101–110) i `README.md` med:

````markdown
### Date and time pickers

`Date` and `Time` are nullable — the field renders empty until a value is picked
(or set from the view model). Set `ShowClearButton="True"` to show a ✕ that resets
the value to `null`.

```xml
<nice:LabeledDatePicker Label="Select a date"
                        Date="{Binding SelectedDate}"
                        ShowClearButton="True" />

<nice:LabeledTimePicker Label="Select a time"
                        Time="{Binding SelectedTime}"
                        IsRequired="True" />
```

> **Breaking change (2.0):** `LabeledDatePicker.Date` is now `DateTime?` (default `null`,
> previously `DateTime.Today`) and `LabeledTimePicker.Time` is now `TimeSpan?` (default
> `null`). Bind to nullable view-model properties, and set an initial value
> (e.g. `Date = DateTime.Today`) to keep the previous behavior.

Platform notes:

- **Android:** the value commits when the user taps OK; Cancel keeps the field empty.
- **iOS:** the displayed value commits when the picker closes (Done), even if the user
  didn't scroll — opened means selected.
- A time picker opened from the empty state starts at 00:00 (MAUI default).
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: document nullable pickers and ShowClearButton"
```

---

## Manuell verifiering efter alla tasks (från spec:en)

Kräver emulator/enhet — utförs av Rickard eller på begäran:

1. Tomt startläge visas blankt (ljust + mörkt tema)
2. Android: öppna från tomt läge, tryck OK utan att ändra → dagens datum / 00:00 committas; Cancel från tomt läge → fortsatt tomt
3. iOS: öppna + Done utan att snurra → visat värde committas
4. Clear-knapp: syns bara med värde + `ShowClearButton` + enabled; tap → tomt igen; disablad kontroll döljer ✕
5. TwoWay-binding: sätt/nollställ värde från VM → UI följer åt båda håll
6. `IsRequired` + validering med nullable VM-properties (demo-appens time-fält)
7. Skärmläsare: fältets etikett annonseras på pickern, ✕ annonseras som "Clear".
   **Bevaka särskilt dubbelannonsering:** `LabelBase.UpdateSemanticDescription` sätter
   etiketten på `View` (= Grid-wrappern) samtidigt som code-behind binder samma text
   till pickern — på Android kan en ViewGroup med contentDescription dubbelannonsera
   eller skugga ✕:ets "Clear". Felar detta ligger fixen i `LabelBase` (skippa
   description när `View` är en layout), inte i picker-kontrollerna.
8. ✕:ets träffyta på iOS: 44 pt bred men bara innehållshög (~25–30 pt) — medvetet
   spec-beslut (ingen `MinimumHeightRequest` för att inte blåsa upp kontrollen), men
   känn på den i emulatorn och ompröva om den upplevs för snål.

PR:en märks med label **major** innan merge.
