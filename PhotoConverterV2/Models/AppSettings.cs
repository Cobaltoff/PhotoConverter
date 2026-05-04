using System.Collections.Generic;

namespace PhotoConverterV2.Models
{
    /// <summary>
    /// Kullanıcı tercihlerini saklar. JSON olarak %AppData%\PhotoConverter\settings.json içine yazılır.
    /// </summary>
    public class AppSettings
    {
        /// <summary>UI teması: "Light" veya "Dark". Varsayılan: Light</summary>
        public string Theme { get; set; } = "Light";

        /// <summary>Arayüz dili: "EN" veya "TR". Varsayılan: EN</summary>
        public string Language { get; set; } = "EN";

        /// <summary>Son kullanılan çıktı klasörü yolu</summary>
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>Son seçilen platform profili adı</summary>
        public string LastPlatform { get; set; } = string.Empty;

        /// <summary>Önizleme panelinin açık/kapalı durumu</summary>
        public bool PreviewPanelVisible { get; set; } = true;

        /// <summary>Dönüştürme sırasında EXIF meta verisi temizlensin mi? Varsayılan: true (güvenli mod)</summary>
        public bool StripExif { get; set; } = true;

        /// <summary>Yerleşik + kullanıcı tanımlı profil listesi</summary>
        public List<PlatformProfile> Profiles { get; set; } = new();

        // ── Watermark ─────────────────────────────────────────────────────────
        public bool   WatermarkEnabled    { get; set; } = false;
        public bool   WatermarkApplyToAll { get; set; } = true;
        public string LastWatermarkPreset { get; set; } = string.Empty;
        public List<WatermarkPreset> WatermarkPresets { get; set; } = new();
    }
}
