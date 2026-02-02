using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WiktionaryNET;
using WeCantSpell.Hunspell;

var gridSize = GeneratorSettings.GridSize;
var iterationsPerSeed = GeneratorSettings.IterationsPerSeed;
const int randomSeed = 4123;
const string outputFileName = "motcroise.pdf";
const string outputSolutionFileName = "motcroiseSolution.pdf";
var runMode = GeneratorSettings.Mode;

QuestPDF.Settings.License = LicenseType.Community;

var clues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["ANNIVERSAIRE"] = "Fete qui revient chaque annee",
    ["JOYEUX"] = "Qui exprime la joie",
    ["GRANDMERE"] = "Mere du pere ou de la mere",
    ["AN"] = "Unite de temps liee a l'age",
    ["AGE"] = "Nombre d'annees ecoulees",
    ["AMI"] = "Personne proche invitee",
    ["AMIS"] = "Proches invites",
    ["DE"] = "Preposition courante",
    ["DU"] = "Contraction de la preposition",
    ["ET"] = "Conjonction courante",
    ["EN"] = "Preposition courante",
    ["LE"] = "Article defini",
    ["LA"] = "Article defini",
    ["LES"] = "Article defini pluriel",
    ["UN"] = "Article indefini",
    ["UNE"] = "Article indefini",
    ["AU"] = "Contraction de la preposition",
    ["AUX"] = "Contraction de la preposition",
    ["SUR"] = "Preposition courante",
    ["PAR"] = "Preposition courante",
    ["POUR"] = "Preposition courante",
    ["SON"] = "Determinant possessif",
    ["SA"] = "Determinant possessif",
    ["SES"] = "Determinant possessif",
    ["TON"] = "Determinant possessif",
    ["TA"] = "Determinant possessif",
    ["TES"] = "Determinant possessif",
    ["OR"] = "Conjonction de coordination",
    ["NI"] = "Conjonction de coordination",
    ["CI"] = "Adverbe de lieu",
    ["CA"] = "Pronom demonstratif",
    ["VA"] = "Forme du verbe aller",
    ["VU"] = "Considere",
    ["IL"] = "Pronom personnel",
    ["ON"] = "Pronom indefini",
    ["OU"] = "Conjonction de choix",
    ["SI"] = "Conjonction conditionnelle",
    ["SE"] = "Pronom reflexif",
    ["CE"] = "Determinant demonstratif",
    ["NE"] = "Particule negative",
    ["MA"] = "Determinant possessif",
    ["ME"] = "Pronom personnel",
    ["TE"] = "Pronom personnel",
    ["MOI"] = "Pronom personnel",
    ["TOI"] = "Pronom personnel",
    ["LUI"] = "Pronom personnel",
    ["ELLE"] = "Pronom personnel",
    ["ELLES"] = "Pronom personnel",
    ["BAL"] = "Soiree dansante",
    ["FEU"] = "Flamme des bougies",
    ["BAR"] = "Espace pour les boissons",
    ["MOT"] = "Element de langage",
    ["TRES"] = "Adverbe d'intensite",
    ["PLUS"] = "Adverbe de quantite",
    ["AVEC"] = "Preposition d'accompagnement",
    ["SANS"] = "Preposition d'absence",
    ["DECO"] = "Decoration abregee",
    ["MENU"] = "Liste des plats",
    ["ROSE"] = "Couleur",
    ["NOIR"] = "Couleur",
    ["BLEU"] = "Couleur",
    ["VERT"] = "Couleur",
    ["LISTE"] = "Ensemble ordonne d'elements",
    ["NOM"] = "Mot d'identite",
    ["NOMS"] = "Mots d'identite",
    ["FETE"] = "Moment de celebration",
    ["FETES"] = "Celebrations partagees",
    ["VOEU"] = "Souhait formule",
    ["VOEUX"] = "Souhaits adresses",
    ["CADEAU"] = "Present offert",
    ["CADEAUX"] = "Presents offerts",
    ["CARTE"] = "Message pour l'anniversaire",
    ["GATEAU"] = "Dessert d'anniversaire",
    ["BOUGIE"] = "Elle se souffle sur le gateau",
    ["BOUGIES"] = "Lumieres posees sur le gateau",
    ["CHANT"] = "Air entonne en groupe",
    ["CHANSON"] = "Air chante pendant la fete",
    ["DANSE"] = "Activite festive",
    ["JEU"] = "Activite ludique",
    ["JEUX"] = "Activites ludiques",
    ["RIRE"] = "Expression de joie",
    ["SOURIRE"] = "Expression de joie douce",
    ["INVITE"] = "Personne conviee",
    ["INVITES"] = "Personnes conviees",
    ["FAMILLE"] = "Proches reunis",
    ["MUSIQUE"] = "Elle accompagne la danse",
    ["CONFETTI"] = "Petits papiers colores",
    ["BALLON"] = "Decor gonfle pour la fete",
    ["DECOR"] = "Ensemble d'elements ornementaux",
    ["DECORS"] = "Elements ornementaux",
    ["BUFFET"] = "Table de mets partages",
    ["FESTIN"] = "Repas abondant pour feter",
    ["GOUTER"] = "Collation prise ensemble",
    ["APERO"] = "Moment convivial avant le repas",
    ["TARTE"] = "Dessert souvent partage",
    ["CREME"] = "Preparation onctueuse",
    ["SUCRE"] = "Ingredient gourmand",
    ["GLACE"] = "Dessert froid",
    ["BONBON"] = "Douceur sucree",
    ["BISCUIT"] = "Gourmandise cuite",
    ["DESSERT"] = "Plat sucre de fin",
    ["FLAN"] = "Dessert cremeux",
    ["BRAVO"] = "Expression d'encouragement",
    ["PHOTO"] = "Image souvenir",
    ["PHOTOS"] = "Images souvenirs",
    ["SOUVENIR"] = "Memoire d'un moment",
    ["CADET"] = "Invite plus jeune",
    ["AINE"] = "Invite plus age",
    ["TOAST"] = "Discours bref porte un verre",
    ["RITUEL"] = "Geste qui revient a chaque fete",
    ["FANFARE"] = "Musique festive",
    ["ORCHESTRE"] = "Ensemble musical",
    ["PRESENT"] = "Don offert",
    ["PRESENTS"] = "Dons offerts",
    ["INVITEE"] = "Personne conviee",
    ["INVITEES"] = "Personnes conviees",
    ["FELICITER"] = "Exprimer des souhaits de bonheur",
    ["SALUTATION"] = "Formule d'accueil",
    ["ANS"] = "Annees comptees",
    ["AGES"] = "Annees ecoulees",
    ["ANNEE"] = "Cycle de douze mois",
    ["ANNEES"] = "Cycles de douze mois",
    ["SALLE"] = "Lieu de la fete",
    ["TABLE"] = "Support pour le repas",
    ["CHAIR"] = "Siege pour les invites",
    ["REPAS"] = "Moment pour manger ensemble",
    ["PLAT"] = "Met servi",
    ["PLATS"] = "Mets servis",
    ["JUS"] = "Boisson fruit",
    ["THE"] = "Boisson chaude",
    ["CAFE"] = "Boisson chaude",
    ["BOISSON"] = "Liquide servi",
    ["BOISSONS"] = "Liquides servis",
    ["SERVIR"] = "Action de presenter les mets",
    ["SERVICE"] = "Action d'accueillir et servir",
    ["CADETTE"] = "Invitee plus jeune",
    ["AINES"] = "Invites plus ages",
    ["TEMPS"] = "Duree de la fete",
    ["HEURE"] = "Moment de la celebration",
    ["HEURES"] = "Moments de la celebration",
    ["SOIR"] = "Moment de la fete",
    ["SOIREE"] = "Fete en fin de journee",
    ["NOCES"] = "Celebrations familiales",
    ["VIEUX"] = "Invite plus age",
    ["NEUF"] = "Age d'une annee",
    ["DIX"] = "Nombre d'annees",
    ["DIZAINE"] = "Ensemble de dix",
    ["DECENNIES"] = "Periodes de dix ans",
    ["DECENNALE"] = "Qui revient tous les dix ans",
    ["HOTE"] = "Personne qui recoit",
    ["HOTES"] = "Personnes qui recoivent",
    ["ACCUEIL"] = "Reception des invites",
    ["ACCUEILLIR"] = "Recevoir les invites",
    ["AMBIANCE"] = "Atmosphere de la fete",
    ["EQUIPE"] = "Groupe organisateur",
    ["EQUIPES"] = "Groupes organisateurs",
    ["PLANNING"] = "Agenda de la fete",
    ["BALADE"] = "Moment dehors pour celebrer",
    ["JEUNES"] = "Invites plus jeunes",
    ["SPECTACLE"] = "Numero propose",
    ["ANIMER"] = "Faire vivre la fete",
    ["ANIMATEUR"] = "Personne qui anime",
    ["ANIMATRICE"] = "Personne qui anime",
    ["COMMEMORATION"] = "Acte de se souvenir d'une date marquante",
    ["CELEBRATION"] = "Action de feter un evenement",
    ["FESTIVITES"] = "Ensemble des ceremonies et fetes",
    ["CONVIVIALITE"] = "Qualite qui favorise les relations agreables",
    ["ORGANISATION"] = "Action d'agencer et preparer la fete",
    ["PLANIFICATION"] = "Action de planifier dans le detail",
    ["ORCHESTRATION"] = "Coordination de tous les elements",
    ["INVITATIONS"] = "Cartons ou messages pour convier",
    ["PARTICIPANTS"] = "Personnes prenant part a la fete",
    ["DECORATION"] = "Elements pour embellir la salle",
    ["ANIMATION"] = "Activites ou jeux proposes",
    ["PREPARATIFS"] = "Ensemble des preparatifs avant l'evenement",
    ["RECEPTIONS"] = "Accueil et celebration officielle",
    ["GASTRONOMIE"] = "Art de la bonne cuisine lors d'une fete",
    ["PASTISSERIE"] = "Art de preparer les desserts",
    ["PHOTOGRAPHIE"] = "Prise d'images pour garder des souvenirs",
    ["REMERCIEMENTS"] = "Paroles exprimees pour remercier",
    ["ALLOCUTION"] = "Courte prise de parole ceremonielle",
    ["CEREMONIE"] = "Rituel organise pour l'evenement",
    ["ILLUMINATIONS"] = "Lumieres festives pour l'ambiance",
    ["IMMORTALISATION"] = "Action de rendre un moment memorable",
    ["PERSONNALISATION"] = "Action d'adapter la fete a la personne",
    ["FELICITATIONS"] = "Paroles pour celebrer et feliciter",
    ["ANTICIPATION"] = "Action de preparer a l'avance",
    ["REUNIONFAMILIALE"] = "Rassemblement de proches pour celebrer",
    ["RECONNAISSANCE"] = "Expression de gratitude envers quelqu'un",
    ["HOSPITALITE"] = "Art d'accueillir les invites",
    ["SONORISATION"] = "Mise en place du son pour l'evenement",
    ["ECLAIRAGE"] = "Disposition des lumieres pour l'ambiance",
    ["LUMINAIRES"] = "Sources de lumiere decoratives",
    ["AGENCEMENT"] = "Disposition organisee de la salle",
    ["EVENEMENTIEL"] = "Relatif a l'organisation d'evenements",
    ["CHOREGRAPHIE"] = "Enchainement organise de mouvements",
    ["SPECTACLES"] = "Representations prevues pour divertir",
    ["RESTAURATION"] = "Service de repas pour la fete",
    ["GOURMANDISES"] = "Douceurs servies en celebration",
    ["CONFECTION"] = "Fabrication de mets ou decorations",
    ["PRESTATIONS"] = "Services proposes pendant l'evenement",
    ["PROGRAMMATION"] = "Plan detaille des activites",
    ["REPERTOIRE"] = "Liste de morceaux selectionnes",
    ["INTERVENANTS"] = "Personnes qui animent ou parlent",
    ["ORGANISATEUR"] = "Personne chargee de la fete",
    ["COLLABORATION"] = "Travail commun pour l'organisation",
    ["CHRONOLOGIE"] = "Ordre des temps de la fete",
    ["CEREMONIAL"] = "Ensemble des usages de ceremonie",
    ["ALLEGRESSE"] = "Etat d'esprit tres joyeux",
    ["EXUBERANCE"] = "Manifestation d'une joie debordante",
    ["ANNONCEMENT"] = "Fait d'annoncer l'evenement",
    ["MEMORABILITE"] = "Qualite de ce qui marque les esprits",
    ["REMEMORATION"] = "Action de se rappeler un souvenir",
    ["SYNCHRONISATION"] = "Coordination precise des moments",
    ["DECORATIF"] = "Qui sert a embellir la fete",
    ["REPARTITION"] = "Distribution des roles ou taches",
    ["CONCEPTION"] = "Action d'imaginer un evenement",
    ["INSCRIPTION"] = "Action d'enregistrer des invites",
    ["EQUIPEMENT"] = "Materiel necessaire a la fete",
    ["PRESENTATION"] = "Maniere de mettre en valeur",
    ["SOUVENANCE"] = "Fait de garder en memoire",
    ["DISCOURS"] = "Prise de parole pour la fete",
    ["REMERCIER"] = "Action d'exprimer sa gratitude",
    ["COMMUNICATION"] = "Diffusion d'informations sur la fete",
    ["DIFFUSION"] = "Action de faire circuler une annonce",
    ["RENCONTRES"] = "Moments de reunion entre invites",
    ["SOUHAITER"] = "Formuler des voeux pour l'anniversaire",
    ["REUNIONS"] = "Rassemblements organises",
    ["CONFERENCES"] = "Interventions orales devant le public",
    ["EMERVEILLEMENT"] = "Etat d'admiration et de surprise",
    ["ENCHANTEMENT"] = "Sentiment de joie intense",
    ["EMOTIONNEL"] = "Qui touche les sentiments",
    ["APPLAUDISSEMENTS"] = "Marques de satisfaction du public",
    ["OVATIONS"] = "Applaudissements enthousiastes",
    ["REUSSITE"] = "Fait d'atteindre le resultat voulu",
    ["ANNONCES"] = "Messages qui informent les invites",
    ["SPECTATEURS"] = "Personnes qui assistent a un spectacle",
    ["CONTRIBUTION"] = "Participation ou aide a l'organisation",
    ["MOBILISATION"] = "Action de rassembler des ressources",
    ["MOTIVATION"] = "Ensemble des raisons d'agir",
    ["COLLATION"] = "Repas leger partage pendant la fete",
    ["CONVIVES"] = "Invites reunis a table",
    ["COORDINATEUR"] = "Personne qui coordonne l'evenement",
    ["PARTENARIAT"] = "Accord pour organiser ensemble",
    ["ATTENTIONS"] = "Petits gestes pour faire plaisir",
    ["HARMONISATION"] = "Mise en accord des elements",
    ["EVENEMENT"] = "Moment marquant celebre ensemble",
    ["TRADITION"] = "Usage transmis qui revient chaque annee",
    ["SOUHAITS"] = "Voeux adresses a la personne fete",
    ["PRESENCE"] = "Fait d'etre la pour partager la fete",
    ["HARMONIE"] = "Accord entre les elements de la fete",
    ["EUPHORIE"] = "Sentiment de joie intense",
    ["EMOTIONS"] = "Sentiments ressentis lors de la fete",
    ["SURPRISE"] = "Evenement inattendu prepare en secret",
    ["FESTIVAL"] = "Suite d'activites festives",
    ["RENCONTRE"] = "Action de se reunir pour celebrer",
    ["RENDEZVOUS"] = "Moment fixe pour se retrouver",
    ["HOMMAGES"] = "Marques de respect pour la personne",
    ["CELEBRATIONNELLE"] = "Qui convient a une celebration",
    ["EVENEMENTIELLE"] = "Relatif a l'organisation d'evenements",
    ["COMMUNAUTAIRE"] = "Fait avec le cercle de proches",
    ["SPECTACULAIRE"] = "Qui produit un effet marquant",
    ["INATTENDU"] = "Qui survient sans prevenir",
    ["SURPRENANT"] = "Qui etonne par sa nouveaute",
    ["DISTINCTION"] = "Marque d'honneur pour la personne",
    ["DECENNIE"] = "Periode de dix ans",
    ["BIENVENUE"] = "Formule d'accueil",
    ["RENOUVELER"] = "Faire revenir une attention",
    ["ACCOMPAGNEMENT"] = "Presence pour soutenir la fete",
    ["EVOCATION"] = "Action de rappeler un souvenir",
    ["SPLENDEUR"] = "Eclat remarquable de la fete",
    ["MAJESTE"] = "Solennite d'un moment",
    ["RAYONNANT"] = "Qui irradie la joie",
};

ClueOverrides.Apply(clues);

var cacheRoot = GeneratorSettings.CacheRoot;
Directory.CreateDirectory(cacheRoot);

IReadOnlyList<string> requiredWords = Array.Empty<string>();
var definitionCachePath = Path.Combine(cacheRoot, "definitions.fr.json");
var definitionProvider = new DefinitionProvider(clues, definitionCachePath);
var resultStore = ResultStore.TryOpen(GeneratorSettings.ResultDbPath, runMode != RunMode.Pdf);

if (runMode == RunMode.Pdf)
{
    try
    {
        RunPdfOnly(resultStore, definitionProvider, outputFileName, outputSolutionFileName);
    }
    finally
    {
        definitionProvider.SaveCache();
        definitionProvider.Dispose();
        resultStore?.Dispose();
    }

    return;
}

var hunspellRoot = Path.Combine(AppContext.BaseDirectory, "data", "hunspell");
var hunspellAll = HunspellWordLibrary.LoadFromHunspell(hunspellRoot, "fr_FR", cacheRoot);

try
{
    PhaseLogger.Write($"Config defs: def_parallel={GeneratorSettings.DefinitionParallelism}, http_max_conn={GeneratorSettings.HttpMaxConnections}, http_timeout={GeneratorSettings.HttpTimeoutSeconds}s, wiktionary_net={GeneratorSettings.UseWiktionaryNet}");
    if (GeneratorSettings.DefinitionParallelism > GeneratorSettings.HttpMaxConnections)
    {
        PhaseLogger.Write("Alerte defs: DEF_PARALLEL > HTTP_MAX_CONN (risque de timeouts/429).");
    }
    if (GeneratorSettings.HttpTimeoutSeconds <= 10)
    {
        PhaseLogger.Write("Note defs: HTTP_TIMEOUT_SEC bas, possible hausse des timeouts.");
    }

    if (runMode == RunMode.Grid)
    {
        PhaseLogger.Write("Mode grid: generation de la grille uniquement (SQLite, pas de reseau).");
        definitionProvider.DisableNetwork();
    }

    var useDefinitionsInSolve = !GeneratorSettings.IgnoreDefinitions && runMode != RunMode.Grid;
    PhaseLogger.Write("Phase 1/4: preparation du dictionnaire");
    var phase1Timer = Stopwatch.StartNew();
    PhaseLogger.Write("Mode sans theme: dictionnaire base sur SQLite/Hunspell.");
    var themeWords = WordPoolBuilder.BuildNoThemeWords(hunspellAll, definitionProvider, gridSize, out var poolFromDefinitions);
    phase1Timer.Stop();
    PhaseLogger.Write($"Pool limits: per_length={GeneratorSettings.PoolMaxPerLength}, total={GeneratorSettings.PoolMaxTotal}, use_defs={GeneratorSettings.PoolUseDefinitions}, min_defs={GeneratorSettings.PoolMinDefinitions}, min_total={GeneratorSettings.PoolMinTotal}");
    PhaseLogger.Write($"Mots utilisables: {themeWords.Count}");
    PhaseLogger.Write($"Phase 1/4 terminee en {phase1Timer.Elapsed:g}");

    if (useDefinitionsInSolve && GeneratorSettings.PrefetchAllDefinitions)
    {
        PhaseLogger.Write($"Prefetch defs global: {themeWords.Count} mots");
        definitionProvider.EnableNetwork();
        definitionProvider.PrefetchDefinitions(themeWords);
        definitionProvider.ReportStats("prefetch-all");
        if (GeneratorSettings.PrefetchOnly)
        {
            Console.WriteLine("Prefetch termine. Arret demande.");
            return;
        }
    }

    if (!GeneratorSettings.IgnoreDefinitions && !poolFromDefinitions && GeneratorSettings.PoolUseDefinitions)
    {
        var filteredWords = definitionProvider.FilterWordsByStoredDefinitions(themeWords);
        PhaseLogger.Write($"Mots disponibles en SQLite: {filteredWords.Count}");
        themeWords = filteredWords;
    }
    WordQuality.Initialize(themeWords, clues, requiredWords);
    definitionProvider.ReportStats("post-expansion");

    PhaseLogger.Write("Phase 2/4: construction du pattern");
    PhaseLogger.Write($"Variantes de pattern: {GeneratorSettings.PatternVariants}");

    PhaseLogger.Write($"Mots utilises par le solveur (phase 3): {themeWords.Count}");
    PhaseLogger.Write("Phase 3/4: resolution deterministe");
    var phase3Timer = Stopwatch.StartNew();
    SolveResult result;
    if (GeneratorSettings.SolverMode == SolverMode.Csp)
    {
        result = SolveRunner.SolveCsp(
            gridSize,
            themeWords,
            requiredWords,
            GeneratorSettings.PatternVariants,
            GeneratorSettings.CspAttempts,
            GeneratorSettings.CspCandidateLimit);
    }
    else if (GeneratorSettings.SolverMode == SolverMode.Incremental)
    {
        result = SolveRunner.SolveIncremental(
            gridSize,
            themeWords,
            GeneratorSettings.IncrementalAttempts,
            GeneratorSettings.IncrementalCandidateLimit);
    }
    else
    {
        result = useDefinitionsInSolve
            ? SolveRunner.SolveWithDefinitionRetries(
                gridSize,
                themeWords,
                requiredWords,
                definitionProvider,
                GeneratorSettings.DefinitionSolvePasses,
                GeneratorSettings.ExtraBlackCandidates,
                GeneratorSettings.PatternVariants)
            : SolveRunner.SolveWithoutDefinitions(
                gridSize,
                themeWords,
                requiredWords,
                GeneratorSettings.ExtraBlackCandidates,
                GeneratorSettings.PatternVariants);
    }
    phase3Timer.Stop();

    // Prefer the solver's filled grid as source of truth. If a solver only returned placements,
    // reconstruct the filled grid from them. Then re-extract placements from the final grid so
    // we persist all across+down words.
    var grid = result.Grid;
    if (grid.CountFilledCells() == 0 && result.Placements.Count > 0)
    {
        grid = CrosswordGridFiller.BuildFilledGrid(grid, result.Placements);
    }

    var placements = PlacementExtractor.ExtractPlacements(grid);

    // Guardrail: persist only fully filled grids (no empty cells).
    var totalCells = grid.Size * grid.Size;
    var filledCells = grid.CountFilledCells();
    var blackCells = grid.CountBlackCells();
    if (filledCells + blackCells != totalCells)
    {
        throw new InvalidOperationException($"Solution incomplete: filled={filledCells}, blacks={blackCells}, total={totalCells}");
    }
    if (GeneratorSettings.SolverMode == SolverMode.Pattern)
    {
        var patternSpec = PatternBuilder.GetPatternSpec(result.PatternVariant);
        Console.WriteLine($"Pattern retenu: variant {result.PatternVariant + 1}/{GeneratorSettings.PatternVariants} (pas {patternSpec.Step}, offset {patternSpec.Offset}), {result.Attempt} cases noires supplementaires");
    }
    else if (GeneratorSettings.SolverMode == SolverMode.Csp)
    {
        var patternSpec = PatternBuilder.GetPatternSpec(result.PatternVariant);
        Console.WriteLine($"Solveur CSP: pattern variant {result.PatternVariant + 1}/{GeneratorSettings.PatternVariants} (pas {patternSpec.Step}, offset {patternSpec.Offset}), cases noires={grid.CountBlackCells()}");
    }
    else
    {
        Console.WriteLine($"Solveur incremental: cases noires={grid.CountBlackCells()}");
    }
    PhaseLogger.Write($"Phase 3/4 terminee en {phase3Timer.Elapsed:g}");

    if (grid is null || placements is null)
    {
        throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
    }

    resultStore?.SaveResult(grid, placements, requiredWords);
    if (resultStore is not null)
    {
        Console.WriteLine($"Resultat enregistre: {GeneratorSettings.ResultDbPath}");
    }

    if (!useDefinitionsInSolve)
    {
        if (runMode == RunMode.Grid)
        {
            Console.WriteLine("Mode grid: generation terminee apres sauvegarde SQLite.");
        }
        else
        {
            Console.WriteLine("Mode sans definitions: generation terminee apres sauvegarde SQLite.");
        }
        return;
    }

    var prefetchTimer = Stopwatch.StartNew();
    var definitionWords = placements
        .Select(p => p.Word)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    PhaseLogger.Write($"Prefetch definitions: {definitionWords.Count} mots");
    definitionProvider.EnableNetwork();
    definitionProvider.PrefetchDefinitions(definitionWords);
    prefetchTimer.Stop();
    PhaseLogger.Write($"Prefetch definitions termine en {prefetchTimer.Elapsed:g}");
    definitionProvider.DisableNetwork();
    definitionProvider.ReportStats("post-prefetch");

    var revealCells = CrosswordReveal.BuildRevealCells(placements, requiredWords);
    var entries = CrosswordNumbering.Generate(grid, definitionProvider.GetDefinition);
    var horizontal = ClueAggregator.Aggregate(entries.Where(e => e.Orientation == Orientation.Horizontal).ToList());
    var vertical = ClueAggregator.Aggregate(entries.Where(e => e.Orientation == Orientation.Vertical).ToList());

    var outputPath = Path.Combine(Environment.CurrentDirectory, outputFileName);
    var solutionPath = Path.Combine(Environment.CurrentDirectory, outputSolutionFileName);
    PhaseLogger.Write("Phase 4/4: generation du PDF");
    var phase4Timer = Stopwatch.StartNew();
    var document = new CrosswordDocument(grid, horizontal, vertical, revealCells, "Mots croises - Theme anniversaire");
    document.GeneratePdf(outputPath);
    var solutionRevealCells = CrosswordReveal.BuildRevealAllCells(grid);
    var solutionDocument = new CrosswordDocument(grid, horizontal, vertical, solutionRevealCells, "Mots croises - Solution");
    solutionDocument.GeneratePdf(solutionPath);
    phase4Timer.Stop();
    PhaseLogger.Write($"Phase 4/4 terminee en {phase4Timer.Elapsed:g}");

    Console.WriteLine($"PDF genere: {outputPath}");
    Console.WriteLine($"PDF solution: {solutionPath}");
    Console.WriteLine($"Mots places: {placements.Count}");
    Console.WriteLine($"Cases remplies: {grid.CountFilledCells()}");
    Console.WriteLine($"Cases noires: {grid.CountBlackCells()}");
    Console.WriteLine($"Lettres revelees: {revealCells.Count}");
}
finally
{
    definitionProvider.SaveCache();
    definitionProvider.Dispose();
    resultStore?.Dispose();
}

static void RunPdfOnly(ResultStore? resultStore, DefinitionProvider definitionProvider, string outputFileName, string outputSolutionFileName)
{
    if (resultStore is null)
    {
        Console.WriteLine("Result DB introuvable. Definir MOTCROISE_RESULT_DB.");
        return;
    }

    var loaded = resultStore.LoadResult();
    if (loaded is null)
    {
        Console.WriteLine("Aucune grille chargee depuis le SQLite resultat.");
        return;
    }

    var grid = loaded.Grid;
    var placements = loaded.Placements;
    var requiredWords = loaded.RequiredWords;
    definitionProvider.DisableNetwork();

    // Some solve modes persist the black pattern and store words as placements.
    // Always rebuild a fully-filled grid for rendering (and correct stats).
    grid = CrosswordGridFiller.BuildFilledGrid(grid, placements);

    var revealCells = CrosswordReveal.BuildRevealCells(placements, requiredWords);
    var entries = CrosswordNumbering.Generate(grid, definitionProvider.GetDefinition);
    var horizontal = ClueAggregator.Aggregate(entries.Where(e => e.Orientation == Orientation.Horizontal).ToList());
    var vertical = ClueAggregator.Aggregate(entries.Where(e => e.Orientation == Orientation.Vertical).ToList());

    var outputPath = Path.Combine(Environment.CurrentDirectory, outputFileName);
    var solutionPath = Path.Combine(Environment.CurrentDirectory, outputSolutionFileName);
    var document = new CrosswordDocument(grid, horizontal, vertical, revealCells, "Mots croises - Theme anniversaire");
    document.GeneratePdf(outputPath);
    var solutionRevealCells = CrosswordReveal.BuildRevealAllCells(grid);
    var solutionDocument = new CrosswordDocument(grid, horizontal, vertical, solutionRevealCells, "Mots croises - Solution");
    solutionDocument.GeneratePdf(solutionPath);

    Console.WriteLine($"PDF genere: {outputPath}");
    Console.WriteLine($"PDF solution: {solutionPath}");
    Console.WriteLine($"Mots places: {placements.Count}");
    Console.WriteLine($"Cases remplies: {grid.CountFilledCells()}");
    Console.WriteLine($"Cases noires: {grid.CountBlackCells()}");
    Console.WriteLine($"Lettres revelees: {revealCells.Count}");
}

static class CrosswordGridFiller
{
    public static CrosswordGrid BuildFilledGrid(CrosswordGrid baseGrid, IEnumerable<WordPlacement> placements)
    {
        var size = baseGrid.Size;
        var filled = new CrosswordGrid(size);

        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (baseGrid.IsBlack(r, c))
                {
                    filled.SetBlack(r, c);
                }
            }
        }

        foreach (var placement in placements)
        {
            var word = WordUtils.Normalize(placement.Word);
            for (var i = 0; i < word.Length; i++)
            {
                var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
                var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);

                if (row < 0 || row >= size || col < 0 || col >= size)
                {
                    continue;
                }

                if (filled.IsBlack(row, col))
                {
                    // Invalid placement against the pattern; skip to avoid corrupting output.
                    break;
                }

                var prev = filled.GetCell(row, col);
                var ch = word[i];
                if (prev != '\0' && prev != ch)
                {
                    // Conflict between placements; skip to avoid corrupting output.
                    break;
                }

                filled.SetLetter(row, col, ch);
            }
        }

        return filled;
    }
}

static class InvalidWordCleaner
{
    public static List<WordEntry> Clean(
        CrosswordGrid grid,
        DefinitionProvider provider,
        HashSet<(int Row, int Col)> protectedCells,
        int maxPasses)
    {
        for (var pass = 0; pass < maxPasses; pass++)
        {
            var entries = CrosswordNumbering.Generate(grid, provider.GetDefinition);
            var invalidEntries = entries
                .Where(entry => !provider.TryGetDefinition(entry.Word, out _))
                .ToList();

            PhaseLogger.Write($"Nettoyage: passe {pass + 1}/{maxPasses} - invalides {invalidEntries.Count}");
            if (invalidEntries.Count == 0)
            {
                return entries;
            }

            var changed = false;
            foreach (var entry in invalidEntries)
            {
                if (TryBreakEntry(grid, entry, protectedCells))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                return entries;
            }
        }

        return CrosswordNumbering.Generate(grid, provider.GetDefinition);
    }

    private static bool TryBreakEntry(CrosswordGrid grid, WordEntry entry, HashSet<(int Row, int Col)> protectedCells)
    {
        var length = entry.Word.Length;
        var indexes = BuildSplitIndexes(length);

        foreach (var index in indexes)
        {
            var row = entry.Row + (entry.Orientation == Orientation.Vertical ? index : 0);
            var col = entry.Col + (entry.Orientation == Orientation.Horizontal ? index : 0);

            if (protectedCells.Contains((row, col)))
            {
                continue;
            }

            grid.SetBlack(row, col);
            return true;
        }

        return false;
    }

