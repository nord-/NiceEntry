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

`NiceButton` är en `ContentView` (XAML + code-behind) som wrappar en MAUI `Border` med
ett internt innehåll (ikon-`Label` + text-`Label`) i en layout-container. Mönstret följer
prior art `ChargeNodeButton` men använder MAUI:s inbyggda `Border` istället för en custom
frame.

- `Border` ger `StrokeShape` (Rectangle/RoundRectangle/Ellipse), `Shadow`,
  `Background`/`BackgroundColor` och `Stroke`/`StrokeThickness`.
- Ikon och text renderas som två `Label`. Ikonen är en font-glyph (MDI-fonten).
- En `TapGestureRecognizer` på `Border` driver tryck.
- `VisualStateManager` (CommonStates: Normal/Disabled) styr färger i disabled-läge.
- BindableProperty-proxying enligt NiceEntrys konvention (`{Property}Property`-fält,
  `propertyChanged`-handler, privat `Update{Property}View()`).

Följer NiceEntrys konventioner: file-scoped namespaces, nullable enabled, implicit usings,
CRLF, spaces.

### Avvisade alternativ
- **Ärva MAUI `Button`:** klarar inte vertikal ikon/text-layout eller cirkelform snyggt.
- **Helt ritad `GraphicsView`:** overkill; tappar text/font-rendering och tillgänglighet.

## 3. Layout- och variantmodell

Sex varianter härleds från två innehållsproperties plus två layoutproperties:

```
Icon (MaterialIcon?)   — default null  → "ingen ikon"
Text (string)          — default ""    → null/empty = "ingen text"
Orientation (enum)     — Horizontal | Vertical   (effekt endast när både Icon och Text är satta)
IconPlacement (enum)   — Start | End             (effekt endast när både Icon och Text är satta)
```

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
  - **Auto-kvadratisk:** i `Circle`-läge mäter kontrollen största sidan och sätter
    width = height så det alltid blir en perfekt cirkel, oavsett consumerns angivna mått.
    Typiskt använt ihop med icon only.

## 5. Färger, tema och skugga

Alla färger får tema-medvetna defaults via `AppThemeBinding` när de inte är satta av
consumern.

```
BackgroundColor (Color)   — knappens fyllning (tema-default)
Background (Brush)         — gradient e.d.; vinner över BackgroundColor om satt
TextColor (Color)          — DELAS av ikon och text (tema-default)
BorderColor (Color)        — Border.Stroke (default transparent)
BorderWidth (double)       — Border.StrokeThickness (default 0)
FontSize (double)          — text; default LabelBase.DefaultFontSize
FontFamily (string)        — text
FontAttributes (enum)      — text; default None
IconSize (double)          — ikonens fontstorlek; default 20
Spacing (double)           — mellanrum ikon/text; default 6
```

En enda `TextColor` driver **både** ikon-`Label` och text-`Label` — de kan aldrig hamna i
otakt (verbatim användarregel: ikon och text följs alltid åt).

**Disabled** (`VisualStateManager`, CommonStates):
- `IsEnabled = false` → byter till dämpade tema-färger (bakgrund + text/ikon), som
  `ChargeNodeButton`.
- `IsEnabled` integreras med `Command.CanExecute`: knappen går automatiskt till Disabled
  när command inte kan köras (lyssnar på `CanExecuteChanged`), och `CommandParameter`-
  ändring re-evaluerar `CanExecute` (fixar null-parameter-glappet från prior art).

**Skugga:**
```
HasShadow (bool)    — default false. true → inbyggd default-skugga.
Shadow (Shadow)     — override; om satt används den istället för default-skuggan.
```
`HasShadow=false` + ingen `Shadow` → ingen skugga. `HasShadow=true` → inbyggd default.
`Shadow` satt → vinner alltid.

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

### Paketering (känd fallgrop)
- `MauiFont` i ett bibliotek följer **inte** med via `PackageReference` per default.
  Lösning: medskickade `.props`/`.targets` i nupkg:en som exponerar fonten för
  consumern (samma klass av fix som `eu_s.svg`-problemet i ChargeNode-paketen).
- Consumern registrerar fonten via en ny `MauiAppBuilder`-extension **`.UseNiceEntry()`**
  som internt kallar `ConfigureFonts(...)` och registrerar MDI-fonten. Mönstret matchar
  `.UseCircularPicker()` / `.UseLicensePlate()`.

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

## 8. Komplett API-yta

| Kategori | Property | Typ | Default |
|---|---|---|---|
| Innehåll | `Text` | string | `""` |
| Innehåll | `Icon` | `MaterialIcon?` | `null` |
| Layout | `Orientation` | enum | `Horizontal` |
| Layout | `IconPlacement` | enum | `Start` |
| Layout | `Spacing` | double | 6 |
| Form | `ButtonShape` | enum | `Rounded` |
| Form | `CornerRadius` | double | 8 |
| Färg | `BackgroundColor` | Color | tema |
| Färg | `Background` | Brush | – |
| Färg | `TextColor` | Color | tema (ikon+text) |
| Färg | `BorderColor` | Color | transparent |
| Färg | `BorderWidth` | double | 0 |
| Text | `FontSize` | double | `LabelBase.DefaultFontSize` |
| Text | `FontFamily` | string | – |
| Text | `FontAttributes` | enum | None |
| Ikon | `IconSize` | double | 20 |
| Skugga | `HasShadow` | bool | `false` |
| Skugga | `Shadow` | Shadow | – |
| Interaktion | `Command` | ICommand | – |
| Interaktion | `CommandParameter` | object | – |
| Interaktion | `IsEnabled` | bool | true (ärvd) |

**Enums:**
- `ButtonShape { Rectangle, Rounded, Circle }`
- `Orientation { Horizontal, Vertical }` *(intern; namnkrock med MAUI `StackOrientation` undviks — egen enum i NiceEntry-namespace)*
- `IconPlacement { Start, End }`
- `MaterialIcon { ... }` (genererad, codepoint-värden)

## 9. Levererat med paketet

- `NiceButton` (ContentView) + tillhörande enums.
- `MaterialIcon`-enum (genererad) + inbäddad MDI-TTF.
- `.UseNiceEntry()` `MauiAppBuilder`-extension som registrerar fonten.
- `.props`/`.targets` så fonten följer med via `PackageReference`.
- **Demo:** ny sektion/sida i `NiceEntryDemoApp` som visar alla 6 layout-varianter, de 3
  formerna, skugga på/av, disabled och command-bindning — i samma stil som övriga demos.

## 10. Öppna implementationsdetaljer (löses i planen)

- Exakt generering av `MaterialIcon` (build-time source generator vs. incheckad genererad
  fil från `meta.json`). Designvalet är en **incheckad genererad fil** för enkelhet.
- Exakta default-tema-färger (light/dark) för bakgrund, text och disabled-läge.
- Auto-kvadratisk mätlogik för `Circle` (SizeChanged-baserad).
- Verifiering av `.props`/`.targets`-paketering i en consumer via `PackageReference`.
