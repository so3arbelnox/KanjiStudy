using System;
using System.IO;
using System.Text.Json;

namespace KanjiStudy.Services
{
    public class AppSettings
    {
        public string? LastDeckPath { get; set; }
    }

    /// <summary>
    /// Persists lightweight app settings (e.g. the last loaded deck) to a JSON file under the
    /// user's config directory, separate from application data so it survives reinstalls.
    /// </summary>
    public static class AppSettingsStore
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KanjiStudy",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Best-effort: a missing/corrupt settings file should never block the app from starting.
            }

            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings));
            }
            catch
            {
                // Best-effort: failing to persist settings shouldn't interrupt studying.
            }
        }
    }
}