    private static List<int> BuildSplitIndexes(int length)
    {
        var indexes = new List<int>();
        if (length >= 4)
        {
            for (var i = 2; i <= length - 3; i++)
            {
                indexes.Add(i);
            }
        }

        for (var i = 1; i < length - 1; i++)
        {
            if (!indexes.Contains(i))
            {
                indexes.Add(i);
            }
        }

        if (length >= 2)
        {
            indexes.Add(0);
            indexes.Add(length - 1);
        }

        return indexes;
    }
}

static class CrosswordGenerator
{
    private static int MaxPlacementCandidates => GeneratorSettings.MaxPlacementCandidates;

    public static GridResult GenerateBest(int size, IEnumerable<string> words, IEnumerable<string> requiredWords, int iterations, int seed)
    {
        var wordList = words.Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2 && word.Length <= size)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var required = new HashSet<string>(requiredWords.Select(WordUtils.Normalize), StringComparer.OrdinalIgnoreCase);
        foreach (var word in required)
        {
            if (!wordList.Contains(word, StringComparer.OrdinalIgnoreCase))
            {
                wordList.Add(word);
            }
        }

        var bestLock = new object();
        GridResult? best = null;
        var progress = new ProgressTracker(
            $"Seed {seed}",
            iterations,
            GeneratorSettings.ShowProgress,
            GeneratorSettings.ProgressInline);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = GeneratorSettings.Parallelism,
        };

        Parallel.For(0, iterations, options, attempt =>
        {
            var localRandom = new Random(unchecked(seed + (attempt * 7919)));
            var result = GenerateOnce(size, wordList, required, localRandom);
            if (result is null)
            {
                progress.Tick();
                return;
            }

            lock (bestLock)
            {
                if (best is null ||
                    result.FilledCells > best.FilledCells ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount > best.SegmentCount) ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount == best.SegmentCount && result.MultiLineScore > best.MultiLineScore) ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount == best.SegmentCount && result.MultiLineScore == best.MultiLineScore && result.BlackRunOverage < best.BlackRunOverage) ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount == best.SegmentCount && result.MultiLineScore == best.MultiLineScore && result.BlackRunOverage == best.BlackRunOverage && result.MaxBlackRun < best.MaxBlackRun) ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount == best.SegmentCount && result.MultiLineScore == best.MultiLineScore && result.BlackRunOverage == best.BlackRunOverage && result.MaxBlackRun == best.MaxBlackRun && result.Placements.Count > best.Placements.Count) ||
                    (result.FilledCells == best.FilledCells && result.SegmentCount == best.SegmentCount && result.MultiLineScore == best.MultiLineScore && result.BlackRunOverage == best.BlackRunOverage && result.MaxBlackRun == best.MaxBlackRun && result.Placements.Count == best.Placements.Count && result.Intersections > best.Intersections))
                {
                    best = result;
                }
            }

            progress.Tick();
        });

        return best ?? throw new InvalidOperationException("Generation de grille impossible.");
    }

    private static GridResult? GenerateOnce(int size, List<string> words, HashSet<string> required, Random random)
    {
        var grid = new CrosswordGrid(size);
        var placements = new List<WordPlacement>();
        var usedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var intersections = 0;

        var anchor = required.OrderByDescending(word => word.Length).First();
        var anchorOrientation = random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
        var anchorPlacement = PlaceAnchor(grid, anchor, anchorOrientation, random);

        placements.Add(anchorPlacement);
        usedWords.Add(anchor);

        foreach (var word in required.Where(word => !word.Equals(anchor, StringComparison.OrdinalIgnoreCase)))
        {
            var placementInfo = TryPlaceWord(grid, word, random);
            if (placementInfo is null)
            {
                return null;
            }

            placements.Add(placementInfo.Placement);
            intersections += placementInfo.Intersections;
            usedWords.Add(word);
        }

        var remaining = PickCandidateWords(words, usedWords, MaxPlacementCandidates, random);
        for (var pass = 0; pass < 3 && remaining.Count > 0; pass++)
        {
            var ordered = pass switch
            {
                0 => remaining.OrderByDescending(word => word.Length).ThenBy(_ => random.Next()).ToList(),
                1 => remaining.OrderBy(word => word.Length).ThenBy(_ => random.Next()).ToList(),
                _ => remaining.OrderBy(_ => random.Next()).ToList(),
            };

            var nextRemaining = new List<string>();
            var placedAny = false;

            foreach (var word in ordered)
            {
                var placementInfo = TryPlaceWord(grid, word, random);
                if (placementInfo is null)
                {
                    nextRemaining.Add(word);
                    continue;
                }

                placements.Add(placementInfo.Placement);
                intersections += placementInfo.Intersections;
                usedWords.Add(word);
                placedAny = true;
            }

            remaining = nextRemaining;
            if (!placedAny)
            {
                break;
            }
        }

        var filledCells = grid.CountFilledCells();
        var multiLineScore = CountMultiLineScore(placements);
        var segmentCount = CountSegments(grid);
        var blackCells = (size * size) - filledCells;
        var (blackRunOverage, maxBlackRun) = CountBlackRunStats(grid);
        return new GridResult(grid, placements, intersections, filledCells, multiLineScore, segmentCount, blackCells, blackRunOverage, maxBlackRun);
    }

    private static List<string> PickCandidateWords(
        List<string> words,
        HashSet<string> usedWords,
        int maxCount,
        Random random)
    {
        if (words.Count <= maxCount)
        {
            return words.Where(word => !usedWords.Contains(word)).ToList();
        }

        var candidates = new List<string>(maxCount);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var attempts = 0;
        var maxAttempts = maxCount * 6;

        while (candidates.Count < maxCount && attempts < maxAttempts)
        {
            attempts++;
            var word = words[random.Next(words.Count)];
            if (usedWords.Contains(word))
            {
                continue;
            }

            if (seen.Add(word))
            {
                candidates.Add(word);
            }
        }

        if (candidates.Count < maxCount)
        {
            foreach (var word in words)
            {
                if (candidates.Count >= maxCount)
                {
                    break;
                }

                if (usedWords.Contains(word))
                {
                    continue;
                }

                if (seen.Add(word))
                {
                    candidates.Add(word);
                }
            }
        }

        return candidates;
    }

    private static WordPlacement PlaceAnchor(CrosswordGrid grid, string anchor, Orientation orientation, Random random)
    {
        var normalized = WordUtils.Normalize(anchor);
        var maxRow = orientation == Orientation.Horizontal ? grid.Size - 1 : grid.Size - normalized.Length;
        var maxCol = orientation == Orientation.Horizontal ? grid.Size - normalized.Length : grid.Size - 1;
        var margin = 2;

        var rowMin = 0;
        var rowMax = maxRow;
        var colMin = 0;
        var colMax = maxCol;

        if (maxRow - margin >= margin)
        {
            rowMin = margin;
            rowMax = maxRow - margin;
        }

        if (maxCol - margin >= margin)
        {
            colMin = margin;
            colMax = maxCol - margin;
        }

        var row = random.Next(rowMin, rowMax + 1);
        var col = random.Next(colMin, colMax + 1);

        grid.PlaceWord(normalized, row, col, orientation);
        return new WordPlacement(normalized, row, col, orientation);
    }

    private static PlacementInfo? TryPlaceWord(CrosswordGrid grid, string word, Random random)
    {
        var normalized = WordUtils.Normalize(word);
        var bestPlacements = new List<WordPlacement>();
        var bestIntersections = -1;

        for (var row = 0; row < grid.Size; row++)
        {
            for (var col = 0; col < grid.Size; col++)
            {
                if (!grid.IsLetter(row, col))
                {
                    continue;
                }

                var existing = grid.GetCell(row, col);
                for (var index = 0; index < normalized.Length; index++)
                {
                    if (normalized[index] != existing)
                    {
                        continue;
                    }

                    var startCol = col - index;
                    if (grid.CanPlace(normalized, row, startCol, Orientation.Horizontal))
                    {
                        var intersections = CountIntersections(grid, normalized, row, startCol, Orientation.Horizontal);
                        if (intersections > bestIntersections)
                        {
                            bestIntersections = intersections;
                            bestPlacements.Clear();
                            bestPlacements.Add(new WordPlacement(normalized, row, startCol, Orientation.Horizontal));
                        }
                        else if (intersections == bestIntersections)
                        {
                            bestPlacements.Add(new WordPlacement(normalized, row, startCol, Orientation.Horizontal));
                        }
                    }

                    var startRow = row - index;
                    if (grid.CanPlace(normalized, startRow, col, Orientation.Vertical))
                    {
                        var intersections = CountIntersections(grid, normalized, startRow, col, Orientation.Vertical);
                        if (intersections > bestIntersections)
                        {
                            bestIntersections = intersections;
                            bestPlacements.Clear();
                            bestPlacements.Add(new WordPlacement(normalized, startRow, col, Orientation.Vertical));
                        }
                        else if (intersections == bestIntersections)
                        {
                            bestPlacements.Add(new WordPlacement(normalized, startRow, col, Orientation.Vertical));
                        }
                    }
                }
            }
        }

        if (bestPlacements.Count > 0)
        {
            var chosen = bestPlacements[random.Next(bestPlacements.Count)];
            grid.PlaceWord(chosen.Word, chosen.Row, chosen.Col, chosen.Orientation);
            return new PlacementInfo(chosen, bestIntersections);
        }

        return null;
    }

    private static int CountIntersections(CrosswordGrid grid, string word, int row, int col, Orientation orientation)
    {
        var count = 0;
        for (var i = 0; i < word.Length; i++)
        {
            var r = row + (orientation == Orientation.Vertical ? i : 0);
            var c = col + (orientation == Orientation.Horizontal ? i : 0);
            if (grid.IsLetter(r, c))
            {
                count++;
            }
        }

        return count;
    }

    internal static int CountMultiLineScore(IEnumerable<WordPlacement> placements)
    {
        var horizontalRows = placements.Where(p => p.Orientation == Orientation.Horizontal)
            .GroupBy(p => p.Row)
            .Count(group => group.Count() >= 2);

        var verticalCols = placements.Where(p => p.Orientation == Orientation.Vertical)
            .GroupBy(p => p.Col)
            .Count(group => group.Count() >= 2);

        return horizontalRows + verticalCols;
    }

    internal static int CountSegments(CrosswordGrid grid)
    {
        var count = 0;

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (!grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && grid.IsLetter(row, col))
                {
                    col++;
                }

                if (col - start >= 2)
                {
                    count++;
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (!grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && grid.IsLetter(row, col))
                {
                    row++;
                }

                if (row - start >= 2)
                {
                    count++;
                }
            }
        }

        return count;
    }

    internal static (int Overage, int MaxRun) CountBlackRunStats(CrosswordGrid grid)
    {
        var overage = 0;
        var maxRun = 0;

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && !grid.IsLetter(row, col))
                {
                    col++;
                }

                var length = col - start;
                if (length > maxRun)
                {
                    maxRun = length;
                }

                if (length > 1)
                {
                    overage += length - 1;
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && !grid.IsLetter(row, col))
                {
                    row++;
                }

                var length = row - start;
                if (length > maxRun)
                {
                    maxRun = length;
                }

                if (length > 1)
                {
                    overage += length - 1;
                }
            }
        }

        return (overage, maxRun);
    }

    internal static (int Overage, int MaxRun) CountBetweenWordRuns(CrosswordGrid grid)
    {
        var overage = 0;
        var maxRun = 0;

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && !grid.IsLetter(row, col))
                {
                    col++;
                }

                var length = col - start;
                var hasBefore = start > 0 && grid.IsLetter(row, start - 1);
                var hasAfter = col < grid.Size && grid.IsLetter(row, col);

                if (hasBefore && hasAfter)
                {
                    if (length > maxRun)
                    {
                        maxRun = length;
                    }

                    if (length > 1)
                    {
                        overage += length - 1;
                    }
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && !grid.IsLetter(row, col))
                {
                    row++;
                }

                var length = row - start;
                var hasBefore = start > 0 && grid.IsLetter(start - 1, col);
                var hasAfter = row < grid.Size && grid.IsLetter(row, col);

                if (hasBefore && hasAfter)
                {
                    if (length > maxRun)
                    {
                        maxRun = length;
                    }

                    if (length > 1)
                    {
                        overage += length - 1;
                    }
                }
            }
        }

        return (overage, maxRun);
    }
}

static class CrosswordReveal
{
    public static HashSet<(int Row, int Col)> BuildRevealCells(IEnumerable<WordPlacement> placements, IEnumerable<string> requiredWords)
    {
        var required = new HashSet<string>(requiredWords.Select(WordUtils.Normalize), StringComparer.OrdinalIgnoreCase);
        var cells = new HashSet<(int Row, int Col)>();

        foreach (var placement in placements)
        {
            if (!required.Contains(placement.Word))
            {
                continue;
            }

            for (var i = 0; i < placement.Word.Length; i++)
            {
                var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
                var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);
                cells.Add((row, col));
            }
        }

        return cells;
    }

    public static HashSet<(int Row, int Col)> BuildRevealAllCells(CrosswordGrid grid)
    {
        var cells = new HashSet<(int Row, int Col)>();
        for (var row = 0; row < grid.Size; row++)
        {
            for (var col = 0; col < grid.Size; col++)
            {
                if (grid.IsLetter(row, col))
                {
                    cells.Add((row, col));
                }
            }
        }

        return cells;
    }
}

static class GapFiller
{
    public static void Fill(CrosswordGrid grid, List<WordPlacement> placements, IEnumerable<string> words, int seed)
    {
        var random = new Random(seed);
        var wordList = words.Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2 && word.Length <= grid.Size)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var wordsByLength = wordList
            .GroupBy(word => word.Length)
            .ToDictionary(group => group.Key, group => group.ToList());

        var usedWords = new HashSet<string>(placements.Select(p => p.Word), StringComparer.OrdinalIgnoreCase);

        for (var pass = 0; pass < 8; pass++)
        {
            var placedAny = false;
            placedAny |= FillOrientation(grid, placements, usedWords, wordsByLength, random, Orientation.Horizontal);
            placedAny |= FillOrientation(grid, placements, usedWords, wordsByLength, random, Orientation.Vertical);

            if (!placedAny)
            {
                break;
            }
        }
    }

    private static bool FillOrientation(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        HashSet<string> usedWords,
        Dictionary<int, List<string>> wordsByLength,
        Random random,
        Orientation orientation)
    {
        var placedAny = false;
        var size = grid.Size;

        for (var line = 0; line < size; line++)
        {
            var pos = 0;
            while (pos < size)
            {
                var isLetter = orientation == Orientation.Horizontal
                    ? grid.IsLetter(line, pos)
                    : grid.IsLetter(pos, line);

                if (isLetter)
                {
                    pos++;
                    continue;
                }

                var runStart = pos;
                while (pos < size)
                {
                    var cellIsLetter = orientation == Orientation.Horizontal
                        ? grid.IsLetter(line, pos)
                        : grid.IsLetter(pos, line);
                    if (cellIsLetter)
                    {
                        break;
                    }

                    pos++;
                }

                var runEnd = pos - 1;
                var runLength = pos - runStart;
                if (runLength < 2)
                {
                    continue;
                }

                var hasBefore = runStart > 0 && (orientation == Orientation.Horizontal
                    ? grid.IsLetter(line, runStart - 1)
                    : grid.IsLetter(runStart - 1, line));
                var hasAfter = runEnd < size - 1 && (orientation == Orientation.Horizontal
                    ? grid.IsLetter(line, runEnd + 1)
                    : grid.IsLetter(runEnd + 1, line));

                var availableStart = runStart + (hasBefore ? 1 : 0);
                var availableEnd = runEnd - (hasAfter ? 1 : 0);
                var availableLength = availableEnd - availableStart + 1;

                if (availableLength < 2)
                {
                    continue;
                }

                if (FillSegment(grid, placements, usedWords, wordsByLength, random, orientation, line, availableStart, availableLength))
                {
                    placedAny = true;
                }
            }
        }

        return placedAny;
    }

    private static bool FillSegment(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        HashSet<string> usedWords,
        Dictionary<int, List<string>> wordsByLength,
        Random random,
        Orientation orientation,
        int line,
        int start,
        int length)
    {
        var placedAny = false;
        var cursor = start;
        var remaining = length;

        while (remaining >= 2)
        {
            var maxLength = Math.Min(remaining, grid.Size);
            var lengths = BuildLengthOptions(maxLength, remaining);
            var placed = false;

            foreach (var wordLength in lengths)
            {
                if (!wordsByLength.TryGetValue(wordLength, out var candidates) || candidates.Count == 0)
                {
                    continue;
                }

                var startIndex = random.Next(candidates.Count);
                for (var i = 0; i < candidates.Count; i++)
                {
                    var word = candidates[(startIndex + i) % candidates.Count];
                    if (!TryPlaceWordAt(grid, word, orientation, line, cursor))
                    {
                        continue;
                    }

                    var placement = orientation == Orientation.Horizontal
                        ? new WordPlacement(word, line, cursor, orientation)
                        : new WordPlacement(word, cursor, line, orientation);

                    placements.Add(placement);
                    usedWords.Add(word);
                    placedAny = true;
                    placed = true;

                    var remainingAfter = remaining - wordLength;
                    if (remainingAfter <= 1)
                    {
                        remaining = 0;
                    }
                    else
                    {
                        cursor += wordLength + 1;
                        remaining = remainingAfter - 1;
                    }

                    break;
                }

                if (placed)
                {
                    break;
                }
            }

            if (!placed)
            {
                break;
            }
        }

        return placedAny;
    }

    private static List<int> BuildLengthOptions(int maxLength, int remaining)
    {
        var lengths = new List<int>();
        for (var length = maxLength; length >= 2; length--)
        {
            lengths.Add(length);
        }

        return lengths;
    }

    private static bool TryPlaceWordAt(CrosswordGrid grid, string word, Orientation orientation, int line, int cursor)
    {
        if (orientation == Orientation.Horizontal)
        {
            if (!grid.CanPlace(word, line, cursor, orientation))
            {
                return false;
            }

            grid.PlaceWord(word, line, cursor, orientation);
            return true;
        }

        if (!grid.CanPlace(word, cursor, line, orientation))
        {
            return false;
        }

        grid.PlaceWord(word, cursor, line, orientation);
        return true;
    }
}

static class BlackRunRepair
{
    public static void Repair(CrosswordGrid grid, List<WordPlacement> placements, IEnumerable<string> words, int seed)
    {
        var random = new Random(seed);
        var wordList = words.Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2 && word.Length <= grid.Size)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shortWords = wordList.Where(word => word.Length <= 6).ToList();
        var usedWords = new HashSet<string>(placements.Select(p => p.Word), StringComparer.OrdinalIgnoreCase);

        for (var pass = 0; pass < 8; pass++)
        {
            var targets = FindBetweenWordTargets(grid);
            if (targets.Count == 0)
            {
                break;
            }

            Shuffle(targets, random);
            var placedAny = false;

            foreach (var target in targets)
            {
                if (TryPlaceThroughCell(grid, placements, usedWords, shortWords, target.Row, target.Col, target.Orientation, random))
                {
                    placedAny = true;
                }
            }

            if (!placedAny)
            {
                break;
            }
        }

        for (var pass = 0; pass < 10; pass++)
        {
            var targets = FindBlackRunTargets(grid);
            if (targets.Count == 0)
            {
                break;
            }

            Shuffle(targets, random);
            var placedAny = false;

            foreach (var target in targets)
            {
                if (TryPlaceThroughCell(grid, placements, usedWords, shortWords, target.Row, target.Col, target.Orientation, random))
                {
                    placedAny = true;
                }
            }

            if (!placedAny)
            {
                break;
            }
        }
    }

    private static bool TryPlaceThroughCell(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        HashSet<string> usedWords,
        List<string> words,
        int row,
        int col,
        Orientation orientation,
        Random random)
    {
        if (grid.IsLetter(row, col))
        {
            return false;
        }

        var candidates = words.ToList();
        Shuffle(candidates, random);

        foreach (var word in candidates)
        {
            for (var index = 0; index < word.Length; index++)
            {
                var startRow = orientation == Orientation.Horizontal ? row : row - index;
                var startCol = orientation == Orientation.Horizontal ? col - index : col;

                if (!grid.CanPlace(word, startRow, startCol, orientation))
                {
                    continue;
                }

                grid.PlaceWord(word, startRow, startCol, orientation);
                placements.Add(new WordPlacement(word, startRow, startCol, orientation));
                usedWords.Add(word);
                return true;
            }
        }

        return false;
    }

    private static List<(int Row, int Col, Orientation Orientation)> FindBlackRunTargets(CrosswordGrid grid)
    {
        var targets = new HashSet<(int Row, int Col, Orientation Orientation)>();

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && !grid.IsLetter(row, col))
                {
                    col++;
                }

                if (col - start >= 2)
                {
                    for (var c = start; c < col; c++)
                    {
                        targets.Add((row, c, Orientation.Vertical));
                    }
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && !grid.IsLetter(row, col))
                {
                    row++;
                }

                if (row - start >= 2)
                {
                    for (var r = start; r < row; r++)
                    {
                        targets.Add((r, col, Orientation.Horizontal));
                    }
                }
            }
        }

        return targets.ToList();
    }

    private static List<(int Row, int Col, Orientation Orientation)> FindBetweenWordTargets(CrosswordGrid grid)
    {
        var targets = new HashSet<(int Row, int Col, Orientation Orientation)>();

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && !grid.IsLetter(row, col))
                {
                    col++;
                }

                var hasBefore = start > 0 && grid.IsLetter(row, start - 1);
                var hasAfter = col < grid.Size && grid.IsLetter(row, col);
                if (hasBefore && hasAfter && col - start >= 2)
                {
                    for (var c = start; c < col; c++)
                    {
                        targets.Add((row, c, Orientation.Vertical));
                    }
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && !grid.IsLetter(row, col))
                {
                    row++;
                }

                var hasBefore = start > 0 && grid.IsLetter(start - 1, col);
                var hasAfter = row < grid.Size && grid.IsLetter(row, col);
                if (hasBefore && hasAfter && row - start >= 2)
                {
                    for (var r = start; r < row; r++)
                    {
                        targets.Add((r, col, Orientation.Horizontal));
                    }
                }
            }
        }

        return targets.ToList();
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

