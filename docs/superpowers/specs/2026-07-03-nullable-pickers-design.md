# Nullable LabeledDatePicker & LabeledTimePicker — design

**Datum:** 2026-07-03
**Status:** Godkänd design, väntar på implementationsplan
**Versionspåverkan:** Breaking change → PR märks `major`

## Syfte

`LabeledDatePicker.Date` och `LabeledTimePicker.Time` ska kunna vara null och då visa
ett tomt fält, i stället för att alltid visa ett värde (idag `DateTime.Today` resp.
`TimeSpan.Zero`). Användaren ska kunna se skillnad på "inget valt" och "ett värde valt",
och `IsRequired`-validering ska fungera naturligt (null = inget valt).

## Plattformsgotcha (styrande för designen)

- **Android:** pickerdialogerna kräver OK/Cancel. MAUI:s `DateSelected`/`TimeSelected`
  triggas bara vid OK — Cancel lämnar värdet orört. Alltså: Cancel från tomt läge
  lämnar fältet tomt utan extra logik.
- **iOS:** värdet ändras direkt när användaren snurrar hjulet; det finns ingen
  OK/Cancel, bara Done/dismiss. Det går inte att skilja "valde" från "ångrade sig".
  **Beslut:** på iOS committas det visade värdet när pickern stängs (`Unfocused`),
  även om användaren inte snurrat. Öppnat = valt.

## API-förändringar (breaking)

| Kontroll | Property | Före | Efter |
|---|---|---|---|
| `LabeledDatePicker` | `Date` | `DateTime`, default `DateTime.Today` | `DateTime?`, default `null` |
| `LabeledTimePicker` | `Time` | `TimeSpan`, default `default(TimeSpan)` | `TimeSpan?`, default `null` |
| Båda | `ShowClearButton` | — | ny `bool` BindableProperty, default `false` |

`MinimumDate`/`MaximumDate` och `FontSize` är oförändrade. `defaultBindingMode`
förblir `TwoWay` på `Date`/`Time`.

## Arkitektur

### Dataflöde — mediering ersätter TwoWay-binding

Dagens direkta `Element.SetBinding(DatePicker.DateProperty, nameof(Date), TwoWay)`
tas bort. Ytterkontrollen medierar i stället:

**Ut → in (propertyChanged på yttre `Date`/`Time`):**
- Icke-null → pusha värdet till `Element.Date`/`Element.Time` och markera inre
  pickern som icke-blank.
- Null → markera inre pickern som blank (se nedan). Inre pickerns eget värde lämnas
  orört — det blir startposition om användaren öppnar pickern.

**In → ut:**
- **Android:** prenumerera på `Element.DateSelected`/`Element.TimeSelected` →
  sätt yttre property. Triggas bara vid OK.
- **iOS:** prenumerera på `Element.Unfocused` → committa `Element.Date`/`Element.Time`
  till yttre property när pickern stängs (Done eller dismiss).
- Plattformsval via `#if ANDROID` / `#if IOS`, samma mönster som `EntryBase` m.fl.

**Loop-skydd:** jämför värdet innan set (både ut→in och in→ut); sätt bara vid
faktisk skillnad.

### Blank-rendering

De nativa pickervyerna kan aldrig visa tom text via MAUI-API:t. Lösning: en intern
flagga (`IsBlank`) på `DatePickerBase`/`TimePickerBase`. De befintliga statiska
handler-mapparna utökas så att när `IsBlank` är sann rensas den nativa textytan:

- **Android:** `handler.PlatformView.Text = ""` (underliggande `AppCompatEditText`
  för både date- och timepicker).
- **iOS:** `handler.PlatformView.Text = ""` (underliggande `UITextField`).

**Kritisk detalj:** MAUI skriver om native-texten varje gång handlern mappar
`Date`/`Time`, `Format`, `FontSize` m.fl. Blank-rensningen måste därför appliceras
via `AppendToMapping` på just de nycklarna (t.ex. `nameof(DatePicker.Date)`,
`nameof(DatePicker.Format)`), plus en re-apply när `IsBlank` själv ändras
(via `handler.UpdateValue(...)` eller motsvarande), så att blanket överlever
alla omritningar. Exakt nyckeluppsättning verifieras mot MAUI:s handler-källa
under implementationen.

### Clear-knappen

- I `LabeledDatePicker.xaml`/`LabeledTimePicker.xaml` wrappas `Element` i en `Grid`
  (kolumner `*,Auto`) tillsammans med ett ✕ — en `Label` med `TapGestureRecognizer`.
- Synlig endast när `ShowClearButton && värde != null && IsEnabled`.
- Tap → yttre `Date`/`Time` sätts till `null` → fältet blir blankt.
- Färg: samma `AppThemeBinding`-par som övrig text (Gray900 ljust / Gray100 mörkt),
  gärna med sänkt opacity likt `Unit`-etiketten.
- Grid-wrappern ligger i `LabelBase.View`-slotten; `Unit`-texten (LabelBases egen
  Auto-kolumn) hamnar till höger om ✕:et. `SemanticProperties`-beskrivningen som
  `LabelBase` sätter på `View` hamnar på wrappern — pickerns semantik verifieras
  under implementationen, och ✕:et får egen semantisk beskrivning ("Rensa").

## Kanteffekter

- Öppnas pickern från tomt läge startar den på inre pickerns aktuella värde
  (default idag / 00:00) — avsiktligt och rimligt.
- `MinimumDate`/`MaximumDate` fungerar oförändrat; de påverkar bara inre pickern.
- Disabled kontroll: clear-knappen döljs; blank-rendering påverkas inte.
- Temaväxling: ✕ och blank-läge ska överleva ljust/mörkt-byte (handler-mappern
  re-appliceras vid omritning).

## Konsumentmigrering

- Bindningar mot `DateTime`/`TimeSpan` i VM:er behöver bli `DateTime?`/`TimeSpan?`
  (eller acceptera att kontrollen startar tom i stället för på dagens datum).
- Konsumenter som vill ha dagens beteende sätter initialvärde i VM:et
  (`Date = DateTime.Today`).
- README uppdateras med breaking change-notis och exempel.

## Demo & verifiering

Inget testprojekt finns; verifiering sker via demo-appen på Android och iOS:

1. Tomt startläge visas blankt (ljust + mörkt tema)
2. Välj värde → visas; Android Cancel från tomt läge → fortsatt tomt
3. iOS: öppna + Done utan att snurra → dagens datum/visad tid committas
4. Clear-knapp: syns bara med värde + `ShowClearButton`, tap → tomt igen
5. TwoWay-binding: sätt/nollställ värde från VM → UI följer
6. `IsRequired` + validering i demo-appens ViewModel med nullable properties

Demo-appens sida uppdateras med nullable-exempel och `ShowClearButton`.
