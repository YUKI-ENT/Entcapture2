using ENTcapture2.Core.Models;

namespace ENTcapture2.Core.Services;

public interface ISettingsStore
{
    string SettingsFilePath { get; }

    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
