# NiceButton — design

**Datum:** 2026-06-09
**Status:** Godkänd design, redo för implementationsplan
**Paket:** NiceEntry

## 1. Syfte och avgränsning

NiceButton är en ny, **fristående** kontroll i NiceEntry: en tryckbar knapp som visar en
ikon och/eller text. Den lever helt utanför `LabelBase`/validerings-hierarkin (ingen
extern fält-label, inget required-indikator, ingen felvisning). Kontrollen täcker sex
layout-varianter (label only, icon only, samt ikon+text i fyra kombinationer), tre
formlägen, skugga, tema-medvetna färger och command-bindning.

**Ej i scope (första versionen):**
- Async-/laddningsläge med spinner (motsvarande `ChargeNodeButton.IsRefreshButton`).
- `Clicked`-event — endast `Command` exponeras.
- Storleks-presets (Small/Default/Large) — storlek sätts fritt via `IconSize`/`FontSize`/`Padding`.

## 2. Arkitektur

`NiceButton` är en **`Layout`-subklass** (ren code-behind, ingen XAML för roten) med en
egen `ILayoutManager`, som hostar en enda MAUI `Border` med internt innehåll (ikon-`Label`
+ text-`Label`). Mönstret följer prior art `ChargeNodeButton` men använder MAUI:s inbyggda
`Border` istället för en custom frame.

