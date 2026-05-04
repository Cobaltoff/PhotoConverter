using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using PhotoConverterV2.Models;

namespace PhotoConverterV2.Services
{
    /// <summary>
    /// Tek bir görüntü dosyasını verilen profile göre dönüştürür.
    /// Desteklenen Aspect Mode: Crop / Pad / Stretch
    /// </summary>
    public class ImageProcessingService
    {
        // ── Toplu Dönüştürme ─────────────────────────────────────────────────
        /// <summary>
        /// Birden fazla dosyayı sırasıyla dönüştürür.
        /// Her dosya tamamlandığında <paramref name="onProgress"/> çağrılır (tamamlanan, toplam).
        /// Başarısız dosyalar exception fırlatmak yerine hata sayısına eklenir.
        /// </summary>
        public async Task<(int Succeeded, int Failed)> ConvertAllAsync(
            string[] sourcePaths,
            string outputFolder,
            PlatformProfile profile,
            bool useRename,
            string baseName,
            bool stripExif = true,
            WatermarkPreset? watermark = null,
            IProgress<(int Done, int Total)>? onProgress = null,
            CancellationToken cancellationToken = default)
        {
            int succeeded = 0;
            int failed = 0;
            int total = sourcePaths.Length;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string src = sourcePaths[i];

                string outputName = useRename && !string.IsNullOrWhiteSpace(baseName)
                    ? $"{baseName}_{i + 1}.{GetExtension(profile.Format)}"
                    : Path.GetFileNameWithoutExtension(src) + $".{GetExtension(profile.Format)}";

                string destPath = Path.Combine(outputFolder, outputName);

                try
                {
                    await ConvertSingleAsync(src, destPath, profile, cropRect: null, stripExif, watermark, cancellationToken);
                    succeeded++;
                }
                catch (OperationCanceledException) { throw; }
                catch { failed++; }

                onProgress?.Report((i + 1, total));
            }

            return (succeeded, failed);
        }

        // ── Tek Dosya Dönüştürme ─────────────────────────────────────────────
        /// <summary>
        /// Tek bir görüntüyü dönüştürür.
        /// <paramref name="cropRect"/> null değilse bu bölge önce kırpılır,
        /// ardından profile göre yeniden boyutlandırılır.
        /// </summary>
        public async Task ConvertSingleAsync(
            string sourcePath,
            string destPath,
            PlatformProfile profile,
            SixLabors.ImageSharp.Rectangle? cropRect = null,
            bool stripExif = true,
            WatermarkPreset? watermark = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                if (profile.AspectRatioMode == "Pad")
                {
                    SaveWithPad(sourcePath, destPath, profile, cropRect, stripExif, watermark);
                }
                else
                {
                    using var image = Image.Load(sourcePath);

                    if (cropRect.HasValue)
                        image.Mutate(ctx => ctx.Crop(cropRect.Value));

                    if (profile.AspectRatioMode == "Stretch")
                        ApplyStretch(image, profile.Width, profile.Height);
                    else
                        ApplyCrop(image, profile.Width, profile.Height);

                    if (watermark != null) WatermarkService.Apply(image, watermark);
                    SaveImage(image, destPath, profile, stripExif);
                }

            }, cancellationToken);
        }

        // ── Aspect Mode: Crop ────────────────────────────────────────────────
        /// <summary>
        /// Görüntüyü hedef orana göre ortadan kırpar, ardından tam boyuta getirir.
        /// </summary>
        private static void ApplyCrop(Image image, int targetW, int targetH)
        {
            double targetRatio = (double)targetW / targetH;
            double srcRatio    = (double)image.Width / image.Height;

            int cropW, cropH;

            if (srcRatio > targetRatio)
            {
                // Kaynak daha geniş → yüksekliği koru, genişliği kırp
                cropH = image.Height;
                cropW = (int)Math.Round(image.Height * targetRatio);
            }
            else
            {
                // Kaynak daha dar → genişliği koru, yüksekliği kırp
                cropW = image.Width;
                cropH = (int)Math.Round(image.Width / targetRatio);
            }

            int x = (image.Width  - cropW) / 2;
            int y = (image.Height - cropH) / 2;

            image.Mutate(ctx =>
            {
                ctx.Crop(new Rectangle(x, y, cropW, cropH));
                ctx.Resize(targetW, targetH);
            });
        }

        // ── Aspect Mode: Pad ─────────────────────────────────────────────────
        /// <summary>
        /// Görüntüyü hedef boyutun içine oranlı sığdırır, boş alanları PadColor ile doldurur.
        /// ImageSharp'ta tek bir Image üzerinde canvas değiştirme mümkün olmadığından
        /// yeni bir canvas Image&lt;Rgba32&gt; oluşturulur ve doğrudan kaydedilir.
        /// </summary>
        private static void SaveWithPad(
            string sourcePath,
            string destPath,
            PlatformProfile profile,
            SixLabors.ImageSharp.Rectangle? cropRect,
            bool stripExif = true,
            WatermarkPreset? watermark = null)
        {
            int targetW = profile.Width;
            int targetH = profile.Height;

            var bgColor = profile.PadColor == "Black" ? Color.Black : Color.White;

            using var src = Image.Load<Rgba32>(sourcePath);

            if (cropRect.HasValue)
                src.Mutate(ctx => ctx.Crop(cropRect.Value));

            double scaleW = (double)targetW / src.Width;
            double scaleH = (double)targetH / src.Height;
            double scale  = Math.Min(scaleW, scaleH);

            int newW = Math.Max(1, (int)Math.Round(src.Width  * scale));
            int newH = Math.Max(1, (int)Math.Round(src.Height * scale));

            src.Mutate(ctx => ctx.Resize(newW, newH));

            int offsetX = (targetW - newW) / 2;
            int offsetY = (targetH - newH) / 2;

            using var canvas = new Image<Rgba32>(targetW, targetH, (Rgba32)bgColor);
            canvas.Mutate(ctx => ctx.DrawImage(src, new Point(offsetX, offsetY), opacity: 1f));

            if (watermark != null) WatermarkService.Apply(canvas, watermark);
            SaveImage(canvas, destPath, profile, stripExif);
        }

        // ── Aspect Mode: Stretch ─────────────────────────────────────────────
        /// <summary>
        /// Görüntüyü oranı dikkate almadan doğrudan hedef boyuta uzatır.
        /// </summary>
        private static void ApplyStretch(Image image, int targetW, int targetH)
        {
            image.Mutate(ctx => ctx.Resize(targetW, targetH));
        }

        // ── Kaydetme ─────────────────────────────────────────────────────────
        private static void SaveImage(Image image, string destPath, PlatformProfile profile, bool stripExif = true)
        {
            // EXIF / meta veri temizleme
            if (stripExif)
            {
                image.Metadata.ExifProfile  = null;
                image.Metadata.IptcProfile  = null;
                image.Metadata.XmpProfile   = null;
            }

            if (profile.Format == "PNG")
            {
                image.Save(destPath, new PngEncoder());
            }
            else
            {
                image.Save(destPath, new JpegEncoder
                {
                    Quality = Math.Clamp(profile.JpegQuality, 1, 100)
                });
            }
        }

        // ── Yardımcı: format uzantısı ─────────────────────────────────────────
        private static string GetExtension(string format) =>
            format == "PNG" ? "png" : "jpg";
    }
}
