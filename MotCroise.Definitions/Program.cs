using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

var resultDbPath = Settings.ResultDbPath;
var defsDbPath = Settings.DefinitionsDbPath;
var jsonlPath = Settings.JsonlPath;

if (string.IsNullOrWhiteSpace(defsDbPath))
{
    Console.WriteLine("Definitions DB introuvable. Definir MOTCROISE_DEFS_DB.");
    return;
}

if (!string.IsNullOrWhiteSpace(jsonlPath))
{
    if (!File.Exists(jsonlPath))
    {
        Console.WriteLine($"JSONL introuvable: {jsonlPath}");
        return;
    }

    Console.WriteLine($"Defs DB: {defsDbPath}");
    Console.WriteLine($"JSONL: {jsonlPath}");

    var defsDbImport = DefinitionDb.TryOpen(defsDbPath);
    if (defsDbImport is null)
    {
        Console.WriteLine("Impossible d'ouvrir le SQLite definitions.");
        return;
    }

    JsonlImporter.Import(jsonlPath, defsDbImport);
    return;
}

if (string.IsNullOrWhiteSpace(resultDbPath))
{
    Console.WriteLine("Result DB introuvable. Definir MOTCROISE_RESULT_DB.");
    return;
}

Console.WriteLine($"Result DB: {resultDbPath}");
Console.WriteLine($"Defs DB: {defsDbPath}");
Console.WriteLine($"DEF_PARALLEL={Settings.DefinitionParallelism}, HTTP_MAX_CONN={Settings.HttpMaxConnections}, HTTP_TIMEOUT_SEC={Settings.HttpTimeoutSeconds}");

var resultDb = ResultDb.TryOpen(resultDbPath);
if (resultDb is null)
{
    Console.WriteLine("Impossible d'ouvrir le resultat SQLite.");
    return;
}

var defsDbFetch = DefinitionDb.TryOpen(defsDbPath);
if (defsDbFetch is null)
{
    Console.WriteLine("Impossible d'ouvrir le SQLite definitions.");
    return;
}

var words = resultDb.LoadWords();
if (words.Count == 0)
{
    Console.WriteLine("Aucun mot trouve dans le resultat.");
    return;
}

var missing = words
    .Where(word => !defsDbFetch.HasDefinition(word))
    .ToList();

Console.WriteLine($"Mots total: {words.Count}, sans definition: {missing.Count}");
if (missing.Count == 0)
{
    Console.WriteLine("Rien a faire.");
    return;
}

var fetcher = new DefinitionFetcher();
var stats = new DefinitionStats();

var options = new ParallelOptions { MaxDegreeOfParallelism = Settings.DefinitionParallelism };
Parallel.ForEach(missing, options, word =>
{
    if (fetcher.TryFetch(word, out var definition, stats))
    {
        defsDbFetch.Upsert(word, definition);
    }
});

Console.WriteLine($"Defs ok={stats.Success}, fail={stats.Failures}, timeouts={stats.Timeouts}, exceptions={stats.Exceptions}");
Console.WriteLine($"HTTP statuses: {stats.FormatStatusCounts(8)}");

sealed class ResultDb : IDisposable
{
    private readonly SqliteConnection _connection;

    private ResultDb(string dbPath)
    {
        _connection = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        _connection.Open();
    }

    public static ResultDb? TryOpen(string dbPath)
    {
        if (!System.IO.File.Exists(dbPath))
        {
            return null;
        }

        try
        {
            return new ResultDb(dbPath);
        }
        catch
        {
            return null;
        }
    }

    public HashSet<string> LoadWords()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT word FROM placements;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var word = reader.GetString(0);
            var normalized = WordUtils.Normalize(word);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

sealed class DefinitionDb : IDisposable
{
    private readonly SqliteConnection _connection;

    private DefinitionDb(string dbPath)
    {
        var directory = System.IO.Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection($"Data Source={dbPath};Cache=Shared");
        _connection.Open();
        EnsureSchema();
    }

    public static DefinitionDb? TryOpen(string dbPath)
    {
        try
        {
            return new DefinitionDb(dbPath);
        }
        catch
        {
            return null;
        }
    }

