using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PhotoConverterV2.Models;

namespace PhotoConverterV2.Services
{
    /// <summary>
    /// Uygulama ayarlarını %AppData%\PhotoConverter\settings.json içine okur/yazar.
    /// Yerleşik profiller her zaman listenin başında yer alır ve silinemez.
    /// </summary>
    public class SettingsService
    {
        // ── Sabitler ───────────────────────────────────────────────────────────
        private static readonly string AppDataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PhotoConverter");

        private static readonly string SettingsFilePath =
            Path.Combine(AppDataFolder, "settings.json");

        // ── Yardımcı: yerleşik profiller ───────────────────────────────────────
        private static List<PlatformProfile> GetBuiltInProfiles() =>
        [
            PlatformProfile.Trendyol(),
            PlatformProfile.N11(),
            PlatformProfile.Hepsiburada()
        ];

        // ── Ayarları Yükle ──────────────────────────────────────────────────────
        /// <summary>
        /// Ayarları diskten okur. Dosya yoksa varsayılan değerlerle yeni bir AppSettings döner.
        /// Yerleşik profiller her zaman listenin başında, güncel bilgilerle eklenir.
        /// </summary>
        public AppSettings Load()
        {
            AppSettings settings;

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    settings = new AppSettings();
                }
            }
            catch
            {
                // Bozuk JSON → sıfırdan başla
                settings = new AppSettings();
            }

            MergeBuiltInProfiles(settings);
            foreach (var preset in settings.WatermarkPresets)
            {
                if (preset.LogoPositionX == 0.0) preset.LogoPositionX = 0.5;
                if (preset.LogoPositionY == 0.0) preset.LogoPositionY = 0.5;
            }
            return settings;
        }

        // ── Ayarları Kaydet ─────────────────────────────────────────────────────
        /// <summary>
        /// AppSettings nesnesini JSON olarak diske yazar.
        /// Klasör yoksa oluşturulur.
        /// </summary>
        public void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Kayıt başarısız olursa sessizce devam et (kullanıcıya bildirim MainWindow'da)
            }
        }

        // ── Dahili: Yerleşik profilleri birleştir ───────────────────────────────
        /// <summary>
        /// Yerleşik profilleri her zaman listenin başında, güncel tanımlarıyla garantiler.
        /// Kullanıcı tanımlı profiller (IsBuiltIn = false) sonunda korunur.
        /// </summary>
        private static void MergeBuiltInProfiles(AppSettings settings)
        {
            var builtIns = GetBuiltInProfiles();

            // Kullanıcıya ait profiller (yerleşik olmayanlar)
            var userProfiles = settings.Profiles
                .Where(p => !p.IsBuiltIn)
                .ToList();

            // Yerleşik + kullanıcı profili → temiz liste
            settings.Profiles = [.. builtIns, .. userProfiles];
        }
    }
}