static class BetweenWordExtender
{
    public static void Repair(CrosswordGrid grid, List<WordPlacement> placements, IEnumerable<string> words, int seed)
    {
        var random = new Random(seed);
        var wordList = words.Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2 && word.Length <= grid.Size)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var pass = 0; pass < 6; pass++)
        {
            var targets = FindBetweenWordRuns(grid);
            if (targets.Count == 0)
            {
                break;
            }

            Shuffle(targets, random);
            var placedAny = false;

            foreach (var target in targets)
            {
                if (target.Orientation == Orientation.Horizontal)
                {
                    if (TryExtendHorizontalRun(grid, placements, wordList, target.Line, target.Start, target.End, random))
                    {
                        placedAny = true;
                    }
                }
                else
                {
                    if (TryExtendVerticalRun(grid, placements, wordList, target.Line, target.Start, target.End, random))
                    {
                        placedAny = true;
                    }
                }
            }

            if (!placedAny)
            {
                break;
            }
        }
    }

    private static bool TryExtendHorizontalRun(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int row,
        int start,
        int end,
        Random random)
    {
        var leftEnd = start - 1;
        var rightStart = end + 1;

        if (leftEnd < 0 || rightStart >= grid.Size)
        {
            return false;
        }

        var leftStart = leftEnd;
        while (leftStart >= 0 && grid.IsLetter(row, leftStart))
        {
            leftStart--;
        }

        leftStart++;
        var leftWord = BuildWord(grid, row, leftStart, leftEnd, Orientation.Horizontal);

        var rightEnd = rightStart;
        while (rightEnd < grid.Size && grid.IsLetter(row, rightEnd))
        {
            rightEnd++;
        }

        rightEnd--;
        var rightWord = BuildWord(grid, row, rightStart, rightEnd, Orientation.Horizontal);

        var options = new List<Func<bool>>
        {
            () => TryExtendLeft(grid, placements, wordList, row, leftStart, leftWord, random),
            () => TryExtendRight(grid, placements, wordList, row, rightStart, rightWord, random),
        };

        Shuffle(options, random);
        foreach (var option in options)
        {
            if (option())
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryExtendVerticalRun(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int col,
        int start,
        int end,
        Random random)
    {
        var topEnd = start - 1;
        var bottomStart = end + 1;

        if (topEnd < 0 || bottomStart >= grid.Size)
        {
            return false;
        }

        var topStart = topEnd;
        while (topStart >= 0 && grid.IsLetter(topStart, col))
        {
            topStart--;
        }

        topStart++;
        var topWord = BuildWord(grid, col, topStart, topEnd, Orientation.Vertical);

        var bottomEnd = bottomStart;
        while (bottomEnd < grid.Size && grid.IsLetter(bottomEnd, col))
        {
            bottomEnd++;
        }

        bottomEnd--;
        var bottomWord = BuildWord(grid, col, bottomStart, bottomEnd, Orientation.Vertical);

        var options = new List<Func<bool>>
        {
            () => TryExtendTop(grid, placements, wordList, col, topStart, topWord, random),
            () => TryExtendBottom(grid, placements, wordList, col, bottomStart, bottomWord, random),
        };

        Shuffle(options, random);
        foreach (var option in options)
        {
            if (option())
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryExtendLeft(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int row,
        int start,
        string word,
        Random random)
    {
        var targetLength = word.Length + 1;
        var candidates = wordList
            .Where(candidate => candidate.Length == targetLength && candidate.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        Shuffle(candidates, random);
        foreach (var candidate in candidates)
        {
            if (!grid.CanPlace(candidate, row, start, Orientation.Horizontal))
            {
                continue;
            }

            grid.PlaceWord(candidate, row, start, Orientation.Horizontal);
            ReplacePlacement(placements, word, row, start, Orientation.Horizontal, candidate, row, start);
            return true;
        }

        return false;
    }

    private static bool TryExtendRight(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int row,
        int start,
        string word,
        Random random)
    {
        var targetLength = word.Length + 1;
        var candidates = wordList
            .Where(candidate => candidate.Length == targetLength && candidate.EndsWith(word, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        Shuffle(candidates, random);
        foreach (var candidate in candidates)
        {
            var newStart = start - 1;
            if (!grid.CanPlace(candidate, row, newStart, Orientation.Horizontal))
            {
                continue;
            }

            grid.PlaceWord(candidate, row, newStart, Orientation.Horizontal);
            ReplacePlacement(placements, word, row, start, Orientation.Horizontal, candidate, row, newStart);
            return true;
        }

        return false;
    }

    private static bool TryExtendTop(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int col,
        int start,
        string word,
        Random random)
    {
        var targetLength = word.Length + 1;
        var candidates = wordList
            .Where(candidate => candidate.Length == targetLength && candidate.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        Shuffle(candidates, random);
        foreach (var candidate in candidates)
        {
            if (!grid.CanPlace(candidate, start, col, Orientation.Vertical))
            {
                continue;
            }

            grid.PlaceWord(candidate, start, col, Orientation.Vertical);
            ReplacePlacement(placements, word, start, col, Orientation.Vertical, candidate, start, col);
            return true;
        }

        return false;
    }

    private static bool TryExtendBottom(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        List<string> wordList,
        int col,
        int start,
        string word,
        Random random)
    {
        var targetLength = word.Length + 1;
        var candidates = wordList
            .Where(candidate => candidate.Length == targetLength && candidate.EndsWith(word, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        Shuffle(candidates, random);
        foreach (var candidate in candidates)
        {
            var newStart = start - 1;
            if (!grid.CanPlace(candidate, newStart, col, Orientation.Vertical))
            {
                continue;
            }

            grid.PlaceWord(candidate, newStart, col, Orientation.Vertical);
            ReplacePlacement(placements, word, start, col, Orientation.Vertical, candidate, newStart, col);
            return true;
        }

        return false;
    }

    private static string BuildWord(CrosswordGrid grid, int line, int start, int end, Orientation orientation)
    {
        var length = end - start + 1;
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            var row = orientation == Orientation.Horizontal ? line : start + i;
            var col = orientation == Orientation.Horizontal ? start + i : line;
            chars[i] = grid.GetCell(row, col);
        }

        return new string(chars);
    }

    private static List<(int Line, int Start, int End, Orientation Orientation)> FindBetweenWordRuns(CrosswordGrid grid)
    {
        var targets = new List<(int Line, int Start, int End, Orientation Orientation)>();

        for (var row = 0; row < grid.Size; row++)
        {
            var col = 0;
            while (col < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < grid.Size && !grid.IsLetter(row, col))
                {
                    col++;
                }

                var length = col - start;
                var hasBefore = start > 0 && grid.IsLetter(row, start - 1);
                var hasAfter = col < grid.Size && grid.IsLetter(row, col);
                if (hasBefore && hasAfter && length >= 2)
                {
                    targets.Add((row, start, col - 1, Orientation.Horizontal));
                }
            }
        }

        for (var col = 0; col < grid.Size; col++)
        {
            var row = 0;
            while (row < grid.Size)
            {
                if (grid.IsLetter(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < grid.Size && !grid.IsLetter(row, col))
                {
                    row++;
                }

                var length = row - start;
                var hasBefore = start > 0 && grid.IsLetter(start - 1, col);
                var hasAfter = row < grid.Size && grid.IsLetter(row, col);
                if (hasBefore && hasAfter && length >= 2)
                {
                    targets.Add((col, start, row - 1, Orientation.Vertical));
                }
            }
        }

        return targets;
    }

    private static void ReplacePlacement(
        List<WordPlacement> placements,
        string oldWord,
        int oldRow,
        int oldCol,
        Orientation orientation,
        string newWord,
        int newRow,
        int newCol)
    {
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement.Orientation == orientation &&
                placement.Row == oldRow &&
                placement.Col == oldCol &&
                placement.Word.Equals(oldWord, StringComparison.OrdinalIgnoreCase))
            {
                placements.RemoveAt(i);
                break;
            }
        }

        placements.Add(new WordPlacement(newWord, newRow, newCol, orientation));
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

static class CrossingEnsurer
{
    public static void EnsureAllCrossings(CrosswordGrid grid, List<WordPlacement> placements, IEnumerable<string> words, int seed)
    {
        var random = new Random(seed);
        var wordList = words.Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2 && word.Length <= grid.Size)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var wordsByLetter = BuildWordsByLetter(wordList);
        var usedWords = new HashSet<string>(placements.Select(p => p.Word), StringComparer.OrdinalIgnoreCase);

        for (var pass = 0; pass < 5; pass++)
        {
            var isolated = placements.Where(p => !HasCrossing(grid, p)).ToList();
            if (isolated.Count == 0)
            {
                break;
            }

            Shuffle(isolated, random);
            var placedAny = false;

            foreach (var placement in isolated)
            {
                if (TryAddCrossingWord(grid, placements, usedWords, wordsByLetter, placement, random))
                {
                    placedAny = true;
                }
            }

            if (!placedAny)
            {
                break;
            }
        }
    }

    private static bool HasCrossing(CrosswordGrid grid, WordPlacement placement)
    {
        for (var i = 0; i < placement.Word.Length; i++)
        {
            var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
            var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);

            if (placement.Orientation == Orientation.Horizontal)
            {
                if ((row > 0 && grid.IsLetter(row - 1, col)) || (row < grid.Size - 1 && grid.IsLetter(row + 1, col)))
                {
                    return true;
                }
            }
            else
            {
                if ((col > 0 && grid.IsLetter(row, col - 1)) || (col < grid.Size - 1 && grid.IsLetter(row, col + 1)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryAddCrossingWord(
        CrosswordGrid grid,
        List<WordPlacement> placements,
        HashSet<string> usedWords,
        Dictionary<char, List<string>> wordsByLetter,
        WordPlacement placement,
        Random random)
    {
        var indexes = Enumerable.Range(0, placement.Word.Length).ToList();
        Shuffle(indexes, random);

        foreach (var index in indexes)
        {
            var row = placement.Row + (placement.Orientation == Orientation.Vertical ? index : 0);
            var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? index : 0);
            var letter = grid.GetCell(row, col);

            if (!wordsByLetter.TryGetValue(letter, out var candidates))
            {
                continue;
            }

            var ordered = candidates
                .OrderBy(word => word.Length)
                .ThenBy(_ => random.Next())
                .ToList();

            foreach (var word in ordered)
            {
                for (var i = 0; i < word.Length; i++)
                {
                    if (word[i] != letter)
                    {
                        continue;
                    }

                    var orientation = placement.Orientation == Orientation.Horizontal
                        ? Orientation.Vertical
                        : Orientation.Horizontal;

                    var startRow = orientation == Orientation.Horizontal ? row : row - i;
                    var startCol = orientation == Orientation.Horizontal ? col - i : col;

                    if (!grid.CanPlace(word, startRow, startCol, orientation))
                    {
                        continue;
                    }

                    grid.PlaceWord(word, startRow, startCol, orientation);
                    placements.Add(new WordPlacement(word, startRow, startCol, orientation));
                    return true;
                }
            }
        }

        return false;
    }

    private static Dictionary<char, List<string>> BuildWordsByLetter(IEnumerable<string> words)
    {
        var map = new Dictionary<char, List<string>>();
        foreach (var word in words)
        {
            foreach (var letter in word.Distinct())
            {
                if (!map.TryGetValue(letter, out var list))
                {
                    list = new List<string>();
                    map[letter] = list;
                }

                list.Add(word);
            }
        }

        return map;
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

record PipelineResult(
    CrosswordGrid Grid,
    List<WordPlacement> Placements,
    int FilledCells,
    int BlackCells,
    int BlackRunOverage,
    int MaxBlackRun,
    int BetweenWordOverage,
    int MaxBetweenWordRun,
    int SegmentCount,
    int MultiLineScore);

static class GridPipeline
{
    private const int RepairAttempts = 6;

    public static PipelineResult Build(
        int size,
        IEnumerable<string> words,
        IEnumerable<string> requiredWords,
        int iterations,
        int seed)
    {
        var baseResult = CrosswordGenerator.GenerateBest(size, words, requiredWords, iterations, seed);
        PipelineResult? best = null;

        for (var attempt = 0; attempt < RepairAttempts; attempt++)
        {
            var attemptSeed = seed + (attempt * 131);
            var grid = baseResult.Grid.Clone();
            var placements = new List<WordPlacement>(baseResult.Placements);

            GapFiller.Fill(grid, placements, words, attemptSeed + 17);
            BlackRunRepair.Repair(grid, placements, words, attemptSeed + 83);
            CrossingEnsurer.EnsureAllCrossings(grid, placements, words, attemptSeed + 131);
            BetweenWordExtender.Repair(grid, placements, words, attemptSeed + 167);
            GapFiller.Fill(grid, placements, words, attemptSeed + 197);
            BlackRunRepair.Repair(grid, placements, words, attemptSeed + 251);
            CrossingEnsurer.EnsureAllCrossings(grid, placements, words, attemptSeed + 293);
            BetweenWordExtender.Repair(grid, placements, words, attemptSeed + 331);

            var candidate = BuildResult(size, grid, placements);
            if (best is null ||
                candidate.BetweenWordOverage < best.BetweenWordOverage ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun < best.MaxBetweenWordRun) ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun == best.MaxBetweenWordRun && candidate.FilledCells > best.FilledCells) ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun == best.MaxBetweenWordRun && candidate.FilledCells == best.FilledCells && candidate.SegmentCount > best.SegmentCount) ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun == best.MaxBetweenWordRun && candidate.FilledCells == best.FilledCells && candidate.SegmentCount == best.SegmentCount && candidate.MultiLineScore > best.MultiLineScore) ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun == best.MaxBetweenWordRun && candidate.FilledCells == best.FilledCells && candidate.SegmentCount == best.SegmentCount && candidate.MultiLineScore == best.MultiLineScore && candidate.BlackRunOverage < best.BlackRunOverage) ||
                (candidate.BetweenWordOverage == best.BetweenWordOverage && candidate.MaxBetweenWordRun == best.MaxBetweenWordRun && candidate.FilledCells == best.FilledCells && candidate.SegmentCount == best.SegmentCount && candidate.MultiLineScore == best.MultiLineScore && candidate.BlackRunOverage == best.BlackRunOverage && candidate.MaxBlackRun < best.MaxBlackRun))
            {
                best = candidate;
            }

            if (best.BetweenWordOverage == 0 && best.MaxBetweenWordRun <= 1)
            {
                break;
            }
        }

        return best ?? throw new InvalidOperationException("Generation de grille impossible.");
    }

    private static PipelineResult BuildResult(int size, CrosswordGrid grid, List<WordPlacement> placements)
    {
        var filledCells = grid.CountFilledCells();
        var blackCells = (size * size) - filledCells;
        var (blackRunOverage, maxBlackRun) = CrosswordGenerator.CountBlackRunStats(grid);
        var (betweenWordOverage, maxBetweenWordRun) = CrosswordGenerator.CountBetweenWordRuns(grid);
        var segmentCount = CrosswordGenerator.CountSegments(grid);
        var multiLineScore = CrosswordGenerator.CountMultiLineScore(placements);

        return new PipelineResult(
            grid,
            placements,
            filledCells,
            blackCells,
            blackRunOverage,
            maxBlackRun,
            betweenWordOverage,
            maxBetweenWordRun,
            segmentCount,
            multiLineScore);
    }
}

record GridResult(
    CrosswordGrid Grid,
    List<WordPlacement> Placements,
    int Intersections,
    int FilledCells,
    int MultiLineScore,
    int SegmentCount,
    int BlackCells,
    int BlackRunOverage,
    int MaxBlackRun);

record PlacementInfo(WordPlacement Placement, int Intersections);

enum Orientation
{
    Horizontal,
    Vertical,
}

enum RunMode
{
    All,
    Grid,
    Pdf,
}

enum SolverMode
{
    Csp,
    Incremental,
    Pattern,
}

record WordPlacement(string Word, int Row, int Col, Orientation Orientation);

record WordEntry(int Number, string Word, string Clue, int Row, int Col, Orientation Orientation);

record AttemptResult(int PatternVariant, int Attempt, CrosswordGrid Grid, List<WordPlacement> Placements);
record SolveResult(int PatternVariant, int Attempt, CrosswordGrid Grid, List<WordPlacement> Placements, IReadOnlyCollection<string> MissingDefinitions);
record LoadedResult(CrosswordGrid Grid, List<WordPlacement> Placements, List<string> RequiredWords);

sealed class CrosswordGrid
{
    private readonly char[,] _cells;
    private readonly bool[,] _locked;

    public CrosswordGrid(int size)
    {
        Size = size;
        _cells = new char[size, size];
        _locked = new bool[size, size];

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                _cells[row, col] = '\0';
            }
        }
    }

    public int Size { get; }

    public bool CanPlace(string word, int row, int col, Orientation orientation)
    {
        var normalized = WordUtils.Normalize(word);
        if (!IsInBounds(row, col))
        {
            return false;
        }

        if (orientation == Orientation.Horizontal)
        {
            if (col < 0 || col + normalized.Length > Size)
            {
                return false;
            }

            if (col > 0 && IsLetter(row, col - 1))
            {
                return false;
            }

            if (col + normalized.Length < Size && IsLetter(row, col + normalized.Length))
            {
                return false;
            }

            for (var i = 0; i < normalized.Length; i++)
            {
                var r = row;
                var c = col + i;
                var cell = _cells[r, c];
                if (cell == '#' && IsLocked(r, c))
                {
                    return false;
                }

                if (cell == '#' && ((r > 0 && IsLetter(r - 1, c)) || (r < Size - 1 && IsLetter(r + 1, c))))
                {
                    return false;
                }

                if (cell != '#' && cell != '\0' && cell != normalized[i])
                {
                    return false;
                }
            }
        }
        else
        {
            if (row < 0 || row + normalized.Length > Size)
            {
                return false;
            }

            if (row > 0 && IsLetter(row - 1, col))
            {
                return false;
            }

            if (row + normalized.Length < Size && IsLetter(row + normalized.Length, col))
            {
                return false;
            }

            for (var i = 0; i < normalized.Length; i++)
            {
                var r = row + i;
                var c = col;
                var cell = _cells[r, c];
                if (cell == '#' && IsLocked(r, c))
                {
                    return false;
                }

                if (cell == '#' && ((c > 0 && IsLetter(r, c - 1)) || (c < Size - 1 && IsLetter(r, c + 1))))
                {
                    return false;
                }

                if (cell != '#' && cell != '\0' && cell != normalized[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void PlaceWord(string word, int row, int col, Orientation orientation)
    {
        var normalized = WordUtils.Normalize(word);

        if (orientation == Orientation.Horizontal)
        {
            for (var i = 0; i < normalized.Length; i++)
            {
                _cells[row, col + i] = normalized[i];
            }

            LockCell(row, col - 1);
            LockCell(row, col + normalized.Length);
        }
        else
        {
            for (var i = 0; i < normalized.Length; i++)
            {
                _cells[row + i, col] = normalized[i];
            }

            LockCell(row - 1, col);
            LockCell(row + normalized.Length, col);
        }
    }

    public bool IsLetter(int row, int col)
    {
        return char.IsLetter(_cells[row, col]);
    }

    public bool IsBlack(int row, int col)
    {
        return _cells[row, col] == '#';
    }

    public bool IsEmpty(int row, int col)
    {
        return _cells[row, col] == '\0';
    }

    public char GetCell(int row, int col)
    {
        return _cells[row, col];
    }

    public void SetLetter(int row, int col, char letter)
    {
        if (!IsInBounds(row, col))
        {
            return;
        }

        _cells[row, col] = letter;
    }

    public void SetBlack(int row, int col)
    {
        if (!IsInBounds(row, col))
        {
            return;
        }

        _cells[row, col] = '#';
        _locked[row, col] = true;
    }

    public void SetEmpty(int row, int col)
    {
        if (!IsInBounds(row, col))
        {
            return;
        }

        if (_cells[row, col] == '#')
        {
            _locked[row, col] = false;
        }

        _cells[row, col] = '\0';
    }

    public CrosswordGrid Clone()
    {
        var clone = new CrosswordGrid(Size);
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                clone._cells[row, col] = _cells[row, col];
                clone._locked[row, col] = _locked[row, col];
            }
        }

        return clone;
    }

    public int CountFilledCells()
    {
        var count = 0;
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                if (IsLetter(row, col))
                {
                    count++;
                }
            }
        }

        return count;
    }

    public int CountBlackCells()
    {
        var count = 0;
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                if (IsBlack(row, col))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < Size && col >= 0 && col < Size;
    }

    private bool IsLocked(int row, int col)
    {
        return _locked[row, col];
    }

    private void LockCell(int row, int col)
    {
        if (!IsInBounds(row, col))
        {
            return;
        }

        if (_cells[row, col] == '#')
        {
            _locked[row, col] = true;
        }
    }
}

static class CrosswordNumbering
{
    public static List<WordEntry> Generate(CrosswordGrid grid, Func<string, string> resolveClue)
    {
        var size = grid.Size;
        var entries = new List<WordEntry>();

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                if (!grid.IsLetter(row, col))
                {
                    continue;
                }

                if (StartsHorizontal(grid, row, col))
                {
                    var word = ReadWordHorizontal(grid, row, col);
                    var number = row + 1; // Horizontal clues are numbered by row (1..size).
                    entries.Add(new WordEntry(number, word, resolveClue(word), row, col, Orientation.Horizontal));
                }

                if (StartsVertical(grid, row, col))
                {
                    var word = ReadWordVertical(grid, row, col);
                    var number = col + 1; // Vertical clues are numbered by column (1..size).
                    entries.Add(new WordEntry(number, word, resolveClue(word), row, col, Orientation.Vertical));
                }
            }
        }

        return entries;
    }

    private static bool StartsHorizontal(CrosswordGrid grid, int row, int col)
    {
        if (!grid.IsLetter(row, col))
        {
            return false;
        }

        if (col > 0 && grid.IsLetter(row, col - 1))
        {
            return false;
        }

        return col + 1 < grid.Size && grid.IsLetter(row, col + 1);
    }

    private static bool StartsVertical(CrosswordGrid grid, int row, int col)
    {
        if (!grid.IsLetter(row, col))
        {
            return false;
        }

        if (row > 0 && grid.IsLetter(row - 1, col))
        {
            return false;
        }

        return row + 1 < grid.Size && grid.IsLetter(row + 1, col);
    }

    private static string ReadWordHorizontal(CrosswordGrid grid, int row, int col)
    {
        var chars = new List<char>();
        var cursor = col;
        while (cursor < grid.Size && grid.IsLetter(row, cursor))
        {
            chars.Add(grid.GetCell(row, cursor));
            cursor++;
        }

        return new string(chars.ToArray());
    }

    private static string ReadWordVertical(CrosswordGrid grid, int row, int col)
    {
        var chars = new List<char>();
        var cursor = row;
        while (cursor < grid.Size && grid.IsLetter(cursor, col))
        {
            chars.Add(grid.GetCell(cursor, col));
            cursor++;
        }

        return new string(chars.ToArray());
    }

}

static class ClueAggregator
{
    public static List<WordEntry> Aggregate(List<WordEntry> entries)
    {
        if (entries.Count == 0)
        {
            return entries;
        }

        return entries
            .GroupBy(e => e.Number)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                var ordered = first.Orientation == Orientation.Horizontal
                    ? g.OrderBy(e => e.Col).ThenBy(e => e.Row)
                    : g.OrderBy(e => e.Row).ThenBy(e => e.Col);
                var clue = string.Join(" . ", ordered.Select(e => e.Clue));
                return new WordEntry(first.Number, first.Word, clue, first.Row, first.Col, first.Orientation);
            })
            .ToList();
    }
}

sealed class DefinitionStore : IDisposable
{
    private readonly object _lock = new();
    private readonly SqliteConnection _connection;

    private DefinitionStore(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        _connection.Open();
        EnsureSchema();
    }

    public static DefinitionStore? TryOpen(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            return null;
        }

        try
        {
            return new DefinitionStore(dbPath);
        }
        catch
        {
            return null;
        }
    }

    public bool TryGet(string word, out string definition)
    {
        definition = string.Empty;
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        var key = WordUtils.Normalize(word);
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        lock (_lock)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT definition FROM definitions WHERE word = $word LIMIT 1;";
            command.Parameters.AddWithValue("$word", key);
            var result = command.ExecuteScalar();
            if (result is string text && !string.IsNullOrWhiteSpace(text))
            {
                definition = text;
                return true;
            }
        }

        return false;
    }

    public void Upsert(string word, string definition)
    {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(definition))
        {
            return;
        }

        var key = WordUtils.Normalize(word);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        lock (_lock)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO definitions(word, definition, updated_utc) VALUES ($word, $definition, $updated);";
            command.Parameters.AddWithValue("$word", key);
            command.Parameters.AddWithValue("$definition", definition);
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public HashSet<string> LoadDefinitionWords()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT word FROM definitions;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var raw = reader.GetString(0);
                var word = WordUtils.Normalize(raw);
                if (!string.IsNullOrWhiteSpace(word))
                {
                    result.Add(word);
                }
            }
        }

        return result;
    }

    public List<string> LoadWordsByLength(int length, int limit)
    {
        if (length <= 0 || limit <= 0)
        {
            return new List<string>();
        }

        var words = new List<string>(limit);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            using var command = _connection.CreateCommand();
            var scan = Math.Max(limit, Math.Min(200000, limit * 8));
            command.CommandText = "SELECT word, definition FROM definitions WHERE word_length = $length ORDER BY RANDOM() LIMIT $scan;";
            command.Parameters.AddWithValue("$length", length);
            command.Parameters.AddWithValue("$scan", scan);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var word = reader.GetString(0);
                var definition = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(definition))
                {
                    continue;
                }

                if (GeneratorSettings.DefsStrictFilter && !DefinitionFilter.IsAcceptableDefinition(definition))
                {
                    continue;
                }

                var normalized = WordUtils.Normalize(word);
                if (normalized.Length != length)
                {
                    continue;
                }

                if (!WordFilter.IsAcceptable(normalized))
                {
                    continue;
                }

                if (seen.Add(normalized))
                {
                    words.Add(normalized);
                }

                if (words.Count >= limit)
                {
                    break;
                }
            }
        }

        return words;
    }

    private void EnsureSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS definitions (
    word TEXT PRIMARY KEY,
    definition TEXT NOT NULL,
    updated_utc TEXT NOT NULL,
    word_length INTEGER
);
";
        command.ExecuteNonQuery();

        EnsureWordLengthColumn();
        using var indexCommand = _connection.CreateCommand();
        indexCommand.CommandText = "CREATE INDEX IF NOT EXISTS idx_definitions_word_length ON definitions(word_length);";
        indexCommand.ExecuteNonQuery();
    }

    private void EnsureWordLengthColumn()
    {
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA table_info(definitions);";
        using var reader = pragma.ExecuteReader();
        var hasColumn = false;
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, "word_length", StringComparison.OrdinalIgnoreCase))
            {
                hasColumn = true;
                break;
            }
        }

        if (!hasColumn)
        {
            using var alter = _connection.CreateCommand();
            alter.CommandText = "ALTER TABLE definitions ADD COLUMN word_length INTEGER;";
            alter.ExecuteNonQuery();
        }

        using var missingCheck = _connection.CreateCommand();
        missingCheck.CommandText = "SELECT 1 FROM definitions WHERE word_length IS NULL OR word_length = 0 LIMIT 1;";
        var hasMissing = missingCheck.ExecuteScalar() is not null;
        if (!hasMissing)
        {
            return;
        }

        PhaseLogger.Write("Defs: remplissage colonne word_length (SQLite).");
        using var update = _connection.CreateCommand();
        update.CommandText = "UPDATE definitions SET word_length = length(word) WHERE word_length IS NULL OR word_length = 0;";
        update.ExecuteNonQuery();
    }
}

sealed class ResultStore : IDisposable
{
    private readonly object _lock = new();
    private readonly SqliteConnection _connection;

    private ResultStore(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        _connection.Open();
        EnsureSchema();
    }

    public static ResultStore? TryOpen(string dbPath, bool createIfMissing = true)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            return null;
        }

        if (!createIfMissing && !File.Exists(dbPath))
        {
            return null;
        }

        try
        {
            return new ResultStore(dbPath);
        }
        catch
        {
            return null;
        }
    }

    public void SaveResult(CrosswordGrid grid, List<WordPlacement> placements, IEnumerable<string> requiredWords)
    {
        lock (_lock)
        {
            using var transaction = _connection.BeginTransaction();
            Execute("DELETE FROM grid_rows;");
            Execute("DELETE FROM placements;");
            Execute("DELETE FROM required_words;");
            Execute("DELETE FROM meta;");

            Execute("INSERT INTO meta(key, value) VALUES ('size', $value);",
                ("$value", grid.Size.ToString(CultureInfo.InvariantCulture)));

            for (var row = 0; row < grid.Size; row++)
            {
                var chars = new char[grid.Size];
                for (var col = 0; col < grid.Size; col++)
                {
                    var cell = grid.GetCell(row, col);
                    if (cell == '\0')
                    {
                        chars[col] = '.';
                    }
                    else
                    {
                        chars[col] = cell;
                    }
                }

                Execute("INSERT INTO grid_rows(row_index, row_text) VALUES ($row, $text);",
                    ("$row", row),
                    ("$text", new string(chars)));
            }

            foreach (var placement in placements)
            {
                Execute(
                    "INSERT INTO placements(word, row_index, col_index, orientation) VALUES ($word, $row, $col, $orientation);",
                    ("$word", placement.Word),
                    ("$row", placement.Row),
                    ("$col", placement.Col),
                    ("$orientation", placement.Orientation == Orientation.Horizontal ? "H" : "V"));
            }

            foreach (var word in requiredWords.Select(WordUtils.Normalize).Where(word => !string.IsNullOrWhiteSpace(word)))
            {
                Execute(
                    "INSERT INTO required_words(word) VALUES ($word);",
                    ("$word", word));
            }

            transaction.Commit();
        }
    }

    public LoadedResult? LoadResult()
    {
        lock (_lock)
        {
            var size = ReadGridSize();
            var rows = LoadGridRows();
            if (rows.Count == 0)
            {
                return null;
            }

            if (size <= 0)
            {
                size = rows.Max(row => row.Text.Length);
            }

            var grid = new CrosswordGrid(size);
            foreach (var row in rows)
            {
                var line = row.Text;
                var length = Math.Min(size, line.Length);
                for (var col = 0; col < length; col++)
                {
                    var ch = line[col];
                    if (ch == '#')
                    {
                        grid.SetBlack(row.RowIndex, col);
                    }
                    else if (ch == '.' || ch == '\0')
                    {
                        continue;
                    }
                    else
                    {
                        grid.SetLetter(row.RowIndex, col, ch);
                    }
                }
            }

            var placements = LoadPlacements();
            var required = LoadRequiredWords();
            return new LoadedResult(grid, placements, required);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void EnsureSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS meta (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS grid_rows (
    row_index INTEGER PRIMARY KEY,
    row_text TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS placements (
    word TEXT NOT NULL,
    row_index INTEGER NOT NULL,
    col_index INTEGER NOT NULL,
    orientation TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS required_words (
    word TEXT PRIMARY KEY
);
";
        command.ExecuteNonQuery();
    }

    private void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        foreach (var param in parameters)
        {
            command.Parameters.AddWithValue(param.Name, param.Value);
        }

        command.ExecuteNonQuery();
    }

    private int ReadGridSize()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT value FROM meta WHERE key = 'size' LIMIT 1;";
        var raw = command.ExecuteScalar() as string;
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private List<(int RowIndex, string Text)> LoadGridRows()
    {
        var rows = new List<(int RowIndex, string Text)>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT row_index, row_text FROM grid_rows ORDER BY row_index;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var rowIndex = reader.GetInt32(0);
            var text = reader.GetString(1);
            rows.Add((rowIndex, text ?? string.Empty));
        }

        return rows;
    }

    private List<WordPlacement> LoadPlacements()
    {
        var placements = new List<WordPlacement>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT word, row_index, col_index, orientation FROM placements;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var word = reader.GetString(0);
            var row = reader.GetInt32(1);
            var col = reader.GetInt32(2);
            var orientationRaw = reader.GetString(3);
            var orientation = string.Equals(orientationRaw, "V", StringComparison.OrdinalIgnoreCase)
                ? Orientation.Vertical
                : Orientation.Horizontal;
            placements.Add(new WordPlacement(word, row, col, orientation));
        }

        return placements;
    }

    private List<string> LoadRequiredWords()
    {
        var words = new List<string>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT word FROM required_words;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var word = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(word))
            {
                words.Add(word);
            }
        }

        return words;
    }
}

sealed class DefinitionProvider : IDisposable
{
    private readonly Dictionary<string, string> _fallback;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _fetchLimiter;
    private readonly string? _cachePath;
    private readonly DefinitionStore? _definitionStore;
    private readonly object _definitionWordsLock = new();
    private HashSet<string>? _definitionWords;
    private readonly ConcurrentDictionary<string, int> _missingCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, int> _httpStatusCounts = new();
    private long _lookupCount;
    private long _cacheHits;
    private long _fallbackHits;
    private long _sqliteHits;
    private long _networkRequests;
    private long _networkSuccess;
    private long _networkFailures;
    private long _networkTimeouts;
    private long _networkNoDefinition;
    private long _networkExceptions;
    private long _missingTotal;
    private volatile bool _allowNetwork = true;
    private static readonly object WiktionaryLock = new();
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public DefinitionProvider(Dictionary<string, string> fallback, string? cachePath)
    {
        _fallback = fallback;
        _fetchLimiter = new SemaphoreSlim(GeneratorSettings.DefinitionParallelism);
        _cachePath = cachePath;
        _definitionStore = DefinitionStore.TryOpen(GeneratorSettings.DefinitionDbPath);
        WiktionaryHelper.EnsureFrenchHeaders();
        LoadCache();
        SeedFallbackDefinitions();
    }

    public bool HasDefinitionStore => _definitionStore is not null;

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = GeneratorSettings.HttpMaxConnections,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = true,
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(GeneratorSettings.HttpTimeoutSeconds),
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("MotCroiseGenerator/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr");
        client.DefaultRequestVersion = GeneratorSettings.HttpRequestVersion;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        return client;
    }

    public string GetDefinition(string word)
    {
        if (TryGetDefinition(word, out var definition))
        {
            return definition;
        }

        return "Definition indisponible";
    }

    public bool TryGetDefinition(string word, out string definition)
    {
        return TryGetDefinitionCore(word, out definition, ignoreEmptyCache: false);
    }

    private bool TryGetDefinitionCore(string word, out string definition, bool ignoreEmptyCache)
    {
        var normalized = WordUtils.Normalize(word);
        Interlocked.Increment(ref _lookupCount);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            definition = string.Empty;
            return false;
        }

        if (_cache.TryGetValue(normalized, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                definition = cached;
                return true;
            }

            if (!ignoreEmptyCache && !_allowNetwork)
            {
                definition = string.Empty;
                RegisterMissing(normalized);
                return false;
            }
        }

        if (_fallback.TryGetValue(normalized, out var fallback) &&
            !string.IsNullOrWhiteSpace(fallback))
        {
            definition = fallback;
            _cache[normalized] = definition;
            Interlocked.Increment(ref _fallbackHits);
            _definitionStore?.Upsert(normalized, definition);
            return true;
        }

        if (_definitionStore is not null &&
            _definitionStore.TryGet(normalized, out var stored) &&
            !string.IsNullOrWhiteSpace(stored))
        {
            definition = stored;
            _cache[normalized] = definition;
            Interlocked.Increment(ref _sqliteHits);
            return true;
        }

        if (!_allowNetwork)
        {
            definition = string.Empty;
            _cache[normalized] = string.Empty;
            RegisterMissing(normalized);
            return false;
        }

        definition = FetchDefinition(normalized);

        if (string.IsNullOrWhiteSpace(definition))
        {
            _cache[normalized] = string.Empty;
            definition = string.Empty;
            RegisterMissing(normalized);
            return false;
        }

        _cache[normalized] = definition;
        _definitionStore?.Upsert(normalized, definition);
        return true;
    }

    private string FetchDefinition(string word)
    {
        _fetchLimiter.Wait();
        try
        {
            var definition = TryFetchFromMediaWiki(word);
            if (string.IsNullOrWhiteSpace(definition))
            {
                definition = TryFetchFromWiktionaryRest(word);
            }
            if (string.IsNullOrWhiteSpace(definition) && GeneratorSettings.UseWiktionaryNet)
            {
                definition = TryFetchFromWiktionary(word);
            }

            return definition;
        }
        finally
        {
            _fetchLimiter.Release();
        }
    }

    private string TryFetchFromMediaWiki(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        try
        {
            Interlocked.Increment(ref _networkRequests);
            var encoded = Uri.EscapeDataString(word);
            var url = $"https://fr.wiktionary.org/w/api.php?action=query&format=json&prop=extracts&exintro=1&explaintext=1&exsentences=1&titles={encoded}";
            using var response = HttpClient.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                RegisterStatus((int)response.StatusCode);
                Interlocked.Increment(ref _networkFailures);
                return string.Empty;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                Interlocked.Increment(ref _networkNoDefinition);
                Interlocked.Increment(ref _networkFailures);
                return string.Empty;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("query", out var queryNode) ||
                !queryNode.TryGetProperty("pages", out var pagesNode) ||
                pagesNode.ValueKind != JsonValueKind.Object)
            {
                Interlocked.Increment(ref _networkNoDefinition);
                Interlocked.Increment(ref _networkFailures);
                return string.Empty;
            }

            foreach (var page in pagesNode.EnumerateObject())
            {
                var pageNode = page.Value;
                if (pageNode.TryGetProperty("missing", out _))
                {
                    continue;
                }

                if (!pageNode.TryGetProperty("extract", out var extractNode))
                {
                    continue;
                }

                var extract = extractNode.GetString();
                if (string.IsNullOrWhiteSpace(extract))
                {
                    continue;
                }

                Interlocked.Increment(ref _networkSuccess);
                return WiktionaryHelper.CleanDefinition(extract);
            }

            Interlocked.Increment(ref _networkNoDefinition);
            Interlocked.Increment(ref _networkFailures);
            return string.Empty;
        }
        catch (TaskCanceledException)
        {
            Interlocked.Increment(ref _networkTimeouts);
            Interlocked.Increment(ref _networkFailures);
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _networkTimeouts);
            Interlocked.Increment(ref _networkFailures);
            return string.Empty;
        }
        catch
        {
            Interlocked.Increment(ref _networkExceptions);
            Interlocked.Increment(ref _networkFailures);
            return string.Empty;
        }
    }

    private string TryFetchFromWiktionary(string word)
    {
        Interlocked.Increment(ref _networkRequests);
        try
        {
            lock (WiktionaryLock)
            {
                var info = Wiktionary.Define(word.ToLowerInvariant(), "fr", null);
                var definition = info?.Definition?.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
                if (string.IsNullOrWhiteSpace(definition))
                {
                    Interlocked.Increment(ref _networkFailures);
                    return string.Empty;
                }

                Interlocked.Increment(ref _networkSuccess);
                return WiktionaryHelper.CleanDefinition(definition);
            }
        }
        catch
        {
            Interlocked.Increment(ref _networkExceptions);
            Interlocked.Increment(ref _networkFailures);
            return string.Empty;
        }
    }

    private string TryFetchFromWiktionaryRest(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var candidates = new[]
        {
            word.ToLowerInvariant(),
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant(),
            word.ToLowerInvariant().Replace("'", string.Empty),
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            try
            {
                Interlocked.Increment(ref _networkRequests);
                var encoded = Uri.EscapeDataString(candidate);
                var url = $"https://fr.wiktionary.org/api/rest_v1/page/definition/{encoded}";
                using var response = HttpClient.GetAsync(url).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    RegisterStatus((int)response.StatusCode);
                    Interlocked.Increment(ref _networkFailures);
                    continue;
                }

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(json))
                {
                    Interlocked.Increment(ref _networkNoDefinition);
                    Interlocked.Increment(ref _networkFailures);
                    continue;
                }

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("fr", out var frNode) ||
                    frNode.ValueKind != JsonValueKind.Array)
                {
                    Interlocked.Increment(ref _networkNoDefinition);
                    Interlocked.Increment(ref _networkFailures);
                    continue;
                }

                var foundDefinition = false;
                foreach (var part in frNode.EnumerateArray())
                {
                    if (!part.TryGetProperty("definitions", out var definitions) ||
                        definitions.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var def in definitions.EnumerateArray())
                    {
                        if (!def.TryGetProperty("definition", out var textNode))
                        {
                            continue;
                        }

                        var text = textNode.GetString();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        foundDefinition = true;
                        Interlocked.Increment(ref _networkSuccess);
                        return WiktionaryHelper.CleanDefinition(text);
                    }
                }

                if (!foundDefinition)
                {
                    Interlocked.Increment(ref _networkNoDefinition);
                    Interlocked.Increment(ref _networkFailures);
                }
            }
            catch (TaskCanceledException)
            {
                Interlocked.Increment(ref _networkTimeouts);
                Interlocked.Increment(ref _networkFailures);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _networkTimeouts);
                Interlocked.Increment(ref _networkFailures);
            }
            catch
            {
                Interlocked.Increment(ref _networkExceptions);
                Interlocked.Increment(ref _networkFailures);
                // Ignore and try next candidate.
            }
        }

        return string.Empty;
    }

    public void DisableNetwork()
    {
        _allowNetwork = false;
    }

    public void EnableNetwork()
    {
        _allowNetwork = true;
    }

    public void PrefetchDefinitions(IEnumerable<string> words)
    {
        var unique = words
            .Select(WordUtils.Normalize)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unique.Count == 0)
        {
            return;
        }

        var progress = new ProgressTracker(
            "Prefetch definitions",
            unique.Count,
            GeneratorSettings.ShowProgress,
            GeneratorSettings.ProgressInline);

        var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.DefinitionParallelism };
        Parallel.ForEach(unique, options, word =>
        {
            TryGetDefinitionCore(word, out _, ignoreEmptyCache: true);
            progress.Tick();
        });
    }

    public void ReportStats(string label, int topMissing = 50)
    {
        var lookups = Interlocked.Read(ref _lookupCount);
        var cacheHits = Interlocked.Read(ref _cacheHits);
        var fallbackHits = Interlocked.Read(ref _fallbackHits);
        var sqliteHits = Interlocked.Read(ref _sqliteHits);
        var networkRequests = Interlocked.Read(ref _networkRequests);
        var networkSuccess = Interlocked.Read(ref _networkSuccess);
        var networkFailures = Interlocked.Read(ref _networkFailures);
        var networkTimeouts = Interlocked.Read(ref _networkTimeouts);
        var networkNoDefinition = Interlocked.Read(ref _networkNoDefinition);
        var networkExceptions = Interlocked.Read(ref _networkExceptions);
        var missingTotal = Interlocked.Read(ref _missingTotal);
        var hitRate = lookups == 0 ? 0 : (cacheHits * 100.0 / lookups);

        PhaseLogger.Write($"Stats defs [{label}]: lookups={lookups}, cache={cacheHits} ({hitRate:0.0}%), sqlite={sqliteHits}, fallback={fallbackHits}, network={networkRequests}, ok={networkSuccess}, fail={networkFailures}, missing={missingTotal}");
        PhaseLogger.Write($"Stats HTTP [{label}]: timeouts={networkTimeouts}, no-def={networkNoDefinition}, exceptions={networkExceptions}, statuses={FormatStatusCounts(6)}");

        if (topMissing > 0)
        {
            LogMissingTop(topMissing);
        }
    }

    public void LogMissingTop(int top)
    {
        var topItems = _missingCounts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();

        if (topItems.Count == 0)
        {
            return;
        }

        var summary = string.Join(", ", topItems.Select(item => $"{item.Key}({item.Value})"));
        PhaseLogger.Write($"Top defs manquantes: {summary}");
    }

    private void RegisterMissing(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        _missingCounts.AddOrUpdate(normalized, 1, (_, count) => count + 1);
        Interlocked.Increment(ref _missingTotal);
    }

    private void RegisterStatus(int status)
    {
        _httpStatusCounts.AddOrUpdate(status, 1, (_, count) => count + 1);
    }

    public List<string> FilterWordsByStoredDefinitions(IEnumerable<string> words)
    {
        if (_definitionStore is null)
        {
            PhaseLogger.Write("Defs: SQLite indisponible, filtrage ignore.");
            return words.ToList();
        }

        var known = GetDefinitionWordSet();
        if (known is null || known.Count == 0)
        {
            PhaseLogger.Write("Defs: SQLite vide, filtrage renvoie une liste vide.");
            return new List<string>();
        }

        return words
            .Select(WordUtils.Normalize)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Where(word => known.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public HashSet<string>? GetDefinitionWordSet()
    {
        if (_definitionStore is null)
        {
            return null;
        }

        if (_definitionWords is not null)
        {
            return _definitionWords;
        }

        lock (_definitionWordsLock)
        {
            _definitionWords ??= _definitionStore.LoadDefinitionWords();
        }

        return _definitionWords;
    }

    public List<string> LoadDefinitionWordsByLength(int length, int limit)
    {
        if (_definitionStore is null || limit <= 0)
        {
            return new List<string>();
        }

        return _definitionStore.LoadWordsByLength(length, limit);
    }

    private void SeedFallbackDefinitions()
    {
        if (_definitionStore is null)
        {
            return;
        }

        foreach (var entry in _fallback)
        {
            var word = WordUtils.Normalize(entry.Key);
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var definition = entry.Value;
            if (string.IsNullOrWhiteSpace(definition))
            {
                continue;
            }

            _definitionStore.Upsert(word, definition);
            _cache[word] = definition;
        }
    }

    private string FormatStatusCounts(int maxItems)
    {
        var items = _httpStatusCounts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .Take(maxItems)
            .Select(entry => $"{entry.Key}={entry.Value}")
            .ToList();

        return items.Count == 0 ? "none" : string.Join(", ", items);
    }

    public void Dispose()
    {
        _definitionStore?.Dispose();
        _fetchLimiter.Dispose();
    }

    private void LoadCache()
    {
        if (!GeneratorSettings.UseCache || string.IsNullOrWhiteSpace(_cachePath))
        {
            return;
        }

        if (!File.Exists(_cachePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_cachePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is null)
            {
                return;
            }

            foreach (var entry in data)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                _cache[entry.Key] = entry.Value;
                _definitionStore?.Upsert(entry.Key, entry.Value);
            }
        }
        catch
        {
        }
    }

    public void SaveCache()
    {
        if (!GeneratorSettings.UseCache || string.IsNullOrWhiteSpace(_cachePath))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var snapshot = _cache
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _cachePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, _cachePath, true);
            File.Delete(tempPath);
        }
        catch
        {
        }
    }

}

