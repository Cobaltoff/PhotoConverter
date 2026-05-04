namespace PhotoConverterV2.Models
{
    /// <summary>
    /// Görüntü dönüştürme için platform profili (Trendyol, N11, Hepsiburada vb.)
    /// </summary>
    public class PlatformProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 1200;

        /// <summary>JPEG veya PNG</summary>
        public string Format { get; set; } = "JPEG";

        /// <summary>1–100 arası JPEG kalitesi</summary>
        public int JpegQuality { get; set; } = 90;

        /// <summary>Crop / Pad / Stretch</summary>
        public string AspectRatioMode { get; set; } = "Crop";

        /// <summary>Pad modunda dolgu rengi: White veya Black</summary>
        public string PadColor { get; set; } = "White";

        /// <summary>true ise kullanıcı bu profili silemez</summary>
        public bool IsBuiltIn { get; set; } = false;

        // ---- Fabrika metotları (Built-in profiller) ----

        public static PlatformProfile Trendyol() => new()
        {
            Name = "Trendyol",
            Width = 1200,
            Height = 1800,
            Format = "JPEG",
            JpegQuality = 90,
            AspectRatioMode = "Crop",
            PadColor = "White",
            IsBuiltIn = true
        };

        public static PlatformProfile N11() => new()
        {
            Name = "N11",
            Width = 1200,
            Height = 1200,
            Format = "JPEG",
            JpegQuality = 90,
            AspectRatioMode = "Crop",
            PadColor = "White",
            IsBuiltIn = true
        };

        public static PlatformProfile Hepsiburada() => new()
        {
            Name = "Hepsiburada",
            Width = 1500,
            Height = 1500,
            Format = "JPEG",
            JpegQuality = 90,
            AspectRatioMode = "Crop",
            PadColor = "White",
            IsBuiltIn = true
        };
    }
}
