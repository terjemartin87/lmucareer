namespace LmuCareerTool.Watching;

/// <summary>
/// Overvåker LMUs Results-mappe og varsler når en ny .xml-fil er ferdig skrevet
/// (LMU kan trigge filsystem-eventet før skrivingen faktisk er ferdig, så vi
/// venter til filen kan åpnes eksklusivt før vi sier ifra).
/// </summary>
public class ResultsWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public event Action<string>? NewResultFile;

    public ResultsWatcher(string resultsFolder)
    {
        if (!Directory.Exists(resultsFolder))
            throw new DirectoryNotFoundException($"Fant ikke mappen: {resultsFolder}");

        _watcher = new FileSystemWatcher(resultsFolder, "*.xml")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = false
        };
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        // Kjør i egen tråd så vi ikke blokkerer watcher-eventet mens vi venter på fil-lås
        Task.Run(async () =>
        {
            if (await WaitUntilReadableAsync(e.FullPath))
            {
                NewResultFile?.Invoke(e.FullPath);
            }
        });
    }

    private static async Task<bool> WaitUntilReadableAsync(string path, int maxAttempts = 10, int delayMs = 300)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return stream.Length > 0;
            }
            catch (IOException)
            {
                // Dekker både "filen er låst" og "filen finnes ikke ennå" (FileNotFoundException
                // er en undertype av IOException, så denne fanger begge tilfellene).
                await Task.Delay(delayMs);
            }
        }
        return false;
    }

    public void Dispose()
    {
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;
        _watcher.Dispose();
    }
}