    public bool HasDefinition(string word)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM definitions WHERE word = $word LIMIT 1;";
        command.Parameters.AddWithValue("$word", word);
        var result = command.ExecuteScalar();
        return result is not null;
    }

    public void Upsert(string word, string definition)
    {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(definition))
        {
            return;
        }

        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO definitions(word, definition, updated_utc, word_length) VALUES ($word, $definition, $updated, $length);";
        command.Parameters.AddWithValue("$word", word);
        command.Parameters.AddWithValue("$definition", definition);
        command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$length", word.Length);
        command.ExecuteNonQuery();
    }

    public bool InsertIfMissing(string word, string definition)
    {
        if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(definition))
        {
            return false;
        }

        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO definitions(word, definition, updated_utc, word_length) VALUES ($word, $definition, $updated, $length);";
        command.Parameters.AddWithValue("$word", word);
        command.Parameters.AddWithValue("$definition", definition);
        command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$length", word.Length);
        return command.ExecuteNonQuery() > 0;
    }

    public int InsertBatch(List<(string Word, string Definition)> entries)
    {
        if (entries.Count == 0)
        {
            return 0;
        }

        using var transaction = _connection.BeginTransaction();
        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO definitions(word, definition, updated_utc, word_length) VALUES ($word, $definition, $updated, $length);";

        var wordParam = command.CreateParameter();
        wordParam.ParameterName = "$word";
        command.Parameters.Add(wordParam);

        var definitionParam = command.CreateParameter();
        definitionParam.ParameterName = "$definition";
        command.Parameters.Add(definitionParam);

        var updatedParam = command.CreateParameter();
        updatedParam.ParameterName = "$updated";
        command.Parameters.Add(updatedParam);

        var lengthParam = command.CreateParameter();
        lengthParam.ParameterName = "$length";
        command.Parameters.Add(lengthParam);

        var inserted = 0;
        var updated = DateTime.UtcNow.ToString("O");
        foreach (var entry in entries)
        {
            wordParam.Value = entry.Word;
            definitionParam.Value = entry.Definition;
            updatedParam.Value = updated;
            lengthParam.Value = entry.Word.Length;
            inserted += command.ExecuteNonQuery();
        }

        transaction.Commit();
        return inserted;
    }

    public void Dispose()
    {
        _connection.Dispose();
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

        Console.WriteLine("Defs DB: remplissage colonne word_length (SQLite).");
        using var update = _connection.CreateCommand();
        update.CommandText = "UPDATE definitions SET word_length = length(word) WHERE word_length IS NULL OR word_length = 0;";
        update.ExecuteNonQuery();
    }
}

