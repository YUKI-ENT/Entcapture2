using ENTcapture2.Core.Models;

namespace ENTcapture2.Core.Legacy;

public sealed class LegacySettingsImportResult
{
    public required ApplicationSettings Settings { get; init; }

    public List<string> Warnings { get; } = [];
}
