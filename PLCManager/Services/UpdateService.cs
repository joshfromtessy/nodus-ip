using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PLCManager.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private const string CurrentVersion = "1.0.0"; // Update this with each release
    private const string GitHubApiUrl = "https://api.github.com/repos/joshfromtessy/nodus-ip/releases/latest";
    private const string ReleasesUrl = "https://github.com/joshfromtessy/nodus-ip/releases";

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nodus-IP-Updater");
    }

    public async Task<(bool updateAvailable, string latestVersion, string downloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);

            if (release?.TagName == null)
                return (false, CurrentVersion, ReleasesUrl);

            // Remove 'v' prefix if present (e.g., "v1.0.0" -> "1.0.0")
            var latestVersion = release.TagName.TrimStart('v');
            var current = new Version(CurrentVersion);
            var latest = new Version(latestVersion);

            var updateAvailable = latest > current;
            var downloadUrl = release.HtmlUrl ?? ReleasesUrl;

            return (updateAvailable, latestVersion, downloadUrl);
        }
        catch
        {
            // If check fails, just return no update available
            return (false, CurrentVersion, ReleasesUrl);
        }
    }

    public string GetCurrentVersion() => CurrentVersion;

    public string GetReleasesUrl() => ReleasesUrl;

    private class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
    }
}
