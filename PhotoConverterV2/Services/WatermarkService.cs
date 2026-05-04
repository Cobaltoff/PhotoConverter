using System;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PhotoConverterV2.Models;

namespace PhotoConverterV2.Services
{
    public static class WatermarkService
    {
        public static void Apply(Image image, WatermarkPreset preset)
        {
            ApplyLogo(image, preset);
            ApplyText(image, preset);
        }

        private static void ApplyLogo(Image image, WatermarkPreset preset)
        {
            if (string.IsNullOrEmpty(preset.LogoPath) || !File.Exists(preset.LogoPath)) return;

            using var logo = Image.Load<Rgba32>(preset.LogoPath);

            // Maksimum boyutlar: genişlik %60, yükseklik %25
            int maxW    = (int)(image.Width  * Math.Clamp(preset.LogoScale, 0.01f, 0.60f));
            int maxH    = (int)(image.Height * 0.25f);
            int targetW = maxW;
            int targetH = (int)((double)logo.Height / logo.Width * targetW);

            if (targetH > maxH)
            {
                targetH = maxH;
                targetW = (int)((double)logo.Width / logo.Height * targetH);
            }

            targetW = Math.Max(1, targetW);
            targetH = Math.Max(1, targetH);

            logo.Mutate(ctx => ctx.Resize(targetW, targetH));

            int margin = Math.Max(10, (int)(Math.Min(image.Width, image.Height) * 0.02f));
            int lx = (int)(preset.LogoPositionX * image.Width - targetW / 2.0);
            int ly = (int)(preset.LogoPositionY * image.Height - targetH / 2.0);
            lx = Math.Clamp(lx, margin, image.Width  - targetW - margin);
            ly = Math.Clamp(ly, margin, image.Height - targetH - margin);

            image.Mutate(ctx => ctx.DrawImage(logo, new Point(lx, ly), preset.LogoOpacity));
        }

        private static void ApplyText(Image image, WatermarkPreset preset)
        {
            if (string.IsNullOrEmpty(preset.Text)) return;

            // Yazı tipi — image genişliğine orantılı boyut
            float scaledSize = preset.FontSize * (image.Width / 1200f);
            scaledSize = Math.Max(8f, scaledSize);

            Font font;
            try   { font = SystemFonts.CreateFont("Arial", scaledSize); }
            catch { font = SystemFonts.CreateFont(SystemFonts.Families.First().Name, scaledSize); }

            // Renk + opaklık
            string hex = preset.Color.TrimStart('#');
            if (hex.Length == 6) hex += "FF";
            byte r = Convert.ToByte(hex[0..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            byte a = (byte)(Math.Clamp(preset.TextOpacity, 0f, 1f) * 255);
            var  color = Color.FromRgba(r, g, b, a);

            var options = new RichTextOptions(font)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Origin = new System.Numerics.Vector2(
                    (float)(preset.PositionX * image.Width),
                    (float)(preset.PositionY * image.Height))
            };

            image.Mutate(ctx => ctx.DrawText(options, preset.Text, new SolidBrush(color)));
        }
    }
}