static class WiktionaryHelper
{
    private static bool _initialized;

    private static readonly string[] FrenchHeaders =
    {
        "===Nom commun===",
        "===Nom propre===",
        "===Adjectif===",
        "===Verbe===",
        "===Adverbe===",
        "===Pronom===",
        "===Préposition===",
        "===Conjonction===",
        "===Interjection===",
        "===Déterminant===",
        "===Locution nominale===",
        "===Locution adjectivale===",
        "===Locution verbale===",
        "===Locution adverbiale===",
        "===Locution prépositive===",
    };

    public static void EnsureFrenchHeaders()
    {
        if (_initialized)
        {
            return;
        }

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        var root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\";
        var packagesRoot = Path.Combine(root, "packages");
        var packageFolders = new[]
        {
            "WiktionaryNET.0.1.1",
            "WiktionaryNET",
        };

        foreach (var folder in packageFolders)
        {
            var headersDir = Path.Combine(packagesRoot, folder, "content", "word_definition_headers");
            Directory.CreateDirectory(headersDir);
            var frPath = Path.Combine(headersDir, "fr.txt");

            if (!File.Exists(frPath))
            {
                File.WriteAllLines(frPath, FrenchHeaders, Encoding.UTF8);
            }
        }

        _initialized = true;
    }

    public static string CleanDefinition(string definition)
    {
        var cleaned = definition.Trim();
        cleaned = cleaned.TrimStart('*', '#', '-', ' ');
        cleaned = cleaned.Replace("[[", string.Empty).Replace("]]", string.Empty);
        cleaned = cleaned.Replace("'''", string.Empty).Replace("''", string.Empty);
        cleaned = cleaned.Replace("{{", string.Empty).Replace("}}", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\\s+", " ");
        return cleaned.Trim();
    }
}

sealed class CrosswordDocument : IDocument
{
    private readonly CrosswordGrid _grid;
    private readonly List<WordEntry> _horizontal;
    private readonly List<WordEntry> _vertical;
    private readonly HashSet<(int Row, int Col)> _revealCells;
    private readonly string _title;

    public CrosswordDocument(
        CrosswordGrid grid,
        List<WordEntry> horizontal,
        List<WordEntry> vertical,
        HashSet<(int Row, int Col)> revealCells,
        string title)
    {
        _grid = grid;
        _horizontal = horizontal;
        _vertical = vertical;
        _revealCells = revealCells;
        _title = title;
    }

    public DocumentMetadata GetMetadata()
    {
        return DocumentMetadata.Default;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Content().Column(column =>
            {
                column.Item().Text(_title).FontSize(18).SemiBold().AlignCenter();
                column.Item().PaddingTop(10).Element(BuildGrid);
                column.Item().PaddingTop(14).Element(BuildClues);
            });
        });
    }

    private void BuildGrid(IContainer container)
    {
        const float cellSize = 18f;
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(cellSize);
                for (var i = 0; i < _grid.Size; i++)
                {
                    columns.ConstantColumn(cellSize);
                }
            });

            table.Cell().Border(1).BorderColor(Colors.Grey.Medium)
                .Width(cellSize).Height(cellSize)
                .AlignCenter().AlignMiddle()
                .Text(string.Empty);

            for (var col = 0; col < _grid.Size; col++)
            {
                table.Cell().Border(1).BorderColor(Colors.Grey.Medium)
                    .Width(cellSize).Height(cellSize)
                    .AlignCenter().AlignMiddle()
                    .Text((col + 1).ToString()).FontSize(7);
            }

            for (var row = 0; row < _grid.Size; row++)
            {
                table.Cell().Border(1).BorderColor(Colors.Grey.Medium)
                    .Width(cellSize).Height(cellSize)
                    .AlignCenter().AlignMiddle()
                    .Text((row + 1).ToString()).FontSize(7);

                for (var col = 0; col < _grid.Size; col++)
                {
                    var cell = _grid.GetCell(row, col);
                    var isBlock = cell == '#';
                    var shouldReveal = _revealCells.Contains((row, col));

                    table.Cell().Border(1).BorderColor(Colors.Grey.Medium)
                        .Width(cellSize).Height(cellSize)
                        .Background(isBlock ? Colors.Grey.Darken3 : Colors.White)
                        .AlignCenter().AlignMiddle()
                        .Text(isBlock || !shouldReveal ? string.Empty : cell.ToString()).FontSize(9).SemiBold();
                }
            }
        });
    }

    private void BuildClues(IContainer container)
    {
        container.MultiColumn(multi =>
        {
            multi.Columns(2);
            multi.Spacing(20);
            multi.Content().Column(col =>
            {
                col.Item().Text("Horizontal").SemiBold();
                foreach (var entry in _horizontal)
                {
                    col.Item().Text(FormatClue(entry));
                }

                col.Item().PaddingTop(10).Text("Vertical").SemiBold();
                foreach (var entry in _vertical)
                {
                    col.Item().Text(FormatClue(entry));
                }
            });
        });
    }

    private static string FormatClue(WordEntry entry)
    {
        return $"{entry.Number}. . {entry.Clue}";
    }
}

static class GeneratorSettings
{
    public static int CpuCount => Math.Max(1, Environment.ProcessorCount);

    public static bool UseCache => ReadEnvBool("MOTCROISE_CACHE", true);

    public static bool IgnoreDefinitions => ReadEnvBool("MOTCROISE_IGNORE_DEFS", false);

    public static RunMode Mode
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("MOTCROISE_MODE");
            if (string.Equals(raw, "grid", StringComparison.OrdinalIgnoreCase))
            {
                return RunMode.Grid;
            }