sealed class DefinitionFetcher
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public bool TryFetch(string word, out string definition, DefinitionStats stats)
    {
        definition = string.Empty;
        if (string.IsNullOrWhiteSpace(word))
        {
            stats.Failures.Increment();
            return false;
        }

        var candidates = new[]
        {
            word.ToLowerInvariant(),
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant(),
            word.ToLowerInvariant().Replace("'", string.Empty),
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (TryFetchFromMediaWiki(candidate, out definition, stats))
            {
                stats.Success.Increment();
                return true;
            }

            if (TryFetchFromWiktionaryRest(candidate, out definition, stats))
            {
                stats.Success.Increment();
                return true;
            }
        }

        stats.Failures.Increment();
        return false;
    }

    private bool TryFetchFromWiktionaryRest(string word, out string definition, DefinitionStats stats)
    {
        definition = string.Empty;
        try
        {
            var encoded = Uri.EscapeDataString(word);
            var url = $"https://fr.wiktionary.org/api/rest_v1/page/definition/{encoded}";
            using var response = HttpClient.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                stats.RegisterStatus((int)response.StatusCode);
                return false;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("fr", out var frNode) ||
                frNode.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

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

                    definition = CleanDefinition(text);
                    return !string.IsNullOrWhiteSpace(definition);
                }
            }
        }
        catch (TaskCanceledException)
        {
            stats.Timeouts.Increment();
        }
        catch (OperationCanceledException)
        {
            stats.Timeouts.Increment();
        }
        catch
        {
            stats.Exceptions.Increment();
        }

        return false;
    }

    private bool TryFetchFromMediaWiki(string word, out string definition, DefinitionStats stats)
    {
        definition = string.Empty;
        try
        {
            var encoded = Uri.EscapeDataString(word);
            var url = $"https://fr.wiktionary.org/w/api.php?action=query&format=json&prop=extracts&exintro=1&explaintext=1&exsentences=1&titles={encoded}";
            using var response = HttpClient.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                stats.RegisterStatus((int)response.StatusCode);
                return false;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("query", out var queryNode) ||
                !queryNode.TryGetProperty("pages", out var pagesNode) ||
                pagesNode.ValueKind != JsonValueKind.Object)
            {
                return false;
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

                definition = CleanDefinition(extract);
                return !string.IsNullOrWhiteSpace(definition);
            }
        }
        catch (TaskCanceledException)
        {
            stats.Timeouts.Increment();
        }
        catch (OperationCanceledException)
        {
            stats.Timeouts.Increment();
        }
        catch
        {
            stats.Exceptions.Increment();
        }

        return false;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = Settings.HttpMaxConnections,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = true,
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Settings.HttpTimeoutSeconds),
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("MotCroiseDefinitions/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr");
        client.DefaultRequestVersion = Settings.HttpRequestVersion;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        return client;
    }

    public static string CleanDefinition(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        trimmed = trimmed.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        while (trimmed.Contains("  ", StringComparison.Ordinal))
        {
            trimmed = trimmed.Replace("  ", " ", StringComparison.Ordinal);
        }

        return trimmed;
    }
}

static class JsonlImporter
{
    private const int MinLen = 2;
    private const int MaxLen = 24;

    public static void Import(string path, DefinitionDb db)
    {
        var stopwatch = Stopwatch.StartNew();
        long total = 0;
        long added = 0;
        long skippedLang = 0;
        long skippedWord = 0;
        long skippedGloss = 0;
        long invalidJson = 0;

        Console.WriteLine($"JSONL import: parallel={Settings.JsonlParallelism}, batch={Settings.JsonlBatchSize}, buffer={Settings.JsonlBuffer}");

        var lineBuffer = new BlockingCollection<string>(Settings.JsonlBuffer);
        var entryBuffer = new BlockingCollection<(string Word, string Definition)>(Settings.JsonlBuffer);

        var writer = Task.Run(() =>
        {
            var batch = new List<(string Word, string Definition)>(Settings.JsonlBatchSize);
            foreach (var entry in entryBuffer.GetConsumingEnumerable())
            {
                batch.Add(entry);
                if (batch.Count >= Settings.JsonlBatchSize)
                {
                    var count = db.InsertBatch(batch);
                    Interlocked.Add(ref added, count);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var count = db.InsertBatch(batch);
                Interlocked.Add(ref added, count);
            }
        });

        var parserTasks = new List<Task>();
        for (var i = 0; i < Settings.JsonlParallelism; i++)
        {
            parserTasks.Add(Task.Run(() =>
            {
                foreach (var line in lineBuffer.GetConsumingEnumerable())
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        if (!root.TryGetProperty("lang_code", out var langNode) ||
                            langNode.GetString() != "fr")
                        {
                            Interlocked.Increment(ref skippedLang);
                            continue;
                        }

                        if (!root.TryGetProperty("word", out var wordNode))
                        {
                            Interlocked.Increment(ref skippedWord);
                            continue;
                        }

                        var rawWord = wordNode.GetString();
                        var normalized = WordUtils.Normalize(rawWord ?? string.Empty);
                        if (normalized.Length < MinLen || normalized.Length > MaxLen)
                        {
                            Interlocked.Increment(ref skippedWord);
                            continue;
                        }

                        if (!TryExtractGloss(root, out var gloss))
                        {
                            Interlocked.Increment(ref skippedGloss);
                            continue;
                        }

                        var cleaned = DefinitionFetcher.CleanDefinition(gloss);
                        if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            Interlocked.Increment(ref skippedGloss);
                            continue;
                        }

                        entryBuffer.Add((normalized, cleaned));
                    }
                    catch
                    {
                        Interlocked.Increment(ref invalidJson);
                    }
                }
            }));
        }

        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineBuffer.Add(line);
            var current = Interlocked.Increment(ref total);
            if (current % 100000 == 0)
            {
                Console.WriteLine($"JSONL: lines={current}, added={Interlocked.Read(ref added)}, skipped_lang={Interlocked.Read(ref skippedLang)}, skipped_word={Interlocked.Read(ref skippedWord)}, skipped_gloss={Interlocked.Read(ref skippedGloss)}, invalid={Interlocked.Read(ref invalidJson)}");
            }
        }

        lineBuffer.CompleteAdding();
        Task.WaitAll(parserTasks.ToArray());
        entryBuffer.CompleteAdding();
        writer.Wait();

        stopwatch.Stop();
        Console.WriteLine($"JSONL done: lines={total}, added={added}, skipped_lang={skippedLang}, skipped_word={skippedWord}, skipped_gloss={skippedGloss}, invalid={invalidJson}, time={stopwatch.Elapsed:g}");
    }

    private static bool TryExtractGloss(JsonElement root, out string gloss)
    {
        gloss = string.Empty;
        if (!root.TryGetProperty("senses", out var senses) ||
            senses.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var sense in senses.EnumerateArray())
        {
            if (!sense.TryGetProperty("glosses", out var glosses) ||
                glosses.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in glosses.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var text = entry.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    gloss = text;
                    return true;
                }
            }
        }

        return false;
    }
}

