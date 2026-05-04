using System.Windows;
using System.Windows.Media;
using PhotoConverterV2.Models;
using PhotoConverterV2.Services;

namespace PhotoConverterV2
{
    public partial class App : Application
    {
        // ── Uygulama geneli tekil servisler ──────────────────────────────────
        public static SettingsService SettingsService { get; } = new SettingsService();
        public static AppSettings Settings { get; private set; } = new AppSettings();

        // ── Başlangıç ─────────────────────────────────────────────────────────
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ayarları diskten yükle
            Settings = SettingsService.Load();

            // Başlangıç temasını uygula
            ApplyTheme(Settings.Theme);
        }

        // ── Kapanışta kaydet ──────────────────────────────────────────────────
        protected override void OnExit(ExitEventArgs e)
        {
            SettingsService.Save(Settings);
            base.OnExit(e);
        }

        // ── Tema Uygulama Motoru ─────────────────────────────────────────────
        /// <summary>
        /// "Light" veya "Dark" temasını uygular.
        /// Spec'e göre MergedDictionaries.Clear() yerine
        /// doğrudan Resources["Key"] = yeni değer ataması yapılır.
        /// Bu sayede tüm DynamicResource binding'leri anında güncellenir.
        /// </summary>
        public static void ApplyTheme(string theme)
        {
            Settings.Theme = theme;

            if (theme == "Dark")
            {
                SetBrush("BackgroundBrush",  "#121212");
                SetBrush("PanelBgBrush",     "#1e1e1e");
                SetBrush("ForegroundBrush",  "#f0f0f0");
                SetBrush("SubtleBrush",       "#a0a0a0");
                SetBrush("BorderBrush2",      "#373737");
                SetBrush("InputBgBrush",      "#282828");
                SetBrush("AccentBrush",       "#0078d4");
                SetBrush("DropZoneBrush",     "#142332");
                SetBrush("ThumbnailBgBrush",  "#2d2d2d");
            }
            else // Light (varsayılan)
            {
                SetBrush("BackgroundBrush",  "#f5f5f5");
                SetBrush("PanelBgBrush",     "#ffffff");
                SetBrush("ForegroundBrush",  "#1a1a1a");
                SetBrush("SubtleBrush",       "#666666");
                SetBrush("BorderBrush2",      "#dddddd");
                SetBrush("InputBgBrush",      "#f9f9f9");
                SetBrush("AccentBrush",       "#0078d4");
                SetBrush("DropZoneBrush",     "#e8f4fd");
                SetBrush("ThumbnailBgBrush",  "#eeeeee");
            }
        }

        // ── Yardımcı: hex renk → SolidColorBrush → Resources ─────────────────
        private static void SetBrush(string key, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Current.Resources[key] = new SolidColorBrush(color);
        }
    }
}
