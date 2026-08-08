namespace LmuCareerTool.App.Localization;

/// <summary>
/// Alle oversettbare tekster for Tier 1-vinduene (ModeSelectWindow, WelcomeWindow,
/// LeagueWelcomeWindow, MainWindow, LeagueMainWindow, App.xaml.cs). Andre vinduer
/// (SeasonSummaryWindow, SeasonReportWindow, DriverProfileWindow, RaceDetailWindow,
/// ManufacturerInterestWindow, MiniWidgetWindow) er bevisst IKKE oversatt ennå - se README.
///
/// Bruk fra XAML: {loc:Loc NøkkelNavn} (se LocExtension.cs). Bruk fra kode-bak: Strings.T("Nøkkel"),
/// evt. string.Format(Strings.T("Nøkkel"), args) for tekster med {0}/{1}-plassholdere.
/// </summary>
public static class Strings
{
    public static AppLanguage Current { get; private set; } = AppLanguage.Norwegian;

    public static void Initialize(AppLanguage language) => Current = language;

    public static string T(string key) =>
        Map.TryGetValue(key, out var pair) ? (Current == AppLanguage.English ? pair.En : pair.Nb) : $"[{key}]";

    private static readonly Dictionary<string, (string Nb, string En)> Map = new()
    {
        // ===== Common (delt på tvers av vinduer) =====
        ["Common_Browse"] = ("Bla gjennom...", "Browse..."),
        ["Common_BrowseResultsFolderTitle"] = ("Velg LMU Results-mappe", "Select LMU Results folder"),
        ["Common_ErrorTitle"] = ("Feil", "Error"),
        ["Common_Close"] = ("Lukk", "Close"),
        ["Common_UnexpectedErrorUiThread"] = ("Uventet feil (UI-tråd)", "Unexpected error (UI thread)"),
        ["Common_UnexpectedErrorBackground"] = ("Uventet feil (bakgrunn)", "Unexpected error (background)"),
        ["Common_UnknownError"] = ("Ukjent feil", "Unknown error"),
        ["Common_LanguageLabel"] = ("Språk", "Language"),
        ["Common_LanguageNorwegian"] = ("Norsk", "Norwegian"),
        ["Common_LanguageEnglish"] = ("Engelsk", "English"),
        ["Common_RestartToApply"] = ("Appen starter på nytt for å bytte språk...", "Restarting the app to switch language..."),

        // ===== ModeSelectWindow =====
        ["ModeSelect_WindowTitle"] = ("LMU Karriere", "LMU Career"),
        ["ModeSelect_Wordmark"] = ("LMU KARRIERE", "LMU CAREER"),
        ["ModeSelect_Quote"] = ("Fra pit lane til podium - din egen Le Mans-karriere.", "From pit lane to podium - your own Le Mans career."),
        ["ModeSelect_SelectMode"] = ("VELG MODUS", "SELECT MODE"),
        ["ModeSelect_CareerTitle"] = ("🏎️  KARRIERE MODUS", "🏎️  CAREER MODE"),
        ["ModeSelect_CareerDesc"] = (
            "Bygg din egen sjåførkarriere mot spillets faste AI-startlister. XP, kontrakter, transfer market og sesongrapporter.",
            "Build your own driver career against the game's fixed AI grids. XP, contracts, transfer market, and season reports."),
        ["ModeSelect_CareerButton"] = ("Gå til Karriere modus", "Go to Career Mode"),
        ["ModeSelect_LeagueTitle"] = ("🏆  LIGA MODUS", "🏆  LEAGUE MODE"),
        ["ModeSelect_LeagueDesc"] = (
            "Kjør et hostet mesterskap mot ekte motstandere. Poengsystem for førere og merker, straffer, og en delbar ligastilling - ingen XP eller transfer market.",
            "Run a hosted championship against real opponents. Points system for drivers and manufacturers, penalties, and a shareable league standings page - no XP or transfer market."),
        ["ModeSelect_LeagueButton"] = ("Gå til Liga modus", "Go to League Mode"),

        // ===== WelcomeWindow (Karriere) =====
        ["Welcome_WindowTitle"] = ("LMU Karriere", "LMU Career"),
        ["Welcome_PlayerNameLabel"] = ("DITT LMU-VISNINGSNAVN", "YOUR LMU DISPLAY NAME"),
        ["Welcome_PlayerNameHelp"] = (
            "Nøyaktig slik det vises i spillet - sjekk et resultat i UserData\\Log\\Results hvis du er usikker.",
            "Exactly as it appears in-game - check a result in UserData\\Log\\Results if you're unsure."),
        ["Welcome_ResultsFolderLabel"] = ("LMU RESULTS-MAPPE", "LMU RESULTS FOLDER"),
        ["Welcome_ResultsFolderHelp"] = (
            "Fylt ut automatisk hvis Steam finner spillet - endre bare hvis du har flyttet det.",
            "Filled in automatically if Steam finds the game - only change it if you've moved it."),
        ["Welcome_ButtonStart"] = ("Start", "Start"),
        ["Welcome_ButtonStartCareer"] = ("Start karriere", "Start Career"),
        ["Welcome_ButtonContinueCareer"] = ("Fortsett karriere", "Continue Career"),
        ["Welcome_WelcomeBack"] = ("👋 Velkommen tilbake, {0}!", "👋 Welcome back, {0}!"),
        ["Welcome_ErrorNoName"] = ("Skriv inn LMU-visningsnavnet ditt først.", "Enter your LMU display name first."),
        ["Welcome_ErrorNoFolder"] = (
            "Fant ikke Results-mappen. Velg riktig mappe med \"Bla gjennom...\".",
            "Couldn't find the Results folder. Pick the right folder with \"Browse...\"."),

        // ===== LeagueWelcomeWindow =====
        ["LeagueWelcome_WindowTitle"] = ("LMU Karriere - Liga", "LMU Career - League"),
        ["LeagueWelcome_Title"] = ("🏆 LIGA MODUS", "🏆 LEAGUE MODE"),
        ["LeagueWelcome_Subtitle"] = ("Kjør et hostet mesterskap mot ekte motstandere.", "Run a hosted championship against real opponents."),
        ["LeagueWelcome_LeagueNameLabel"] = ("LIGANAVN", "LEAGUE NAME"),
        ["LeagueWelcome_HostNameLabel"] = ("DITT VISNINGSNAVN (VERT)", "YOUR DISPLAY NAME (HOST)"),
        ["LeagueWelcome_HostNameHelp"] = (
            "Du er verten - kun du kan generere sesonger og gi straffer.",
            "You're the host - only you can generate seasons and issue penalties."),
        ["LeagueWelcome_ResultsFolderHelp"] = (
            "Kjør løpene som vert - din fil inneholder hele feltet, uansett hvem som deltar.",
            "Run the races as host - your file contains the whole field, no matter who's taking part."),
        ["LeagueWelcome_ButtonStart"] = ("Start liga", "Start League"),
        ["LeagueWelcome_ButtonCreate"] = ("Opprett liga", "Create League"),
        ["LeagueWelcome_ButtonContinue"] = ("Fortsett liga", "Continue League"),
        ["LeagueWelcome_WelcomeBack"] = ("👋 Velkommen tilbake til {0}!", "👋 Welcome back to {0}!"),
        ["LeagueWelcome_Back"] = ("← Tilbake", "← Back"),
        ["LeagueWelcome_ErrorNoLeagueName"] = ("Skriv inn et liganavn først.", "Enter a league name first."),
        ["LeagueWelcome_ErrorNoHostName"] = ("Skriv inn visningsnavnet ditt som vert.", "Enter your display name as host."),

        // ===== MainWindow: shell/nav =====
        ["Main_WindowTitle"] = ("LMU Karriere", "LMU Career"),
        ["Main_LogoLine1"] = ("🏁 LMU", "🏁 LMU"),
        ["Main_LogoLine2"] = ("KARRIERE", "CAREER"),
        ["Main_Nav_Dashboard"] = ("🏠  Dashboard", "🏠  Dashboard"),
        ["Main_Nav_Season"] = ("📅  Sesong", "📅  Season"),
        ["Main_Nav_Championship"] = ("🏆  Mesterskap", "🏆  Championship"),
        ["Main_Nav_History"] = ("📜  Historikk", "📜  History"),
        ["Main_Nav_Help"] = ("❓  Hjelp", "❓  Help"),
        ["Main_Nav_Settings"] = ("⚙  Innstillinger", "⚙  Settings"),

        // ===== MainWindow: header =====
        ["Main_Header_DriverEmpty"] = ("Fører: -", "Driver: -"),
        ["Main_Header_Driver"] = ("Fører: {0}", "Driver: {0}"),
        ["Main_Header_ClassEmpty"] = ("Klasse: -", "Class: -"),
        ["Main_Header_Class"] = ("Klasse: {0}   ·   Opplåst: {1}", "Class: {0}   ·   Unlocked: {1}"),
        ["Main_Header_MakeEmpty"] = ("Merke: -", "Manufacturer: -"),
        ["Main_Header_MakePrivateer"] = ("Merke: Privatlag (betalt sete)", "Manufacturer: Privateer (paid seat)"),
        ["Main_Header_MakeFreeAgent"] = ("Merke: Fri kjøring (ingen merke-oppsett i klassen)", "Manufacturer: Free agent (no manufacturer setup in this class)"),
        ["Main_Header_MakeContract"] = (
            "Merke: {0}   ·   {1} sesong(er) igjen   ·   {2} cr/runde   ·   Mål: {3}",
            "Manufacturer: {0}   ·   {1} season(s) left   ·   {2} cr/round   ·   Goal: {3}"),
        ["Main_Header_Level"] = ("NIVÅ", "LEVEL"),
        ["Main_Header_TotalXp"] = ("TOTAL XP", "TOTAL XP"),
        ["Main_Header_SeasonPoints"] = ("SESONGPOENG", "SEASON POINTS"),
        ["Main_Header_Rating"] = ("RATING", "RATING"),
        ["Main_Header_Credits"] = ("CREDITS", "CREDITS"),

        // ===== MainWindow: Dashboard =====
        ["Main_NextRace"] = ("NESTE LØP", "NEXT RACE"),
        ["Main_NextRaceTitle"] = ("Runde {0}: {1} ({2})", "Round {0}: {1} ({2})"),
        ["Main_NextRaceDetail"] = (
            "Sett opp i LMU: {0} - {1}   ·   ~{2} min race   ·   Vær: {3}",
            "Set up in LMU: {0} - {1}   ·   ~{2} min race   ·   Weather: {3}"),
        ["Main_NextRaceWaiting"] = ("Venter på valg av klasse...", "Waiting for class selection..."),
        ["Main_CopyRecipeButton"] = ("📋 Kopier oppskrift", "📋 Copy setup"),
        ["Main_MiniWidgetButton"] = ("🗗 Kompakt visning", "🗗 Compact view"),
        ["Main_LiveLog"] = ("LIVE-LOGG", "LIVE LOG"),
        ["Main_HowDoesThisWork"] = ("❓ Hvordan fungerer dette?", "❓ How does this work?"),
        ["Main_DashboardStatusDefault"] = ("Fyll ut oppsett og trykk Start for å komme i gang.", "Fill in the setup and press Start to get going."),
        ["Main_RecentRaces"] = ("SISTE LØP", "RECENT RACES"),

        // ===== MainWindow: Sesong =====
        ["Main_SeasonCalendar"] = ("SESONGKALENDER", "SEASON CALENDAR"),
        ["Main_Col_Round"] = ("Runde", "Round"),
        ["Main_Col_Track"] = ("Bane", "Track"),
        ["Main_Col_Format"] = ("Format", "Format"),
        ["Main_Col_Weather"] = ("Vær", "Weather"),
        ["Main_Col_Status"] = ("Status", "Status"),
        ["Main_Col_Result"] = ("Resultat", "Result"),
        ["Main_Col_Points"] = ("Poeng", "Points"),

        // ===== MainWindow: Mesterskap =====
        ["Main_ChampionshipNoSeason"] = ("Ingen aktiv sesong ennå - velg klasse og signer en kontrakt.", "No active season yet - pick a class and sign a contract."),
        ["Main_Championship_Header"] = ("Sesong {0} ({1})", "Season {0} ({1})"),
        ["Main_Championship_RoundsDone"] = ("{0} av {1} runder kjørt", "{0} of {1} rounds completed"),
        ["Main_Championship_FieldLocked"] = ("Feltet ble låst med {0} sjåfører etter runde 1", "The field was locked with {0} drivers after round 1"),
        ["Main_Championship_FieldLocksNext"] = ("Feltet låses når runde 1 er fullført", "The field locks once round 1 is completed"),
        ["Main_DriverChampionship"] = ("FØRERMESTERSKAP", "DRIVER CHAMPIONSHIP"),
        ["Main_ManufacturerChampionship"] = ("MERKEMESTERSKAP", "MANUFACTURER CHAMPIONSHIP"),
        ["Main_Col_Pos"] = ("#", "#"),
        ["Main_Col_Driver"] = ("Fører", "Driver"),
        ["Main_Col_Team"] = ("Team", "Team"),
        ["Main_Col_Wins"] = ("Seire", "Wins"),
        ["Main_Col_Podiums"] = ("Podium", "Podiums"),
        ["Main_Col_Manufacturer"] = ("Merke", "Manufacturer"),

        // ===== MainWindow: Historikk =====
        ["Main_FullHistory"] = ("HELE LØPSHISTORIKKEN", "FULL RACE HISTORY"),
        ["Main_FullHistoryHelp"] = ("Dobbeltklikk for detaljer (Practice/Qualifying/Race)", "Double-click for details (Practice/Qualifying/Race)"),

        // ===== MainWindow: Hjelp =====
        ["Main_Help_WhatIsThisHeader"] = ("HVA ER DETTE?", "WHAT IS THIS?"),
        ["Main_Help_WhatIsThisBody"] = (
            "LMU Karriere styrer ikke spillet - det kan ikke sette opp Race Weekend for deg. I stedet forteller det deg nøyaktig hva du skal sette opp (bane, bil, format, vær), du kjører løpet manuelt i LMU, og verktøyet oppdager automatisk resultatfilen etterpå og oppdaterer karrieren din: XP, poeng, mesterskapstabell, Rating, Credits og kontrakter.",
            "LMU Career does not control the game - it can't set up a Race Weekend for you. Instead, it tells you exactly what to set up (track, car, format, weather), you run the race manually in LMU, and the tool automatically detects the result file afterward and updates your career: XP, points, championship standings, Rating, Credits, and contracts."),
        ["Main_Help_SetupHeader"] = ("SLIK SETTER DU OPP ET LØP", "HOW TO SET UP A RACE"),
        ["Main_Help_Setup1"] = (
            "I LMUs hovedmeny: velg 'Race Weekend' - IKKE Multiplayer. Kun Race Weekend bruker spillets faste WEC-startliste, som gjør et ekte mesterskap med faste rivaler mulig. Multiplayer-løp blir logget, men teller aldri mot karrieren.",
            "In LMU's main menu: choose 'Race Weekend' - NOT Multiplayer. Only Race Weekend uses the game's fixed WEC grid, which is what makes a real championship with consistent rivals possible. Multiplayer races get logged but never count toward your career."),
        ["Main_Help_Setup2"] = (
            "Match 'Neste løp'-kortet på Dashboard nøyaktig: klasse, bil (merket krever EN spesifikk bil for hele sesongen), bane og vær. Bruk 'Kopier oppskrift'-knappen for å ha alt ved siden av deg mens du setter opp.",
            "Match the 'Next Race' card on the Dashboard exactly: class, car (the manufacturer requires ONE specific car for the whole season), track, and weather. Use the 'Copy setup' button to have everything next to you while you set up."),
        ["Main_Help_Setup3"] = (
            "Kjør Practice/Qualifying/Race som normalt - antall AI-motstandere styres av spillets egne baneinnstillinger, ikke av verktøyet.",
            "Run Practice/Qualifying/Race as normal - the number of AI opponents is controlled by the game's own track settings, not by the tool."),
        ["Main_Help_Setup4"] = (
            "Ferdig! Så snart løpet er over, oppdager verktøyet den nye resultatfilen automatisk - du trenger ikke gjøre noe mer. Sjekk live-loggen på Dashboard.",
            "Done! As soon as the race is over, the tool automatically detects the new result file - you don't need to do anything else. Check the live log on the Dashboard."),
        ["Main_Help_MismatchHeader"] = ("HVIS OPPSETTET IKKE STEMMER", "IF THE SETUP DOESN'T MATCH"),
        ["Main_Help_MismatchBody"] = (
            "Kjørte du feil bane eller feil bil for sesongrunden? Verktøyet varsler deg med et gult ikon i loggen og et popup-vindu der du kan velge å godkjenne runden likevel - din egen karriere, dine egne regler. Sier du nei, kan du bare kjøre runden på nytt med riktig oppsett.",
            "Ran the wrong track or wrong car for the season round? The tool warns you with a yellow icon in the log and a popup where you can choose to approve the round anyway - your own career, your own rules. Say no, and you can just re-run the round with the correct setup."),
        ["Main_Help_SystemsHeader"] = ("SYSTEMENE, KORT FORKLART", "THE SYSTEMS, BRIEFLY EXPLAINED"),
        ["Main_Help_SystemsXp"] = (
            "XP og Nivå - tjent per fullført sesongrunde. Nok XP låser opp nye klasser (GT3 → LMP3 → LMP2 → Hypercar).",
            "XP and Level - earned per completed season round. Enough XP unlocks new classes (GT3 → LMP3 → LMP2 → Hypercar)."),
        ["Main_Help_SystemsPoints"] = (
            "Sesongpoeng - F1-poengskala (25-18-15...) per plassering. Din egen sammenlagtsum for sesongen, vist i Mesterskap-fanen sammen med resten av feltet.",
            "Season points - F1 points scale (25-18-15...) per finishing position. Your own running total for the season, shown in the Championship tab alongside the rest of the field."),
        ["Main_Help_SystemsRating"] = (
            "Rating - stiger av gode og rene resultater (0-100, start 50). Avgjør hvor interessert et merke er i deg.",
            "Rating - rises from good, clean results (0-100, starting at 50). Determines how interested a manufacturer is in you."),
        ["Main_Help_SystemsCredits"] = (
            "Credits - spillvaluta tjent per løp + kontraktlønn. Brukes til bruddsummer og betalte privatlag-seter - aldri til å kjøpe deg en fabrikkontrakt du ikke har fortjent.",
            "Credits - in-game currency earned per race plus contract salary. Used for buyout fees and paid privateer seats - never to buy yourself a factory contract you haven't earned."),
        ["Main_Help_SystemsContracts"] = ("Kontrakter - se eget avsnitt under.", "Contracts - see the separate section below."),
        ["Main_Help_TransferHeader"] = ("TRANSFER-MARKEDET - HVOR KOMMER TILBUD FRA?", "THE TRANSFER MARKET - WHERE DO OFFERS COME FROM?"),
        ["Main_Help_TransferBody1"] = (
            "Merkene sender IKKE tilbud midt i en sesong - de vurderer deg på to faste tidspunkt: når du starter en helt ny karriere, og hver gang en sesong er fullført. Hvert merke regner ut en interesse-score fra Rating din (mot merkets krav), hvordan forrige sesong gikk, og hvor rent du kjørte. Er du mer enn 10 Rating-poeng under kravet, vurderer de deg ikke i det hele tatt ennå.",
            "Manufacturers do NOT send offers mid-season - they evaluate you at two fixed points: when you start a brand new career, and every time a season finishes. Each manufacturer calculates an interest score from your Rating (against their requirement), how last season went, and how clean you drove. If you're more than 10 Rating points below their requirement, they don't consider you at all yet."),
        ["Main_Help_TransferBody2"] = (
            "Har du allerede kontrakt med et merke og innfridde forrige sesongmål, får du en fornyelse i stedet for et nytt tilbud. Innfrir du IKKE målet, kan merket si deg opp. Uansett hvor dårlig det går, finnes alltid et betalt sete hos et privatlag som fallback (mot en engangskostnad, ingen lønn).",
            "If you already have a contract with a manufacturer and met last season's goal, you get a renewal instead of a new offer. If you DON'T meet the goal, the manufacturer can drop you. No matter how badly it goes, a paid privateer seat is always available as a fallback (for a one-time cost, no salary)."),
        ["Main_Help_CheckInterestButton"] = ("🔍 Sjekk merkeinteresse akkurat nå", "🔍 Check manufacturer interest right now"),

        // ===== MainWindow: Innstillinger =====
        ["Main_Settings_Setup"] = ("OPPSETT", "SETUP"),
        ["Main_Settings_ResultsFolder"] = ("LMU Results-mappe", "LMU Results folder"),
        ["Main_Settings_PlayerName"] = ("Ditt LMU-visningsnavn", "Your LMU display name"),
        ["Main_Settings_StartWatching"] = ("Start overvåking", "Start monitoring"),
        ["Main_Settings_StopWatching"] = ("Stopp overvåking", "Stop monitoring"),
        ["Main_Settings_StatusNotStarted"] = ("Ikke startet.", "Not started."),
        ["Main_Settings_StatusWatching"] = ("Overvåker: {0}", "Monitoring: {0}"),
        ["Main_Settings_StatusStopped"] = ("Stoppet.", "Stopped."),

        // ===== MainWindow: kode-bak (toasts/logg/MessageBox) =====
        ["Main_Msg_MissingSetup"] = ("Fyll ut både Results-mappe og visningsnavn først.", "Fill in both the Results folder and display name first."),
        ["Main_Msg_MissingSetupTitle"] = ("Mangler info", "Missing info"),
        ["Main_Msg_FolderNotFound"] = ("Fant ikke mappen:\n{0}", "Couldn't find the folder:\n{0}"),
        ["Main_Msg_FolderNotFoundTitle"] = ("Feil mappe", "Wrong folder"),
        ["Main_Msg_StartFailed"] = ("Klarte ikke å starte: {0}", "Failed to start: {0}"),
        ["Main_Toast_WatchingStarted"] = ("Overvåking startet - klar for løp!", "Monitoring started - ready to race!"),
        ["Main_Log_IndexingFiles"] = ("Fant {0} eksisterende fil(er), indekserer...", "Found {0} existing file(s), indexing..."),
        ["Main_Log_Waiting"] = ("Venter på nye løpsresultater...", "Waiting for new race results..."),
        ["Main_DashboardStatus_Watching"] = (
            "Overvåker Results-mappen. Kjør et løp i LMU (Race Weekend) - resultatet dukker opp her automatisk når det er ferdig.",
            "Monitoring the Results folder. Run a race in LMU (Race Weekend) - the result will show up here automatically once it's done."),
        ["Main_DashboardStatus_Stopped"] = (
            "Overvåking stoppet. Trykk Start overvåking i Innstillinger for å fortsette.",
            "Monitoring stopped. Press Start monitoring in Settings to continue."),
        ["Main_Log_Stopped"] = ("Sluttet å overvåke.", "Stopped monitoring."),
        ["Main_Log_SessionIgnored"] = (
            "↪ {0} ({1}) - teller ikke mot karrieren (kun 'Race Weekend' telles).",
            "↪ {0} ({1}) - doesn't count toward the career (only 'Race Weekend' counts)."),
        ["Main_Log_NewFile"] = ("Ny fil: {0}", "New file: {0}"),
        ["Main_Log_FileReadError"] = ("[FEIL] Klarte ikke å lese filen: {0}", "[ERROR] Failed to read the file: {0}"),
        ["Main_Log_PlayerNotFound"] = ("⚠ Fant ikke deg i resultatet for {0}. Sjekk visningsnavnet.", "⚠ Couldn't find you in the result for {0}. Check your display name."),
        ["Main_Toast_SetupMismatch"] = ("Oppsettet stemte ikke - se loggen for detaljer.", "The setup didn't match - see the log for details."),
        ["Main_Msg_SetupMismatchBody"] = ("{0}\n\nGodkjenne runden likevel med dette resultatet (P{1})?", "{0}\n\nApprove the round anyway with this result (P{1})?"),
        ["Main_Msg_SetupMismatchTitle"] = ("Oppsettet stemte ikke", "Setup didn't match"),
        ["Main_Log_ApprovedManually"] = ("✅ Godkjent manuelt: {0} - +{1} XP, +{2} poeng, Rating {3}, +{4} cr", "✅ Approved manually: {0} - +{1} XP, +{2} points, Rating {3}, +{4} cr"),
        ["Main_Log_RaceResult"] = ("🏁 {0}: P{1} av {2} - +{3} XP, +{4} poeng, Rating {5}, +{6} cr", "🏁 {0}: P{1} of {2} - +{3} XP, +{4} points, Rating {5}, +{6} cr"),
        ["Main_Toast_RaceResult"] = ("{0}: P{1} - +{2} XP", "{0}: P{1} - +{2} XP"),
        ["Main_Log_ContractSalary"] = ("💰 Kontraktlønn: +{0} cr", "💰 Contract salary: +{0} cr"),
        ["Main_Log_ClassUnlocked"] = ("🔓 Ny klasse låst opp: {0}!", "🔓 New class unlocked: {0}!"),
        ["Main_Toast_ClassUnlocked"] = ("Ny klasse låst opp: {0}!", "New class unlocked: {0}!"),
        ["Main_Log_SeasonComplete"] = ("🏆 Sesong fullført! {0} poeng sammenlagt.", "🏆 Season complete! {0} points total."),
        ["Main_Toast_SeasonComplete"] = ("Sesong fullført!", "Season complete!"),
        ["Main_Log_DroppedByManufacturer"] = ("📉 Merket var ikke fornøyd med resultatene og har sagt opp kontrakten din.", "📉 The manufacturer wasn't happy with the results and has terminated your contract."),
        ["Main_Log_ContractExpired"] = ("📄 Kontrakten din har løpt ut. Tid for et nytt tilbud.", "📄 Your contract has expired. Time for a new offer."),
        ["Main_Log_NewSeasonStarted"] = ("Ny sesong startet: {0}", "New season started: {0}"),
        ["Main_Log_NewSeasonPrivateer"] = (" (privatlag)", " (privateer)"),
        ["Main_Log_NewSeasonManufacturer"] = (" hos {0} ({1} sesong(er) igjen, {2} cr/runde)", " with {0} ({1} season(s) left, {2} cr/round)"),
        ["Main_Msg_NoActiveCareer"] = ("Start overvåking først.", "Start monitoring first."),
        ["Main_Msg_NoActiveCareerTitle"] = ("Ingen aktiv karriere", "No active career"),
        ["Main_Log_CopiedRecipe"] = ("📋 Oppskrift for neste løp kopiert til utklippstavlen.", "📋 Setup for the next race copied to clipboard."),

        // ===== LeagueMainWindow: shell/nav =====
        ["League_WindowTitle"] = ("LMU Karriere - Liga", "LMU Career - League"),
        ["League_LogoLine1"] = ("🏆 LIGA", "🏆 LEAGUE"),
        ["League_LogoLineDefault"] = ("MODUS", "MODE"),
        ["League_Nav_Dashboard"] = ("🏠  Dashboard", "🏠  Dashboard"),
        ["League_Nav_Standings"] = ("🏆  Stilling", "🏆  Standings"),
        ["League_Nav_Calendar"] = ("📅  Kalender", "📅  Calendar"),
        ["League_Nav_Penalties"] = ("🚩  Straffer", "🚩  Penalties"),
        ["League_Nav_Settings"] = ("⚙  Innstillinger", "⚙  Settings"),

        // ===== LeagueMainWindow: header =====
        ["League_HeaderLeagueNameEmpty"] = ("Liga: -", "League: -"),
        ["League_HeaderLeagueName"] = ("Liga: {0}", "League: {0}"),
        ["League_HeaderNoSeasonGenerate"] = ("Ingen aktiv sesong - generer en fra Innstillinger.", "No active season - generate one from Settings."),
        ["League_HeaderSeason"] = ("Sesong {0} - {1}", "Season {0} - {1}"),
        ["League_Rounds"] = ("RUNDER", "ROUNDS"),
        ["League_Leader"] = ("LEDER", "LEADER"),

        // ===== LeagueMainWindow: Dashboard =====
        ["League_DashboardStatusDefault"] = ("Fyll ut oppsett i Innstillinger og trykk Start overvåking.", "Fill in the setup in Settings and press Start monitoring."),
        ["League_DashboardStatusWatching"] = (
            "Overvåker Results-mappen. Kjør et hostet løp (Multiplayer) i LMU - resultatet dukker opp her automatisk når det er ferdig.",
            "Monitoring the Results folder. Run a hosted race (Multiplayer) in LMU - the result will show up here automatically once it's done."),

        // ===== LeagueMainWindow: Stilling =====
        ["League_ClassLabel"] = ("KLASSE", "CLASS"),
        ["League_ClassHelp"] = (
            "Et hostet løp kan blande flere klasser samtidig - poeng regnes alltid innenfor egen klasse.",
            "A hosted race can mix multiple classes at once - points are always calculated within your own class."),
        ["League_Col_Top5"] = ("Top 5", "Top 5"),
        ["League_Col_Top10"] = ("Top 10", "Top 10"),
        ["League_Col_Penalty"] = ("Straff", "Penalty"),

        // ===== LeagueMainWindow: Kalender =====
        ["League_RaceCalendar"] = ("LØPSKALENDER", "RACE CALENDAR"),
        ["League_Col_Winner"] = ("Vinner", "Winner"),
        ["League_Status_Completed"] = ("Fullført", "Completed"),
        ["League_Status_NotRun"] = ("Ikke kjørt", "Not run"),

        // ===== LeagueMainWindow: Straffer =====
        ["League_PenaltiesGiven"] = ("GITTE STRAFFER", "PENALTIES GIVEN"),
        ["League_Col_Consequence"] = ("Konsekvens", "Consequence"),
        ["League_Col_Reason"] = ("Begrunnelse", "Reason"),
        ["League_GiveNewPenalty"] = ("GI NY STRAFF", "ISSUE NEW PENALTY"),
        ["League_PointsDeduction"] = ("Poengtrekk", "Points deducted"),
        ["League_Disqualify"] = ("Diskvalifiser", "Disqualify"),
        ["League_GivePenaltyButton"] = ("Gi straff", "Issue penalty"),
        ["League_Consequence_Disqualified"] = ("Diskvalifisert", "Disqualified"),
        ["League_Consequence_PointsDeducted"] = ("-{0} poeng", "-{0} points"),

        // ===== LeagueMainWindow: Innstillinger =====
        ["League_Settings_Monitoring"] = ("OVERVÅKING", "MONITORING"),
        ["League_Settings_GenerateSeason"] = ("GENERER NY SESONG", "GENERATE NEW SEASON"),
        ["League_Settings_GenerateSeasonHelp"] = ("Overskriver ikke en pågående sesong uten bekreftelse.", "Won't overwrite an ongoing season without confirmation."),
        ["League_Settings_ClassesLabel"] = (
            "Klasser (velg én eller flere - flere klasser sammen kjører som ett hostet multiklasse-løp, med egen mesterskapstabell per klasse)",
            "Classes (pick one or more - multiple classes together run as one hosted multiclass race, with its own championship table per class)"),
        ["League_Settings_RoundCount"] = ("Antall løp", "Number of races"),
        ["League_Settings_GenerateButton"] = ("Generer ny sesong", "Generate new season"),
        ["League_Settings_ShareTitle"] = ("DEL LIGASTILLINGEN", "SHARE THE LEAGUE STANDINGS"),
        ["League_Settings_ShareBody"] = (
            "Publiser et statisk HTML-øyeblikksbilde av stillingen. Send filen til hvem du vil (e-post, Discord, opplasting) - alle kan åpne og SE den, ingen kan endre den. Trykk igjen etter hver runde for å oppdatere.",
            "Publish a static HTML snapshot of the standings. Send the file to whoever you like (email, Discord, file upload) - anyone can open and VIEW it, no one can edit it. Click again after each round to update it."),
        ["League_Settings_PublishButton"] = ("📤 Publiser HTML-øyeblikksbilde...", "📤 Publish HTML snapshot..."),

        // ===== LeagueMainWindow: kode-bak =====
        ["League_Log_Waiting"] = ("Venter på nye løpsresultater fra Multiplayer-økter...", "Waiting for new race results from Multiplayer sessions..."),
        ["League_Log_SessionIgnored"] = (
            "↪ {0} ({1}) - teller ikke mot ligaen (kun 'Multiplayer' telles).",
            "↪ {0} ({1}) - doesn't count toward the league (only 'Multiplayer' counts)."),
        ["League_Log_TrackMismatch"] = ("⚠ {0} matchet ikke neste runde ({1}).", "⚠ {0} didn't match the next round ({1})."),
        ["League_Msg_TrackMismatchBody"] = (
            "Løpet ble kjørt på {0}, men neste runde i kalenderen er {1}.\n\nGodkjenne runden likevel med dette resultatet?",
            "The race was run at {0}, but the next round in the calendar is {1}.\n\nApprove the round anyway with this result?"),
        ["League_Msg_TrackMismatchTitle"] = ("Bane stemte ikke", "Track didn't match"),
        ["League_Log_ApprovedManually"] = ("✅ Godkjent manuelt: runde {0} - {1}, {2} deltakere.", "✅ Approved manually: round {0} - {1}, {2} participants."),
        ["League_Log_NoActiveSeason"] = (
            "↪ {0}: fullført løp registrert, men ingen aktiv sesong å knytte det til.",
            "↪ {0}: completed race recorded, but no active season to attach it to."),
        ["League_Log_RoundCompleted"] = ("🏁 Runde {0} fullført: {1} - {2} deltakere.", "🏁 Round {0} completed: {1} - {2} participants."),
        ["League_Log_SeasonComplete"] = (
            "🏆 Sesongen er fullført! Generer en ny sesong fra Innstillinger når dere er klare.",
            "🏆 The season is complete! Generate a new season from Settings when you're ready."),
        ["League_Msg_NoActiveLeague"] = ("Start overvåking først.", "Start monitoring first."),
        ["League_Msg_NoActiveLeagueTitle"] = ("Ingen aktiv liga", "No active league"),
        ["League_Msg_OngoingSeasonBody"] = (
            "Det finnes allerede en pågående sesong som ikke er fullført. Generere en ny sesong vil erstatte den (den fullførte historikken beholdes ikke).\n\nFortsette?",
            "There's already an ongoing season that isn't complete. Generating a new season will replace it (the completed history isn't kept).\n\nContinue?"),
        ["League_Msg_OngoingSeasonTitle"] = ("Pågående sesong", "Ongoing season"),
        ["League_Msg_MissingClass"] = ("Velg minst én klasse først.", "Select at least one class first."),
        ["League_Msg_MissingClassTitle"] = ("Mangler klasse", "Missing class"),
        ["League_Msg_InvalidRoundCount"] = ("Antall løp må være et positivt tall.", "Number of races must be a positive number."),
        ["League_Msg_InvalidRoundCountTitle"] = ("Ugyldig antall", "Invalid number"),
        ["League_Log_SeasonGenerated"] = ("📅 Ny sesong generert: {0}, {1} runder, {2}.", "📅 New season generated: {0}, {1} rounds, {2}."),
        ["League_Log_MulticlassNote"] = (" Multiklasse-løp - hver klasse scores og vises separat.", " Multiclass race - each class is scored and shown separately."),
        ["League_Msg_PublishSuccess"] = (
            "Ligastillingen er publisert. Del filen med hvem du vil - den er statisk og read-only.",
            "The league standings have been published. Share the file with whoever you like - it's static and read-only."),
        ["League_Msg_PublishedTitle"] = ("Publisert", "Published"),
        ["League_Msg_PublishFailed"] = ("Klarte ikke å publisere: {0}", "Failed to publish: {0}"),
        ["League_Log_Published"] = ("📤 Publisert: {0}", "📤 Published: {0}"),
        ["League_Msg_MissingRound"] = ("Velg en runde.", "Select a round."),
        ["League_Msg_MissingRoundTitle"] = ("Mangler runde", "Missing round"),
        ["League_Msg_MissingDriver"] = ("Velg eller skriv inn en fører.", "Select or enter a driver."),
        ["League_Msg_MissingDriverTitle"] = ("Mangler fører", "Missing driver"),
        ["League_Log_PenaltyGiven"] = ("🚩 Straff gitt: {0} (runde {1}) - {2}", "🚩 Penalty given: {0} (round {1}) - {2}"),
        ["League_Log_PenaltyDisqualified"] = ("diskvalifisert", "disqualified"),
        ["League_Log_PenaltyPointsDeducted"] = ("-{0} poeng", "-{0} points"),
        ["League_DriverHistory_Title"] = ("Historikk - {0}", "History - {0}"),
        ["League_DriverHistory_Body"] = (
            "Løp: {0}\nPoeng totalt: {1}\nSeire: {2}\nPodier: {3}\nTop 5: {4}\nTop 10: {5}\nBeste plassering: {6}",
            "Races: {0}\nTotal points: {1}\nWins: {2}\nPodiums: {3}\nTop 5: {4}\nTop 10: {5}\nBest finish: {6}"),
        ["League_PublishDialogTitle"] = ("Publiser ligastilling", "Publish league standings"),
        ["Common_HtmlFileFilter"] = ("HTML-fil (*.html)|*.html", "HTML file (*.html)|*.html"),
    };
}
