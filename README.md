# LMU Karriere-tool

Et tredjeparts karrieresystem for **Le Mans Ultimate**, som midlertidig erstatning frem til
Studio 397 slipper en offisiell karrieremodus.

Verktøyet **styrer ikke spillet**. Det forteller deg hva du skal sette opp (bane, bil, format,
vær, motstandere), du setter opp Race Weekend manuelt i LMU, og etterpå leser verktøyet
resultatfilene fra `UserData\Log\Results` og oppdaterer karrieren din: XP, nivå, poeng,
mesterskapstabell, rating, credits og kontrakter.

---

## Innhold

1. [Status akkurat nå](#status-akkurat-nå)
2. [Kjente feil (rotårsaker)](#kjente-feil-rotårsaker)
3. [Arkitektur og filstruktur](#arkitektur-og-filstruktur)
4. [Fase-plan](#fase-plan)
5. [Designnotater](#designnotater)
6. [Bygge og kjøre](#bygge-og-kjøre)
7. [Vedlegg A: verifiserte navn fra spillet](#vedlegg-a-verifiserte-navn-fra-spillet)

---

## Status akkurat nå

| Område | Status |
|---|---|
| Parsing av resultat-XML (P/Q/R, runder, sektorer, incidents) | ✅ Fungerer |
| Gruppering av Practice/Qualifying/Race til én løpshelg | ✅ Overlever nå restart av appen (`PendingWeekendStore`) |
| XP, nivå, poeng, rating, credits | ✅ Regnes ut |
| Sesonggenerering (baner, format, vær, bil) | ✅ Verifiserte navn, kun ekte GT3/Hypercar-baner brukes |
| Garasje / valg av klasse og merke | ✅ Fase 0-fiksene er på plass (se F1-F4 under) |
| Bilvalidering mot sesongens bil | ✅ Riktige navn + `validate`-kommando (Fase 1) |
| Oppsett-avvik (feil bane/bil) | ✅ Tydelig varsel + «godkjenn likevel» (Fase 1) |
| Mesterskapstabell mot faste rivaler | ✅ Fører- og merkemesterskap, fastlåst startfelt (Fase 2) |
| Sesongavslutning med oppsummering | ⚠️ Minimalt vindu (kun tabell) - full rapport kommer i Fase 4 |
| Fører-profil / 3D | ❌ Finnes ikke - Fase 5 |
| Installer | ❌ Finnes ikke - Fase 7 |
| Tester | ❌ Finnes ikke - Fase 8 |

Fase 0, 1 og 2 er gjennomført. Se avsnittene under for hva som faktisk ble bygget, og hva som
bevisst ble utelatt eller forenklet fra den opprinnelige planen.

---

## Kjente feil (rotårsaker)

> **Alle F1-F9 under er nå fikset** (F1-F4 i Fase 0, F5-F9 i Fase 1). Beholdt som dokumentasjon
> av hva som faktisk var galt og hvorfor - nyttig hvis noe av dette regredierer.

Dette er det faktiske svaret på «nedtrekksmenyen har ingen valg, og jeg får ikke lukket eller
valgt bil». Det var **tre feil som opptrer samtidig** i samme vindu.

### F1 – Usynlig tekst i alle ComboBox-er (rotårsaken til «ingen valg»)

`LmuCareerTool.App/App.xaml:18` definerer en **implisitt stil** for `TextBlock`:

```xml
<Style TargetType="TextBlock">
    <Setter Property="Foreground" Value="{StaticResource TextColor}" />  <!-- #FFEDEDED -->
</Style>
```

Implisitte `TextBlock`-stiler på `Application.Resources`-nivå lekker inn i alle
`ContentPresenter`-genererte tekstblokker i hele appen – også de inne i ComboBox-popupen.
Popupen bruker Windows' egen **lyse** bakgrunn, så elementene tegnes som **nesten hvit tekst på
hvit bakgrunn**. Valgene er der (klasselista inneholder «GT3»), de er bare usynlige.

**Fiks:** gi TextBlock-stilen en `x:Key` og påfør den eksplisitt, eller legg inn en skikkelig
mørk `ComboBox`/`ComboBoxItem`-stil i `Theme/Controls.xaml`. Sistnevnte er riktig løsning,
siden hele appen skal ha samme mørke tema.

### F2 – «Start ny sesong»-knappen faller utenfor vinduet

`SeasonSummaryWindow.xaml` har `Height="760"`, `ResizeMode="NoResize"` og seks `Auto`-rader.
GT3 har 8 merker à ~54 px = ~430 px bare i garasjelista. Til sammen blir innholdet høyere enn
vinduet, og rad 5 (`StartButton`) havner utenfor synlig område. Vinduet er modalt, så det føles
som om appen har hengt seg.

**Fiks:** `SizeToContent="Height"` + `MaxHeight`, gjør merkelista til en `ScrollViewer`, gi
knapperaden `Height="Auto"` nederst i en `DockPanel`, og skru på `ResizeMode="CanResize"`.

### F3 – Ingen radioknapp blir noen gang markert

`SeasonSummaryWindow.xaml.cs:69-80` setter `_selectedManufacturer` i kode, men ingenting binder
`RadioButton.IsChecked`. Kommentaren i koden innrømmer det. Resultat: forhåndsvalget er usynlig,
og hvis du klikker feil sted får du «Velg et merke først».

**Fiks:** `ManufacturerRowVm` implementerer `INotifyPropertyChanged` med en `IsSelected`-property
som bindes `TwoWay` mot `IsChecked`. Fjern `Checked`-event-handleren.

### F4 – Karrierefila kan ikke leses tilbake (krasj ved andre oppstart)

`CareerStore.cs:37` skriver med `JsonStringEnumConverter` (`"Format": "Sprint"`), men
`CareerStore.cs:26` leser **uten** converteren. Så snart karrierefila inneholder en sesong,
kaster `Deserialize` og appen viser «Klarte ikke å starte». Dette er en garantert krasj så snart
F1–F3 er fikset og du faktisk får startet en sesong.

**Fiks:** bruk samme `JsonOptions` i begge retninger.

### F5 – Bil- og banenavn i `game-content.json` matcher ikke spillet

Verifisert mot 38 ekte resultatfiler i din egen Results-mappe. De aller fleste navnene er feil:

| I `game-content.json` | Faktisk `<CarType>` i spillet |
|---|---|
| `Corvette Z06 LMGT3` | `Chevrolet Corvette Z06 LMGT3.R` |
| `Lamborghini Huracan LMGT3` | `Lamborghini Huracan LMGT3 Evo2` |
| `McLaren 720S LMGT3` | `McLaren 720S LMGT3 Evo` |
| `Aston Martin Vantage LMGT3` | `Aston Martin Vantage AMR LMGT3` |
| `Ferrari 296 LMGT3` | finnes, men også `Ferrari 296 LMGT3 Evo` |
| `Toyota GR010` | `Toyota TR010` |
| `Genesis GMR-001` | `Genesis GMR001` |
| `Spa-Francorchamps` | `Circuit de Spa-Francorchamps` |
| `Imola` | `Autodromo Enzo e Dino Ferrari` |
| *(mangler helt)* | `Lexus RCF LMGT3`, `Mercedes-AMG LMGT3` |
| *(mangler helt)* | `Alpine A424`, `Aston Martin Valkyrie LMH`, `Lamborghini SC63` |

Konsekvensen: `CareerEngine.cs:154` sammenligner `race.CarType` mot `matchedEvent.AssignedCar`,
matchen feiler alltid, du får **0 XP** og runden markeres aldri som fullført. Sesongen kan i
praksis aldri gjøres ferdig.

Full verifisert liste ligger i [Vedlegg A](#vedlegg-a-verifiserte-navn-fra-spillet).

### F6 – Multiplayer-løp telles som karriereløp

Resultatfilene har `<Setting>Race Weekend</Setting>` (offline mot AI) eller
`<Setting>Multiplayer</Setting>`. Verktøyet skiller ikke, så et tilfeldig offentlig lobbyløp på
samme bane kan «bruke opp» en sesongrunde. Dette må filtreres.

### F7 – Sesongrunder matches kun på banenavn

`CareerEngine.cs:143` finner første ikke-fullførte event med samme bane. Hvis samme bane går
igjen i kalenderen, eller du kjører runde 5 før runde 3, blir feil runde kreditert.

### F8 – Karrierefila skrives ved siden av .exe-en

`AppContext.BaseDirectory` fungerer under `dotnet run`, men når appen installeres til
`Program Files` er mappa skrivebeskyttet. Må flyttes til `%LOCALAPPDATA%` før Fase 7.

### F9 – Småting

* `SeasonModel.BestFinish` gir `P0` når ingen runder er fullført (`DefaultIfEmpty(0).Min()`).
* `WeekendGrouper` lever kun i minnet – stopper du appen mellom kvalifisering og løp, mistes
  kvalik-dataene.
* `SeasonGenerator` kan gi samme bane to ganger hvis banelista er kortere enn antall runder.
* Ingen tester i det hele tatt.

---

## Arkitektur og filstruktur

Målbildet etter alle fasene. `[N]` viser hvilken fase filen kommer inn i; filer uten markør
finnes allerede. **`[0]`, `[1]` og `[2]` er nå bygget** (bortsett fra `Directory.Build.props`,
`Views/`/`ViewModels/`-flyttingen og `AiResultSynthesizer.cs` - se avvik under hver fase).

```
LmuCareerTool/
├── LmuCareerTool.sln
├── README.md
├── Directory.Build.props                      [0]  felles versjon/nullable/langversion
│
├── LmuCareerTool.Core/                        all logikk, ingen UI-avhengigheter
│   ├── Content/
│   │   ├── game-content.json                       biler, merker, baner, vær
│   │   ├── entry-list.json                    [2]  LMUs AI-førerstall (navn + team + bil)
│   │   ├── points-systems.json                [4]  valgbare poengskalaer (WEC/F1/egen)
│   │   ├── GameContent.cs
│   │   ├── ContentLoader.cs
│   │   └── ContentValidator.cs                [1]  sjekker content mot ekte resultatfiler
│   │
│   ├── Models/
│   │   ├── SessionResult.cs
│   │   ├── RaceWeekendResult.cs
│   │   ├── CareerProfile.cs
│   │   ├── DriverProfile.cs                   [5]  bilde, nasjonalitet, nummer, hjelm
│   │   ├── ChampionshipStanding.cs            [2]
│   │   └── Contract.cs                        [3]
│   │
│   ├── Parsing/
│   │   ├── ResultXmlParser.cs
│   │   ├── WeekendGrouper.cs
│   │   ├── SessionModeFilter.cs               [1]  Race Weekend vs Multiplayer
│   │   └── PendingWeekendStore.cs             [1]  P/Q overlever restart av appen
│   │
│   ├── Season/
│   │   ├── SeasonModel.cs
│   │   ├── SeasonGenerator.cs
│   │   ├── CalendarBuilder.cs                 [1]  unike baner, sesongtema, kalenderregler
│   │   └── SeasonRules.cs                     [1]  antall runder, endurance-andel, feltstørrelse
│   │
│   ├── Championship/                          [2]
│   │   ├── ChampionshipTable.cs                    tabell for førere og merker
│   │   ├── FieldRoster.cs                          låser feltet ved sesongstart
│   │   ├── RosterMatcher.cs                        matcher AI-navn mellom løp
│   │   └── AiResultSynthesizer.cs                  fyller inn fravær / hopper over runder
│   │
│   ├── Career/
│   │   ├── CareerEngine.cs
│   │   ├── CareerStore.cs
│   │   ├── XpCalculator.cs
│   │   ├── PointsCalculator.cs
│   │   ├── RatingCalculator.cs
│   │   ├── CreditsCalculator.cs
│   │   ├── ClassUnlockService.cs
│   │   ├── SeasonSummaryBuilder.cs            [4]  bygger hele avslutningsrapporten
│   │   └── AwardService.cs                    [4]  sesongpriser og milepæler
│   │
│   ├── Transfers/                             [3]  (erstatter ManufacturerUnlockService)
│   │   ├── ContractService.cs                      aktiv kontrakt, lengde, mål, lønn
│   │   ├── InterestModel.cs                        hvor mye hvert merke vil ha deg
│   │   ├── OfferGenerator.cs                       konkrete tilbud ved sesongslutt
│   │   └── TransferWindow.cs                       flyten: tilbud → forhandling → signering
│   │
│   ├── Validation/                            [1]
│   │   ├── PreRaceChecklist.cs                     hva du SKAL sette opp i LMU
│   │   └── SetupValidator.cs                       bil/bane/klasse/feltstørrelse-avvik
│   │
│   ├── Settings/
│   │   ├── AppSettings.cs
│   │   ├── AppSettingsStore.cs
│   │   └── LmuPathLocator.cs                  [1]  finner LMU via Steam-registry
│   │
│   └── Watching/
│       └── ResultsWatcher.cs
│
├── LmuCareerTool.App/                         WPF-desktop-app (Windows)
│   ├── App.xaml / App.xaml.cs
│   ├── Theme/                                 [0]
│   │   ├── Colors.xaml                             farger og pensler
│   │   ├── Controls.xaml                           ComboBox, RadioButton, DataGrid, Button
│   │   └── Typography.xaml
│   ├── Views/                                 [0]  (flyttes hit fra rotmappa)
│   │   ├── MainWindow.xaml
│   │   ├── SeasonSummaryWindow.xaml
│   │   ├── RaceDetailWindow.xaml
│   │   ├── GarageWindow.xaml                  [3]  kontrakter og tilbud, eget vindu
│   │   ├── ChampionshipView.xaml              [2]  tabell som egen fane
│   │   ├── DriverProfileWindow.xaml           [5]  fører-kort med 3D-hjelm
│   │   └── FirstRunWizard.xaml                [7]  førstegangsoppsett
│   ├── ViewModels/                            [0]
│   │   ├── ViewModelBase.cs                        INotifyPropertyChanged
│   │   ├── MainViewModel.cs
│   │   ├── SeasonEventRow.cs
│   │   ├── ManufacturerRowVm.cs
│   │   ├── StandingRowVm.cs                   [2]
│   │   └── OfferRowVm.cs                      [3]
│   ├── Controls/                              [5]
│   │   ├── DriverAvatar.xaml                       rundt bilde + 3D-ikon
│   │   └── HelmetViewport.xaml                     Viewport3D med roterbar hjelm
│   └── Assets/                                [5]
│       ├── helmet.obj
│       └── flags/
│
├── LmuCareerTool.Console/                     testverktøy (replay, season, validate)
│
├── LmuCareerTool.Tests/                       [8]  xUnit
│   ├── Fixtures/                                   ekte, anonymiserte resultat-XML-er
│   ├── ParserTests.cs
│   ├── ChampionshipTests.cs
│   ├── TransferTests.cs
│   └── SeasonGeneratorTests.cs
│
└── Installer/                                 [7]
    ├── LmuCareerTool.iss                           Inno Setup-skript
    ├── build-installer.ps1                         publish + kompiler installer
    └── Assets/ (ikon, lisens, banner)
```

---

## Fase-plan

### Fase 0 – Få appen brukbar igjen 🔴

Kortest mulig vei fra «ubrukelig» til «kan spilles». Ingen nye funksjoner.

* Opprett `Theme/Colors.xaml` + `Theme/Controls.xaml` med eksplisitte, mørke stiler for
  `ComboBox`, `ComboBoxItem`, `RadioButton`, `CheckBox`, `DataGrid`, `ScrollViewer`, `TextBox`.
* Fjern den implisitte globale `TextBlock`-stilen (**F1**).
* Bygg `SeasonSummaryWindow` på nytt: `DockPanel` med fast knapperad nederst, scrollbar
  garasjeliste, `SizeToContent="Height"`, `MaxHeight`, resizable (**F2**).
* `ManufacturerRowVm` → `INotifyPropertyChanged` + `IsSelected` bundet `TwoWay` (**F3**).
* Samme `JsonSerializerOptions` for lesing og skriving i `CareerStore` (**F4**).
* Flytt views/viewmodels inn i `Views/` og `ViewModels/`.
* **Ferdig når:** du kan skrive inn navnet ditt, velge GT3 + BMW, se en 9-runders kalender,
  lukke og åpne appen igjen uten at noe krasjer.

### Fase 1 – Riktige data og reell validering ✅ Ferdig

* `game-content.json` skrevet på nytt fra verifiserte navn (**F5**, se Vedlegg A) - lagt til
  Lexus og Mercedes-AMG som nye GT3-merker siden de dukket opp i dine faktiske resultater, og
  merket LMP2/LMP3 tydelig som uverifiserte (`"_verified": false`) siden ingen resultatfiler
  finnes for dem ennå.
* `ContentValidator.cs` + `dotnet run --project LmuCareerTool.Console -- validate [mappe]`
  skanner hele Results-mappa og rapporterer hvert `CarType`/`TrackVenue` som ikke finnes i
  content-fila. Kjørt mot dine egne 38 filer: **0 ukjente baner, 0 ukjente biler.**
* `SessionModeFilter.cs`: kun `<Setting>Race Weekend</Setting>` teller for karrieren.
  Multiplayer-økter logges som «teller ikke mot karrieren» i loggen, både i appen og
  konsollverktøyet (**F6**). Testet mot en ekte multiplayer-fil - filtreres korrekt.
* `PreRaceChecklist.cs`: bygger en kopierbar tekst-oppskrift (bane, klasse/merke, bil, format,
  varighet, vær) for neste runde. **Kopier oppskrift**-knapp lagt til i "Neste løp"-kortet i
  appen.
* `SetupValidator.cs`: sammenligner faktisk bane OG bil mot det sesongen krever, ikke bare
  bilen som før. Feil oppsett gir en tydelig varselmelding i loggen, og i appen et
  ja/nei-dialogvindu: *«Godkjenne runden likevel med dette resultatet?»* - trykker du ja,
  krediteres runden manuelt med `CareerEngine.ApproveDespiteMismatch(...)`. Verifisert med et
  ekte feil-bil-scenario (kjørte Porsche der sesongen krevde BMW) - ga korrekt 0 XP og riktig
  varsel.
* Match sesongrunde på **kun neste ikke-fullførte runde**, ikke et løst banesøk (**F7**) - du
  kan ikke lenger fullføre runder ute av rekkefølge eller kreditere feil runde ved dupliserte
  baner.
* `LmuPathLocator.cs`: finner LMU automatisk via Steam sin registry-nøkkel +
  `libraryfolders.vdf`, brukes som fallback for standard Results-sti ved førstegangsoppsett.
* `PendingWeekendStore.cs`: Practice/Qualifying-cachen lagres nå til
  `pending_career_<navn>.json` og gjenopprettes ved oppstart (**F9**).

**Ikke gjort i konsollverktøyet:** «godkjenn likevel»-flyten er kun bygget i desktop-appen -
konsollverktøyet viser avviket, men har ingen kommando for å godkjenne det manuelt ennå.

### Fase 2 – Mesterskap med faste rivaler ✅ Ferdig

Dette er det som gjør det til en karriere i stedet for en resultatlogg, og det **går an** – LMU
bruker sin virkelige WEC-førerstall i «Race Weekend»-modus (Ahmad Al Harthy / Team WRT,
José María López / Akkodis ASP, Rahel Frey / Iron Dames …), ikke tilfeldige navn. Verifisert
med en ekte 25-førers Daytona-startliste: alle 25 navnene ble korrekt låst som sesongens felt.

* `FieldRoster.cs`: den første fullførte runden i sesongen **låser feltet**
  (`SeasonModel.LockedRosterNames`). Alle navn (normalisert) lagres som sesongens startliste.
* `RosterMatcher.cs`: normaliserer navn (fjerner `#1234`-suffiks, trim, case-insensitivt) slik
  at samme sjåfør gjenkjennes selv om LMU endrer suffikset mellom runder.
* Håndtering av avvik i `ChampionshipTable.cs`:
  * Fører i den låste rosteren, ikke i rundens resultat → telles som deltatt runde uten poeng
    (DNS).
  * Ny fører dukker opp senere → legges automatisk til og merkes **"(reserve)"** i tabellen,
    får poeng fra og med den runden.
* `ChampionshipTable.ComputeDriverStandings` / `ComputeManufacturerStandings`: fører- og
  merkemesterskap fra sesongens lagrede rundedata, med samme poengskala (F1 25-18-15…) som din
  egen sesongpoengsum. Merke-mapping gjenbruker `game-content.json` sine
  merke→bil-definisjoner, med bilnavnet som fallback for Hypercar (som ikke har
  merke-oppsett ennå).
  Kjøring gjennom hvert runde-resultat er `O(runder × feltstørrelse)`, helt greit for en
  9-runders sesong.
* `ChampionshipWindow`: åpnes via en ny **Mesterskap**-knapp i hovedvinduet. Viser
  førermesterskap (med ▲▼-trend fra forrige runde, din egen rad merket med ★) og
  merkemesterskap side om side.

**Avvik fra opprinnelig plan** (bevisste forenklinger, ikke glemt):
* `AiResultSynthesizer` ble aldri en egen fil - DNS-/reserve-håndtering var enkel nok til å
  høre hjemme direkte i `ChampionshipTable`, en egen "syntese"-abstraksjon ga ingen verdi.
* `ChampionshipView` er et eget modalt vindu (samme mønster som `RaceDetailWindow`), ikke en
  fane i hovedvinduet ennå - ekte faneinndeling er en del av Fase 6 (UX-oppussing), som ikke er
  gjort.
* «Neste løp»-kortet viser fortsatt feltoppsettet som en **anbefaling**, ikke et håndhevet
  krav (feltstørrelse valideres ikke av `SetupValidator`) - LMUs AI-feltstørrelse styres uansett
  av spillets egne baneinnstillinger, ikke noe verktøyet kan tvinge frem.
* Kun testet med data fra én ekte "Race Weekend"-fil (Daytona, 25 førere) siden det er alt som
  finnes i din Results-mappe per nå - selve rosterlåsingen og feltlagringen er verifisert, men
  et fullt sesongforløp med flere runder (DNS, reserver, ▲▼-trend i praksis) er ikke testet mot
  ekte spilldata ennå. Kjør et par runder til og si ifra hvis noe ser rart ut i
  mesterskapstabellen.

### Fase 3 – Transfer-marked på nytt 🟡

Dagens modell er selvmotsigende, akkurat som du sier: du får et «tilbud», men samtidig kan du
bare kjøpe deg inn hos hvem som helst med credits, og et merke som er låst opp er låst opp for
alltid. Forslag til en modell som henger sammen:

**Kontrakt i stedet for opplåsing.** Du har til enhver tid **én kontrakt** med **ett merke**,
med lengde i sesonger, en avtalt lønn i credits per runde, og et **sesongmål** («topp 5
sammenlagt»). Ingenting «låses opp permanent» – du kjører for den du har kontrakt med.

**Interesse i stedet for terskler.** Hvert merke regner ut en interesse-score (0-100) fra:

| Faktor | Vekt |
|---|---|
| Driver Rating | tyngst |
| Form siste sesong (plassering + poeng) | tung |
| Renhet (incidents/straffer per løp) | middels |
| Om merket har ledig sete (mettet stall vil ikke ha deg) | gate |
| Lojalitet / historikk hos merket | liten bonus |
| Prestisje-gap (Porsche vil ikke ha en rating-45-fører) | dempende |

**Overgangsvindu ved sesongslutt.** Når sesongen er ferdig åpnes `TransferWindow`:

1. Merker med høy nok interesse sender **konkrete tilbud** – merke, bil, kontraktslengde,
   lønn, sesongmål, og en kort begrunnelse.
2. Du kan **bli** hos nåværende merke (de gir et fornyelsestilbud hvis de er fornøyde).
3. Du kan **si opp tidlig** ved å betale en bruddsum i credits – der hører credits hjemme.
4. Har du ingen tilbud og ingen kontrakt, finnes alltid et **betalsete** hos et privatlag: du
   *betaler* credits for setet, får ingen lønn, og en liten rating-straff. Realistisk nødutgang,
   og mye mer interessant enn «kjøp Porsche for 7500 cr».
5. Innfrir du ikke sesongmålet, kan merket **si deg opp**.

**Klasseopprykk** styres fortsatt av XP (Rating åpner merker *innad* i klassen), men et opprykk
til LMP2/Hypercar krever nå også et **tilbud** fra et lag i den klassen – ikke bare en terskel.

Filer: `Transfers/ContractService.cs`, `InterestModel.cs`, `OfferGenerator.cs`,
`TransferWindow.cs`, `Views/GarageWindow.xaml`. `ManufacturerUnlockService.cs` utgår.

### Fase 4 – Sesongavslutning som føles som en avslutning 🟢

`SeasonSummaryBuilder` produserer en flersides rapport i stedet for dagens ene tabell:

* **Sammendrag:** sluttplassering i mesterskapet, poeng, seire, podier, pole positions,
  raskeste runder, DNF-er, snittplassering, gjennomkjørte kilometer.
* **Runde for runde:** din plassering, poeng, kumulativ posisjon i tabellen – som graf.
* **Sluttabell:** førere og merker.
* **Personlige rekorder:** beste runde per bane, sammenlignet med tidligere sesonger.
* **Sesongpriser** (`AwardService`): «Årets fører», «Mest forbedret», «Renest kjørestil»,
  «Comeback of the season» (flest posisjoner vunnet i ett løp).
* **Kontraktsoppgjør:** innfridde du sesongmålet? Hva sier merket?
* **Eksport til PNG/HTML**, så du kan dele resultatet med kompisen din.
* Deretter: overgangsvinduet (Fase 3) → ny sesong genereres automatisk med **nye baner**
  (`CalendarBuilder` unngår å gjenbruke fjorårets kalender og garanterer unike baner).

### Fase 5 – Fører-profil og 3D 🟢

* `DriverProfile`: navn, nasjonalitet (flagg), fast startnummer, valgt bilde, hjelmfarger.
* `DriverAvatar`-kontroll i headeren: rundt bilde + et lite **3D-ikon** ved siden av navnet.
* Klikk på 3D-ikonet åpner `DriverProfileWindow` med en `Viewport3D`
  (**HelixToolkit.Wpf** via NuGet) som viser en roterbar hjelm-modell teksturert med
  hjelmfargene dine. Du kan dra for å rotere, og laste opp et eget bilde til visiret/profilen.
* Samme vindu viser karrierestatistikk på tvers av alle sesonger.

### Fase 6 – UX-oppussing 🟢

* Ekte MVVM (Fase 0 legger grunnlaget) i stedet for kode-bak som roter i kontroller.
* Venstremeny med faner: **Neste løp · Sesong · Mesterskap · Garasje · Fører · Historikk**.
* Oppsettspanelet skjules etter førstegangsoppsett.
* Toast-varsler for opplåsinger, tilbud og feil oppsett, i stedet for kun logglinjer.
* «Neste løp»-kortet får kopier-knapp og eventuelt en alltid-øverst mini-widget du kan ha
  synlig mens du er i LMU-menyene.

### Fase 7 – Windows-installer 🟡

Målet ditt: kompisen laster ned én fil, kjører den, og alt er klart.

* **Inno Setup** (ikke WiX – langt enklere, og du trenger ingen MSI-funksjoner).
* `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
  slik at .NET 10 **ikke** må installeres på forhånd.
* **Per-bruker-installasjon** til `%LOCALAPPDATA%\Programs\LmuCareerTool` → ingen
  administrator-rettigheter, ingen UAC-prompt.
* Karriere-, innstillings- og innholdsfiler flyttes til
  `%LOCALAPPDATA%\LmuCareerTool\` (**F8**). Avinstallering spør om karrieren skal beholdes.
* Startmeny- og skrivebordssnarvei, riktig ikon, versjonsinfo i .exe-en.
* `FirstRunWizard`: finner LMU automatisk (Steam-registry + `libraryfolders.vdf`), foreslår
  Results-mappa, ber om førernavn, verifiserer at navnet faktisk finnes i en av resultatfilene
  dine, og lar deg velge startklasse. Full validering før du kommer inn i appen.
* `build-installer.ps1` gjør alt i én kommando.
* Bonus senere: oppdateringssjekk mot GitHub Releases.

### Fase 8 – Tester og vedlikehold 🟢

* `LmuCareerTool.Tests` (xUnit) med ekte resultat-XML-er som fixtures.
* Dekning på det som er lett å ødelegge: parser, weekend-gruppering, roster-matching,
  poengberegning, interesse-/tilbudsmodellen, sesonggenerator.
* GitHub Actions: bygg + test + publiser installer på tag.
* **Sett prosjektet under git** – det er ikke et git-repo i dag, og det bør det være før Fase 1.

---

## Designnotater

### Hvorfor «Race Weekend»-modus er et krav for mesterskapet

Resultatfilene skiller på `<Setting>`. I `Race Weekend` (offline) fyller LMU feltet fra sin
virkelige WEC-startliste med faste navn og team. I `Multiplayer` er navnene tilfeldige
mennesker som aldri kommer igjen. Mesterskapstabellen er derfor kun meningsfull for offline
Race Weekend, og verktøyet må håndheve det.

### Feltstørrelse må være konsistent

Kjører du runde 1 med 25 biler og runde 2 med 44, blir tabellen meningsløs. `PreRaceChecklist`
oppgir eksakt antall motstandere, og `SetupValidator` advarer ved avvik.

### Credits skal ikke kunne kjøpe alt

Å kunne kjøpe seg rett inn hos Porsche undergraver hele progresjonen. Etter Fase 3 brukes
credits til bruddsummer, betalseter og (senere) laguppgraderinger – aldri til å hoppe over
sportslig kvalifisering.

### Innholdsfila skal aldri gjettes på igjen

`dotnet run -- validate` leser dine faktiske resultatfiler og sier ifra om hvert navn som ikke
stemmer. Alle biler og baner som legges til i `game-content.json` skal være verifisert med den
kommandoen først.

---

## Bygge og kjøre

```powershell
dotnet build
```

```powershell
dotnet run --project LmuCareerTool.App
```

```powershell
dotnet run --project LmuCareerTool.Console -- validate
```

Innstillinger lagres i `settings.json`, karrieren i `career_<navn>.json`
(flyttes til `%LOCALAPPDATA%\LmuCareerTool\` i Fase 7).

---

## Vedlegg A: verifiserte navn fra spillet

Hentet ut av 38 resultatfiler i `UserData\Log\Results` 8. august 2026. Bruk **nøyaktig** disse
skrivemåtene i `game-content.json`.

**GT3 (`<CarClass>GT3</CarClass>`)**

```
Aston Martin Vantage AMR LMGT3
BMW M4 LMGT3
Chevrolet Corvette Z06 LMGT3.R
Ferrari 296 LMGT3
Ferrari 296 LMGT3 Evo
Ford Mustang LMGT3
Lamborghini Huracan LMGT3 Evo2
Lexus RCF LMGT3
McLaren 720S LMGT3 Evo
Mercedes-AMG LMGT3
Porsche 911 GT3 R LMGT3
```

**Hypercar (`<CarClass>Hyper</CarClass>`)**

```
Alpine A424
Aston Martin Valkyrie LMH
BMW M Hybrid V8
Cadillac V-Series.R
Ferrari 499P
Genesis GMR001
Lamborghini SC63
Peugeot 9x8
Porsche 963
Toyota TR010
```

**Baner (bekreftet)**

```
Autodromo Enzo e Dino Ferrari
Bahrain International Circuit
Circuit de Spa-Francorchamps
Circuit de la Sarthe
Daytona International Speedway
Fuji Speedway
WeatherTech Raceway Laguna Seca
```

> Dette er kun banene du selv har kjørt så langt. Resten (Sebring, Portimão, Interlagos, COTA,
> Monza, Barcelona) må bekreftes med én kort testøkt per bane før de legges inn – `validate`
> viser deg riktig skrivemåte.

**LMP2/LMP3:** ingen resultatfiler ennå. Må bekreftes på samme måte.
