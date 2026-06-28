using System.Text.Json;
using ENTcapture2.Core.Models;
using ENTcapture2.Core.Services;
using Microsoft.Data.Sqlite;

namespace ENTcapture2.WinForms.Integration;

internal sealed class PatientMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _databasePath;
    private bool _initialized;

    public PatientMetadataStore(string? databasePath = null)
    {
        _databasePath = databasePath ?? GetDefaultDatabasePath();
        if (databasePath is null)
        {
            CopyLegacyDatabaseIfNeeded();
        }
    }

    public async Task UpsertPatientAsync(
        string patientId,
        string patientName,
        CancellationToken cancellationToken = default)
    {
        patientId = patientId.Trim();
        patientName = patientName.Trim();
        if (patientId.Length == 0)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken);
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Patients (PatientId, PatientName, LastSeenAt, CreatedAt, UpdatedAt)
            VALUES ($patientId, $patientName, $now, $now, $now)
            ON CONFLICT(PatientId) DO UPDATE SET
                PatientName = CASE
                    WHEN excluded.PatientName <> '' THEN excluded.PatientName
                    ELSE Patients.PatientName
                END,
                LastSeenAt = excluded.LastSeenAt,
                UpdatedAt = excluded.UpdatedAt;
            """;
        string now = DateTimeOffset.Now.ToString("O");
        command.Parameters.AddWithValue("$patientId", patientId);
        command.Parameters.AddWithValue("$patientName", patientName);
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> FindPatientNameAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        patientId = patientId.Trim();
        if (patientId.Length == 0)
        {
            return null;
        }

        await EnsureInitializedAsync(cancellationToken);
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT PatientName FROM Patients WHERE PatientId = $patientId;";
        command.Parameters.AddWithValue("$patientId", patientId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task RecordFileAsync(
        string filePath,
        string patientId,
        string patientName,
        string examinationName,
        DateTime capturedAt,
        CapturePreset? preset,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        patientId = patientId.Trim();
        patientName = patientName.Trim();
        examinationName = examinationName.Trim();
        if (patientId.Length > 0)
        {
            await UpsertPatientAsync(patientId, patientName, cancellationToken);
        }

        await EnsureInitializedAsync(cancellationToken);
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO CapturedFiles
                (FilePath, FileName, PatientId, PatientName, ExaminationName,
                 CapturedAt, PresetId, PresetName, PresetJson, CreatedAt, UpdatedAt)
            VALUES
                ($filePath, $fileName, $patientId, $patientName, $examinationName,
                 $capturedAt, $presetId, $presetName, $presetJson, $now, $now)
            ON CONFLICT(FilePath) DO UPDATE SET
                FileName = excluded.FileName,
                PatientId = excluded.PatientId,
                PatientName = excluded.PatientName,
                ExaminationName = excluded.ExaminationName,
                CapturedAt = excluded.CapturedAt,
                PresetId = excluded.PresetId,
                PresetName = excluded.PresetName,
                PresetJson = excluded.PresetJson,
                UpdatedAt = excluded.UpdatedAt;
            """;
        string fullPath = Path.GetFullPath(filePath);
        string now = DateTimeOffset.Now.ToString("O");
        command.Parameters.AddWithValue("$filePath", fullPath);
        command.Parameters.AddWithValue("$fileName", Path.GetFileName(fullPath));
        command.Parameters.AddWithValue("$patientId", patientId);
        command.Parameters.AddWithValue("$patientName", patientName);
        command.Parameters.AddWithValue("$examinationName", examinationName);
        command.Parameters.AddWithValue("$capturedAt", capturedAt.ToString("O"));
        command.Parameters.AddWithValue("$presetId", preset?.Id.ToString("D") ?? string.Empty);
        command.Parameters.AddWithValue("$presetName", preset?.Name ?? string.Empty);
        command.Parameters.AddWithValue(
            "$presetJson",
            preset is null ? string.Empty : JsonSerializer.Serialize(preset, JsonOptions));
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<CapturedFileMetadata?> FindFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        await EnsureInitializedAsync(cancellationToken);
        string fullPath = Path.GetFullPath(filePath);
        CapturedFileMetadata? metadata = await FindByColumnAsync(
            "FilePath",
            fullPath,
            cancellationToken);
        if (metadata is not null)
        {
            return metadata;
        }

        return await FindByColumnAsync(
            "FileName",
            Path.GetFileName(fullPath),
            cancellationToken);
    }

    public static ParsedCaptureFileName? ParseCaptureFileName(string filePath)
    {
        string stem = Path.GetFileNameWithoutExtension(filePath);
        string[] parts = stem.Split('~');
        if (parts.Length < 5)
        {
            return null;
        }

        string patientId = parts[0].Trim();
        string examinationName = parts[3].Trim();
        if (patientId.Length == 0)
        {
            return null;
        }

        DateTime? capturedAt = DateTime.TryParseExact(
            parts[2],
            "yyyy_MM_dd",
            null,
            System.Globalization.DateTimeStyles.None,
            out DateTime date)
                ? date
                : null;
        return new ParsedCaptureFileName(
            patientId,
            examinationName,
            capturedAt);
    }

    private async Task<CapturedFileMetadata?> FindByColumnAsync(
        string columnName,
        string value,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT FilePath, PatientId, PatientName, ExaminationName,
                   CapturedAt, PresetId, PresetName, PresetJson
            FROM CapturedFiles
            WHERE {columnName} = $value
            ORDER BY UpdatedAt DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$value", value);
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        CapturePreset? preset = null;
        string presetJson = reader.GetString(7);
        if (!string.IsNullOrWhiteSpace(presetJson))
        {
            try
            {
                preset = JsonSerializer.Deserialize<CapturePreset>(
                    presetJson,
                    JsonOptions);
            }
            catch (JsonException)
            {
            }
        }

        return new CapturedFileMetadata(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTime.TryParse(reader.GetString(4), out DateTime capturedAt)
                ? capturedAt
                : null,
            Guid.TryParse(reader.GetString(5), out Guid presetId)
                ? presetId
                : null,
            reader.GetString(6),
            preset);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Patients (
                PatientId TEXT PRIMARY KEY,
                PatientName TEXT NOT NULL DEFAULT '',
                LastSeenAt TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CapturedFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FilePath TEXT NOT NULL UNIQUE,
                FileName TEXT NOT NULL,
                PatientId TEXT NOT NULL DEFAULT '',
                PatientName TEXT NOT NULL DEFAULT '',
                ExaminationName TEXT NOT NULL DEFAULT '',
                CapturedAt TEXT NOT NULL DEFAULT '',
                PresetId TEXT NOT NULL DEFAULT '',
                PresetName TEXT NOT NULL DEFAULT '',
                PresetJson TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_CapturedFiles_FileName
                ON CapturedFiles(FileName);
            CREATE INDEX IF NOT EXISTS IX_CapturedFiles_PatientId
                ON CapturedFiles(PatientId);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        _initialized = true;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath
            }.ToString());
        connection.Open();
        return connection;
    }

    private static string GetDefaultDatabasePath()
    {
        return AppDataPaths.DatabaseFilePath;
    }

    private static void CopyLegacyDatabaseIfNeeded()
    {
        if (File.Exists(AppDataPaths.DatabaseFilePath) ||
            !File.Exists(AppDataPaths.LegacyDatabaseFilePath))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(AppDataPaths.DatabaseFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(
            AppDataPaths.LegacyDatabaseFilePath,
            AppDataPaths.DatabaseFilePath);
    }
}

internal sealed record CapturedFileMetadata(
    string FilePath,
    string PatientId,
    string PatientName,
    string ExaminationName,
    DateTime? CapturedAt,
    Guid? PresetId,
    string PresetName,
    CapturePreset? Preset);

internal sealed record ParsedCaptureFileName(
    string PatientId,
    string ExaminationName,
    DateTime? CapturedAt);
