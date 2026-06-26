using System.Text;

namespace ENTcapture2.WinForms.Integration;

public sealed class RsBaseTheptWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;
    private string _directory = string.Empty;
    private string? _lastLine;
    private CancellationTokenSource? _readCancellation;

    public event Action<string, string>? PatientChanged;

    public void Start(string directory)
    {
        Stop();
        _directory = directory;
        if (!Directory.Exists(directory))
        {
            return;
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _watcher = new FileSystemWatcher(directory, "thept.txt")
        {
            NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        _watcher.Changed += FileChanged;
        _watcher.Created += FileChanged;
        _watcher.Renamed += FileChanged;
        _ = ReadAsync();
    }

    public void Dispose()
    {
        Stop();
    }

    private void Stop()
    {
        _readCancellation?.Cancel();
        _readCancellation?.Dispose();
        _readCancellation = null;
        _watcher?.Dispose();
        _watcher = null;
        _lastLine = null;
    }

    private void FileChanged(object sender, FileSystemEventArgs e)
    {
        _ = ReadAsync();
    }

    private async Task ReadAsync()
    {
        _readCancellation?.Cancel();
        _readCancellation?.Dispose();
        _readCancellation = new CancellationTokenSource();
        CancellationToken token = _readCancellation.Token;
        string path = Path.Combine(_directory, "thept.txt");

        try
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    await using var stream = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);
                    using var reader = new StreamReader(
                        stream,
                        Encoding.GetEncoding(932),
                        false);
                    string? line = await reader.ReadLineAsync(token);
                    if (string.IsNullOrWhiteSpace(line) ||
                        string.Equals(line, _lastLine, StringComparison.Ordinal))
                    {
                        return;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length < 3)
                    {
                        return;
                    }

                    string id = parts[1].Trim();
                    string name = parts[2].Replace("\"", string.Empty).Trim();
                    if (id.Length == 0)
                    {
                        return;
                    }

                    _lastLine = line;
                    PatientChanged?.Invoke(id, name);
                    return;
                }
                catch (Exception) when (attempt < 9)
                {
                    await Task.Delay(120, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
