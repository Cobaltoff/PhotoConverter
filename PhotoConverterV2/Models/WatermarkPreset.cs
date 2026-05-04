namespace PhotoConverterV2.Models
{
    public class WatermarkPreset
    {
        public string  Name         { get; set; } = "Watermark 1";

        // Metin
        public string? Text         { get; set; }
        public float   TextOpacity  { get; set; } = 0.70f;
        public float   FontSize     { get; set; } = 24f;
        public string  Color        { get; set; } = "#FFFFFF";

        // Logo
        public string? LogoPath     { get; set; }
        public float   LogoOpacity  { get; set; } = 0.70f;
        public float   LogoScale    { get; set; } = 0.20f;  // görüntü genişliğinin %'si (0–1)

        // Konum (0–1 normalize)
        public double  PositionX    { get; set; } = 0.50;   // yatay: merkez
        public double  PositionY    { get; set; } = 0.85;   // dikey:  alt

        public double LogoPositionX { get; set; } = 0.50;
        public double LogoPositionY { get; set; } = 0.85;
    }
}
