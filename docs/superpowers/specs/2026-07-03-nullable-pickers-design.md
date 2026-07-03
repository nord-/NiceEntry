# Nullable LabeledDatePicker & LabeledTimePicker — design

**Datum:** 2026-07-03 (rev. 2 efter spec-review)
**Status:** Godkänd design, väntar på implementationsplan
**Versionspåverkan:** Breaking change → PR märks `major`

## Syfte

`LabeledDatePicker.Date` och `LabeledTimePicker.Time` ska kunna vara null och då visa
ett tomt fält, i stället för att alltid visa ett värde (idag `DateTime.Today` resp.
`TimeSpan.Zero`). Användaren ska kunna se skillnad på "inget valt" och "ett värde valt",
och `IsRequired`-validering ska fungera naturligt (null = inget valt).

## Grundfynd: MAUI 10 har nativt nullable-stöd

Projektet refererar `Microsoft.Maui.Controls 10.0.41`. Verifierat mot MAUI-källan
(branch `release/10.0.1xx`):

- `DatePicker.Date` är `DateTime?` (`DateProperty` skapas med `typeof(DateTime?)`);
  `TimePicker.Time` är `TimeSpan?`.
- Null renderas nativt som tom text:
  `platformDatePicker.Text = datePicker.Date?.ToString(datePicker.Format) ?? string.Empty`
  (Android `DatePickerExtensions`; iOS motsvarande).
- **Android:** dialogen öppnar på dagens datum vid null
  (`date?.Year ?? DateTime.Today.Year` …); OK anropar `VirtualView.Date = e.Date` —
  null → idag är en faktisk propertyändring och propageras. Cancel rör ingenting →
  fältet förblir tomt. Detta löser även review-fyndet "OK utan att ändra värde":
  från tomt läge är varje OK en ändring (null → värde). Det enda o-ändrings-fallet
  är OK på ett redan valt värde — och då är utfallet korrekt oavsett (värdet består).
- **iOS:** Done-knappen anropar `SetVirtualViewDate()` villkorslöst — det visade
  värdet committas även om användaren inte snurrat. Detta ÄR beslutet
  "öppnat = valt", implementerat nativt av MAUI. Dismiss utan Done lämnar värdet.

Konsekvens: ingen medieringslogik, ingen blank-rendering via handler-mappers,
inga custom handlers behövs. Dagens enkla TwoWay-`SetBinding` behålls — endast
de yttre propertyernas typer ändras.

## API-förändringar (breaking)

| Kontroll | Property | Före | Efter |
|---|---|---|---|
| `LabeledDatePicker` | `Date` | `DateTime`, default `DateTime.Today` | `DateTime?`, default `null` |
| `LabeledTimePicker` | `Time` | `TimeSpan`, default `default(TimeSpan)` | `TimeSpan?`, default `null` |
| Båda | `ShowClearButton` | — | ny `bool` BindableProperty, default `false` |

- `defaultBindingMode` förblir `TwoWay`; `defaultValueCreator: DateTime.Today` tas bort.
- `MinimumDate`/`MaximumDate` och `FontSize` oförändrade.
- Inre `DatePickerBase`/`TimePickerBase` behåller sina befintliga handler-mappers
  (transparent bakgrund, borderless, höjdmätning) — inget nytt där.

## Arkitektur

### Databindning

Oförändrat mönster: `Element.SetBinding(DatePicker.DateProperty, nameof(Date), TwoWay)`.
Typerna matchar nu på båda sidor (`DateTime?` ↔ `DateTime?`). Yttre default null
propageras till inre pickern när bindningen appliceras → fältet startar tomt.

### Clear-knappen

- I `LabeledDatePicker.xaml`/`LabeledTimePicker.xaml` wrappas `Element` i en `Grid`
  (kolumner `*,Auto`) tillsammans med ett ✕ — en `Label` med `TapGestureRecognizer`.
- Synlig endast när `ShowClearButton && värde != null && IsEnabled`.
  **OBS:** `SetVisualElementBinding` kopplar `IsEnabled` till `Element`, inte till
  wrappern — synlighetslogiken behöver egen lyssning på yttre `IsEnabled`
  (propertyChanged på `IsEnabledProperty` via `PropertyChanged`-event eller
  motsvarande), så ✕ döljs när kontrollen disablas.
- Tap → yttre `Date`/`Time` sätts till `null` → fältet blir blankt (nativt).
- **Träffyta:** ≥ 44×44 pt — `MinimumWidthRequest`/`MinimumHeightRequest` + padding
  på tap-ytan, inte bara glyfens naturliga storlek.
- **Semantik:** ✕ får `SemanticProperties.Description` ("Rensa" / lokaliserbar via
  befintlig resx-mekanism om sådan finns, annars engelska "Clear").
  `LabelBase.UpdateSemanticDescription` sätter beskrivningen på `View` = wrappern;
  picker-kontrollerna vidarebefordrar därför beskrivningen till inre `Element`
  själva så skärmläsare annonserar fältets etikett på själva pickern.
- Färg: samma `AppThemeBinding`-par som övrig text (Gray900 ljust / Gray100 mörkt),
  med sänkt opacity likt `Unit`-etiketten.
- Grid-wrappern ligger i `LabelBase.View`-slotten; `Unit`-texten (LabelBases egen
  Auto-kolumn) hamnar till höger om ✕:et.

## Kanteffekter och accepterade begränsningar

- **TimePicker från tomt läge öppnar på 00:00** (verifierat: `time?.Hours ?? 0`).
  Att öppna på aktuell tid i stället kräver custom handler-registrering
  (`CreateTimePickerDialog`-override + `UseNiceEntry()`-builder-extension) —
  bedömt som inte värt maskineriet. Accepterad MAUI-nativ egenhet.
- DatePicker från tomt läge öppnar på dagens datum (MAUI-nativt) — bra default.
- `MinimumDate`/`MaximumDate` påverkar bara inre pickern, oförändrat.
- Temaväxling: ✕ följer `AppThemeBinding`; blank-läget ägs av MAUI och är stabilt.

## Konsumentmigrering

- Bindningar mot `DateTime`/`TimeSpan` i VM:er behöver bli `DateTime?`/`TimeSpan?`
  (eller acceptera att kontrollen startar tom i stället för på dagens datum).
- Konsumenter som vill ha dagens beteende sätter initialvärde i VM:et
  (`Date = DateTime.Today`).
- README uppdateras med breaking change-notis och exempel.

## Demo & verifiering

Inget testprojekt finns; verifiering sker via demo-appen på Android och iOS:

1. Tomt startläge visas blankt (ljust + mörkt tema)
2. Android: öppna från tomt läge och tryck OK **utan att ändra värdet** →
   dagens datum / 00:00 committas; Cancel från tomt läge → fortsatt tomt
3. iOS: öppna + Done utan att snurra → visat värde committas
4. Clear-knapp: syns bara med värde + `ShowClearButton` + enabled; tap → tomt igen;
   disablad kontroll döljer ✕
5. TwoWay-binding: sätt/nollställ värde från VM → UI följer åt båda håll
6. `IsRequired` + validering i demo-appens ViewModel med nullable properties
7. Skärmläsare: fältets etikett annonseras på pickern, ✕ annonseras som "Clear"

Demo-appens sida uppdateras med nullable-exempel och `ShowClearButton`.