> **Varför `Layout` och inte `ContentView`:** `ContentView` har kända layout-buggar med
> inbäddade custom-layouts/mätning på Android (dotnet/maui#19471), vilket gör auto-kvadrat
> via mätning opålitligt. En egen `Layout` + `ILayoutManager` är den dokumenterade,
> pålitliga vägen för att styra mätning (se sektion 4). NiceButton bygger sitt innehåll i
> code-behind (som `LabelBase` bygger sin Grid i kod), så ingen XAML behövs för roten.

- `Border` ger `StrokeShape` (Rectangle/RoundRectangle/Ellipse), `Background`/
  `BackgroundColor` och `Stroke`/`StrokeThickness`.
- Ikon och text renderas som två `Label`. Ikonen är en font-glyph (MDI-fonten).
- En `TapGestureRecognizer` på `Border` driver tryck.
- Disabled-läget styrs av en `ApplyColors()`-metod (manuell färgväxling Normal/Disabled
  baserat på `IsEnabled`) snarare än `VisualStateManager` — KISS för två tillstånd i en
  code-only kontroll.
- BindableProperty-proxying enligt NiceEntrys konvention (`{Property}Property`-fält,
  `propertyChanged`-handler, privat `Update{Property}View()`). `BackgroundColor`/`Background`
  är **ärvda** från `VisualElement` och vidarebefordras till inre `Border` via
  `OnPropertyChanged` (ingen egen BindableProperty — undviker namnkrock).

Följer NiceEntrys konventioner: file-scoped namespaces, nullable enabled, implicit usings,
CRLF, spaces.

### Avvisade alternativ
- **Ärva MAUI `Button`:** klarar inte vertikal ikon/text-layout eller cirkelform snyggt.
- **Helt ritad `GraphicsView`:** overkill; tappar text/font-rendering och tillgänglighet.

## 3. Layout- och variantmodell

Sex varianter härleds från två innehållsproperties plus två layoutproperties:

```
Icon (MaterialIcon?)                     — default null  → "ingen ikon"
Text (string)                            — default ""    → null/empty = "ingen text"
Orientation (ButtonContentOrientation)   — Horizontal | Vertical   (effekt endast när både Icon och Text är satta)
IconPlacement (IconPlacement)            — Start | End             (effekt endast när både Icon och Text är satta)
```

Property-namnet är `Orientation`, men typen heter `ButtonContentOrientation` för att
undvika namnkrock med MAUI-typer i implicit-usings-scope (file-scoped namespace gör att
fully-qualified inte krävs i NiceEntry-kod).

Innehållsläget **härleds** (ingen separat enum):

| `Text` | `Icon` | Resultat |
|---|---|---|
| satt | satt | ikon + text (styrs av `Orientation`/`IconPlacement`) |
| tom | satt | icon only (auto-kvadratisk i `Circle`-läge) |
| satt | ej satt | label only |
| tom | ej satt | inget innehåll renderas |

Mappning mot de sex varianterna:

| Variant | Text | Icon | Orientation | IconPlacement |
|---|---|---|---|---|
| Label only | satt | – | – | – |
| Icon only | – | satt | – | – |
| Ikon vänster + text | satt | satt | `Horizontal` | `Start` |
| Text + ikon höger | satt | satt | `Horizontal` | `End` |
| Ikon topp + text | satt | satt | `Vertical` | `Start` |
| Text + ikon botten | satt | satt | `Vertical` | `End` |

`Orientation`/`IconPlacement` ignoreras tyst när läget inte är ikon+text.
Enumnamnen `Start`/`End` följer MAUI-konvention (`TextAlignment`, `LayoutOptions`).

Internt sitter ikon och text i en container vars riktning (rad/kolumn) och barn-ordning
sätts om vid ändring av `Orientation`/`IconPlacement`/`Icon`/`Text`. `Spacing` styr
mellanrummet mellan ikon och text.

## 4. Form (border) och hörnradie

```
ButtonShape (enum)     — Rectangle | Rounded | Circle   (default Rounded)
CornerRadius (double)  — default 8, bindable
```

- `Rectangle` → `StrokeShape = Rectangle` (raka hörn). `CornerRadius` ignoreras.
- `Rounded` → `StrokeShape = RoundRectangle` med `CornerRadius`. Enda läget som använder `CornerRadius`.
- `Circle` → `StrokeShape = Ellipse`. `CornerRadius` ignoreras.
  - **Auto-kvadratisk:** i `Circle`-läge ska knappen alltid bli en perfekt cirkel oavsett
    consumerns angivna mått. Detta löses via NiceButtons egna **`ILayoutManager`**: dess
    `Measure` mäter `Border` och returnerar en kvadratisk storlek (sida = max av uppmätt
    bredd/höjd) när `ButtonShape == Circle`, annars den naturliga storleken. `ArrangeChildren`
    placerar `Border` så att den fyller de tilldelade bounds. Detta undviker **både**
    `SizeChanged` + `WidthRequest`/`HeightRequest` (layout-rundtrips/race på Android) **och**
    `ContentView.MeasureOverride` (opålitligt per dotnet/maui#19471). Circle används typiskt
    ihop med icon only.

## 5. Färger, tema och skugga

Alla färger får tema-medvetna defaults via `AppThemeBinding` när de inte är satta av
consumern.

```
BackgroundColor (Color)   — knappens fyllning (tema-default)
Background (Brush)         — gradient e.d.; vinner över BackgroundColor om satt
TextColor (Color)          — DELAS av ikon och text (tema-default)
BorderColor (Color)        — Border.Stroke (default transparent)
BorderWidth (double)       — Border.StrokeThickness (default 0)
FontSize (double)          — text; default NiceButton.DefaultFontSize (14.0)
FontFamily (string)        — text
FontAttributes (enum)      — text; default None
IconSize (double)          — ikonens fontstorlek; default 20
Spacing (double)           — mellanrum ikon/text; default 6
ContentPadding (Thickness) — avstånd mellan border-kant och innehåll; proxas till inre Border.Padding
```

`NiceButton.DefaultFontSize = 14.0` (egen konstant, inte `LabelBase.DefaultFontSize` —
som är fältanpassad och blir för liten för knapptext på iOS). `ContentPadding` är en egen
BindableProperty som sätter den inre `Border.Padding` (det visuella innehållsavståndet),
**inte** den ärvda `Layout.Padding`. Namnet `ContentPadding` följer `LabelBase`-konventionen
och undviker krock med ärvda `Padding`. Default följer `LabelBase.ContentPadding`:
iOS `(12, 12)`, Android `(12, 10)`.

En enda `TextColor` driver **både** ikon-`Label` och text-`Label` — de kan aldrig hamna i
otakt (verbatim användarregel: ikon och text följs alltid åt).

**Disabled** (`ApplyColors()`, manuell färgväxling — inte `VisualStateManager`):
- `IsEnabled = false` → byter till dämpade tema-färger (bakgrund + text/ikon). Med bara två
  tillstånd och en code-only kontroll är en `ApplyColors()`-metod som anropas vid relevanta
  property-ändringar enklare och mer DRY än VSM.
- `IsEnabled` integreras med `Command.CanExecute`: knappen går automatiskt till Disabled
  när command inte kan köras (lyssnar på `CanExecuteChanged`), och `CommandParameter`-
  ändring re-evaluerar `CanExecute` (fixar null-parameter-glappet från prior art).

**Skugga:**
```
HasShadow (bool)         — default false. true → inbyggd default-skugga.
CustomShadow (Shadow)    — override; om satt används den istället för default-skuggan.
```
`HasShadow=false` + ingen `CustomShadow` → ingen skugga. `HasShadow=true` → inbyggd
default. `CustomShadow` satt → vinner alltid. Skuggan appliceras på den inre `Border`.

> **Namnval:** override-propertyn heter `CustomShadow`, **inte** `Shadow`, eftersom
> `VisualElement.Shadow` redan finns ärvd sedan .NET 7. Att redeklarera en `Shadow`-
> BindableProperty skulle skugga den ärvda och ge tvetydiga bindningar. Den ärvda
> `Shadow` på själva kontrollen lämnas oanvänd; all skugg-rendering sker på inre `Border`.

## 6. Ikoner (Material Design Icons)

```
Icon (MaterialIcon?)   — vald ikon, t.ex. MaterialIcon.Pencil
IconSize (double)      — ikonens fontstorlek
```

- Paketet bäddar in MDI:s TTF (`materialdesignicons-webfont.ttf`) som `MauiFont` med
  aliaset `"MaterialDesignIcons"`.
- En **genererad** `MaterialIcon`-enum (incheckad genererad C#-fil från MDI:s `meta.json`,
  ~7000 medlemmar) där **enum-värdet är glyph-codepointen**, t.ex.
  `Pencil = 0xF03EB`. Glyph fås med `char.ConvertFromUtf32((int)Icon)` — ingen separat
  uppslagstabell behövs.
- Ikon-`Label` får `FontFamily = "MaterialDesignIcons"`,
  `Text = char.ConvertFromUtf32((int)Icon)`, `TextColor = TextColor`, `FontSize = IconSize`.

### Paketering (löser känd fallgrop)
- `MauiFont` i ett bibliotek följer **inte** med via `PackageReference` per default, vilket
  annars kräver bräckliga medskickade `.props`/`.targets` (samma klass av problem som
  `eu_s.svg` i ChargeNode-paketen). Vi **undviker** detta helt: fonten bäddas in som
  `EmbeddedResource` och registreras med `IFontCollection.AddEmbeddedResourceFont(...)`.
  Embedded resources följer automatiskt med i nupkg:en — inga `.props`/`.targets` behövs.
  (Begränsning: embedded fonts fungerar inte i Windows-unpackaged-appar, men NiceEntry
  targetar bara Android/iOS.)
- Consumern registrerar fonten via en ny `MauiAppBuilder`-extension **`.UseNiceEntry()`**
  som internt kallar `ConfigureFonts(...)` och registrerar MDI-fonten. Mönstret matchar
  `.UseCircularPicker()` / `.UseLicensePlate()`.
- `.UseNiceEntry()` ska vara **idempotent och konfliktfri**: om consumern redan har en egen
  `ConfigureFonts(...)` eller råkar anropa extensionen två gånger får det inte krascha.
  `AddFont` med samma alias upprepat är ofarligt (sista vinner), men extensionen ska inte
  förutsätta att den är ensam om att registrera fonter.

Licens: MDI-fonten är Apache-2.0 / SIL OFL — inkludera attribuering i paketet enligt
licenskrav.

## 7. Interaktion

```
Command (ICommand)         — körs vid tryck
CommandParameter (object)  — skickas till Command
IsEnabled (bool)           — ärvd; kopplad till Command.CanExecute
```

- `TapGestureRecognizer` på `Border` driver tryck.
- Tryck-feedback: opacity-fade på innehållet — `FadeTo(0.3, 100)` → `FadeTo(1, 100)` —
  därefter körs `Command` (prior art `ChargeNodeButton`).
- `Command.CanExecuteChanged` lyssnas på → automatiskt Disabled-state.
- Inget `Clicked`-event, inget spinner-/async-läge (YAGNI).

**IsEnabled och tap (explicit mönster):** `TapGestureRecognizer` ska inte förlita sig på
att `IsEnabled` på parent-containern automatiskt blockerar tryck. Två lager:
1. Inre `Border.IsEnabled` binds till kontrollens `IsEnabled` (som `ChargeNodeButton`).
2. Tap-handlern guardar explicit: `if (!IsEnabled) return;` följt av
   `Command?.CanExecute(CommandParameter)`-kontroll innan `Command` körs (och innan
   fade-animationen, så en disabled knapp inte ger feedback).

## 8. Komplett API-yta

| Kategori | Property | Typ | Default |
|---|---|---|---|
| Innehåll | `Text` | string | `""` |
| Innehåll | `Icon` | `MaterialIcon?` | `null` |
| Layout | `Orientation` | `ButtonContentOrientation` | `Horizontal` |
| Layout | `IconPlacement` | `IconPlacement` | `Start` |
| Layout | `Spacing` | double | 6 |
| Layout | `ContentPadding` | Thickness | iOS `(12,12)` / Android `(12,10)` |
| Form | `ButtonShape` | `ButtonShape` | `Rounded` |
| Form | `CornerRadius` | double | 8 |
| Färg | `BackgroundColor` | Color | tema |
| Färg | `Background` | Brush | – |
| Färg | `TextColor` | Color | tema (ikon+text) |
| Färg | `BorderColor` | Color | transparent |
| Färg | `BorderWidth` | double | 0 |
| Text | `FontSize` | double | `NiceButton.DefaultFontSize` (14.0) |
| Text | `FontFamily` | string | – |
| Text | `FontAttributes` | FontAttributes | None |
| Ikon | `IconSize` | double | 20 |
| Skugga | `HasShadow` | bool | `false` |
| Skugga | `CustomShadow` | Shadow | – |
| Interaktion | `Command` | ICommand | – |
| Interaktion | `CommandParameter` | object | – |
| Interaktion | `IsEnabled` | bool | true (ärvd) |

**Enums:**
- `ButtonShape { Rectangle, Rounded, Circle }`
- `ButtonContentOrientation { Horizontal, Vertical }` *(egen enum; property heter `Orientation`, typnamnet är explicit för att undvika krock med MAUI-typer i implicit-usings-scope)*
- `IconPlacement { Start, End }`
- `MaterialIcon { ... }` (genererad, codepoint-värden)

**Konstant:**
- `NiceButton.DefaultFontSize = 14.0` (default för `FontSize`)

## 9. Levererat med paketet

- `NiceButton` (ContentView) + tillhörande enums.
- `MaterialIcon`-enum (genererad) + MDI-TTF som `EmbeddedResource`.
- `.UseNiceEntry()` `MauiAppBuilder`-extension som registrerar fonten via
  `AddEmbeddedResourceFont`.
- **Demo:** ny sektion/sida i `NiceEntryDemoApp` som visar alla 6 layout-varianter, de 3
  formerna, skugga på/av, disabled och command-bindning — i samma stil som övriga demos.

## 10. Öppna implementationsdetaljer (löses i planen)

- Exakt generering av `MaterialIcon` (build-time source generator vs. incheckad genererad
  fil från `meta.json`). Designvalet är en **incheckad genererad fil** för enkelhet.
- Exakta default-tema-färger (light/dark) för bakgrund, text och disabled-läge.
- Detaljerad `MeasureOverride`-implementation för `Circle` (kvadratisk mätning).
- Verifiering av att den inbäddade fonten följer med och kan registreras i en consumer
  via `PackageReference` (embedded resource).
- Generering av `MaterialIcon` från MDI `scss/_variables.scss` (MDI 7.4.47).
