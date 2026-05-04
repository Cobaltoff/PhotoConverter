using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PhotoConverterV2.Dialogs
{
    /// <summary>
    /// Mouse ile sürükleyerek kırpma alanı seçme dialog'u.
    /// Sonuç: <see cref="CropResult"/> (null ise kullanıcı iptal etti).
    /// ÖNEMLI: CropResult, gerçek piksel koordinatlarında SixLabors.ImageSharp.Rectangle döner.
    /// </summary>
    public partial class CropDialog : Window
    {
        // ── Alanlar ──────────────────────────────────────────────────────────
        private readonly string _imagePath;
        private readonly int    _targetW;
        private readonly int    _targetH;
        private readonly string _lang;

        // Gerçek görüntü boyutu
        private int _srcPixelW;
        private int _srcPixelH;

        // Canvas üzerindeki görüntünün render edildiği alan (Uniform Stretch sebebiyle kenarlarda boşluk olur)
        private Rect _imageRect; // canvas koordinatlarında

        // Seçim (canvas koordinatları)
        private bool   _isDragging;
        private Point  _dragStart;
        private Rect   _selection; // canvas koordinatları

        // ── Sonuç ────────────────────────────────────────────────────────────
        /// <summary>
        /// Null: kullanıcı iptal etti veya seçim yapmadı.
        /// Değer: gerçek piksel koordinatlarında kırpma dikdörtgeni.
        /// </summary>
        public SixLabors.ImageSharp.Rectangle? CropResult { get; private set; }

        // ── Constructor ──────────────────────────────────────────────────────
        public CropDialog(string imagePath, int targetWidth, int targetHeight, string lang = "EN")
        {
            InitializeComponent();
            _imagePath = imagePath;
            _targetW   = targetWidth;
            _targetH   = targetHeight;
            _lang      = lang;
        }

        // ── Başlangıç ────────────────────────────────────────────────────────
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();
            LblTargetSize.Text = $"{_targetW} × {_targetH}";

            // Görüntüyü yükle
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource   = new Uri(_imagePath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            _srcPixelW = bmp.PixelWidth;
            _srcPixelH = bmp.PixelHeight;
            ImgSource.Source = bmp;

            // Layout tamamen tamamlandıktan sonra imageRect hesapla.
            // ContentRendered, SizeChanged'dan daha güvenli: ActualWidth kesinlikle hazır.
            ContentRendered += (_, _) => RecalcImageRect();
            CropCanvas.SizeChanged += (_, _) => RecalcImageRect();
        }

        // ── Görüntünün Canvas İçindeki Gerçek Alanını Hesapla ────────────────
        private void RecalcImageRect()
        {
            double canvasW = CropCanvas.ActualWidth;
            double canvasH = CropCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0 || _srcPixelW <= 0) return;

            // Uniform Stretch: oranı koru, kutuya sığdır
            double scaleW = canvasW / _srcPixelW;
            double scaleH = canvasH / _srcPixelH;
            double scale  = Math.Min(scaleW, scaleH);

            double rendW = _srcPixelW * scale;
            double rendH = _srcPixelH * scale;
            double offX  = (canvasW - rendW) / 2.0;
            double offY  = (canvasH - rendH) / 2.0;

            _imageRect = new Rect(offX, offY, rendW, rendH);

            // Karartmayı sıfırla
            HideDimOverlay();
        }

        // ── Mouse Events ─────────────────────────────────────────────────────
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // imageRect henüz hesaplanmadıysa tekrar dene (ilk tıklamada güvence)
            if (_imageRect.Width <= 0 || _imageRect.Height <= 0)
                RecalcImageRect();

            // imageRect hâlâ geçersizse sürüklemeye başlama
            if (_imageRect.Width <= 0 || _imageRect.Height <= 0) return;

            _dragStart  = e.GetPosition(CropCanvas);
            _isDragging = true;
            CropCanvas.CaptureMouse();
            SelectionRect.Visibility = Visibility.Collapsed;
            HideDimOverlay();
            BtnApply.IsEnabled = false;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            // imageRect geçersizse çizim yapma
            if (_imageRect.Width <= 0 || _imageRect.Height <= 0) return;

            Point current = e.GetPosition(CropCanvas);

            // Seçim dikdörtgeni: her iki köşeden normalize et, imageRect içinde kal
            double x  = Math.Max(_imageRect.Left,   Math.Min(_dragStart.X, current.X));
            double y  = Math.Max(_imageRect.Top,    Math.Min(_dragStart.Y, current.Y));
            double x2 = Math.Min(_imageRect.Right,  Math.Max(_dragStart.X, current.X));
            double y2 = Math.Min(_imageRect.Bottom, Math.Max(_dragStart.Y, current.Y));

            // Rect negatif genişlik/yükseklik kabul etmez — Math.Max ile güvence
            double w = Math.Max(0d, x2 - x);
            double h = Math.Max(0d, y2 - y);

            if (w == 0 || h == 0) return; // henüz geçerli seçim yok

            _selection = new Rect(x, y, w, h);

            // Seçim çerçevesini çiz
            Canvas.SetLeft(SelectionRect, _selection.X);
            Canvas.SetTop(SelectionRect,  _selection.Y);
            SelectionRect.Width  = _selection.Width;
            SelectionRect.Height = _selection.Height;
            SelectionRect.Visibility = Visibility.Visible;

            // Karartma overlay'ini güncelle
            UpdateDimOverlay();

            // Piksel koordinatlarını göster
            var px = CanvasRectToPixels(_selection);
            LblSelectionInfo.Text = $"  {px.Width} × {px.Height} px";
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            CropCanvas.ReleaseMouseCapture();

            bool hasSelection = _selection.Width > 5 && _selection.Height > 5;
            BtnApply.IsEnabled = hasSelection;
            if (!hasSelection) { HideDimOverlay(); LblSelectionInfo.Text = ""; }
        }

        // ── Karartma Overlay ─────────────────────────────────────────────────
        private void UpdateDimOverlay()
        {
            double cW = CropCanvas.ActualWidth;
            double cH = CropCanvas.ActualHeight;
            Rect s = _selection;

            // Üst
            DimTop.Width = cW; DimTop.Height = s.Top;
            // Sol
            Canvas.SetTop(DimLeft, s.Top);
            DimLeft.Width = s.Left; DimLeft.Height = s.Height;
            // Sağ
            Canvas.SetLeft(DimRight, s.Right);
            Canvas.SetTop(DimRight, s.Top);
            DimRight.Width  = cW - s.Right; DimRight.Height = s.Height;
            // Alt
            Canvas.SetTop(DimBottom, s.Bottom);
            DimBottom.Width = cW; DimBottom.Height = cH - s.Bottom;
        }

        private void HideDimOverlay()
        {
            DimTop.Width = 0; DimTop.Height = 0;
            DimLeft.Width = 0; DimLeft.Height = 0;
            DimRight.Width = 0; DimRight.Height = 0;
            DimBottom.Width = 0; DimBottom.Height = 0;
        }

        // ── Canvas → Piksel Koordinat Dönüşümü ──────────────────────────────
        private SixLabors.ImageSharp.Rectangle CanvasRectToPixels(Rect canvasRect)
        {
            if (_imageRect.Width <= 0 || _imageRect.Height <= 0)
                return new SixLabors.ImageSharp.Rectangle(0, 0, _srcPixelW, _srcPixelH);

            double scaleX = _srcPixelW / _imageRect.Width;
            double scaleY = _srcPixelH / _imageRect.Height;

            int px = (int)Math.Round((canvasRect.X - _imageRect.X) * scaleX);
            int py = (int)Math.Round((canvasRect.Y - _imageRect.Y) * scaleY);
            int pw = (int)Math.Round(canvasRect.Width  * scaleX);
            int ph = (int)Math.Round(canvasRect.Height * scaleY);

            // Sınır güvencesi
            px = Math.Clamp(px, 0, _srcPixelW - 1);
            py = Math.Clamp(py, 0, _srcPixelH - 1);
            pw = Math.Clamp(pw, 1, _srcPixelW - px);
            ph = Math.Clamp(ph, 1, _srcPixelH - py);

            return new SixLabors.ImageSharp.Rectangle(px, py, pw, ph);
        }

        // ── Butonlar ─────────────────────────────────────────────────────────
        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            CropResult   = CanvasRectToPixels(_selection);
            DialogResult = true;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _selection = Rect.Empty;
            SelectionRect.Visibility = Visibility.Collapsed;
            HideDimOverlay();
            BtnApply.IsEnabled    = false;
            LblSelectionInfo.Text = "";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; e.Handled = true; }
        }

        // ── Dil ──────────────────────────────────────────────────────────────
        private void ApplyLanguage()
        {
            bool tr = _lang == "TR";
            Title               = tr ? "Kırp"                                       : "Crop";
            LblInstruction.Text = tr ? "Kırpma alanı seçmek için sürükleyin"        : "Click and drag to select crop area";
            LblTargetLabel.Text = tr ? "Hedef:"                                     : "Target:";
            BtnApply.Content    = tr ? "✂ Kırpmayı Uygula"                          : "✂ Apply Crop";
            BtnReset.Content    = tr ? "Sıfırla"                                    : "Reset";
            BtnCancel.Content   = tr ? "İptal"                                      : "Cancel";
        }
    }
}