            if (string.Equals(raw, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                return RunMode.Pdf;
            }

            return RunMode.All;
        }
    }

    public static SolverMode SolverMode
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("MOTCROISE_SOLVER");
            if (string.Equals(raw, "pattern", StringComparison.OrdinalIgnoreCase))
            {
                return SolverMode.Pattern;
            }

            if (string.Equals(raw, "incremental", StringComparison.OrdinalIgnoreCase))
            {
                return SolverMode.Incremental;
            }

            if (string.Equals(raw, "csp", StringComparison.OrdinalIgnoreCase))
            {
                return SolverMode.Csp;
            }

            // Default: incremental solver (fills rows+cols interleaved).
            return SolverMode.Incremental;
        }
    }

    public static string CacheRoot
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("MOTCROISE_CACHE_DIR");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                return custom;
            }

            return Path.Combine(AppContext.BaseDirectory, "data", "cache");
        }
    }

    public static string ResultDbPath
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("MOTCROISE_RESULT_DB");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                return custom;
            }

            return Path.Combine(CacheRoot, "motcroise.result.sqlite");
        }
    }

    public static string DefinitionDbPath
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("MOTCROISE_DEFS_DB");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                return custom;
            }

            return Path.Combine(CacheRoot, "definitions.fr.sqlite");
        }
    }

    public static string LastStartWordPath => Path.Combine(CacheRoot, "last-start-word.txt");

    public static int GridSize => ReadGridSizeEnv("MOTCROISE_GRID_SIZE", 20);

    public static int IterationsPerSeed => ReadEnvInt("MOTCROISE_ITERATIONS", 800, 200, 4000);

    public static int SeedCount => ReadEnvInt(
        "MOTCROISE_SEEDS",
        Math.Min(4, Math.Max(2, CpuCount / 2)),
        1,
        8);

    public static int Parallelism => ReadEnvInt("MOTCROISE_PARALLEL", CpuCount, 1, CpuCount);

    public static int ExpansionParallelism => ReadEnvInt(
        "MOTCROISE_EXPAND_PARALLEL",
        Parallelism,
        1,
        CpuCount);

    public static int PatternParallelism => ReadEnvInt(
        "MOTCROISE_PATTERN_PARALLEL",
        Parallelism,
        1,
        CpuCount);

    public static int PatternVariantParallelism => ReadEnvInt(
        "MOTCROISE_PATTERN_VARIANT_PARALLEL",
        1,
        1,
        CpuCount);

    public static int PatternShuffles => ReadEnvInt(
        "MOTCROISE_PATTERN_SHUFFLES",
        4,
        1,
        64);

    public static int PatternMaxAttempts => ReadEnvInt(
        "MOTCROISE_PATTERN_MAX_ATTEMPTS",
        400,
        10,
        200000);

    public static int PatternAttemptTimeoutMs => ReadEnvInt(
        "MOTCROISE_PATTERN_ATTEMPT_TIMEOUT_MS",
        4000,
        250,
        600000);

    public static int PatternMinExtraPct => ReadEnvInt(
        "MOTCROISE_PATTERN_MIN_EXTRA_PCT",
        50,
        0,
        100);

    public static int PatternRandomDensityPct => ReadEnvInt(
        "MOTCROISE_PATTERN_RANDOM_DENSITY_PCT",
        18,
        0,
        60);

    public static bool DefsStrictFilter => ReadEnvBool(
        "MOTCROISE_DEFS_STRICT_FILTER",
        false);

    public static int SolverParallelism => ReadEnvInt(
        "MOTCROISE_SOLVER_PARALLEL",
        Parallelism,
        1,
        CpuCount);

    public static bool ExpandThemeAndFillerParallel =>
        ReadEnvBool("MOTCROISE_EXPAND_PARALLEL_TF", true);

    public static int DefinitionParallelism => ReadEnvInt(
        "MOTCROISE_DEF_PARALLEL",
        Math.Min(8, CpuCount),
        1,
        256);

    public static int HttpMaxConnections => ReadEnvInt(
        "MOTCROISE_HTTP_MAX_CONN",
        Math.Max(64, CpuCount * 4),
        4,
        512);

    public static int HttpTimeoutSeconds => ReadEnvInt(
        "MOTCROISE_HTTP_TIMEOUT_SEC",
        8,
        2,
        30);

    public static Version HttpRequestVersion
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("MOTCROISE_HTTP_VERSION");
            if (string.Equals(raw, "1.1", StringComparison.OrdinalIgnoreCase))
            {
                return HttpVersion.Version11;
            }

            if (string.Equals(raw, "2.0", StringComparison.OrdinalIgnoreCase))
            {
                return HttpVersion.Version20;
            }

            return HttpVersion.Version20;
        }
    }

    public static int MaxPlacementCandidates => ReadEnvInt(
        "MOTCROISE_MAX_CANDIDATES",
        Math.Min(3000, Math.Max(1200, CpuCount * 200)),
        600,
        6000);

    public static int MinSlotLength => ReadEnvInt(
        "MOTCROISE_MIN_SLOT_LENGTH",
        2,
        2,
        6);

    public static int HunspellExtraLimit => ReadEnvInt(
        "MOTCROISE_HUNSPELL_EXTRA",
        600,
        0,
        5000);

    public static int HunspellExtraPerLength => ReadEnvInt(
        "MOTCROISE_HUNSPELL_PER_LENGTH",
        60,
        0,
        300);

    public static int HunspellThemeScanLimit => ReadEnvInt(
        "MOTCROISE_HUNSPELL_THEME_SCAN",
        0,
        0,
        200000);

    public static bool AllowFillerWords => ReadEnvBool("MOTCROISE_ALLOW_FILLER", false);

    public static int HunspellFillerLimit => ReadEnvInt(
        "MOTCROISE_HUNSPELL_FILLER",
        2000,
        0,
        10000);

    public static int HunspellFillerPerLength => ReadEnvInt(
        "MOTCROISE_HUNSPELL_FILLER_PER_LENGTH",
        120,
        0,
        500);

    public static int HunspellFillerScanLimit => ReadEnvInt(
        "MOTCROISE_HUNSPELL_FILLER_SCAN",
        0,
        0,
        200000);

    public static int ExtraBlackCandidates => ReadEnvInt(
        "MOTCROISE_EXTRA_BLACKS",
        40,
        0,
        800);

    public static int PatternVariants => ReadEnvInt(
        "MOTCROISE_PATTERN_VARIANTS",
        4,
        1,
        2000);

    public static int RequiredPlacementCandidates => ReadEnvInt(
        "MOTCROISE_REQUIRED_CANDIDATES",
        30,
        3,
        200);

    public static int RequiredPlacementCombos => ReadEnvInt(
        "MOTCROISE_REQUIRED_COMBOS",
        4000,
        100,
        20000);

    public static bool FilterDefinitionsInExpansion => ReadEnvBool("MOTCROISE_FILTER_DEFS", true);

    public static bool DefinitionCacheOnlyExpansion =>
        ReadEnvBool("MOTCROISE_DEF_CACHE_ONLY_EXPAND", false);

    public static bool PrefetchAllDefinitions =>
        ReadEnvBool("MOTCROISE_PREFETCH_ALL", false);

    public static bool PrefetchOnly =>
        ReadEnvBool("MOTCROISE_PREFETCH_ONLY", false);

    public static int DefinitionSolvePasses => ReadEnvInt(
        "MOTCROISE_DEF_PASSES",
        4,
        1,
        10);

    public static bool StopOnFirstSolution =>
        ReadEnvBool("MOTCROISE_STOP_ON_FIRST", true);

    public static bool UseWiktionaryNet => ReadEnvBool("MOTCROISE_USE_WIKTIONARY_NET", true);
    public static bool ShowProgress => ReadEnvBool("MOTCROISE_PROGRESS", true);
    public static bool ProgressInline => ReadEnvBool("MOTCROISE_PROGRESS_INLINE", false);
    public static bool ShowPhaseProgress => ReadEnvBool("MOTCROISE_PHASES", ShowProgress);
    public static bool ShowPatternLogs => ReadEnvBool("MOTCROISE_PATTERN_LOGS", false);
    public static bool ShowStep3Progress => ReadEnvBool("MOTCROISE_STEP3_PROGRESS", true);

    public static int PoolMaxPerLength => ReadEnvLimit("MOTCROISE_POOL_PER_LENGTH", 1500, 100, 20000);

    public static int PoolMaxTotal => ReadEnvLimit("MOTCROISE_POOL_MAX", 60000, 1000, 2000000);

    public static bool PoolUseDefinitions => ReadEnvBool("MOTCROISE_POOL_USE_DEFS", true);

    public static int PoolMinDefinitions => ReadEnvInt("MOTCROISE_POOL_MIN_DEFS", 500, 0, 2000000);

    public static int PoolMinTotal => ReadEnvInt("MOTCROISE_POOL_MIN_TOTAL", 0, 0, 2000000);

    public static int IncrementalAttempts => ReadEnvInt("MOTCROISE_INCREMENTAL_ATTEMPTS", 8, 1, 5000);

    public static int IncrementalCandidateLimit => ReadEnvInt("MOTCROISE_INCREMENTAL_CANDIDATES", 250, 50, 5000);

    public static bool IncrementalInterleavedRowCol =>
        ReadEnvBool("MOTCROISE_INCREMENTAL_INTERLEAVED", true);

    public static int CspAttempts => ReadEnvInt("MOTCROISE_CSP_ATTEMPTS", 200, 1, 20000);

    public static int CspCandidateLimit => ReadEnvInt("MOTCROISE_CSP_CANDIDATES", 800, 50, 50000);

    public static int CspParallelism => ReadEnvInt(
        "MOTCROISE_CSP_PARALLEL",
        Math.Min(Parallelism, 8),
        1,
        CpuCount);

    public static long CspMaxNodes =>
        ReadEnvLong("MOTCROISE_CSP_MAX_NODES", 5_000_000, 100_000, 500_000_000);

    public static int CspMaxSeconds =>
        ReadEnvInt("MOTCROISE_CSP_MAX_SECONDS", 30, 1, 3600);

    public static int IncrementalParallelism => ReadEnvInt(
        "MOTCROISE_INCREMENTAL_PARALLEL",
        CpuCount,
        1,
        CpuCount);

    public static int IncrementalProgressEvery => ReadEnvInt(
        "MOTCROISE_INCREMENTAL_PROGRESS_EVERY",
        50000,
        1000,
        5000000);

    public static int IncrementalMinFirstLength =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_MIN_FIRST", 14, 2, 24);

    public static long IncrementalMaxNodes =>
        ReadEnvLong("MOTCROISE_INCREMENTAL_MAX_NODES", 2_000_000, 100_000, 200_000_000);

    public static bool IncrementalValidateVerticals =>
        ReadEnvBool("MOTCROISE_INCREMENTAL_VALIDATE_VERTICALS", false);

    public static bool IncrementalFinalValidate =>
        ReadEnvBool("MOTCROISE_INCREMENTAL_FINAL_VALIDATE", true);

    public static int IncrementalCrossDepth =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_CROSS_DEPTH", 3, 0, 8);

    public static int IncrementalCrossCandidates =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_CROSS_CANDIDATES", 80, 10, 5000);

    public static int IncrementalMaxSeconds =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_MAX_SECONDS", 900, 30, 86400);

    public static bool IncrementalSlotCache =>
        ReadEnvBool("MOTCROISE_INCREMENTAL_SLOT_CACHE", true);

    public static int IncrementalSlotCacheMax =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_SLOT_CACHE_MAX", 20000, 1000, 500000);

    public static int IncrementalSlotCachePerKey =>
        ReadEnvInt("MOTCROISE_INCREMENTAL_SLOT_CACHE_PER_KEY", 600, 50, 5000);

    private static int ReadEnvInt(string name, int defaultValue, int min, int max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!int.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static int ReadGridSizeEnv(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!int.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        if (value == 5 || value == 10 || value == 15 || value == 20)
        {
            return value;
        }

        return defaultValue;
    }

    private static int ReadEnvLimit(string name, int defaultValue, int min, int max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!int.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        if (value <= 0)
        {
            return int.MaxValue;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static long ReadEnvLong(string name, long defaultValue, long min, long max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!long.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        if (value <= 0)
        {
            return long.MaxValue;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static bool ReadEnvBool(string name, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return raw.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}

static class PhaseLogger
{
    public static void Write(string message)
    {
        if (!GeneratorSettings.ShowPhaseProgress)
        {
            return;
        }

        Console.WriteLine(message);
    }
}

static class WordQuality
{
    private static readonly HashSet<char> Vowels = new(new[] { 'A', 'E', 'I', 'O', 'U', 'Y' });
    private static Dictionary<string, int> _scores = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(
        IEnumerable<string> words,
        IReadOnlyDictionary<string, string> clues,
        IEnumerable<string> requiredWords)
    {
        var required = new HashSet<string>(requiredWords.Select(WordUtils.Normalize), StringComparer.OrdinalIgnoreCase);
        var clueWords = new HashSet<string>(clues.Keys.Select(WordUtils.Normalize), StringComparer.OrdinalIgnoreCase);
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in words)
        {
            var word = WordUtils.Normalize(raw);
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var score = 0;
            if (required.Contains(word))
            {
                score += 100;
            }

            if (clueWords.Contains(word))
            {
                score += 10;
            }

            if (word.Length >= 8)
            {
                score += 2;
            }
            else if (word.Length >= 5)
            {
                score += 1;
            }

            if (word.Length <= 3)
            {
                score -= 3;
            }
            else if (word.Length == 4 && CountVowels(word) <= 1)
            {
                score -= 2;
            }

            if (HasTripleLetter(word))
            {
                score -= 2;
            }

            if (HasLongConsonantRun(word))
            {
                score -= 1;
            }

            if (ContainsRareLetters(word))
            {
                score -= 1;
            }

            scores[word] = score;
        }

        _scores = scores;
    }

    public static int GetScore(string word)
    {
        var normalized = WordUtils.Normalize(word);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return 0;
        }

        return _scores.TryGetValue(normalized, out var score) ? score : 0;
    }

    private static int CountVowels(string word)
    {
        var count = 0;
        foreach (var ch in word)
        {
            if (Vowels.Contains(ch))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasTripleLetter(string word)
    {
        for (var i = 2; i < word.Length; i++)
        {
            if (word[i] == word[i - 1] && word[i] == word[i - 2])
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLongConsonantRun(string word)
    {
        var run = 0;
        foreach (var ch in word)
        {
            if (!Vowels.Contains(ch))
            {
                run++;
                if (run >= 4)
                {
                    return true;
                }
            }
            else
            {
                run = 0;
            }
        }

        return false;
    }

    private static bool ContainsRareLetters(string word)
    {
        foreach (var ch in word)
        {
            if (ch is 'K' or 'W' or 'X' or 'Y' or 'Z')
            {
                return true;
            }
        }

        return false;
    }
}

record FixedWordPlacement(string Word, int Row, int Col, Orientation Orientation);

readonly record struct CharPosKey(int Pos, char Letter);

static class FixedPlacementBuilder
{
    public static List<FixedWordPlacement> Build(IEnumerable<string> requiredWords)
    {
        var required = requiredWords.Select(WordUtils.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var placements = new List<FixedWordPlacement>
        {
            new("JOYEUX", 1, 7, Orientation.Horizontal),
            new("ANNIVERSAIRE", 8, 4, Orientation.Horizontal),
            new("GRANDMERE", 14, 5, Orientation.Horizontal),
        };

        var missing = required
            .Where(word => placements.All(p => !p.Word.Equals(word, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Placements requis manquants: {string.Join(", ", missing)}");
        }

        return placements;
    }
}

static class ThemeWordBuilder
{
    public static List<string> Build(
        Dictionary<string, string> clues,
        IEnumerable<string> requiredWords,
        IReadOnlyCollection<string> hunspellWords,
        int gridSize)
    {
        var maxLength = Math.Min(24, gridSize);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasHunspell = hunspellWords.Count > 0;

        foreach (var word in clues.Keys)
        {
            var normalized = WordUtils.Normalize(word);
            if (normalized.Length < 2 || normalized.Length > maxLength)
            {
                continue;
            }

            if (hasHunspell && !hunspellWords.Contains(normalized))
            {
                continue;
            }

            result.Add(normalized);
        }

        foreach (var required in requiredWords)
        {
            var normalized = WordUtils.Normalize(required);
            if (normalized.Length >= 2 && normalized.Length <= maxLength)
            {
                result.Add(normalized);
            }
        }

        return result.OrderBy(word => word.Length).ThenBy(word => word, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

static class WordPoolBuilder
{
    public static List<string> BuildNoThemeWords(
        IReadOnlyCollection<string> hunspellWords,
        DefinitionProvider definitionProvider,
        int gridSize,
        out bool usedDefinitions)
    {
        var maxLength = Math.Min(24, gridSize);
        var maxPerLength = GeneratorSettings.PoolMaxPerLength;
        var maxTotal = GeneratorSettings.PoolMaxTotal;
        var lengthCount = Math.Max(1, maxLength - 1);
        var perLengthTarget = maxPerLength;
        if (maxTotal != int.MaxValue)
        {
            perLengthTarget = Math.Min(maxPerLength, Math.Max(1, maxTotal / lengthCount));
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        usedDefinitions = definitionProvider.HasDefinitionStore && GeneratorSettings.PoolUseDefinitions;

        if (usedDefinitions)
        {
            for (var length = 2; length <= maxLength; length++)
            {
                var words = definitionProvider.LoadDefinitionWordsByLength(length, perLengthTarget);
                foreach (var word in words)
                {
                    var normalized = WordUtils.Normalize(word);
                    if (normalized.Length != length)
                    {
                        continue;
                    }

                    if (WordFilter.IsAcceptable(normalized))
                    {
                        result.Add(normalized);
                    }
                }
            }
        }

        if (usedDefinitions && result.Count < GeneratorSettings.PoolMinDefinitions)
        {
            usedDefinitions = false;
        }

        if (result.Count == 0 || !usedDefinitions)
        {
            usedDefinitions = false;
            var rng = new Random();
            for (var length = 2; length <= maxLength; length++)
            {
                var words = SampleHunspellByLength(hunspellWords, length, perLengthTarget, rng);
                foreach (var word in words)
                {
                    var normalized = WordUtils.Normalize(word);
                    if (WordFilter.IsAcceptable(normalized))
                    {
                        result.Add(normalized);
                    }
                }
            }
        }

        var minTotal = GeneratorSettings.PoolMinTotal;
        if (minTotal > 0 && result.Count < minTotal)
        {
            usedDefinitions = false;
            var rng = new Random();
            var candidates = hunspellWords
                .Select(WordUtils.Normalize)
                .Where(word => word.Length >= 2 && word.Length <= maxLength)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Shuffle(candidates, rng);
            foreach (var word in candidates)
            {
                if (result.Count >= minTotal)
                {
                    break;
                }

                if (WordFilter.IsAcceptable(word))
                {
                    result.Add(word);
                }
            }
        }

        return result
            .OrderBy(word => word.Length)
            .ThenBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> SampleHunspellByLength(
        IReadOnlyCollection<string> hunspellWords,
        int length,
        int limit,
        Random rng)
    {
        var bucket = new List<string>();
        foreach (var word in hunspellWords)
        {
            if (word.Length == length)
            {
                bucket.Add(word);
            }
        }

        if (bucket.Count <= limit)
        {
            return bucket;
        }

        for (var i = bucket.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (bucket[i], bucket[j]) = (bucket[j], bucket[i]);
        }

        return bucket.Take(limit).ToList();
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

static class PatternValidator
{
    public static bool HasNoOrphanCells(CrosswordGrid grid)
    {
        var size = grid.Size;
        var minSlotLength = GeneratorSettings.MinSlotLength;
        var h = new int[size, size];
        var v = new int[size, size];

        // Horizontal segments
        for (var r = 0; r < size; r++)
        {
            var c = 0;
            while (c < size)
            {
                while (c < size && grid.IsBlack(r, c))
                {
                    c++;
                }

                var start = c;
                while (c < size && !grid.IsBlack(r, c))
                {
                    c++;
                }

                var len = c - start;
                for (var i = 0; i < len; i++)
                {
                    h[r, start + i] = len;
                }
            }
        }

        // Vertical segments
        for (var c = 0; c < size; c++)
        {
            var r = 0;
            while (r < size)
            {
                while (r < size && grid.IsBlack(r, c))
                {
                    r++;
                }

                var start = r;
                while (r < size && !grid.IsBlack(r, c))
                {
                    r++;
                }

                var len = r - start;
                for (var i = 0; i < len; i++)
                {
                    v[start + i, c] = len;
                }
            }
        }

        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (grid.IsBlack(r, c))
                {
                    continue;
                }

                if (h[r, c] < minSlotLength && v[r, c] < minSlotLength)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

static class WordFilter
{
    private static readonly HashSet<char> Vowels = new(new[] { 'A', 'E', 'I', 'O', 'U', 'Y' });
    private static readonly HashSet<string> AllowedTwoLetterWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AI","AS","AU","AX","CI","DE","DU","EN","ES","ET","EU","IL","JE","LA","LE","LI","LU",
        "MA","ME","MI","MO","NE","NI","NO","NU","ON","OR","OU","SA","SE","SI","SO","SU",
        "TA","TE","TI","TO","TU","UN","VA","VU"
    };

    public static bool IsAcceptable(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        var normalized = WordUtils.Normalize(word);
        if (normalized.Length < 2)
        {
            return false;
        }

        // When we build the pool from a definitions database, 2-letter entries are already "real" words.
        // When we build from Hunspell-only, strongly restrict 2-letter words to a small whitelist
        // (avoids abbreviations like BZ/GW/CF).
        if (normalized.Length == 2 && !GeneratorSettings.PoolUseDefinitions && !AllowedTwoLetterWords.Contains(normalized))
        {
            return false;
        }

        // Reject words with non A-Z after normalization.
        for (var i = 0; i < normalized.Length; i++)
        {
            var ch = normalized[i];
            if (ch is < 'A' or > 'Z')
            {
                return false;
            }
        }

        // Reject trivial repeats (e.g. AAAAA / AA).
        var distinct = 0;
        Span<bool> seen = stackalloc bool[26];
        foreach (var ch in normalized)
        {
            var idx = ch - 'A';
            if (!seen[idx])
            {
                seen[idx] = true;
                distinct++;
                if (distinct >= 2)
                {
                    break;
                }
            }
        }

        if (distinct < 2)
        {
            return false;
        }

        // Reject triple-letter runs.
        for (var i = 2; i < normalized.Length; i++)
        {
            if (normalized[i] == normalized[i - 1] && normalized[i] == normalized[i - 2])
            {
                return false;
            }
        }

        // Require at least one vowel for length >= 4.
        if (normalized.Length >= 4)
        {
            var vowels = 0;
            foreach (var ch in normalized)
            {
                if (Vowels.Contains(ch))
                {
                    vowels++;
                    break;
                }
            }

            if (vowels == 0)
            {
                return false;
            }
        }

        return true;
    }
}

static class DefinitionFilter
{
    // Heuristic: keep clueable common words, avoid proper nouns / flexions / acronyms.
    private static readonly string[] RejectStarts =
    {
        "COMMUNE", "SECTION DE LA COMMUNE", "VILLE", "FLEUVE", "LAC", "MONT", "REGION", "RÉGION",
        "PRENOM", "PRÉNOM", "NOM PROPRE", "NOM DE FAMILLE",
        "SIGLE", "ACRONYME", "ABREVIATION", "ABRÉVIATION",
        "PARTICIPE", "CONJUGAISON", "PLURIEL DE", "FEMININ DE", "FÉMININ DE",
        "MASCULIN DE", "VARIANTE", "FORME", "SYNONYME DE"
    };

    private static readonly string[] RejectContains =
    {
        "PREMIERE PERSONNE", "PREMIÈRE PERSONNE",
        "DEUXIEME PERSONNE", "DEUXIÈME PERSONNE",
        "TROISIEME PERSONNE", "TROISIÈME PERSONNE",
        "PASSE SIMPLE", "PASSÉ SIMPLE",
        "IMPARFAIT", "SUBJONCTIF", "INDICATIF", "CONDITIONNEL", "IMPERATIF", "IMPÉRATIF",
        "FEMININ SINGULIER DE", "FÉMININ SINGULIER DE",
        "MASCULIN SINGULIER DE",
        "FEMININ PLURIEL DE", "FÉMININ PLURIEL DE",
        "MASCULIN PLURIEL DE",
        "PLURIEL DE"
    };

    public static bool IsAcceptableDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
        {
            return false;
        }

        var trimmed = definition.Trim();
        var upper = trimmed.ToUpperInvariant();
        foreach (var prefix in RejectStarts)
        {
            if (upper.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }
        }

        foreach (var needle in RejectContains)
        {
            if (upper.Contains(needle, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

static class ThemeWordExpander
{
    private static readonly string[] ThemeStems =
    {
        "ANNIVER", "ANNIV", "FETE", "FETER", "GATEA", "BOUG", "CADEA", "JOI", "JOYE",
        "FAMIL", "GRAND", "MERE", "PERE", "SURPR", "INVIT", "SOUHAI", "VOEU", "GOURM",
        "DANSE", "CHANT", "MUSIQ", "FEST", "CELEBR", "DECOR", "CONFET", "BALLON",
        "PHOTO", "SOUVEN", "APERO", "BUFFET", "REUNI", "FELICIT", "PRESENT", "CARTE",
        "BOISSON", "DESSERT", "PATISS", "RITUEL", "FANFA", "ORCHE", "ACCUEIL",
        "AMBIAN", "ANIM", "SPECTA"
    };

    public static List<string> Expand(
        List<string> baseWords,
        IReadOnlyCollection<string> hunspellWords,
        DefinitionProvider definitionProvider,
        int gridSize,
        bool allowFiller,
        bool filterDefinitions)
    {
        if (hunspellWords.Count == 0)
        {
            return baseWords;
        }

        var maxExtra = GeneratorSettings.HunspellExtraLimit;
        var maxPerLength = GeneratorSettings.HunspellExtraPerLength;
        var maxFiller = GeneratorSettings.HunspellFillerLimit;
        var maxFillerPerLength = GeneratorSettings.HunspellFillerPerLength;
        if ((maxExtra <= 0 || maxPerLength <= 0) &&
            !(allowFiller && maxFiller > 0 && maxFillerPerLength > 0))
        {
            return baseWords;
        }

        var maxLength = Math.Min(24, gridSize);
        var words = new HashSet<string>(baseWords, StringComparer.OrdinalIgnoreCase);
        var lengthCounts = baseWords
            .GroupBy(word => word.Length)
            .ToDictionary(group => group.Key, group => group.Count());

        HashSet<string>? definitionWords = null;
        if (filterDefinitions)
        {
            definitionWords = definitionProvider.GetDefinitionWordSet();
            if (definitionWords is null || definitionWords.Count == 0)
            {
                definitionWords = null;
                PhaseLogger.Write("Defs: filtre SQLite indisponible, fallback provider.");
            }
            else
            {
                PhaseLogger.Write($"Defs: filtre SQLite actif ({definitionWords.Count} mots).");
            }
        }

        var orderedTheme = hunspellWords
            .Where(word => word.Length >= 2 && word.Length <= maxLength)
            .Where(word => ContainsThemeStem(word))
            .OrderBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToList();
        orderedTheme = ApplyScanLimit(orderedTheme, GeneratorSettings.HunspellThemeScanLimit);

        if (allowFiller && maxFiller > 0 && maxFillerPerLength > 0)
        {
            var fillerCandidates = hunspellWords
                .Where(word => word.Length >= 2 && word.Length <= maxLength)
                .Where(word => !ContainsThemeStem(word))
                .OrderBy(word => word, StringComparer.OrdinalIgnoreCase)
                .ToList();
            fillerCandidates = ApplyScanLimit(fillerCandidates, GeneratorSettings.HunspellFillerScanLimit);

            var fillerProgress = new ProgressTracker(
                "Expansion Hunspell (filler)",
                fillerCandidates.Count,
                GeneratorSettings.ShowProgress,
                GeneratorSettings.ProgressInline);

            var progress = new ProgressTracker(
                "Expansion Hunspell (theme)",
                orderedTheme.Count,
                GeneratorSettings.ShowProgress,
                GeneratorSettings.ProgressInline);

            if (GeneratorSettings.ExpandThemeAndFillerParallel)
            {
                var sharedLock = new object();
                var themeTask = Task.Run(() =>
                    AddCandidates(
                        orderedTheme,
                        words,
                        lengthCounts,
                        maxExtra,
                        maxPerLength,
                        definitionProvider,
                        progress,
                        filterDefinitions,
                        definitionWords,
                        sharedLock));

                var fillerTask = Task.Run(() =>
                    AddCandidates(
                        fillerCandidates,
                        words,
                        lengthCounts,
                        maxFiller,
                        maxFillerPerLength,
                        definitionProvider,
                        fillerProgress,
                        filterDefinitions,
                        definitionWords,
                        sharedLock));

                Task.WaitAll(themeTask, fillerTask);
            }
            else
            {
                AddCandidates(
                    orderedTheme,
                    words,
                    lengthCounts,
                    maxExtra,
                    maxPerLength,
                    definitionProvider,
                    progress,
                    filterDefinitions,
                    definitionWords);

                AddCandidates(
                    fillerCandidates,
                    words,
                    lengthCounts,
                    maxFiller,
                    maxFillerPerLength,
                    definitionProvider,
                    fillerProgress,
                    filterDefinitions,
                    definitionWords);
            }
        }
        else
        {
            var progress = new ProgressTracker(
                "Expansion Hunspell (theme)",
                orderedTheme.Count,
                GeneratorSettings.ShowProgress,
                GeneratorSettings.ProgressInline);

            AddCandidates(
                orderedTheme,
                words,
                lengthCounts,
                maxExtra,
                maxPerLength,
                definitionProvider,
                progress,
                filterDefinitions,
                definitionWords);
        }

        return words
            .OrderBy(word => word.Length)
            .ThenBy(word => word, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddCandidates(
        List<string> candidates,
        HashSet<string> words,
        Dictionary<int, int> lengthCounts,
        int maxToAdd,
        int maxPerLength,
        DefinitionProvider definitionProvider,
        ProgressTracker progress,
        bool filterDefinitions,
        HashSet<string>? definitionWords,
        object? sharedLock = null)
    {
        if (maxToAdd <= 0 || maxPerLength <= 0)
        {
            return;
        }

        var gate = sharedLock ?? new object();
        var seen = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words)
        {
            seen.TryAdd(word, 0);
        }

        var added = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.ExpansionParallelism };

        Parallel.ForEach(candidates, options, (candidate, state) =>
        {
            progress.Tick();
            if (Volatile.Read(ref added) >= maxToAdd)
            {
                state.Stop();
                return;
            }

            if (seen.ContainsKey(candidate))
            {
                return;
            }

            if (filterDefinitions)
            {
                if (definitionWords is not null)
                {
                    if (!definitionWords.Contains(candidate))
                    {
                        return;
                    }
                }
                else if (!definitionProvider.TryGetDefinition(candidate, out _))
                {
                    return;
                }
            }

            lock (gate)
            {
                if (added >= maxToAdd)
                {
                    state.Stop();
                    return;
                }

                if (seen.ContainsKey(candidate))
                {
                    return;
                }

                if (!lengthCounts.TryGetValue(candidate.Length, out var count))
                {
                    count = 0;
                }

                if (count >= maxPerLength)
                {
                    return;
                }

                words.Add(candidate);
                lengthCounts[candidate.Length] = count + 1;
                seen[candidate] = 0;
                added++;
                if (added >= maxToAdd)
                {
                    state.Stop();
                }
            }
        });
    }

    private static List<string> ApplyScanLimit(List<string> candidates, int maxScan)
    {
        if (maxScan <= 0 || candidates.Count <= maxScan)
        {
            return candidates;
        }

        return candidates.Take(maxScan).ToList();
    }

    private static bool ContainsThemeStem(string word)
    {
        foreach (var stem in ThemeStems)
        {
            if (word.Contains(stem, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

static class SolveRunner
{
    public static SolveResult SolveCsp(
        int gridSize,
        List<string> words,
        IReadOnlyList<string> requiredWords,
        int patternVariants,
        int attempts,
        int candidateLimit)
    {
        var total = Math.Max(1, attempts);
        var tracker = new ProgressTracker(
            "CSP attempts",
            total,
            GeneratorSettings.ShowPhaseProgress,
            GeneratorSettings.ProgressInline);

        var foundLock = new object();
        SolveResult? found = null;
        var cts = new CancellationTokenSource();

        var jobs = Enumerable.Range(1, total)
            .Select(i => new { Attempt = i, Variant = patternVariants <= 0 ? 0 : (i - 1) % Math.Max(1, patternVariants) })
            .ToList();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(GeneratorSettings.CspParallelism, total),
            CancellationToken = cts.Token,
        };

        try
        {
            Parallel.ForEach(jobs, options, (job, state) =>
            {
                if (cts.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                tracker.Tick();

                var seed = unchecked(Environment.TickCount + (job.Attempt * 7919));
                var fixedWords = new List<FixedWordPlacement>();
                var basePattern = PatternBuilder.Build(
                    gridSize,
                    fixedWords,
                    Array.Empty<(int Row, int Col)>(),
                    job.Variant);

                // Add extra blacks to make the CSP solvable (reduce word-square constraints).
                var maxExtra = Math.Max(0, GeneratorSettings.ExtraBlackCandidates);
                var extraCandidates = PatternBuilder.BuildExtraBlackCandidates(basePattern, fixedWords, maxExtra);
                var rng = new Random(seed);
                var extraMax = Math.Min(maxExtra, extraCandidates.Count);
                var extraCount = 0;
                if (extraMax > 0)
                {
                    // Small grids need blacks to avoid forcing a "word square" (very hard).
                    if (gridSize <= 7)
                    {
                        var cap = Math.Min(2, extraMax);
                        extraCount = rng.Next(0, cap + 1);
                    }
                    else
                    {
                        extraCount = rng.Next(0, extraMax + 1);
                    }
                }
                var extraBlacks = new List<(int Row, int Col)>(extraCount);
                if (extraCount > 0)
                {
                    for (var i = extraCandidates.Count - 1; i > 0; i--)
                    {
                        var j = rng.Next(i + 1);
                        (extraCandidates[i], extraCandidates[j]) = (extraCandidates[j], extraCandidates[i]);
                    }

                    extraBlacks.AddRange(extraCandidates.Take(extraCount));
                }

                var pattern = PatternBuilder.Build(
                    gridSize,
                    fixedWords,
                    extraBlacks,
                    job.Variant);

                if (!IsGoodPattern(pattern, gridSize))
                {
                    return;
                }

                var solver = new CspFillSolver(
                    pattern,
                    words,
                    seed,
                    candidateLimit,
                    GeneratorSettings.CspMaxNodes,
                    GeneratorSettings.CspMaxSeconds);
                if (solver.TryFill(out var grid, out var placements))
                {
                    var result = new SolveResult(job.Variant, job.Attempt, grid, placements, Array.Empty<string>());
                    lock (foundLock)
                    {
                        if (found is null)
                        {
                            found = result;
                        }
                    }

                    cts.Cancel();
                    state.Stop();
                }
            });
        }
        catch (OperationCanceledException)
        {
        }

        if (found is null)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return found;
    }

    private static bool IsGoodPattern(CrosswordGrid grid, int gridSize)
    {
        // For small grids, avoid patterns that force many 2-letter "words".
        var restrictLen2 = gridSize <= 7;
        if (!restrictLen2)
        {
            return true;
        }

        var len2Count = 0;

        // Horizontal runs
        for (var r = 0; r < gridSize; r++)
        {
            var c = 0;
            while (c < gridSize)
            {
                while (c < gridSize && grid.IsBlack(r, c))
                {
                    c++;
                }

                var start = c;
                while (c < gridSize && !grid.IsBlack(r, c))
                {
                    c++;
                }

                var len = c - start;
                if (len == 2)
                {
                    len2Count++;
                }
            }
        }

        // Vertical runs
        for (var c = 0; c < gridSize; c++)
        {
            var r = 0;
            while (r < gridSize)
            {
                while (r < gridSize && grid.IsBlack(r, c))
                {
                    r++;
                }

                var start = r;
                while (r < gridSize && !grid.IsBlack(r, c))
                {
                    r++;
                }

                var len = r - start;
                if (len == 2)
                {
                    len2Count++;
                }
            }
        }

        // Allow some 2-letter slots (whitelisted), but avoid grids dominated by them.
        return len2Count <= 12;
    }

    public static SolveResult SolveIncremental(
        int gridSize,
        List<string> words,
        int maxAttempts,
        int candidateLimit)
    {
        var minFirstLength = Math.Min(GeneratorSettings.IncrementalMinFirstLength, gridSize);
        var attempts = Math.Max(1, maxAttempts);
        var attemptProgress = new ProgressTracker(
            "Incremental attempts",
            attempts,
            GeneratorSettings.ShowPhaseProgress,
            GeneratorSettings.ProgressInline);

        var bannedStarts = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var lastStartWord = TryReadLastStartWord();
        if (!string.IsNullOrWhiteSpace(lastStartWord))
        {
            bannedStarts.TryAdd(lastStartWord, 0);
            if (GeneratorSettings.ShowPhaseProgress)
            {
                Console.WriteLine($"Evite mot de depart precedent: {lastStartWord}");
            }
        }

        var foundLock = new object();
        SolveResult? found = null;
        var cts = new CancellationTokenSource();
        var parallelism = Math.Min(GeneratorSettings.IncrementalParallelism, attempts);
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cts.Token };

        try
        {
            Parallel.ForEach(Enumerable.Range(1, attempts), options, (attempt, state) =>
            {
                if (cts.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                attemptProgress.Tick();
                var seed = unchecked(Environment.TickCount + (attempt * 7919));
                IncrementalProgressReporter? reporter = null;
                if (parallelism == 1 && GeneratorSettings.ShowPhaseProgress)
                {
                    reporter = new IncrementalProgressReporter(
                        $"Incremental nodes (attempt {attempt})",
                        GeneratorSettings.IncrementalProgressEvery,
                        GeneratorSettings.ProgressInline);
                }

                var solver = new IncrementalFillSolver(
                    gridSize,
                    words,
                    candidateLimit,
                    seed,
                    reporter,
                    minFirstLength,
                    GeneratorSettings.IncrementalMaxNodes,
                    GeneratorSettings.IncrementalValidateVerticals,
                    GeneratorSettings.IncrementalFinalValidate,
                    GeneratorSettings.IncrementalCrossDepth,
                    GeneratorSettings.IncrementalCrossCandidates,
                    bannedStarts,
                    GeneratorSettings.IncrementalMaxSeconds,
                    GeneratorSettings.IncrementalSlotCache,
                    GeneratorSettings.IncrementalSlotCacheMax,
                    GeneratorSettings.IncrementalSlotCachePerKey);
                if (solver.TryFill(out var grid, out var placements))
                {
                    var result = new SolveResult(0, 0, grid, placements, Array.Empty<string>());
                    lock (foundLock)
                    {
                        if (found is null)
                        {
                            found = result;
                            if (!string.IsNullOrWhiteSpace(solver.FirstWordUsed))
                            {
                                TryWriteLastStartWord(solver.FirstWordUsed!);
                            }
                        }
                    }

                    cts.Cancel();
                    state.Stop();
                }
            });
        }
        catch (OperationCanceledException)
        {
        }

        if (found is null)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return found;
    }

    private static string? TryReadLastStartWord()
    {
        try
        {
            var path = GeneratorSettings.LastStartWordPath;
            if (!File.Exists(path))
            {
                return null;
            }

            var word = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(word) ? null : WordUtils.Normalize(word);
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteLastStartWord(string word)
    {
        try
        {
            var path = GeneratorSettings.LastStartWordPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, WordUtils.Normalize(word));
        }
        catch
        {
            // Best-effort only.
        }
    }

    public static SolveResult SolveWithoutDefinitions(
        int gridSize,
        List<string> words,
        IReadOnlyList<string> requiredWords,
        int extraBlackCandidates,
        int patternVariants)
    {
        var attempt = SolveAcrossPatterns(
            gridSize,
            words,
            requiredWords,
            extraBlackCandidates,
            patternVariants,
            GeneratorSettings.StopOnFirstSolution);

        return new SolveResult(
            attempt.PatternVariant,
            attempt.Attempt,
            attempt.Grid,
            attempt.Placements,
            Array.Empty<string>());
    }

    public static SolveResult SolveWithDefinitionRetries(
        int gridSize,
        List<string> words,
        IReadOnlyList<string> requiredWords,
        DefinitionProvider definitionProvider,
        int maxPasses,
        int extraBlackCandidates,
        int patternVariants)
    {
        var currentWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        AttemptResult? lastResult = null;

        for (var pass = 1; pass <= Math.Max(1, maxPasses); pass++)
        {
            if (GeneratorSettings.ShowPhaseProgress)
            {
                Console.WriteLine($"Pass definitions {pass}/{maxPasses}");
            }

            var attempt = SolveAcrossPatterns(
                gridSize,
                currentWords.ToList(),
                requiredWords,
                extraBlackCandidates,
                patternVariants,
                GeneratorSettings.StopOnFirstSolution);
            lastResult = attempt;

            missing = FindMissingDefinitions(attempt.Placements, definitionProvider);
            if (missing.Count == 0)
            {
                return new SolveResult(
                    attempt.PatternVariant,
                    attempt.Attempt,
                    attempt.Grid,
                    attempt.Placements,
                    missing);
            }

            foreach (var word in missing)
            {
                currentWords.Remove(word);
            }
        }

        if (lastResult is null)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return new SolveResult(
            lastResult.PatternVariant,
            lastResult.Attempt,
            lastResult.Grid,
            lastResult.Placements,
            missing);
    }

    private static AttemptResult SolveAcrossPatterns(
        int gridSize,
        List<string> words,
        IReadOnlyList<string> requiredWords,
        int extraBlackCandidates,
        int patternVariants,
        bool stopOnFirst)
    {
        var variantCount = Math.Max(1, patternVariants);
        var placementCombos = RequiredPlacementPlanner.BuildPlacementCombos(
            gridSize,
            requiredWords,
            GeneratorSettings.RequiredPlacementCandidates,
            GeneratorSettings.RequiredPlacementCombos);
        if (placementCombos.Count == 0)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        var totalCombos = (long)variantCount * placementCombos.Count;
        var globalProgress = new MilestoneProgress(
            "Phase 3/4 (global)",
            totalCombos,
            GeneratorSettings.ShowPhaseProgress && GeneratorSettings.ShowStep3Progress);

        AttemptResult? bestResult = null;
        var bestAttempt = int.MaxValue;
        var updateLock = new object();
        var consoleLock = new object();
        CancellationTokenSource? cts = stopOnFirst ? new CancellationTokenSource() : null;

        bool ShouldStop()
        {
            if (stopOnFirst && cts is not null && cts.IsCancellationRequested)
            {
                return true;
            }

            return Volatile.Read(ref bestAttempt) == 0;
        }

        void EvaluateVariant(int variant, ParallelLoopState? state)
        {
            if (ShouldStop())
            {
                state?.Stop();
                return;
            }

            if (GeneratorSettings.ShowPhaseProgress && GeneratorSettings.ShowPatternLogs)
            {
                var spec = PatternBuilder.GetPatternSpec(variant);
                lock (consoleLock)
                {
                    Console.WriteLine($"Pattern variant {variant + 1}/{variantCount} (pas {spec.Step}, offset {spec.Offset})");
                }
            }

            foreach (var fixedPlacements in placementCombos)
            {
                if (ShouldStop())
                {
                    state?.Stop();
                    return;
                }

                var baseGrid = PatternBuilder.Build(gridSize, fixedPlacements, Array.Empty<(int Row, int Col)>(), variant);
                var candidateSets = PatternBuilder.BuildExtraBlackCandidateSets(
                    baseGrid,
                    fixedPlacements,
                    extraBlackCandidates);

                AttemptResult attempt;
                try
                {
                    attempt = SolveWithCandidateSets(gridSize, words, fixedPlacements, candidateSets, variant, stopOnFirst, cts);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
                finally
                {
                    globalProgress.Tick();
                }

                if (stopOnFirst)
                {
                    lock (updateLock)
                    {
                        if (bestResult is null)
                        {
                            bestResult = attempt;
                            bestAttempt = attempt.Attempt;
                        }
                    }

                    cts?.Cancel();
                    state?.Stop();
                    return;
                }

                var attemptCount = attempt.Attempt;
                var current = Volatile.Read(ref bestAttempt);
                while (attemptCount < current)
                {
                    var previous = Interlocked.CompareExchange(ref bestAttempt, attemptCount, current);
                    if (previous == current)
                    {
                        lock (updateLock)
                        {
                            if (bestResult is null || attemptCount < bestResult.Attempt)
                            {
                                bestResult = attempt;
                            }
                        }

                        break;
                    }

                    current = previous;
                }

                if (attemptCount == 0)
                {
                    state?.Stop();
                    return;
                }
            }
        }

        if (GeneratorSettings.PatternVariantParallelism > 1 && variantCount > 1)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.PatternVariantParallelism };
            if (cts is not null)
            {
                options.CancellationToken = cts.Token;
            }

            try
            {
                Parallel.ForEach(
                    Enumerable.Range(0, variantCount),
                    options,
                    (variant, state) => EvaluateVariant(variant, state));
            }
            catch (OperationCanceledException)
            {
            }
        }
        else
        {
            for (var variant = 0; variant < variantCount; variant++)
            {
                if (stopOnFirst && cts is not null && cts.IsCancellationRequested)
                {
                    break;
                }

                EvaluateVariant(variant, null);
            }
        }

        if (bestResult is null)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return bestResult;
    }

    private static AttemptResult SolveOnce(
        int gridSize,
        List<string> words,
        List<FixedWordPlacement> fixedPlacements,
        List<(int Row, int Col)> extraCandidates,
        int patternVariant,
        bool stopOnFirst,
        CancellationTokenSource? globalCts)
    {
        if (stopOnFirst && extraCandidates.Count == 0)
        {
            var candidateGrid = PatternBuilder.Build(gridSize, fixedPlacements, Array.Empty<(int Row, int Col)>(), patternVariant);
            if (!PatternValidator.HasNoOrphanCells(candidateGrid))
            {
                throw new InvalidOperationException("Pattern invalide (orphan cells).");
            }

            var timeoutMs = GeneratorSettings.PatternAttemptTimeoutMs;
            using var attemptCts = globalCts is null
                ? new CancellationTokenSource(timeoutMs)
                : CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token);
            if (globalCts is not null)
            {
                attemptCts.CancelAfter(timeoutMs);
            }

            var solver = new CrosswordSolver(candidateGrid, words, fixedPlacements, attemptCts.Token);
            if (!solver.TrySolve(out var foundPlacements))
            {
                throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
            }

            return new AttemptResult(patternVariant, 0, candidateGrid, foundPlacements);
        }

        // If we stop on first solution, we should not scan all subset sizes (can be huge).
        // Instead, try a capped number of random subsets and cancel all workers on first success.
        var subsetStart = 0;
        if (stopOnFirst && extraCandidates.Count > 0)
        {
            var pct = Math.Clamp(GeneratorSettings.PatternMinExtraPct, 0, 100);
            subsetStart = (int)Math.Round(extraCandidates.Count * (pct / 100.0));
            subsetStart = Math.Clamp(subsetStart, 0, extraCandidates.Count);
        }

        if (stopOnFirst)
        {
            var attempts = Math.Max(10, GeneratorSettings.PatternMaxAttempts);
            var attemptTracker = new ProgressTracker(
                "Tentatives pattern",
                attempts,
                GeneratorSettings.ShowProgress && GeneratorSettings.ShowPatternLogs,
                GeneratorSettings.ProgressInline);

            AttemptResult? firstResult = null;
            var resultLock = new object();

            var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.PatternParallelism };
            if (globalCts is not null)
            {
                options.CancellationToken = globalCts.Token;
            }

            try
            {
                Parallel.ForEach(
                    Enumerable.Range(0, attempts),
                    options,
                    (job, state) =>
                    {
                        if (globalCts is not null && globalCts.IsCancellationRequested)
                        {
                            state.Stop();
                            return;
                        }

                        attemptTracker.Tick();

                        var seed = unchecked(
                            Environment.TickCount ^
                            (patternVariant * 1000003) ^
                            (job * 9176));
                        var rng = new Random(seed);
                        var subsetSize = extraCandidates.Count == 0
                            ? 0
                            : rng.Next(subsetStart, extraCandidates.Count + 1);

                        List<(int Row, int Col)> extras;
                        if (subsetSize == 0)
                        {
                            extras = new List<(int Row, int Col)>();
                        }
                        else
                        {
                            var temp = new List<(int Row, int Col)>(extraCandidates);
                            for (var i = temp.Count - 1; i > 0; i--)
                            {
                                var j = rng.Next(i + 1);
                                (temp[i], temp[j]) = (temp[j], temp[i]);
                            }

                            extras = temp.Take(subsetSize).ToList();
                        }

                        var candidateGrid = PatternBuilder.Build(gridSize, fixedPlacements, extras, patternVariant);
                        if (!PatternValidator.HasNoOrphanCells(candidateGrid))
                        {
                            return;
                        }

                        var timeoutMs = GeneratorSettings.PatternAttemptTimeoutMs;
                        using var attemptCts = globalCts is null
                            ? new CancellationTokenSource(timeoutMs)
                            : CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token);
                        if (globalCts is not null)
                        {
                            attemptCts.CancelAfter(timeoutMs);
                        }

                        var solver = new CrosswordSolver(candidateGrid, words, fixedPlacements, attemptCts.Token);
                        if (!solver.TrySolve(out var foundPlacements))
                        {
                            return;
                        }

                        var result = new AttemptResult(patternVariant, subsetSize, candidateGrid, foundPlacements);
                        lock (resultLock)
                        {
                            if (firstResult is null)
                            {
                                firstResult = result;
                            }
                        }

                        PhaseLogger.Write("Solution trouvee: arret des threads.");
                        globalCts?.Cancel();
                        state.Stop();
                    });
            }
            catch (OperationCanceledException)
            {
            }

            if (firstResult is null)
            {
                throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
            }

            return firstResult;
        }

        // Minimization mode (no stop-on-first): scan subset sizes with shuffles.
        var shuffles = Math.Max(1, GeneratorSettings.PatternShuffles);
        var totalJobs = ((extraCandidates.Count - subsetStart) + 1L) * shuffles;
        var attemptTracker2 = new ProgressTracker(
            "Tentatives pattern",
            (int)Math.Min(int.MaxValue, totalJobs),
            GeneratorSettings.ShowProgress && GeneratorSettings.ShowPatternLogs,
            GeneratorSettings.ProgressInline);

        AttemptResult? bestResult = null;
        var resultLock2 = new object();

        for (var subsetSize = subsetStart; subsetSize <= extraCandidates.Count; subsetSize++)
        {
            AttemptResult? sizeResult = null;
            var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.PatternParallelism };

            try
            {
                Parallel.ForEach(
                    Enumerable.Range(0, shuffles),
                    options,
                    (shuffleIndex, state) =>
                    {
                        attemptTracker2.Tick();

                        List<(int Row, int Col)> extras;
                        if (subsetSize == 0)
                        {
                            extras = new List<(int Row, int Col)>();
                        }
                        else if (shuffleIndex == 0)
                        {
                            extras = extraCandidates.Take(subsetSize).ToList();
                        }
                        else
                        {
                            var seed = unchecked(
                                (patternVariant * 1000003) ^
                                (subsetSize * 9176) ^
                                (shuffleIndex * 53) ^
                                Environment.TickCount);
                            var rng = new Random(seed);
                            var temp = new List<(int Row, int Col)>(extraCandidates);
                            for (var i = temp.Count - 1; i > 0; i--)
                            {
                                var j = rng.Next(i + 1);
                                (temp[i], temp[j]) = (temp[j], temp[i]);
                            }

                            extras = temp.Take(subsetSize).ToList();
                        }

                        var candidateGrid = PatternBuilder.Build(gridSize, fixedPlacements, extras, patternVariant);
                        if (!PatternValidator.HasNoOrphanCells(candidateGrid))
                        {
                            return;
                        }

                        var solver = new CrosswordSolver(candidateGrid, words, fixedPlacements);
                        if (!solver.TrySolve(out var foundPlacements))
                        {
                            return;
                        }

                        var result = new AttemptResult(patternVariant, subsetSize, candidateGrid, foundPlacements);
                        lock (resultLock2)
                        {
                            sizeResult ??= result;
                            bestResult ??= result;
                            if (result.Attempt < bestResult.Attempt)
                            {
                                bestResult = result;
                            }
                        }
                    });
            }
            catch
            {
            }

            if (sizeResult is not null)
            {
                return sizeResult;
            }
        }

        if (bestResult is null)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return bestResult;
    }

    private static AttemptResult SolveWithCandidateSets(
        int gridSize,
        List<string> words,
        List<FixedWordPlacement> fixedPlacements,
        List<List<(int Row, int Col)>> candidateSets,
        int patternVariant,
        bool stopOnFirst,
        CancellationTokenSource? globalCts)
    {
        AttemptResult bestResult = null!;
        var hasResult = false;

        foreach (var set in candidateSets)
        {
            if (stopOnFirst && globalCts is not null && globalCts.IsCancellationRequested)
            {
                break;
            }

            AttemptResult attempt;
            try
            {
                attempt = SolveOnce(gridSize, words, fixedPlacements, set, patternVariant, stopOnFirst, globalCts);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (stopOnFirst)
            {
                return attempt;
            }

            if (!hasResult || attempt.Attempt < bestResult.Attempt)
            {
                bestResult = attempt;
                hasResult = true;
            }
        }

        if (!hasResult)
        {
            throw new InvalidOperationException("Aucune solution trouvee pour ce pattern.");
        }

        return bestResult;
    }

    private static List<string> FindMissingDefinitions(
        List<WordPlacement> placements,
        DefinitionProvider definitionProvider)
    {
        var missing = new List<string>();
        var unique = placements
            .Select(p => p.Word)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var word in unique)
        {
            if (!definitionProvider.TryGetDefinition(word, out _))
            {
                missing.Add(word);
            }
        }

        return missing;
    }
}

sealed class IncrementalFillSolver
{
    private const int MinWordLength = 2;
    private const int MaxReusableLength = 20;
    private readonly CrosswordGrid _grid;
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly Dictionary<int, HashSet<string>> _wordSetsByLength;
    private readonly Dictionary<string, int> _useCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _size;
    private readonly int _candidateLimit;
    private readonly Random _rng;
    private readonly IncrementalProgressReporter? _progress;
    private readonly int _minFirstLength;
    private readonly long _maxNodes;
    private readonly bool _validateVerticals;
    private readonly bool _finalValidate;
    private readonly int _crossDepth;
    private readonly int _crossCandidateLimit;
    private readonly ConcurrentDictionary<string, byte>? _bannedStarts;
    private readonly int _maxSeconds;
    private readonly bool _slotCacheEnabled;
    private readonly int _slotCacheMaxEntries;
    private readonly int _slotCachePerKey;
    private readonly Dictionary<string, List<string>> _slotCandidateCache = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Dictionary<CharPosKey, List<string>>> _indexByLength;
    private readonly bool _interleavedRowCol;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _nodeCount;
    private volatile bool _abort;
    private string? _firstWordUsed;

    public IncrementalFillSolver(
        int size,
        IEnumerable<string> words,
        int candidateLimit,
        int seed,
        IncrementalProgressReporter? progress,
        int minFirstLength,
        long maxNodes,
        bool validateVerticals,
        bool finalValidate,
        int crossDepth,
        int crossCandidateLimit,
        ConcurrentDictionary<string, byte>? bannedStarts,
        int maxSeconds,
        bool slotCacheEnabled,
        int slotCacheMaxEntries,
        int slotCachePerKey)
    {
        _size = size;
        _grid = new CrosswordGrid(size);
        _candidateLimit = Math.Max(10, candidateLimit);
        _rng = new Random(seed);
        _progress = progress;
        _minFirstLength = Math.Clamp(minFirstLength, MinWordLength, size);
        _maxNodes = maxNodes <= 0 ? long.MaxValue : maxNodes;
        _validateVerticals = validateVerticals;
        _finalValidate = finalValidate;
        _crossDepth = Math.Max(0, crossDepth);
        _crossCandidateLimit = Math.Max(10, crossCandidateLimit);
        _bannedStarts = bannedStarts;
        _maxSeconds = Math.Max(10, maxSeconds);
        _slotCacheEnabled = slotCacheEnabled;
        _slotCacheMaxEntries = Math.Max(1000, slotCacheMaxEntries);
        _slotCachePerKey = Math.Max(50, slotCachePerKey);
        _interleavedRowCol = GeneratorSettings.IncrementalInterleavedRowCol;
        _wordsByLength = words
            .Select(WordUtils.Normalize)
            .Where(word => word.Length >= MinWordLength && word.Length <= size)
            .GroupBy(word => word.Length)
            .ToDictionary(group => group.Key, group => Shuffle(group.Distinct(StringComparer.OrdinalIgnoreCase).ToList()));

        _wordSetsByLength = _wordsByLength.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.OrdinalIgnoreCase));

        _indexByLength = BuildIndex(_wordsByLength);
    }

    public string? FirstWordUsed => _firstWordUsed;

    public bool TryFill(out CrosswordGrid grid, out List<WordPlacement> placements)
    {
        placements = new List<WordPlacement>();
        var ok = _interleavedRowCol ? FillRowColStep(0, 0) : FillRow(0, 0);
        if (!ok)
        {
            grid = _grid;
            return false;
        }

        if (_finalValidate && (!ValidateAllVerticalWords() || !ValidateAllHorizontalWords()))
        {
            grid = _grid;
            return false;
        }

        grid = _grid;
        placements = PlacementExtractor.ExtractPlacements(_grid);
        return true;
    }

    // Interleave row/col filling: row 0, col 0, row 1, col 1, ...
    // This makes vertical words "real" words during construction (not only checked at the end).
    private bool FillRowColStep(int index, int phase)
    {
        if (_abort)
        {
            return false;
        }

        if (index >= _size)
        {
            return true;
        }

        if (phase == 0)
        {
            return FillRowOnly(index, 0) && FillRowColStep(index, 1);
        }

        return FillColOnly(index, 0) && FillRowColStep(index + 1, 0);
    }

    private bool FillRowOnly(int row, int col)
    {
        if (_abort)
        {
            return false;
        }

        if (col >= _size)
        {
            return true;
        }

        if (_grid.IsBlack(row, col))
        {
            return FillRowOnly(row, col + 1);
        }

        var remaining = _size - col;
        var isFirstSlot = row == 0 && col == 0;
        var lengths = BuildPossibleLengths(remaining, isFirstSlot);
        foreach (var length in lengths)
        {
            var candidates = GetHorizontalCandidates(row, col, length, isFirstSlot);
            foreach (var word in candidates)
            {
                if (!TryConsumeNode())
                {
                    return false;
                }

                if (!CanUse(word))
                {
                    continue;
                }

                if (!TryPlaceWord(row, col, length, word, out var snapshot))
                {
                    continue;
                }

                var nextCol = col + length;
                if (nextCol < _size)
                {
                    nextCol++;
                }

                if (FillRowOnly(row, nextCol))
                {
                    return true;
                }

                Undo(snapshot);
            }
        }

        return false;
    }

    private bool FillColOnly(int col, int row)
    {
        if (_abort)
        {
            return false;
        }

        if (row >= _size)
        {
            return true;
        }

        if (_grid.IsBlack(row, col))
        {
            return FillColOnly(col, row + 1);
        }

        var remaining = _size - row;
        var isFirstSlot = row == 0 && col == 0 && string.IsNullOrWhiteSpace(_firstWordUsed);
        var lengths = BuildPossibleLengths(remaining, isFirstSlot);
        foreach (var length in lengths)
        {
            var candidates = GetVerticalCandidates(row, col, length, isFirstSlot);
            foreach (var word in candidates)
            {
                if (!TryConsumeNode())
                {
                    return false;
                }

                if (!CanUse(word))
                {
                    continue;
                }

                if (!TryPlaceWordVertical(row, col, length, word, out var snapshot))
                {
                    continue;
                }

                var nextRow = row + length;
                if (nextRow < _size)
                {
                    nextRow++;
                }

                if (FillColOnly(col, nextRow))
                {
                    return true;
                }

                Undo(snapshot);
            }
        }

        return false;
    }

    private bool FillRow(int row, int col)
    {
        if (_abort)
        {
            return false;
        }

        if (row >= _size)
        {
            return true;
        }

        if (col >= _size)
        {
            return FillRow(row + 1, 0);
        }

        if (_grid.IsBlack(row, col))
        {
            return FillRow(row, col + 1);
        }

        var remaining = _size - col;
        var isFirstSlot = row == 0 && col == 0;
        var lengths = BuildPossibleLengths(remaining, isFirstSlot);
        foreach (var length in lengths)
        {
            var candidates = GetHorizontalCandidates(row, col, length, isFirstSlot);
            foreach (var word in candidates)
            {
                if (!TryConsumeNode())
                {
                    return false;
                }

                if (!CanUse(word))
                {
                    continue;
                }

                if (!TryPlaceWord(row, col, length, word, out var snapshot))
                {
                    continue;
                }

                var nextCol = col + length;
                if (nextCol < _size)
                {
                    nextCol++;
                }

                var nextRow = nextCol >= _size ? row + 1 : row;
                var actualNextCol = nextCol >= _size ? 0 : nextCol;

                if (FillRow(nextRow, actualNextCol))
                {
                    return true;
                }

                Undo(snapshot);
            }
        }

        return false;
    }

    private bool TryConsumeNode()
    {
        if (_stopwatch.Elapsed.TotalSeconds >= _maxSeconds)
        {
            _abort = true;
            return false;
        }

        var nodes = Interlocked.Increment(ref _nodeCount);
        _progress?.Tick();
        if (nodes >= _maxNodes)
        {
            _abort = true;
            return false;
        }

        return true;
    }

    private List<int> BuildPossibleLengths(int remaining, bool isFirstSlot)
    {
        var lengths = new List<int>();
        var maxLength = Math.Min(_size, remaining);
        for (var length = MinWordLength; length <= maxLength; length++)
        {
            if (isFirstSlot && length < _minFirstLength)
            {
                continue;
            }

            if (length == remaining)
            {
                lengths.Add(length);
                continue;
            }

            var leftover = remaining - length - 1;
            if (leftover == 1)
            {
                continue;
            }

            if (leftover >= 0)
            {
                lengths.Add(length);
            }
        }

        // Bias towards longer words (fewer tiny slots -> easier fill).
        // Still randomized per attempt/seed.
        var ordered = lengths
            .Select(length => (Length: length, Key: length * 1000 + _rng.Next(1000)))
            .OrderByDescending(pair => pair.Key)
            .Select(pair => pair.Length)
            .ToList();

        return ordered;
    }

    private List<string> GetHorizontalCandidates(int row, int col, int length, bool isFirstSlot)
    {
        if (!_wordsByLength.TryGetValue(length, out var words))
        {
            return new List<string>();
        }

        var candidates = new List<string>();
        foreach (var word in words)
        {
            if (MatchesHorizontalPattern(row, col, word))
            {
                candidates.Add(word);
                if (candidates.Count >= _candidateLimit)
                {
                    break;
                }
            }
        }

        if (isFirstSlot && _bannedStarts is not null && _bannedStarts.Count > 0)
        {
            var filtered = candidates
                .Where(word => !_bannedStarts.ContainsKey(word))
                .ToList();
            if (filtered.Count > 0)
            {
                candidates = filtered;
            }
        }

        Shuffle(candidates);
        return candidates;
    }

    private bool MatchesHorizontalPattern(int row, int col, string word)
    {
        for (var i = 0; i < word.Length; i++)
        {
            var cell = _grid.GetCell(row, col + i);
            if (cell == '#')
            {
                return false;
            }

            if (cell != '\0' && cell != word[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool CanUse(string word)
    {
        var usage = _useCounts.TryGetValue(word, out var count) ? count : 0;
        if (word.Length > MaxReusableLength && usage > 0)
        {
            return false;
        }

        return true;
    }

    private bool TryPlaceWord(int row, int col, int length, string word, out PlacementSnapshot snapshot)
    {
        snapshot = new PlacementSnapshot();
        var lettersBefore = snapshot.ChangedLetters.Count;
        var blacksBefore = snapshot.ChangedBlacks.Count;
        var wordsBefore = snapshot.WordsUsed.Count;
        var isFirstSlot = row == 0 && col == 0;

        var slot = new SlotSegment(row, col, Orientation.Horizontal, length, true);
        if (!TryPlaceSlotWord(slot, word, snapshot, out var changedCells))
        {
            return false;
        }

        if (col + length < _size)
        {
            var blackCol = col + length;
            var prev = _grid.GetCell(row, blackCol);
            if (prev != '\0')
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }

            _grid.SetBlack(row, blackCol);
            snapshot.ChangedBlacks.Add((row, blackCol, prev));

            if (_validateVerticals && !CheckClosedVerticalAtBlack(row, blackCol))
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }
        }

        if (_crossDepth > 0 && changedCells.Count > 0)
        {
            if (!ResolveCrossings(changedCells, Orientation.Horizontal, snapshot, depth: 0))
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }
        }

        if (isFirstSlot && string.IsNullOrWhiteSpace(_firstWordUsed))
        {
            _firstWordUsed = word;
        }

        return true;
    }

    private List<string> GetVerticalCandidates(int row, int col, int length, bool isFirstSlot)
    {
        if (!_wordsByLength.TryGetValue(length, out var words))
        {
            return new List<string>();
        }

        var candidates = new List<string>();
        foreach (var word in words)
        {
            if (MatchesVerticalPattern(row, col, word))
            {
                candidates.Add(word);
                if (candidates.Count >= _candidateLimit)
                {
                    break;
                }
            }
        }

        if (isFirstSlot && _bannedStarts is not null && _bannedStarts.Count > 0)
        {
            var filtered = candidates
                .Where(word => !_bannedStarts.ContainsKey(word))
                .ToList();
            if (filtered.Count > 0)
            {
                candidates = filtered;
            }
        }

        Shuffle(candidates);
        return candidates;
    }

    private bool MatchesVerticalPattern(int row, int col, string word)
    {
        for (var i = 0; i < word.Length; i++)
        {
            var cell = _grid.GetCell(row + i, col);
            if (cell == '#')
            {
                return false;
            }

            if (cell != '\0' && cell != word[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool TryPlaceWordVertical(int row, int col, int length, string word, out PlacementSnapshot snapshot)
    {
        snapshot = new PlacementSnapshot();
        var lettersBefore = snapshot.ChangedLetters.Count;
        var blacksBefore = snapshot.ChangedBlacks.Count;
        var wordsBefore = snapshot.WordsUsed.Count;
        var isFirstSlot = row == 0 && col == 0 && string.IsNullOrWhiteSpace(_firstWordUsed);

        var slot = new SlotSegment(row, col, Orientation.Vertical, length, true);
        if (!TryPlaceSlotWord(slot, word, snapshot, out var changedCells))
        {
            return false;
        }

        if (row + length < _size)
        {
            var blackRow = row + length;
            var prev = _grid.GetCell(blackRow, col);
            if (prev != '\0')
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }

            _grid.SetBlack(blackRow, col);
            snapshot.ChangedBlacks.Add((blackRow, col, prev));

            if (_validateVerticals && !CheckClosedHorizontalAtBlack(blackRow, col))
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }
        }

        if (_crossDepth > 0 && changedCells.Count > 0)
        {
            if (!ResolveCrossings(changedCells, Orientation.Vertical, snapshot, depth: 0))
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }
        }

        if (isFirstSlot && string.IsNullOrWhiteSpace(_firstWordUsed))
        {
            _firstWordUsed = word;
        }

        return true;
    }

    private bool TryPlaceSlotWord(
        SlotSegment slot,
        string word,
        PlacementSnapshot snapshot,
        out List<(int Row, int Col)> changedCells)
    {
        changedCells = new List<(int Row, int Col)>(slot.Length);
        if (slot.Length != word.Length)
        {
            return false;
        }

        var lettersBefore = snapshot.ChangedLetters.Count;
        var blacksBefore = snapshot.ChangedBlacks.Count;
        var wordsBefore = snapshot.WordsUsed.Count;
        var crossOrientation = slot.Orientation == Orientation.Horizontal
            ? Orientation.Vertical
            : Orientation.Horizontal;

        for (var i = 0; i < slot.Length; i++)
        {
            var (r, c) = GetSlotCell(slot, i);
            var cell = _grid.GetCell(r, c);
            if (cell == '#')
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }

            if (cell == '\0')
            {
                snapshot.ChangedLetters.Add((r, c, cell));
                _grid.SetLetter(r, c, word[i]);
                changedCells.Add((r, c));

                // Optional early feasibility check in the crossing direction.
                if (_validateVerticals && !CheckOpen(r, c, crossOrientation))
                {
                    UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                    return false;
                }
            }
            else if (cell != word[i])
            {
                UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
                return false;
            }
        }

        snapshot.WordsUsed.Add(word);
        IncrementUsage(word);
        return true;
    }

    private bool ResolveCrossings(
        List<(int Row, int Col)> changedCells,
        Orientation placedOrientation,
        PlacementSnapshot snapshot,
        int depth)
    {
        if (_abort)
        {
            return false;
        }

        // Always validate bounded, fully-filled slots in the placed direction.
        if (!ValidateBoundedSlots(changedCells, placedOrientation))
        {
            return false;
        }

        if (_crossDepth <= 0)
        {
            return true;
        }

        var crossOrientation = placedOrientation == Orientation.Horizontal
            ? Orientation.Vertical
            : Orientation.Horizontal;
        var slots = CollectBoundedSlots(changedCells, crossOrientation);
        if (slots.Count == 0)
        {
            return true;
        }

        Shuffle(slots);
        foreach (var slot in slots)
        {
            if (SlotHasEmpty(slot))
            {
                if (depth < _crossDepth)
                {
                    var filled = FillSlot(slot, snapshot, depth + 1);
                    if (!filled)
                    {
                        if (_abort)
                        {
                            return false;
                        }

                        // In strict mode, a bounded slot with no candidates means this branch is impossible.
                        if (_validateVerticals)
                        {
                            return false;
                        }
                    }
                }

                // Soft mode: do not fail the whole branch on miss.
                continue;
            }

            if (!ValidateSlot(slot))
            {
                return false;
            }
        }

        return true;
    }

    private bool FillSlot(SlotSegment slot, PlacementSnapshot snapshot, int depth)
    {
        var candidates = GetSlotCandidates(slot, _crossCandidateLimit);
        foreach (var word in candidates)
        {
            if (!TryConsumeNode())
            {
                return false;
            }

            if (!CanUse(word))
            {
                continue;
            }

            var lettersBefore = snapshot.ChangedLetters.Count;
            var blacksBefore = snapshot.ChangedBlacks.Count;
            var wordsBefore = snapshot.WordsUsed.Count;

            if (!TryPlaceSlotWord(slot, word, snapshot, out var changedCells))
            {
                continue;
            }

            if (ResolveCrossings(changedCells, slot.Orientation, snapshot, depth))
            {
                return true;
            }

            UndoTo(snapshot, lettersBefore, blacksBefore, wordsBefore);
        }

        return false;
    }

    private List<SlotSegment> CollectBoundedSlots(List<(int Row, int Col)> cells, Orientation orientation)
    {
        var slots = new List<SlotSegment>();
        var seen = new HashSet<SlotKey>();
        foreach (var (row, col) in cells)
        {
            if (!TryGetBoundedSlot(row, col, orientation, out var slot))
            {
                continue;
            }

            if (!slot.Bounded || slot.Length < MinWordLength)
            {
                continue;
            }

            var key = new SlotKey(slot.Row, slot.Col, slot.Orientation, slot.Length);
            if (seen.Add(key))
            {
                slots.Add(slot);
            }
        }

        return slots;
    }

    private bool ValidateBoundedSlots(List<(int Row, int Col)> cells, Orientation orientation)
    {
        var slots = CollectBoundedSlots(cells, orientation);
        foreach (var slot in slots)
        {
            if (!SlotHasEmpty(slot) && !ValidateSlot(slot))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateSlot(SlotSegment slot)
    {
        if (!_wordSetsByLength.TryGetValue(slot.Length, out var set))
        {
            return false;
        }

        var word = ReadSlotWord(slot);
        return set.Contains(word);
    }

    private bool TryGetBoundedSlot(int row, int col, Orientation orientation, out SlotSegment slot)
    {
        var startRow = row;
        var startCol = col;
        var endRow = row;
        var endCol = col;

        if (orientation == Orientation.Horizontal)
        {
            while (startCol > 0 && !_grid.IsBlack(row, startCol - 1))
            {
                startCol--;
            }

            while (endCol + 1 < _size && !_grid.IsBlack(row, endCol + 1))
            {
                endCol++;
            }

            // Crossword convention: the grid border acts like an implicit boundary.
            var beforeIsBoundary = startCol == 0 || _grid.IsBlack(row, startCol - 1);
            var afterIsBoundary = endCol == _size - 1 || _grid.IsBlack(row, endCol + 1);
            var bounded = beforeIsBoundary && afterIsBoundary;

            slot = new SlotSegment(row, startCol, orientation, endCol - startCol + 1, bounded);
            return true;
        }

        while (startRow > 0 && !_grid.IsBlack(startRow - 1, col))
        {
            startRow--;
        }

        while (endRow + 1 < _size && !_grid.IsBlack(endRow + 1, col))
        {
            endRow++;
        }

        // Crossword convention: the grid border acts like an implicit boundary.
        var beforeBoundary = startRow == 0 || _grid.IsBlack(startRow - 1, col);
        var afterBoundary = endRow == _size - 1 || _grid.IsBlack(endRow + 1, col);
        var isBounded = beforeBoundary && afterBoundary;
        slot = new SlotSegment(startRow, col, orientation, endRow - startRow + 1, isBounded);
        return true;
    }

    private bool SlotHasEmpty(SlotSegment slot)
    {
        for (var i = 0; i < slot.Length; i++)
        {
            var (r, c) = GetSlotCell(slot, i);
            if (_grid.GetCell(r, c) == '\0')
            {
                return true;
            }
        }

        return false;
    }

    private string ReadSlotWord(SlotSegment slot)
    {
        var chars = new char[slot.Length];
        for (var i = 0; i < slot.Length; i++)
        {
            var (r, c) = GetSlotCell(slot, i);
            chars[i] = _grid.GetCell(r, c);
        }

        return new string(chars);
    }

    private List<string> GetSlotCandidates(SlotSegment slot, int limit)
    {
        if (!_wordsByLength.TryGetValue(slot.Length, out var words))
        {
            return new List<string>();
        }

        var (cacheKey, constraints) = BuildSlotPattern(slot);
        if (_slotCacheEnabled &&
            constraints.Count > 0 &&
            _slotCandidateCache.TryGetValue(cacheKey, out var cached))
        {
            var fromCache = new List<string>(cached);
            Shuffle(fromCache);
            if (fromCache.Count > limit)
            {
                fromCache = fromCache.Take(limit).ToList();
            }

            return fromCache;
        }

        IEnumerable<string> baseCandidates = words;
        if (constraints.Count > 0 &&
            _indexByLength.TryGetValue(slot.Length, out var index))
        {
            var bestList = default(List<string>);
            var bestCount = int.MaxValue;
            foreach (var constraint in constraints)
            {
                var key = new CharPosKey(constraint.Pos, constraint.Letter);
                if (!index.TryGetValue(key, out var list))
                {
                    return new List<string>();
                }

                if (list.Count < bestCount)
                {
                    bestCount = list.Count;
                    bestList = list;
                }
            }

            if (bestList is not null)
            {
                baseCandidates = bestList;
            }
        }

        var maxLimit = Math.Max(limit, _slotCachePerKey);
        var candidates = new List<string>(Math.Min(maxLimit, 2048));
        foreach (var word in baseCandidates)
        {
            if (!MatchesSlotPattern(slot, word))
            {
                continue;
            }

            candidates.Add(word);
            if (candidates.Count >= maxLimit)
            {
                break;
            }
        }

        if (_slotCacheEnabled && constraints.Count > 0)
        {
            if (_slotCandidateCache.Count >= _slotCacheMaxEntries)
            {
                _slotCandidateCache.Clear();
            }

            var toCache = candidates;
            if (toCache.Count > _slotCachePerKey)
            {
                toCache = toCache.Take(_slotCachePerKey).ToList();
            }

            _slotCandidateCache[cacheKey] = toCache;
        }

        Shuffle(candidates);
        if (candidates.Count > limit)
        {
            candidates = candidates.Take(limit).ToList();
        }

        return candidates;
    }

    private (string CacheKey, List<(int Pos, char Letter)> Constraints) BuildSlotPattern(SlotSegment slot)
    {
        var pattern = new char[slot.Length];
        Array.Fill(pattern, '.');
        var constraints = new List<(int Pos, char Letter)>(slot.Length);
        for (var i = 0; i < slot.Length; i++)
        {
            var (r, c) = GetSlotCell(slot, i);
            var cell = _grid.GetCell(r, c);
            if (cell == '\0' || cell == '#')
            {
                continue;
            }

            pattern[i] = cell;
            constraints.Add((i, cell));
        }

        var key = $"{slot.Length}:{new string(pattern)}";
        return (key, constraints);
    }

    private bool MatchesSlotPattern(SlotSegment slot, string word)
    {
        for (var i = 0; i < slot.Length; i++)
        {
            var (r, c) = GetSlotCell(slot, i);
            var cell = _grid.GetCell(r, c);
            if (cell == '#')
            {
                return false;
            }

            if (cell != '\0' && cell != word[i])
            {
                return false;
            }
        }

        return true;
    }

    private static (int Row, int Col) GetSlotCell(SlotSegment slot, int index)
    {
        return slot.Orientation == Orientation.Horizontal
            ? (slot.Row, slot.Col + index)
            : (slot.Row + index, slot.Col);
    }

    private bool CheckOpen(int row, int col, Orientation orientation)
    {
        if (orientation == Orientation.Horizontal)
        {
            var startCol = col;
            while (startCol > 0 && !_grid.IsBlack(row, startCol - 1))
            {
                startCol--;
            }

            var endCol = col;
            while (endCol + 1 < _size && !_grid.IsBlack(row, endCol + 1))
            {
                endCol++;
            }

            var currentLen = col - startCol + 1;
            var minLen = Math.Max(MinWordLength, currentLen);
            var maxLen = endCol - startCol + 1;
            for (var length = minLen; length <= maxLen; length++)
            {
                if (HasHorizontalMatch(row, startCol, col, length))
                {
                    return true;
                }
            }

            return false;
        }

        var startRow = row;
        while (startRow > 0 && !_grid.IsBlack(startRow - 1, col))
        {
            startRow--;
        }

        var endRow = row;
        while (endRow + 1 < _size && !_grid.IsBlack(endRow + 1, col))
        {
            endRow++;
        }

        var curLen = row - startRow + 1;
        var minLength = Math.Max(MinWordLength, curLen);
        var maxLength = endRow - startRow + 1;
        for (var length = minLength; length <= maxLength; length++)
        {
            if (HasVerticalMatch(startRow, row, col, length))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasHorizontalMatch(int row, int startCol, int currentCol, int length)
    {
        if (!_wordsByLength.TryGetValue(length, out var words))
        {
            return false;
        }

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var matches = true;
            for (var c = startCol; c <= currentCol; c++)
            {
                var expected = _grid.GetCell(row, c);
                if (expected == '\0')
                {
                    continue;
                }

                var pos = c - startCol;
                if (word[pos] != expected)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckVerticalOpen(int row, int col)
    {
        var start = row;
        while (start > 0 && !_grid.IsBlack(start - 1, col))
        {
            start--;
        }

        var end = row;
        while (end + 1 < _size && !_grid.IsBlack(end + 1, col))
        {
            end++;
        }

        var currentLen = row - start + 1;
        var minLen = Math.Max(MinWordLength, currentLen);
        var maxLen = end - start + 1;

        for (var length = minLen; length <= maxLen; length++)
        {
            if (HasVerticalMatch(start, row, col, length))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasVerticalMatch(int startRow, int currentRow, int col, int length)
    {
        if (!_wordsByLength.TryGetValue(length, out var words))
        {
            return false;
        }

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var matches = true;
            for (var r = startRow; r <= currentRow; r++)
            {
                var expected = _grid.GetCell(r, col);
                if (expected == '\0')
                {
                    continue;
                }

                var pos = r - startRow;
                if (word[pos] != expected)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckClosedVerticalAtBlack(int row, int col)
    {
        if (row == 0 || !_grid.IsLetter(row - 1, col))
        {
            return true;
        }

        var start = row - 1;
        while (start > 0 && _grid.IsLetter(start - 1, col))
        {
            start--;
        }

        var length = row - start;
        if (length < MinWordLength)
        {
            return false;
        }

        if (!_wordSetsByLength.TryGetValue(length, out var set))
        {
            return false;
        }

        var word = ReadVerticalWord(start, row - 1, col);
        return set.Contains(word);
    }

    private bool CheckClosedHorizontalAtBlack(int row, int col)
    {
        if (col == 0 || !_grid.IsLetter(row, col - 1))
        {
            return true;
        }

        var start = col - 1;
        while (start > 0 && _grid.IsLetter(row, start - 1))
        {
            start--;
        }

        var length = col - start;
        if (length < MinWordLength)
        {
            return false;
        }

        if (!_wordSetsByLength.TryGetValue(length, out var set))
        {
            return false;
        }

        var word = ReadHorizontalWord(row, start, col - 1);
        return set.Contains(word);
    }

    private bool ValidateAllVerticalWords()
    {
        for (var col = 0; col < _size; col++)
        {
            var row = 0;
            while (row < _size)
            {
                while (row < _size && !_grid.IsLetter(row, col))
                {
                    row++;
                }

                if (row >= _size)
                {
                    break;
                }

                var start = row;
                while (row < _size && _grid.IsLetter(row, col))
                {
                    row++;
                }

                var length = row - start;
                if (length < MinWordLength)
                {
                    return false;
                }

                if (!_wordSetsByLength.TryGetValue(length, out var set))
                {
                    return false;
                }

                var word = ReadVerticalWord(start, row - 1, col);
                if (!set.Contains(word))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool ValidateAllHorizontalWords()
    {
        for (var row = 0; row < _size; row++)
        {
            var col = 0;
            while (col < _size)
            {
                while (col < _size && !_grid.IsLetter(row, col))
                {
                    col++;
                }

                if (col >= _size)
                {
                    break;
                }

                var start = col;
                while (col < _size && _grid.IsLetter(row, col))
                {
                    col++;
                }

                var length = col - start;
                if (length < MinWordLength)
                {
                    return false;
                }

                if (!_wordSetsByLength.TryGetValue(length, out var set))
                {
                    return false;
                }

                var word = ReadHorizontalWord(row, start, col - 1);
                if (!set.Contains(word))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private string ReadVerticalWord(int startRow, int endRow, int col)
    {
        var chars = new char[endRow - startRow + 1];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = _grid.GetCell(startRow + i, col);
        }

        return new string(chars);
    }

    private string ReadHorizontalWord(int row, int startCol, int endCol)
    {
        var chars = new char[endCol - startCol + 1];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = _grid.GetCell(row, startCol + i);
        }

        return new string(chars);
    }

    private void IncrementUsage(string word)
    {
        if (_useCounts.TryGetValue(word, out var count))
        {
            _useCounts[word] = count + 1;
        }
        else
        {
            _useCounts[word] = 1;
        }
    }

    private static Dictionary<int, Dictionary<CharPosKey, List<string>>> BuildIndex(
        Dictionary<int, List<string>> wordsByLength)
    {
        var index = new Dictionary<int, Dictionary<CharPosKey, List<string>>>();
        foreach (var (length, words) in wordsByLength)
        {
            var map = new Dictionary<CharPosKey, List<string>>();
            foreach (var word in words)
            {
                for (var pos = 0; pos < word.Length; pos++)
                {
                    var key = new CharPosKey(pos, word[pos]);
                    if (!map.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        map[key] = list;
                    }

                    list.Add(word);
                }
            }

            index[length] = map;
        }

        return index;
    }

    private void Undo(PlacementSnapshot snapshot)
    {
        UndoTo(snapshot, 0, 0, 0);
    }

    private void UndoTo(PlacementSnapshot snapshot, int lettersBefore, int blacksBefore, int wordsBefore)
    {
        for (var i = snapshot.ChangedBlacks.Count - 1; i >= blacksBefore; i--)
        {
            var (row, col, prev) = snapshot.ChangedBlacks[i];
            RestoreCell(row, col, prev);
        }

        if (snapshot.ChangedBlacks.Count > blacksBefore)
        {
            snapshot.ChangedBlacks.RemoveRange(blacksBefore, snapshot.ChangedBlacks.Count - blacksBefore);
        }

        for (var i = snapshot.ChangedLetters.Count - 1; i >= lettersBefore; i--)
        {
            var (row, col, prev) = snapshot.ChangedLetters[i];
            RestoreCell(row, col, prev);
        }

        if (snapshot.ChangedLetters.Count > lettersBefore)
        {
            snapshot.ChangedLetters.RemoveRange(lettersBefore, snapshot.ChangedLetters.Count - lettersBefore);
        }

        for (var i = snapshot.WordsUsed.Count - 1; i >= wordsBefore; i--)
        {
            var word = snapshot.WordsUsed[i];
            if (_useCounts.TryGetValue(word, out var count))
            {
                if (count <= 1)
                {
                    _useCounts.Remove(word);
                }
                else
                {
                    _useCounts[word] = count - 1;
                }
            }
        }

        if (snapshot.WordsUsed.Count > wordsBefore)
        {
            snapshot.WordsUsed.RemoveRange(wordsBefore, snapshot.WordsUsed.Count - wordsBefore);
        }
    }

    private void RestoreCell(int row, int col, char prev)
    {
        if (prev == '#')
        {
            _grid.SetBlack(row, col);
            return;
        }

        if (prev == '\0')
        {
            _grid.SetEmpty(row, col);
            return;
        }

        _grid.SetLetter(row, col, prev);
    }

    private List<T> Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private readonly record struct SlotSegment(
        int Row,
        int Col,
        Orientation Orientation,
        int Length,
        bool Bounded);

    private readonly record struct CharPosKey(int Pos, char Letter);

    private readonly record struct SlotKey(
        int Row,
        int Col,
        Orientation Orientation,
        int Length);

    private sealed class PlacementSnapshot
    {
        public List<(int Row, int Col, char Prev)> ChangedLetters { get; } = new();
        public List<(int Row, int Col, char Prev)> ChangedBlacks { get; } = new();
        public List<string> WordsUsed { get; } = new();
    }
}

sealed class CspFillSolver
{
    private const int MinWordLength = 2;

    private readonly CrosswordGrid _grid;
    private readonly List<CspSlot> _slots;
    private readonly List<int>[] _slotsByCell;
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly Dictionary<int, HashSet<string>> _wordSetsByLength;
    private readonly Dictionary<int, Dictionary<CharPosKey, List<string>>> _indexByLength;
    private readonly Dictionary<string, List<string>> _candidateCache = new(StringComparer.Ordinal);
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool[] _assigned;
    private readonly int _size;
    private readonly int _candidateLimit;
    private readonly Random _rng;
    private long _nodes;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly long _maxNodes;
    private readonly int _maxSeconds;

    private readonly record struct CharPosKey(int Pos, char Letter);

    private readonly record struct CspSlot(int Row, int Col, Orientation Orientation, int Length, (int Row, int Col)[] Cells);

    public CspFillSolver(CrosswordGrid patternGrid, IEnumerable<string> words, int seed, int candidateLimit, long maxNodes, int maxSeconds)
    {
        _grid = patternGrid;
        _size = patternGrid.Size;
        _candidateLimit = Math.Max(50, candidateLimit);
        _rng = new Random(seed);
        _maxNodes = maxNodes <= 0 ? long.MaxValue : maxNodes;
        _maxSeconds = Math.Max(1, maxSeconds);

        _wordsByLength = words
            .Select(WordUtils.Normalize)
            .Where(w => w.Length >= MinWordLength && w.Length <= _size)
            .GroupBy(w => w.Length)
            .ToDictionary(g => g.Key, g => g.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        _wordSetsByLength = _wordsByLength.ToDictionary(
            p => p.Key,
            p => new HashSet<string>(p.Value, StringComparer.OrdinalIgnoreCase));

        _indexByLength = BuildIndex(_wordsByLength);
        _slots = ExtractSlots(_grid);
        _assigned = new bool[_slots.Count];

        _slotsByCell = new List<int>[_size * _size];
        for (var i = 0; i < _slotsByCell.Length; i++)
        {
            _slotsByCell[i] = new List<int>(2);
        }

        for (var slotId = 0; slotId < _slots.Count; slotId++)
        {
            foreach (var cell in _slots[slotId].Cells)
            {
                _slotsByCell[cell.Row * _size + cell.Col].Add(slotId);
            }
        }
    }

    public long Nodes => _nodes;
    public TimeSpan Elapsed => _sw.Elapsed;

    public bool TryFill(out CrosswordGrid grid, out List<WordPlacement> placements)
    {
        placements = new List<WordPlacement>();
        if (!Solve())
        {
            grid = _grid;
            return false;
        }

        grid = _grid;
        placements = PlacementExtractor.ExtractPlacements(_grid);
        return true;
    }

    private bool Solve()
    {
        _nodes++;
        if (_nodes >= _maxNodes)
        {
            return false;
        }

        if (_sw.Elapsed.TotalSeconds >= _maxSeconds)
        {
            return false;
        }

        var next = SelectNextSlot();
        if (next < 0)
        {
            return ValidateAllWords();
        }

        var slot = _slots[next];
        var candidates = GetCandidates(slot);
        foreach (var word in candidates)
        {
            if (_used.Contains(word))
            {
                continue;
            }

            if (!TryPlaceSlot(next, slot, word, out var snap))
            {
                continue;
            }

            if (ForwardCheck(snap.ChangedCells))
            {
                if (Solve())
                {
                    return true;
                }
            }

            Undo(snap);
        }

        return false;
    }

    private int SelectNextSlot()
    {
        var bestId = -1;
        var bestCount = int.MaxValue;

        for (var i = 0; i < _slots.Count; i++)
        {
            if (_assigned[i])
            {
                continue;
            }

            // If already fully filled by crossings, we still assign it (single forced word).
            var slot = _slots[i];
            var candidates = GetCandidates(slot, max: bestCount - 1);
            var count = candidates.Count;
            if (count == 0)
            {
                return i; // dead end quickly
            }

            if (count < bestCount)
            {
                bestCount = count;
                bestId = i;
                if (bestCount <= 1)
                {
                    break;
                }
            }
        }

        return bestId;
    }

    private readonly record struct PlacementSnap(int SlotId, string Word, List<(int Row, int Col, char Prev)> Changed, List<(int Row, int Col)> ChangedCells);

    private bool TryPlaceSlot(int slotId, CspSlot slot, string word, out PlacementSnap snap)
    {
        snap = new PlacementSnap(slotId, word, new List<(int, int, char)>(), new List<(int, int)>());

        if (word.Length != slot.Length)
        {
            return false;
        }

        for (var i = 0; i < slot.Cells.Length; i++)
        {
            var (r, c) = slot.Cells[i];
            var prev = _grid.GetCell(r, c);
            if (prev == '#')
            {
                Undo(snap);
                return false;
            }

            var ch = word[i];
            if (prev == '\0')
            {
                snap.Changed.Add((r, c, prev));
                snap.ChangedCells.Add((r, c));
                _grid.SetLetter(r, c, ch);
            }
            else if (prev != ch)
            {
                Undo(snap);
                return false;
            }
        }

        _assigned[slotId] = true;
        _used.Add(word);
        return true;
    }

    private void Undo(PlacementSnap snap)
    {
        foreach (var (r, c, prev) in snap.Changed.AsEnumerable().Reverse())
        {
            if (prev == '\0')
            {
                _grid.SetEmpty(r, c);
            }
            else
            {
                _grid.SetLetter(r, c, prev);
            }
        }

        if (snap.SlotId >= 0 && snap.SlotId < _assigned.Length)
        {
            _assigned[snap.SlotId] = false;
        }

        if (!string.IsNullOrWhiteSpace(snap.Word))
        {
            _used.Remove(snap.Word);
        }
    }

    private bool ForwardCheck(List<(int Row, int Col)> changedCells)
    {
        var seen = new HashSet<int>();
        foreach (var (r, c) in changedCells)
        {
            foreach (var slotId in _slotsByCell[r * _size + c])
            {
                if (_assigned[slotId])
                {
                    // If this slot is fully filled, make sure it is a valid word.
                    if (IsSlotFilled(_slots[slotId]) && !ValidateSlotWord(_slots[slotId]))
                    {
                        return false;
                    }

                    continue;
                }

                if (!seen.Add(slotId))
                {
                    continue;
                }

                var slot = _slots[slotId];
                if (GetCandidates(slot, max: 1).Count == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool ValidateAllWords()
    {
        foreach (var slot in _slots)
        {
            if (!IsSlotFilled(slot))
            {
                return false;
            }

            if (!ValidateSlotWord(slot))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateSlotWord(CspSlot slot)
    {
        if (!_wordSetsByLength.TryGetValue(slot.Length, out var set))
        {
            return false;
        }

        var word = ReadSlot(slot);
        return set.Contains(word);
    }

    private bool IsSlotFilled(CspSlot slot)
    {
        foreach (var (r, c) in slot.Cells)
        {
            if (!_grid.IsLetter(r, c))
            {
                return false;
            }
        }

        return true;
    }

    private string ReadSlot(CspSlot slot)
    {
        var chars = new char[slot.Length];
        for (var i = 0; i < slot.Cells.Length; i++)
        {
            var (r, c) = slot.Cells[i];
            chars[i] = _grid.GetCell(r, c);
        }

        return new string(chars);
    }

    private List<string> GetCandidates(CspSlot slot, int max = -1)
    {
        if (!_wordsByLength.TryGetValue(slot.Length, out var words))
        {
            return new List<string>();
        }

        var (key, constraints) = BuildPatternKey(slot);
        if (_candidateCache.TryGetValue(key, out var cached))
        {
            if (max > 0 && cached.Count > max)
            {
                return cached.Take(max).ToList();
            }

            return new List<string>(cached);
        }

        IEnumerable<string> baseCandidates = words;
        if (constraints.Count > 0 && _indexByLength.TryGetValue(slot.Length, out var index))
        {
            List<string>? best = null;
            var bestCount = int.MaxValue;
            foreach (var (pos, ch) in constraints)
            {
                var k = new CharPosKey(pos, ch);
                if (!index.TryGetValue(k, out var list))
                {
                    _candidateCache[key] = new List<string>();
                    return new List<string>();
                }

                if (list.Count < bestCount)
                {
                    bestCount = list.Count;
                    best = list;
                }
            }

            if (best is not null)
            {
                baseCandidates = best;
            }
        }

        var limit = max > 0 ? Math.Min(_candidateLimit, max) : _candidateLimit;
        if (_size <= 7)
        {
            // Small grids are hard; avoid truncating candidate lists too aggressively.
            limit = int.MaxValue;
        }
        var results = new List<string>(Math.Min(limit, 2048));
        foreach (var w in baseCandidates)
        {
            if (!Matches(slot, w))
            {
                continue;
            }

            if (!WordFilter.IsAcceptable(w))
            {
                continue;
            }

            results.Add(w);
            if (results.Count >= limit)
            {
                break;
            }
        }

        results = results
            .OrderByDescending(WordQuality.GetScore)
            .ThenBy(w => w.Length)
            .ThenBy(w => w, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // For small grids, we don't apply the limit.
        if (_size > 7 && results.Count > limit)
        {
            results = results.Take(limit).ToList();
        }

        _candidateCache[key] = results;
        return results;
    }

    private (string Key, List<(int Pos, char Ch)> Constraints) BuildPatternKey(CspSlot slot)
    {
        var pattern = new char[slot.Length];
        Array.Fill(pattern, '.');
        var constraints = new List<(int, char)>();
        for (var i = 0; i < slot.Cells.Length; i++)
        {
            var (r, c) = slot.Cells[i];
            var cell = _grid.GetCell(r, c);
            if (cell == '\0' || cell == '#')
            {
                continue;
            }

            pattern[i] = cell;
            constraints.Add((i, cell));
        }

        return ($"{slot.Length}:{new string(pattern)}", constraints);
    }

    private bool Matches(CspSlot slot, string word)
    {
        for (var i = 0; i < slot.Cells.Length; i++)
        {
            var (r, c) = slot.Cells[i];
            var cell = _grid.GetCell(r, c);
            if (cell == '#')
            {
                return false;
            }

            if (cell != '\0' && cell != word[i])
            {
                return false;
            }
        }

        return true;
    }

    private void Shuffle(List<string> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static List<CspSlot> ExtractSlots(CrosswordGrid grid)
    {
        var slots = new List<CspSlot>();
        var size = grid.Size;

        for (var r = 0; r < size; r++)
        {
            var c = 0;
            while (c < size)
            {
                while (c < size && grid.IsBlack(r, c))
                {
                    c++;
                }

                var start = c;
                while (c < size && !grid.IsBlack(r, c))
                {
                    c++;
                }

                var len = c - start;
                if (len >= MinWordLength)
                {
                    var cells = new (int, int)[len];
                    for (var i = 0; i < len; i++)
                    {
                        cells[i] = (r, start + i);
                    }

                    slots.Add(new CspSlot(r, start, Orientation.Horizontal, len, cells));
                }
            }
        }

        for (var c = 0; c < size; c++)
        {
            var r = 0;
            while (r < size)
            {
                while (r < size && grid.IsBlack(r, c))
                {
                    r++;
                }

                var start = r;
                while (r < size && !grid.IsBlack(r, c))
                {
                    r++;
                }

                var len = r - start;
                if (len >= MinWordLength)
                {
                    var cells = new (int, int)[len];
                    for (var i = 0; i < len; i++)
                    {
                        cells[i] = (start + i, c);
                    }

                    slots.Add(new CspSlot(start, c, Orientation.Vertical, len, cells));
                }
            }
        }

        return slots;
    }

    private static Dictionary<int, Dictionary<CharPosKey, List<string>>> BuildIndex(
        Dictionary<int, List<string>> wordsByLength)
    {
        var index = new Dictionary<int, Dictionary<CharPosKey, List<string>>>();
        foreach (var (length, words) in wordsByLength)
        {
            var map = new Dictionary<CharPosKey, List<string>>();
            foreach (var word in words)
            {
                for (var pos = 0; pos < word.Length; pos++)
                {
                    var key = new CharPosKey(pos, word[pos]);
                    if (!map.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        map[key] = list;
                    }

                    list.Add(word);
                }
            }

            index[length] = map;
        }

        return index;
    }
}

sealed class IncrementalProgressReporter
{
    private readonly string _label;
    private readonly long _every;
    private readonly bool _inline;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly object _lock = new();
    private long _count;
    private long _lastReport;
    private int _lastWidth;

    public IncrementalProgressReporter(string label, long every, bool inline)
    {
        _label = label;
        _every = Math.Max(1000, every);
        _inline = inline;
    }

    public void Tick()
    {
        var count = Interlocked.Increment(ref _count);
        if (count - Interlocked.Read(ref _lastReport) < _every)
        {
            return;
        }

        lock (_lock)
        {
            if (count - _lastReport < _every)
            {
                return;
            }

            _lastReport = count;
            var elapsed = Math.Max(1, _stopwatch.Elapsed.TotalSeconds);
            var rate = count / elapsed;
            var message = $"{_label}: {count:n0} nodes ({rate:n0}/s)";
            if (_inline)
            {
                _lastWidth = Math.Max(_lastWidth, message.Length);
                Console.Write($"\r{message.PadRight(_lastWidth)}");
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}

static class PlacementExtractor
{
    public static List<WordPlacement> ExtractPlacements(CrosswordGrid grid)
    {
        var placements = new List<WordPlacement>();
        var size = grid.Size;

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                if (!grid.IsLetter(row, col))
                {
                    continue;
                }

                if ((col == 0 || grid.IsBlack(row, col - 1)) &&
                    col + 1 < size && grid.IsLetter(row, col + 1))
                {
                    var word = ReadHorizontalWord(grid, row, col);
                    if (word.Length >= 2)
                    {
                        placements.Add(new WordPlacement(word, row, col, Orientation.Horizontal));
                    }
                }

                if ((row == 0 || grid.IsBlack(row - 1, col)) &&
                    row + 1 < size && grid.IsLetter(row + 1, col))
                {
                    var word = ReadVerticalWord(grid, row, col);
                    if (word.Length >= 2)
                    {
                        placements.Add(new WordPlacement(word, row, col, Orientation.Vertical));
                    }
                }
            }
        }

        return placements;
    }

    private static string ReadHorizontalWord(CrosswordGrid grid, int row, int col)
    {
        var chars = new List<char>();
        var cursor = col;
        while (cursor < grid.Size && grid.IsLetter(row, cursor))
        {
            chars.Add(grid.GetCell(row, cursor));
            cursor++;
        }

        return new string(chars.ToArray());
    }

    private static string ReadVerticalWord(CrosswordGrid grid, int row, int col)
    {
        var chars = new List<char>();
        var cursor = row;
        while (cursor < grid.Size && grid.IsLetter(cursor, col))
        {
            chars.Add(grid.GetCell(cursor, col));
            cursor++;
        }

        return new string(chars.ToArray());
    }
}

static class PatternBuilder
{
    public static CrosswordGrid Build(int size, List<FixedWordPlacement> fixedWords)
    {
        return Build(size, fixedWords, Array.Empty<(int Row, int Col)>(), 0);
    }

    public static CrosswordGrid Build(
        int size,
        List<FixedWordPlacement> fixedWords,
        IReadOnlyList<(int Row, int Col)> extraBlacks)
    {
        return Build(size, fixedWords, extraBlacks, 0);
    }

    public static CrosswordGrid Build(
        int size,
        List<FixedWordPlacement> fixedWords,
        IReadOnlyList<(int Row, int Col)> extraBlacks,
        int patternVariant)
    {
        var grid = new CrosswordGrid(size);
        var patterns = BuildRowPatterns(size, patternVariant);

        if (patterns.Count != size)
        {
            throw new InvalidOperationException("Le pattern ne correspond pas a la taille de la grille.");
        }

        for (var row = 0; row < size; row++)
        {
            ApplyRowPattern(grid, row, patterns[row]);
        }

        ClearFixedPlacements(grid, fixedWords);
        var protectedLetters = BuildProtectedLetters(fixedWords);
        ApplyExtraBlacks(grid, protectedLetters, extraBlacks);

        var lockedBlacks = BuildLockedBlacks(fixedWords, size);
        foreach (var cell in lockedBlacks)
        {
            grid.SetBlack(cell.Row, cell.Col);
        }

        FixShortRuns(grid, lockedBlacks);
        return grid;
    }

    private static void ClearFixedPlacements(CrosswordGrid grid, List<FixedWordPlacement> fixedWords)
    {
        foreach (var placement in fixedWords)
        {
            var word = WordUtils.Normalize(placement.Word);
            for (var i = 0; i < word.Length; i++)
            {
                var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
                var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);
                if (row < 0 || row >= grid.Size || col < 0 || col >= grid.Size)
                {
                    continue;
                }

                grid.SetEmpty(row, col);
            }
        }
    }

    private static List<int[]> BuildRowPatterns(int size, int patternVariant)
    {
        var patterns = new List<int[]>();

        var specs = BuildPatternSpecs();
        if (patternVariant >= 0 && patternVariant < specs.Count)
        {
            var spec = specs[patternVariant];
            var offset = ((spec.Offset % spec.Step) + spec.Step) % spec.Step;
            var baseBlacks = new List<int>();
            for (var col = 3 + offset; col < size - 1; col += spec.Step)
            {
                baseBlacks.Add(col);
            }

            for (var row = 0; row < size; row++)
            {
                var blacks = baseBlacks
                    .Select(pos => (pos + row) % size)
                    .OrderBy(pos => pos)
                    .ToArray();

                var segments = new List<int>(blacks.Length + 1);
                var previous = -1;
                foreach (var black in blacks)
                {
                    segments.Add(black - previous - 1);
                    previous = black;
                }

                segments.Add(size - previous - 1);
                patterns.Add(segments.ToArray());
            }

            return patterns;
        }

        // Random patterns (more variety than step/offset).
        var minSlotLength = GeneratorSettings.MinSlotLength;
        var density = Math.Clamp(GeneratorSettings.PatternRandomDensityPct / 100.0, 0.0, 0.60);
        var seed = unchecked((patternVariant * 1000003) ^ (size * 7919) ^ Environment.TickCount);
        var rng = new Random(seed);

        for (var row = 0; row < size; row++)
        {
            var blacks = new List<int>();
            var previousBlack = -1;

            // Keep edges open; place blacks in [1, size-2]
            for (var col = 1; col <= size - 2; col++)
            {
                // Enforce minimum slot length before this black.
                if (col - previousBlack - 1 < minSlotLength)
                {
                    continue;
                }

                // Enforce that the tail after this black can still fit a slot.
                if (size - col - 1 < minSlotLength)
                {
                    continue;
                }

                if (rng.NextDouble() < density)
                {
                    blacks.Add(col);
                    previousBlack = col;
                }
            }

            var segments = new List<int>(blacks.Count + 1);
            var previous = -1;
            foreach (var black in blacks)
            {
                segments.Add(black - previous - 1);
                previous = black;
            }

            segments.Add(size - previous - 1);
            patterns.Add(segments.ToArray());
        }

        return patterns;
    }

    public static PatternSpec GetPatternSpec(int variantIndex)
    {
        var specs = BuildPatternSpecs();
        if (specs.Count == 0)
        {
            return new PatternSpec(4, 0);
        }

        var index = variantIndex % specs.Count;
        if (index < 0)
        {
            index += specs.Count;
        }

        return specs[index];
    }

    private static List<PatternSpec> BuildPatternSpecs()
    {
        var specs = new List<PatternSpec>();
        AddStep(specs, 4);
        AddStep(specs, 5);
        AddStep(specs, 3);
        return specs;
    }

    private static void AddStep(List<PatternSpec> specs, int step)
    {
        for (var offset = 0; offset < step; offset++)
        {
            specs.Add(new PatternSpec(step, offset));
        }
    }

    public record PatternSpec(int Step, int Offset);

    private static void ApplyRowPattern(CrosswordGrid grid, int row, int[] segments)
    {
        var size = grid.Size;
        var total = segments.Sum() + (segments.Length - 1);
        if (total != size)
        {
            throw new InvalidOperationException($"Pattern invalide ligne {row}: {total} != {size}");
        }

        var col = 0;
        for (var index = 0; index < segments.Length; index++)
        {
            var length = segments[index];
            col += length;
            if (index < segments.Length - 1)
            {
                grid.SetBlack(row, col);
                col++;
            }
        }
    }

    private static HashSet<(int Row, int Col)> BuildLockedBlacks(List<FixedWordPlacement> fixedWords, int size)
    {
        var locked = new HashSet<(int Row, int Col)>();
        foreach (var placement in fixedWords)
        {
            var word = WordUtils.Normalize(placement.Word);
            var length = word.Length;
            if (placement.Orientation == Orientation.Horizontal)
            {
                var before = placement.Col - 1;
                var after = placement.Col + length;
                if (before >= 0)
                {
                    locked.Add((placement.Row, before));
                }

                if (after < size)
                {
                    locked.Add((placement.Row, after));
                }
            }
            else
            {
                var before = placement.Row - 1;
                var after = placement.Row + length;
                if (before >= 0)
                {
                    locked.Add((before, placement.Col));
                }

                if (after < size)
                {
                    locked.Add((after, placement.Col));
                }
            }
        }

        return locked;
    }

    private static HashSet<(int Row, int Col)> BuildProtectedLetters(List<FixedWordPlacement> fixedWords)
    {
        var protectedCells = new HashSet<(int Row, int Col)>();
        foreach (var placement in fixedWords)
        {
            var word = WordUtils.Normalize(placement.Word);
            for (var i = 0; i < word.Length; i++)
            {
                var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
                var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);
                protectedCells.Add((row, col));
            }
        }

        return protectedCells;
    }

    private static void ApplyEdgeBlocks(CrosswordGrid grid, HashSet<(int Row, int Col)> protectedLetters)
    {
        var left2 = new[] { 2, 6, 10, 14, 18 };
        var left3 = new[] { 4, 8, 12, 16 };
        var right2 = new[] { 1, 5, 9, 13, 17 };
        var right3 = new[] { 3, 7, 11, 15, 19 };

        foreach (var row in left2)
        {
            SetBlackIfSafe(grid, protectedLetters, row, 0);
            SetBlackIfSafe(grid, protectedLetters, row, 1);
        }

        foreach (var row in left3)
        {
            SetBlackIfSafe(grid, protectedLetters, row, 0);
            SetBlackIfSafe(grid, protectedLetters, row, 1);
            SetBlackIfSafe(grid, protectedLetters, row, 2);
        }

        foreach (var row in right2)
        {
            SetBlackIfSafe(grid, protectedLetters, row, 18);
            SetBlackIfSafe(grid, protectedLetters, row, 19);
        }

        foreach (var row in right3)
        {
            SetBlackIfSafe(grid, protectedLetters, row, 17);
            SetBlackIfSafe(grid, protectedLetters, row, 18);
            SetBlackIfSafe(grid, protectedLetters, row, 19);
        }
    }

    private static void ApplyExtraBlacks(
        CrosswordGrid grid,
        HashSet<(int Row, int Col)> protectedLetters,
        IReadOnlyList<(int Row, int Col)> extraBlacks)
    {
        if (extraBlacks.Count == 0)
        {
            return;
        }

        foreach (var cell in extraBlacks)
        {
            SetBlackIfSafe(grid, protectedLetters, cell.Row, cell.Col);
        }
    }

    private static void SetBlackIfSafe(
        CrosswordGrid grid,
        HashSet<(int Row, int Col)> protectedLetters,
        int row,
        int col)
    {
        if (protectedLetters.Contains((row, col)))
        {
            return;
        }

        grid.SetBlack(row, col);
    }

    public static List<(int Row, int Col)> BuildExtraBlackCandidates(
        CrosswordGrid grid,
        List<FixedWordPlacement> fixedWords,
        int maxCandidates)
    {
        if (maxCandidates <= 0)
        {
            return new List<(int Row, int Col)>();
        }

        var protectedLetters = BuildProtectedLetters(fixedWords);
        var slots = BuildSlots(grid);
        var candidates = new List<(int Row, int Col)>();
        var seen = new HashSet<(int Row, int Col)>();

        foreach (var slot in slots
                     .OrderByDescending(item => item.Length)
                     .ThenBy(item => item.Row)
                     .ThenBy(item => item.Col)
                     .ThenBy(item => item.Orientation))
        {
            var index = slot.Length / 2;
            var cell = slot.Cells[index];
            if (grid.IsBlack(cell.Row, cell.Col))
            {
                continue;
            }

            if (protectedLetters.Contains(cell))
            {
                continue;
            }

            if (!seen.Add(cell))
            {
                continue;
            }

            candidates.Add(cell);
            if (candidates.Count >= maxCandidates)
            {
                break;
            }
        }

        return candidates;
    }

    public static List<List<(int Row, int Col)>> BuildExtraBlackCandidateSets(
        CrosswordGrid grid,
        List<FixedWordPlacement> fixedWords,
        int maxCandidates)
    {
        var baseList = BuildExtraBlackCandidates(grid, fixedWords, maxCandidates);
        if (baseList.Count == 0)
        {
            return new List<List<(int Row, int Col)>> { baseList };
        }

        var size = grid.Size;
        var centerRow = (size - 1) / 2.0;
        var centerCol = (size - 1) / 2.0;

        var rowMajor = baseList
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Col)
            .ToList();

        var colMajor = baseList
            .OrderBy(cell => cell.Col)
            .ThenBy(cell => cell.Row)
            .ToList();

        var centerFirst = baseList
            .OrderBy(cell => Math.Abs(cell.Row - centerRow) + Math.Abs(cell.Col - centerCol))
            .ThenBy(cell => cell.Row)
            .ThenBy(cell => cell.Col)
            .ToList();

        return new List<List<(int Row, int Col)>>
        {
            baseList,
            rowMajor,
            colMajor,
            centerFirst,
        };
    }

    private static List<Slot> BuildSlots(CrosswordGrid grid)
    {
        var size = grid.Size;
        var minSlotLength = GeneratorSettings.MinSlotLength;
        var slots = new List<Slot>();

        for (var row = 0; row < size; row++)
        {
            var col = 0;
            while (col < size)
            {
                if (grid.IsBlack(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < size && !grid.IsBlack(row, col))
                {
                    col++;
                }

                var length = col - start;
                if (length >= minSlotLength)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (row, start + i);
                    }

                    slots.Add(new Slot(row, start, Orientation.Horizontal, length, cells));
                }
            }
        }

        for (var col = 0; col < size; col++)
        {
            var row = 0;
            while (row < size)
            {
                if (grid.IsBlack(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < size && !grid.IsBlack(row, col))
                {
                    row++;
                }

                var length = row - start;
                if (length >= minSlotLength)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (start + i, col);
                    }

                    slots.Add(new Slot(start, col, Orientation.Vertical, length, cells));
                }
            }
        }

        return slots;
    }

    private static void FixShortRuns(CrosswordGrid grid, HashSet<(int Row, int Col)> lockedBlacks)
    {
        var minSlotLength = GeneratorSettings.MinSlotLength;
        for (var pass = 0; pass < 6; pass++)
        {
            var changed = false;
            changed |= FixShortRuns(grid, lockedBlacks, Orientation.Horizontal, minSlotLength);
            changed |= FixShortRuns(grid, lockedBlacks, Orientation.Vertical, minSlotLength);
            if (!changed)
            {
                break;
            }
        }
    }

    private static bool FixShortRuns(
        CrosswordGrid grid,
        HashSet<(int Row, int Col)> lockedBlacks,
        Orientation orientation,
        int minSlotLength)
    {
        var size = grid.Size;
        var changed = false;

        for (var line = 0; line < size; line++)
        {
            var pos = 0;
            while (pos < size)
            {
                var row = orientation == Orientation.Horizontal ? line : pos;
                var col = orientation == Orientation.Horizontal ? pos : line;
                if (grid.IsBlack(row, col))
                {
                    pos++;
                    continue;
                }

                var start = pos;
                while (pos < size)
                {
                    row = orientation == Orientation.Horizontal ? line : pos;
                    col = orientation == Orientation.Horizontal ? pos : line;
                    if (grid.IsBlack(row, col))
                    {
                        break;
                    }

                    pos++;
                }

                var length = pos - start;
                if (length >= minSlotLength)
                {
                    continue;
                }

                var before = start - 1;
                var after = pos;
                var beforeRow = orientation == Orientation.Horizontal ? line : before;
                var beforeCol = orientation == Orientation.Horizontal ? before : line;
                var afterRow = orientation == Orientation.Horizontal ? line : after;
                var afterCol = orientation == Orientation.Horizontal ? after : line;

                if (before >= 0 && !lockedBlacks.Contains((beforeRow, beforeCol)) && grid.IsBlack(beforeRow, beforeCol))
                {
                    grid.SetEmpty(beforeRow, beforeCol);
                    changed = true;
                }
                else if (after < size && !lockedBlacks.Contains((afterRow, afterCol)) && grid.IsBlack(afterRow, afterCol))
                {
                    grid.SetEmpty(afterRow, afterCol);
                    changed = true;
                }
            }
        }

        return changed;
    }
}

static class RequiredPlacementPlanner
{
    public static List<List<FixedWordPlacement>> BuildPlacementCombos(
        int gridSize,
        IReadOnlyList<string> requiredWords,
        int maxCandidatesPerWord,
        int maxCombos)
    {
        var normalized = requiredWords
            .Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            return new List<List<FixedWordPlacement>> { new List<FixedWordPlacement>() };
        }

        var candidatesByWord = new Dictionary<string, List<(FixedWordPlacement Placement, double Score)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in normalized)
        {
            var candidates = BuildCandidates(word, gridSize)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => DistanceToCenter(gridSize, item.Placement))
                .Take(Math.Max(1, maxCandidatesPerWord))
                .ToList();

            if (candidates.Count == 0)
            {
                return new List<List<FixedWordPlacement>>();
            }

            candidatesByWord[word] = candidates;
        }

        var orderedWords = normalized
            .OrderBy(word => candidatesByWord[word].Count)
            .ToList();

        var combos = new List<List<FixedWordPlacement>>();
        BuildCombos(orderedWords, candidatesByWord, new List<FixedWordPlacement>(), combos, maxCombos, gridSize);
        return combos;
    }

    private static void BuildCombos(
        List<string> words,
        Dictionary<string, List<(FixedWordPlacement Placement, double Score)>> candidatesByWord,
        List<FixedWordPlacement> current,
        List<List<FixedWordPlacement>> combos,
        int maxCombos,
        int gridSize)
    {
        if (combos.Count >= maxCombos)
        {
            return;
        }

        if (current.Count == words.Count)
        {
            combos.Add(new List<FixedWordPlacement>(current));
            return;
        }

        var word = words[current.Count];
        foreach (var candidate in candidatesByWord[word])
        {
            if (!IsCompatibleWithAll(candidate.Placement, current, gridSize))
            {
                continue;
            }

            current.Add(candidate.Placement);
            BuildCombos(words, candidatesByWord, current, combos, maxCombos, gridSize);
            current.RemoveAt(current.Count - 1);

            if (combos.Count >= maxCombos)
            {
                return;
            }
        }
    }

    private static IEnumerable<(FixedWordPlacement Placement, double Score)> BuildCandidates(string word, int gridSize)
    {
        var normalized = WordUtils.Normalize(word);
        if (normalized.Length == 0)
        {
            yield break;
        }

        var length = normalized.Length;
        var maxRow = gridSize - length;
        var maxCol = gridSize - length;

        for (var row = 0; row < gridSize; row++)
        {
            for (var col = 0; col <= maxCol; col++)
            {
                var placement = new FixedWordPlacement(normalized, row, col, Orientation.Horizontal);
                var score = -DistanceToCenter(gridSize, placement);
                yield return (placement, score);
            }
        }

        for (var row = 0; row <= maxRow; row++)
        {
            for (var col = 0; col < gridSize; col++)
            {
                var placement = new FixedWordPlacement(normalized, row, col, Orientation.Vertical);
                var score = -DistanceToCenter(gridSize, placement);
                yield return (placement, score);
            }
        }
    }

    private static bool IsCompatibleWithAll(FixedWordPlacement candidate, List<FixedWordPlacement> placements, int gridSize)
    {
        foreach (var placement in placements)
        {
            if (!AreCompatible(candidate, placement, gridSize))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreCompatible(FixedWordPlacement first, FixedWordPlacement second, int gridSize)
    {
        if (first.Orientation == second.Orientation)
        {
            if (OverlapsSameOrientation(first, second))
            {
                return false;
            }

            if (HasLockedBlackConflict(first, second, gridSize) || HasLockedBlackConflict(second, first, gridSize))
            {
                return false;
            }

            return true;
        }

        var map = BuildLetterMap(first);
        foreach (var cell in EnumerateCells(second))
        {
            if (map.TryGetValue(cell.Cell, out var letter) && letter != cell.Letter)
            {
                return false;
            }
        }

        if (HasLockedBlackConflict(first, second, gridSize) || HasLockedBlackConflict(second, first, gridSize))
        {
            return false;
        }

        return true;
    }

    private static bool OverlapsSameOrientation(FixedWordPlacement first, FixedWordPlacement second)
    {
        foreach (var cell in EnumerateCells(first))
        {
            foreach (var other in EnumerateCells(second))
            {
                if (cell.Cell == other.Cell)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Dictionary<(int Row, int Col), char> BuildLetterMap(FixedWordPlacement placement)
    {
        var map = new Dictionary<(int Row, int Col), char>();
        foreach (var cell in EnumerateCells(placement))
        {
            map[cell.Cell] = cell.Letter;
        }

        return map;
    }

    private static bool HasLockedBlackConflict(FixedWordPlacement source, FixedWordPlacement target, int gridSize)
    {
        foreach (var locked in EnumerateLockedBlacks(source, gridSize))
        {
            foreach (var cell in EnumerateCells(target))
            {
                if (cell.Cell == locked)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<(int Row, int Col)> EnumerateLockedBlacks(FixedWordPlacement placement, int gridSize)
    {
        var word = WordUtils.Normalize(placement.Word);
        if (word.Length == 0)
        {
            yield break;
        }

        if (placement.Orientation == Orientation.Horizontal)
        {
            var before = placement.Col - 1;
            var after = placement.Col + word.Length;
            if (before >= 0)
            {
                yield return (placement.Row, before);
            }

            if (after < gridSize)
            {
                yield return (placement.Row, after);
            }
        }
        else
        {
            var before = placement.Row - 1;
            var after = placement.Row + word.Length;
            if (before >= 0)
            {
                yield return (before, placement.Col);
            }

            if (after < gridSize)
            {
                yield return (after, placement.Col);
            }
        }
    }

    private static IEnumerable<((int Row, int Col) Cell, char Letter)> EnumerateCells(FixedWordPlacement placement)
    {
        var word = WordUtils.Normalize(placement.Word);
        for (var i = 0; i < word.Length; i++)
        {
            var row = placement.Row + (placement.Orientation == Orientation.Vertical ? i : 0);
            var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? i : 0);
            yield return ((row, col), word[i]);
        }
    }

    private static double DistanceToCenter(int size, FixedWordPlacement placement)
    {
        var word = WordUtils.Normalize(placement.Word);
        var midIndex = word.Length / 2.0;
        var row = placement.Row + (placement.Orientation == Orientation.Vertical ? midIndex : 0);
        var col = placement.Col + (placement.Orientation == Orientation.Horizontal ? midIndex : 0);
        var center = (size - 1) / 2.0;
        var dr = row - center;
        var dc = col - center;
        return Math.Abs(dr) + Math.Abs(dc);
    }

    private static List<Slot> BuildSlots(CrosswordGrid grid)
    {
        var size = grid.Size;
        var slots = new List<Slot>();

        for (var row = 0; row < size; row++)
        {
            var col = 0;
            while (col < size)
            {
                if (grid.IsBlack(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < size && !grid.IsBlack(row, col))
                {
                    col++;
                }

                var length = col - start;
                if (length >= 2)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (row, start + i);
                    }

                    slots.Add(new Slot(row, start, Orientation.Horizontal, length, cells));
                }
            }
        }

        for (var col = 0; col < size; col++)
        {
            var row = 0;
            while (row < size)
            {
                if (grid.IsBlack(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < size && !grid.IsBlack(row, col))
                {
                    row++;
                }

                var length = row - start;
                if (length >= 2)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (start + i, col);
                    }

                    slots.Add(new Slot(start, col, Orientation.Vertical, length, cells));
                }
            }
        }

        return slots;
    }
}

record Slot(int Row, int Col, Orientation Orientation, int Length, (int Row, int Col)[] Cells);

sealed class CrosswordSolver
{
    private readonly CrosswordGrid _grid;
    private readonly List<Slot> _slots;
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly Dictionary<int, Dictionary<CharPosKey, List<string>>> _indexByLength;
    private readonly Random _rng;
    private readonly Dictionary<string, int> _useCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly string?[] _assignments;
    private readonly Dictionary<(int Row, int Col, Orientation Orientation), int> _slotLookup;
    private readonly List<FixedWordPlacement> _fixedPlacements;
    private readonly CancellationToken _token;
    private const int MaxReusableLength = 20;

    public CrosswordSolver(CrosswordGrid grid, IReadOnlyCollection<string> words, List<FixedWordPlacement> fixedPlacements, CancellationToken token = default)
    {
        _grid = grid;
        _slots = BuildSlots(grid);
        _assignments = new string?[_slots.Count];
        _wordsByLength = words
            .GroupBy(word => word.Length)
            .ToDictionary(group => group.Key, group => group.ToList());
        _indexByLength = BuildIndex(_wordsByLength);
        _rng = new Random(unchecked(Environment.TickCount ^ (grid.Size * 7919) ^ words.Count));
        _slotLookup = _slots.ToDictionary(
            slot => (slot.Row, slot.Col, slot.Orientation),
            slot => _slots.IndexOf(slot));
        _fixedPlacements = fixedPlacements;
        _token = token;
    }

    private CrosswordSolver(
        CrosswordGrid grid,
        List<Slot> slots,
        Dictionary<int, List<string>> wordsByLength,
        Dictionary<int, Dictionary<CharPosKey, List<string>>> indexByLength,
        Random rng,
        Dictionary<(int Row, int Col, Orientation Orientation), int> slotLookup,
        List<FixedWordPlacement> fixedPlacements,
        string?[] assignments,
        Dictionary<string, int> useCounts,
        CancellationToken token)
    {
        _grid = grid;
        _slots = slots;
        _wordsByLength = wordsByLength;
        _indexByLength = indexByLength;
        _rng = rng;
        _slotLookup = slotLookup;
        _fixedPlacements = fixedPlacements;
        _assignments = assignments;
        _useCounts = useCounts;
        _token = token;
    }

    public bool TrySolve(out List<WordPlacement> placements)
    {
        placements = new List<WordPlacement>();
        if (_token.IsCancellationRequested)
        {
            return false;
        }

        if (!ApplyFixedPlacements())
        {
            return false;
        }

        if (GeneratorSettings.SolverParallelism > 1)
        {
            return TrySolveParallel(out placements);
        }

        if (!Backtrack())
        {
            return false;
        }

        placements = BuildPlacements();
        return true;
    }

    private bool ApplyFixedPlacements()
    {
        foreach (var placement in _fixedPlacements)
        {
            var normalized = WordUtils.Normalize(placement.Word);
            if (!_slotLookup.TryGetValue((placement.Row, placement.Col, placement.Orientation), out var slotIndex))
            {
                return false;
            }

            var slot = _slots[slotIndex];
            if (slot.Length != normalized.Length)
            {
                return false;
            }

            if (!CanFit(slot, normalized))
            {
                return false;
            }

            PlaceWord(slotIndex, normalized, out _);
        }

        return true;
    }

    private bool TrySolveParallel(out List<WordPlacement> placements)
    {
        placements = new List<WordPlacement>();
        var (slotIndex, candidates) = SelectNextSlot();
        if (slotIndex == -2)
        {
            return false;
        }

        if (slotIndex == -1)
        {
            placements = BuildPlacements();
            return true;
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        var best = new object();
        List<WordPlacement>? found = null;
        var options = new ParallelOptions { MaxDegreeOfParallelism = GeneratorSettings.SolverParallelism };
        var cts = new CancellationTokenSource();

        Parallel.ForEach(
            candidates,
            options,
            (candidate, state) =>
            {
                if (_token.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                if (cts.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                var branch = Clone();
                branch.PlaceWord(slotIndex, candidate, out _);
                if (branch.Backtrack())
                {
                    var branchPlacements = branch.BuildPlacements();
                    lock (best)
                    {
                        if (found is null)
                        {
                            found = branchPlacements;
                            cts.Cancel();
                            state.Stop();
                        }
                    }
                }
            });

        if (found is null)
        {
            return false;
        }

        placements = found;
        return true;
    }

    private CrosswordSolver Clone()
    {
        var gridClone = _grid.Clone();
        var assignmentsCopy = new string?[_assignments.Length];
        Array.Copy(_assignments, assignmentsCopy, _assignments.Length);
        var useCountsCopy = new Dictionary<string, int>(_useCounts, StringComparer.OrdinalIgnoreCase);

        return new CrosswordSolver(
            gridClone,
            _slots,
            _wordsByLength,
            _indexByLength,
            _rng,
            _slotLookup,
            _fixedPlacements,
            assignmentsCopy,
            useCountsCopy,
            _token);
    }

    private bool Backtrack()
    {
        if (_token.IsCancellationRequested)
        {
            return false;
        }

        var (slotIndex, candidates) = SelectNextSlot();
        if (slotIndex == -2)
        {
            return false;
        }

        if (slotIndex == -1)
        {
            return true;
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (_token.IsCancellationRequested)
            {
                return false;
            }

            PlaceWord(slotIndex, candidate, out var changedCells);
            if (Backtrack())
            {
                return true;
            }

            UndoWord(slotIndex, candidate, changedCells);
        }

        return false;
    }

    private (int SlotIndex, List<string> Candidates) SelectNextSlot()
    {
        var bestIndex = -1;
        List<string>? bestCandidates = null;
        var bestCount = int.MaxValue;

        for (var i = 0; i < _slots.Count; i++)
        {
            if (_token.IsCancellationRequested)
            {
                return (-2, new List<string>());
            }

            if (_assignments[i] is not null)
            {
                continue;
            }

            var max = bestCount == int.MaxValue ? -1 : Math.Max(0, bestCount - 1);
            var candidates = GetCandidates(_slots[i], max);
            if (candidates.Count == 0)
            {
                return (i, candidates);
            }

            if (candidates.Count < bestCount)
            {
                bestIndex = i;
                bestCandidates = candidates;
                bestCount = candidates.Count;
                if (bestCount == 1)
                {
                    break;
                }
            }
        }

        return (bestIndex, bestCandidates ?? new List<string>());
    }

    private List<string> GetCandidates(Slot slot)
    {
        return GetCandidates(slot, -1);
    }

    private List<string> GetCandidates(Slot slot, int max)
    {
        if (!_wordsByLength.TryGetValue(slot.Length, out var candidates) || candidates.Count == 0)
        {
            return new List<string>();
        }

        // Build constraints from already-filled letters.
        var constraints = new List<(int Pos, char Letter)>(slot.Length);
        var overlap = 0;
        for (var i = 0; i < slot.Length; i++)
        {
            var (row, col) = slot.Cells[i];
            var cell = _grid.GetCell(row, col);
            if (cell == '#')
            {
                return new List<string>();
            }

            if (cell != '\0')
            {
                overlap++;
                constraints.Add((i, cell));
            }
        }

        IEnumerable<string> baseCandidates = candidates;
        if (constraints.Count > 0 &&
            _indexByLength.TryGetValue(slot.Length, out var index))
        {
            List<string>? best = null;
            var bestCount = int.MaxValue;
            foreach (var (pos, letter) in constraints)
            {
                var key = new CharPosKey(pos, letter);
                if (!index.TryGetValue(key, out var list))
                {
                    return new List<string>();
                }

                if (list.Count < bestCount)
                {
                    bestCount = list.Count;
                    best = list;
                }
            }

            if (best is not null)
            {
                baseCandidates = best;
            }
        }
        else if (overlap == 0 && candidates.Count > GeneratorSettings.MaxPlacementCandidates)
        {
            // Unconstrained slot: sample to avoid scanning tens of thousands of words.
            baseCandidates = Sample(candidates, GeneratorSettings.MaxPlacementCandidates);
        }

        var limit = max >= 0 ? max + 1 : int.MaxValue;
        var matches = new List<(string Word, int Score, int Usage, int Quality)>();
        foreach (var word in baseCandidates)
        {
            if (_token.IsCancellationRequested)
            {
                return new List<string>();
            }

            var usage = _useCounts.TryGetValue(word, out var count) ? count : 0;
            if (word.Length > MaxReusableLength && usage > 0)
            {
                continue;
            }

            var score = 0;
            var fits = true;
            for (var i = 0; i < slot.Length; i++)
            {
                var (row, col) = slot.Cells[i];
                var cell = _grid.GetCell(row, col);
                if (cell == '#')
                {
                    fits = false;
                    break;
                }

                if (cell != '\0' && cell != word[i])
                {
                    fits = false;
                    break;
                }

                if (cell != '\0')
                {
                    score++;
                }
            }

            if (!fits)
            {
                continue;
            }

            var quality = WordQuality.GetScore(word);
            matches.Add((word, score, usage, quality));
            if (matches.Count >= limit)
            {
                break;
            }
        }

        return matches
            .OrderBy(item => item.Usage)
            .ThenByDescending(item => item.Quality)
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.Word.Length)
            .ThenBy(item => item.Word, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Word)
            .ToList();
    }

    private List<string> Sample(List<string> list, int limit)
    {
        if (limit <= 0 || list.Count <= limit)
        {
            return list;
        }

        var start = _rng.Next(list.Count);
        var result = new List<string>(limit);
        for (var i = 0; i < list.Count && result.Count < limit; i++)
        {
            result.Add(list[(start + i) % list.Count]);
        }

        return result;
    }

    private static Dictionary<int, Dictionary<CharPosKey, List<string>>> BuildIndex(
        Dictionary<int, List<string>> wordsByLength)
    {
        var index = new Dictionary<int, Dictionary<CharPosKey, List<string>>>();
        foreach (var (length, words) in wordsByLength)
        {
            var map = new Dictionary<CharPosKey, List<string>>();
            foreach (var word in words)
            {
                for (var pos = 0; pos < word.Length; pos++)
                {
                    var key = new CharPosKey(pos, word[pos]);
                    if (!map.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        map[key] = list;
                    }

                    list.Add(word);
                }
            }

            index[length] = map;
        }

        return index;
    }

    private bool CanFit(Slot slot, string word)
    {
        for (var i = 0; i < slot.Length; i++)
        {
            var (row, col) = slot.Cells[i];
            var cell = _grid.GetCell(row, col);
            if (cell == '#')
            {
                return false;
            }

            if (cell != '\0' && cell != word[i])
            {
                return false;
            }
        }

        return true;
    }

    private void PlaceWord(int slotIndex, string word, out List<(int Row, int Col)> changedCells)
    {
        var slot = _slots[slotIndex];
        changedCells = new List<(int Row, int Col)>();
        for (var i = 0; i < slot.Length; i++)
        {
            var (row, col) = slot.Cells[i];
            if (_grid.IsEmpty(row, col))
            {
                _grid.SetLetter(row, col, word[i]);
                changedCells.Add((row, col));
            }
        }

        _assignments[slotIndex] = word;
        if (_useCounts.TryGetValue(word, out var count))
        {
            _useCounts[word] = count + 1;
        }
        else
        {
            _useCounts[word] = 1;
        }
    }

    private void UndoWord(int slotIndex, string word, List<(int Row, int Col)> changedCells)
    {
        foreach (var cell in changedCells)
        {
            _grid.SetEmpty(cell.Row, cell.Col);
        }

        _assignments[slotIndex] = null;
        if (_useCounts.TryGetValue(word, out var count))
        {
            if (count <= 1)
            {
                _useCounts.Remove(word);
            }
            else
            {
                _useCounts[word] = count - 1;
            }
        }
    }

    private List<WordPlacement> BuildPlacements()
    {
        var placements = new List<WordPlacement>();
        for (var i = 0; i < _slots.Count; i++)
        {
            var word = _assignments[i];
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var slot = _slots[i];
            placements.Add(new WordPlacement(word, slot.Row, slot.Col, slot.Orientation));
        }

        return placements;
    }

    private static List<Slot> BuildSlots(CrosswordGrid grid)
    {
        var size = grid.Size;
        var slots = new List<Slot>();

        for (var row = 0; row < size; row++)
        {
            var col = 0;
            while (col < size)
            {
                if (grid.IsBlack(row, col))
                {
                    col++;
                    continue;
                }

                var start = col;
                while (col < size && !grid.IsBlack(row, col))
                {
                    col++;
                }

                var length = col - start;
                if (length >= 2)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (row, start + i);
                    }

                    slots.Add(new Slot(row, start, Orientation.Horizontal, length, cells));
                }
            }
        }

        for (var col = 0; col < size; col++)
        {
            var row = 0;
            while (row < size)
            {
                if (grid.IsBlack(row, col))
                {
                    row++;
                    continue;
                }

                var start = row;
                while (row < size && !grid.IsBlack(row, col))
                {
                    row++;
                }

                var length = row - start;
                if (length >= 2)
                {
                    var cells = new (int Row, int Col)[length];
                    for (var i = 0; i < length; i++)
                    {
                        cells[i] = (start + i, col);
                    }

                    slots.Add(new Slot(start, col, Orientation.Vertical, length, cells));
                }
            }
        }

        return slots;
    }
}

sealed class ProgressTracker
{
    private readonly string _label;
    private readonly long _total;
    private readonly bool _enabled;
    private readonly bool _inline;
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _done;
    private int _lastPercent = -1;
    private long _lastReportMs;
    private int _lastWidth;

    public ProgressTracker(string label, int total, bool enabled, bool inline)
    {
        _label = label;
        _total = Math.Max(1, total);
        _enabled = enabled;
        _inline = inline;
    }

    public void Tick()
    {
        if (!_enabled)
        {
            return;
        }

        var current = Interlocked.Increment(ref _done);
        var percent = (int)(current * 100L / _total);
        if (percent == _lastPercent && percent < 100)
        {
            return;
        }

        var elapsed = _stopwatch.ElapsedMilliseconds;
        if (percent < 100 && elapsed - Interlocked.Read(ref _lastReportMs) < 800)
        {
            return;
        }

        lock (_lock)
        {
            if (percent == _lastPercent && percent < 100)
            {
                return;
            }

            if (percent < 100 && elapsed - _lastReportMs < 800)
            {
                return;
            }

            _lastPercent = percent;
            _lastReportMs = elapsed;
            var message = $"{_label}: {percent}% ({current}/{_total})";
            if (!_inline)
            {
                Console.WriteLine(message);
            }
            else
            {
                _lastWidth = Math.Max(_lastWidth, message.Length);
                if (percent >= 100)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.Write($"\r{message.PadRight(_lastWidth)}");
                }
            }
        }
    }
}

sealed class MilestoneProgress
{
    private readonly string _label;
    private readonly long _total;
    private readonly bool _enabled;
    private readonly object _lock = new();
    private long _done;
    private int _nextPercent = 1;

    public MilestoneProgress(string label, long total, bool enabled)
    {
        _label = label;
        _total = Math.Max(1, total);
        _enabled = enabled;
    }

    public void Tick()
    {
        if (!_enabled || _nextPercent > 100)
        {
            return;
        }

        var done = Interlocked.Increment(ref _done);
        var percent = (int)(done * 100L / _total);
        if (percent < _nextPercent)
        {
            return;
        }

        lock (_lock)
        {
            while (_nextPercent <= 100 && percent >= _nextPercent)
            {
                PhaseLogger.Write($"{_label}: {_nextPercent}%");
                _nextPercent += 1;
            }
        }
    }
}

static class WordUtils
{
    public static string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var trimmed = word.Trim();
        var normalized = trimmed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (ch is 'Œ' or 'œ')
            {
                builder.Append("OE");
                continue;
            }

            if (ch is 'Æ' or 'æ')
            {
                builder.Append("AE");
                continue;
            }

            if (!char.IsLetter(ch))
            {
                continue;
            }

            var upper = char.ToUpperInvariant(ch);
            // Keep only ASCII A-Z so we don't end up with Greek/Cyrillic "words" in the grid.
            if (upper is >= 'A' and <= 'Z')
            {
                builder.Append(upper);
            }
        }

        return builder.ToString();
    }
}

static class ClueOverrides
{
    public static void Apply(Dictionary<string, string> clues)
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DE"] = "Preposition dans 'fete de famille'",
            ["DU"] = "Contraction dans 'cadeau du jour'",
            ["ET"] = "Lien dans 'gateau et bougies'",
            ["EN"] = "Preposition dans 'en famille'",
            ["LE"] = "Article dans 'le gateau'",
            ["LES"] = "Article dans 'les invites'",
            ["UN"] = "Article dans 'un cadeau'",
            ["UNE"] = "Article dans 'une surprise'",
            ["AU"] = "Contraction dans 'au buffet'",
            ["AUX"] = "Contraction dans 'aux invites'",
            ["SUR"] = "Preposition dans 'bougies sur le gateau'",
            ["PAR"] = "Preposition dans 'par surprise'",
            ["POUR"] = "Preposition dans 'pour toi'",
            ["SON"] = "Determinant dans 'son anniversaire'",
            ["SA"] = "Determinant dans 'sa fete'",
            ["SES"] = "Determinant dans 'ses cadeaux'",
            ["TON"] = "Determinant dans 'ton anniversaire'",
            ["TA"] = "Determinant dans 'ta fete'",
            ["TES"] = "Determinant dans 'tes bougies'",
            ["OR"] = "Metal d'un cadeau precieux",
            ["NI"] = "Coordination dans 'ni retard ni oubli'",
            ["CI"] = "Demonstratif dans 'ceci est le gateau'",
            ["CA"] = "Demonstratif dans 'ca commence par un gateau'",
            ["VA"] = "Se dit quand tout va bien a la fete",
            ["VU"] = "Vu sur les photos de la fete",
            ["IL"] = "Pronom dans 'il souffle les bougies'",
            ["ON"] = "Pronom dans 'on chante'",
            ["OU"] = "Choix dans 'cadeau ou surprise'",
            ["SE"] = "Pronom dans 'se reunir'",
            ["CE"] = "Determinant dans 'ce jour de fete'",
            ["NE"] = "Negation dans 'ne pas oublier'",
            ["MA"] = "Determinant dans 'ma fete'",
            ["ME"] = "Pronom dans 'tu me felicites'",
            ["TE"] = "Pronom dans 'je te souhaite'",
            ["MOI"] = "Pronom dans 'moi, je souffle'",
            ["TOI"] = "Pronom dans 'toi, l'invite'",
            ["LUI"] = "Pronom dans 'lui offrir'",
            ["ELLE"] = "Pronom dans 'elle fete'",
            ["ELLES"] = "Pronom dans 'elles chantent'",
            ["DO"] = "Note du chant d'anniversaire",
            ["RE"] = "Note du chant d'anniversaire",
            ["MI"] = "Note du chant d'anniversaire",
            ["FA"] = "Note du chant d'anniversaire",
            ["SOL"] = "Note du chant d'anniversaire",
            ["LA"] = "Note du chant d'anniversaire",
            ["SI"] = "Note du chant d'anniversaire",
            ["BON"] = "Souhait dans 'bon anniversaire'",
            ["BIS"] = "On le crie pour rejouer la chanson",
            ["VIE"] = "Ce que l'on celebre chaque annee",
            ["NEE"] = "Qui fete sa naissance aujourd'hui",
            ["FEE"] = "Personnage de conte pour une fete",
            ["GAG"] = "Blague pour l'ambiance",
            ["OUI"] = "Reponse a une invitation",
            ["NON"] = "Reponse a un empechement",
        };

        foreach (var entry in overrides)
        {
            clues[entry.Key] = entry.Value;
        }
    }
}

static class HunspellWordLibrary
{
    public static IReadOnlyCollection<string> LoadFromHunspell(string rootDirectory, string locale, string cacheRoot)
    {
        var dicPath = Path.Combine(rootDirectory, $"{locale}.dic");
        var affPath = Path.Combine(rootDirectory, $"{locale}.aff");
        return LoadFromHunspellFiles(dicPath, affPath, cacheRoot, locale);
    }

    public static IReadOnlyCollection<string> LoadFromHunspellFiles(string dicPath, string affPath)
    {
        if (!File.Exists(dicPath))
        {
            return Array.Empty<string>();
        }

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isFirstLine = true;

        foreach (var line in File.ReadLines(dicPath))
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                if (int.TryParse(line.Trim(), out _))
                {
                    continue;
                }
            }

            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var separators = new[] { '/', ' ', '\t' };
            var raw = trimmed.Split(separators, 2)[0];
            var normalized = WordUtils.Normalize(raw);
            if (normalized.Length < 2)
            {
                continue;
            }

            words.Add(normalized);
        }

        if (words.Count == 0)
        {
            return Array.Empty<string>();
        }

        return words;
    }

    private static IReadOnlyCollection<string> LoadFromHunspellFiles(
        string dicPath,
        string affPath,
        string cacheRoot,
        string locale)
    {
        if (!GeneratorSettings.UseCache)
        {
            return LoadFromHunspellFiles(dicPath, affPath);
        }

        var cacheDir = Path.Combine(cacheRoot, "hunspell");
        Directory.CreateDirectory(cacheDir);
        var cachePath = Path.Combine(cacheDir, $"hunspell.{locale}.txt");
        var metaPath = Path.Combine(cacheDir, $"hunspell.{locale}.meta.json");

        var dicTicks = File.Exists(dicPath) ? File.GetLastWriteTimeUtc(dicPath).Ticks : 0;
        var affTicks = File.Exists(affPath) ? File.GetLastWriteTimeUtc(affPath).Ticks : 0;

        var meta = LoadMeta(metaPath);
        if (meta is not null &&
            meta.DicTicks == dicTicks &&
            meta.AffTicks == affTicks &&
            File.Exists(cachePath))
        {
            var cached = LoadWordsFromCache(cachePath);
            if (cached.Count > 0)
            {
                return cached;
            }
        }

        var words = LoadFromHunspellFiles(dicPath, affPath).ToList();
        if (words.Count > 0)
        {
            WriteCache(cachePath, words);
            WriteMeta(metaPath, new HunspellCacheMeta(dicTicks, affTicks));
        }

        return words;
    }

    private static IReadOnlyCollection<string> LoadWordsFromCache(string cachePath)
    {
        if (!File.Exists(cachePath))
        {
            return Array.Empty<string>();
        }

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(cachePath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 2)
            {
                continue;
            }

            words.Add(trimmed);
        }

        return words;
    }

    private static void WriteCache(string cachePath, List<string> words)
    {
        try
        {
            File.WriteAllLines(cachePath, words);
        }
        catch
        {
        }
    }

    private static HunspellCacheMeta? LoadMeta(string metaPath)
    {
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metaPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<HunspellCacheMeta>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteMeta(string metaPath, HunspellCacheMeta meta)
    {
        try
        {
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metaPath, json);
        }
        catch
        {
        }
    }

    private record HunspellCacheMeta(long DicTicks, long AffTicks);

    public static IReadOnlyCollection<string> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        var rawWords = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(WordUtils.Normalize)
            .Where(word => word.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rawWords.Count == 0)
        {
            return Array.Empty<string>();
        }

        try
        {
            var wordList = WordList.CreateFromWords(rawWords);
            var filtered = new List<string>(rawWords.Count);
            foreach (var word in rawWords)
            {
                if (wordList.Check(word))
                {
                    filtered.Add(word);
                }
            }

            return filtered;
        }
        catch
        {
            return rawWords;
        }
    }
}