sealed class DefinitionStats
{
    public Counter Success { get; } = new();
    public Counter Failures { get; } = new();
    public Counter Timeouts { get; } = new();
    public Counter Exceptions { get; } = new();
    private readonly ConcurrentDictionary<int, int> _statusCounts = new();

    public void RegisterStatus(int status)
    {
        _statusCounts.AddOrUpdate(status, 1, (_, count) => count + 1);
    }

    public string FormatStatusCounts(int maxItems)
    {
        var items = _statusCounts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .Take(maxItems)
            .Select(entry => $"{entry.Key}={entry.Value}")
            .ToList();

        return items.Count == 0 ? "none" : string.Join(", ", items);
    }
}

sealed class Counter
{
    private long _value;

    public long Value => Interlocked.Read(ref _value);

    public void Increment()
    {
        Interlocked.Increment(ref _value);
    }

    public static implicit operator long(Counter counter) => counter.Value;
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
        var builder = new System.Text.StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (ch is '\u0152' or '\u0153')
            {
                builder.Append("OE");
                continue;
            }

            if (ch is '\u00C6' or '\u00E6')
            {
                builder.Append("AE");
                continue;
            }

            if (char.IsLetter(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        return builder.ToString();
    }
}

static class Settings
{
    public static string ResultDbPath =>
        Environment.GetEnvironmentVariable("MOTCROISE_RESULT_DB") ?? string.Empty;

    public static string DefinitionsDbPath =>
        Environment.GetEnvironmentVariable("MOTCROISE_DEFS_DB") ?? string.Empty;

    public static string JsonlPath =>
        Environment.GetEnvironmentVariable("MOTCROISE_JSONL") ?? string.Empty;

    public static int DefinitionParallelism => ReadEnvInt("MOTCROISE_DEF_PARALLEL", 32, 1, 256);

    public static int HttpMaxConnections => ReadEnvInt("MOTCROISE_HTTP_MAX_CONN", 64, 4, 512);

    public static int HttpTimeoutSeconds => ReadEnvInt("MOTCROISE_HTTP_TIMEOUT_SEC", 15, 2, 60);

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

    private static int ReadEnvInt(string name, int defaultValue, int min, int max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        return Math.Clamp(value, min, max);
    }

    public static int JsonlParallelism => ReadEnvInt("MOTCROISE_JSONL_PARALLEL", Math.Max(2, Environment.ProcessorCount / 2), 1, 64);

    public static int JsonlBatchSize => ReadEnvInt("MOTCROISE_JSONL_BATCH", 2000, 100, 20000);

    public static int JsonlBuffer => ReadEnvInt("MOTCROISE_JSONL_BUFFER", 50000, 1000, 500000);
}
