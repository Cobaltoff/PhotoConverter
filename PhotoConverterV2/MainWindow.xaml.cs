using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PhotoConverterV2.Models;
using PhotoConverterV2.Services;

namespace PhotoConverterV2
{
    public partial class MainWindow : Window
    {
        private readonly ImageProcessingService _imgService = new();
        private readonly List<string> _iImagePaths = new();
        private string? _selectedImagePath;
        private CancellationTokenSource? _cts;

        private Border? _dragThumb;
        private Point _dragStartPoint;
        private bool _thumbDragging;
        private System.Windows.Shapes.Rectangle? _iInsertionLine;

        private AppSettings Settings => App.Settings;
        private string Lang => Settings.Language;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnWatermarkSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_watermarkDialog != null && _watermarkDialog.IsLoaded)
            {
                _watermarkDialog.Activate();
                return;
            }

            _watermarkDialog = new Dialogs.WatermarkDialog(Settings, Lang) { Owner = this };
            _watermarkDialog.WatermarkChanged += () => Dispatcher.Invoke(RefreshWatermarkOverlay);
            _watermarkDialog.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProfilesToCombo();

            TxtWidth.Text = "1200";
            TxtHeight.Text = "1200";
            TxtQuality.Text = "90";
            TxtOutputFolder.Text = Settings.OutputFolder;

            

            BtnTogglePreview.Checked -= BtnTogglePreview_Checked;
            BtnTogglePreview.Unchecked -= BtnTogglePreview_Unchecked;
            BtnTogglePreview.IsChecked = Settings.PreviewPanelVisible;
            PreviewPanel.Visibility = Settings.PreviewPanelVisible ? Visibility.Visible : Visibility.Collapsed;
            PreviewColumn.Width = Settings.PreviewPanelVisible ? new GridLength(300) : new GridLength(0);
            BtnTogglePreview.Checked += BtnTogglePreview_Checked;
            BtnTogglePreview.Unchecked += BtnTogglePreview_Unchecked;

            BtnStripExif.Checked -= BtnStripExif_Changed;
            BtnStripExif.Unchecked -= BtnStripExif_Changed;
            BtnStripExif.IsChecked = Settings.StripExif;
            BtnStripExif.Checked += BtnStripExif_Changed;
            BtnStripExif.Unchecked += BtnStripExif_Changed;

            App.ApplyTheme(Settings.Theme);
            ThemeIcon.Text = Settings.Theme == "Dark" ? "☀" : "🌙";
            ApplyLanguage();
            UpdateStatusBar();
        }

        // ── Platform ─────────────────────────────────────────────────────────

        private void LoadProfilesToCombo()
        {
            string? previousSelection = (CboPlatform.SelectedItem as ComboBoxItem)?.Content?.ToString();

            CboPlatform.Items.Clear();
            CboPlatform.Items.Add(new ComboBoxItem
            {
                Content = Lang == "TR" ? "Platform (İsteğe Bağlı)" : "Platform (Optional)"
            });
            foreach (var p in Settings.Profiles)
                CboPlatform.Items.Add(new ComboBoxItem { Content = p.Name, Tag = p });

            // Önceki seçimi veya LastPlatform'u geri yükle
            string toSelect = previousSelection ?? Settings.LastPlatform ?? "";
            bool found = false;
            foreach (ComboBoxItem item in CboPlatform.Items)
            {
                if (item.Content?.ToString() == toSelect)
                {
                    CboPlatform.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found) CboPlatform.SelectedIndex = 0;
        }

        private void CboPlatform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboPlatform.SelectedItem is ComboBoxItem { Tag: PlatformProfile p })
            {
                _platformSelecting = true;
                TxtWidth.Text = p.Width.ToString();
                TxtHeight.Text = p.Height.ToString();
                TxtQuality.Text = p.JpegQuality.ToString();
                foreach (ComboBoxItem item in CboFormat.Items)
                    if (item.Content?.ToString() == p.Format) { CboFormat.SelectedItem = item; break; }
                foreach (ComboBoxItem item in CboAspect.Items)
                    if (item.Content?.ToString() == p.AspectRatioMode) { CboAspect.SelectedItem = item; break; }
                Settings.LastPlatform = p.Name;
                App.SettingsService.Save(Settings);
                _platformSelecting = false;
            }
        }

        // ── Tema ─────────────────────────────────────────────────────────────

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            string newTheme = Settings.Theme == "Dark" ? "Light" : "Dark";
            App.ApplyTheme(newTheme);
            ThemeIcon.Text = newTheme == "Dark" ? "☀" : "🌙";
            App.SettingsService.Save(Settings);
        }

        // ── Dil ──────────────────────────────────────────────────────────────

        private void BtnLanguage_Click(object sender, RoutedEventArgs e)
        {
            Settings.Language = Settings.Language == "TR" ? "EN" : "TR";
            ApplyLanguage();
            App.SettingsService.Save(Settings);

            // Watermark dialog açıksa dilini güncelle
            if (_watermarkDialog != null && _watermarkDialog.IsLoaded)
                _watermarkDialog.UpdateLanguage(Lang);
        }

        private void ApplyLanguage()
        {
            bool tr = Lang == "TR";
            LblPlatform.Text = tr ? "Platform (İsteğe Bağlı)" : "Platform (Optional)";
            LblOutput.Text = tr ? "Çıktı:" : "Output:";
            LblRenameBtn.Text = tr ? "✏ Yeniden Adlandır" : "✏ Rename";
            LblPreviewBtn.Text = tr ? "👁 Önizleme" : "👁 Preview";
            LblFormat.Text = tr ? "Çıktı Format" : "Output Format";
            LblWidth.Text = tr ? "Genişlik (px)" : "Width (px)";
            LblHeight.Text = tr ? "Yükseklik (px)" : "Height (px)";
            LblQuality.Text = tr ? "JPEG Kalitesi" : "Quality";
            LblAspect.Text = tr ? "Oran Modu" : "Aspect Mode";
            LblDropHint1.Text = tr ? "Görselleri buraya bırakın" : "Drop images here";
            LblClearAll.Text = tr ? "Temizle" : "Clear All";
            LblConvert.Text = tr ? "✓ Dönüştür" : "✓ Convert";
            LblBaseName.Text = tr ? "Temel isim:" : "Base name:";
            LblPadColor.Text = tr ? "Dolgu Rengi:" : "Pad Color:";
            LblRenameHint.Text = tr ? "Dosyalar şu şekilde kaydedilir: isim_1.jpg ..." : "Files will be saved as: name_1.jpg ...";
            LblPreviewHint.Text = tr ? "Küçük resme tıklayarak önizleyin" : "Select a thumbnail to preview";
            BtnStripExif.ToolTip = tr ? "EXIF verilerini temizle (açık = temizle)" : "Strip EXIF metadata (on = strip)";
            LblWatermarkBtn.Text = tr ? "🎨 Watermark" : "🎨 Watermark";
            UpdateStatusBar();
            if (CboPlatform.Items.Count > 0 && CboPlatform.Items[0] is ComboBoxItem first)
                first.Content = tr ? "Platform (İsteğe Bağlı)" : "Platform (Optional)";
        }

        // ── Profil Yönetimi ───────────────────────────────────────────────────

        private void BtnProfiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Dialogs.ProfileDialog(Settings.Profiles, Lang) { Owner = this };
            dlg.ShowDialog();
            Settings.Profiles = dlg.UpdatedProfiles;
            App.SettingsService.Save(Settings);
            string lastPlatform = Settings.LastPlatform;
            LoadProfilesToCombo();
            foreach (ComboBoxItem item in CboPlatform.Items)
                if (item.Content?.ToString() == lastPlatform) { CboPlatform.SelectedItem = item; break; }
        }

        // ── Çıktı Klasörü ─────────────────────────────────────────────────────

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = Lang == "TR" ? "Çıktı klasörü seçin" : "Select output folder"
            };
            if (dialog.ShowDialog() == true)
            {
                Settings.OutputFolder = dialog.FolderName;
                TxtOutputFolder.Text = dialog.FolderName;
                App.SettingsService.Save(Settings);
            }
        }

        // ── Ayar Değişiklikleri ───────────────────────────────────────────────
        private bool _platformSelecting = false;
        private void Settings_Changed(object sender, TextChangedEventArgs e)
        {
            // Kullanıcı manuel değiştirince platform seçimini sıfırla
            // ama CboPlatform_SelectionChanged tarafından tetikleniyorsa sıfırlama
            if (_platformSelecting) return;
            if (CboPlatform.SelectedIndex > 0)
                CboPlatform.SelectedIndex = 0;
        }

        private void CboFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void CboAspect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string mode = (CboAspect.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Crop";
            if (PadColorPanel != null)
                PadColorPanel.Visibility = mode == "Pad" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CboPadColor_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnStripExif_Changed(object sender, RoutedEventArgs e)
        {
            Settings.StripExif = BtnStripExif.IsChecked == true;
            App.SettingsService.Save(Settings);
        }

        // ── Rename Toggle ─────────────────────────────────────────────────────

        private void BtnToggleRename_Checked(object sender, RoutedEventArgs e)
            => RenameBanner.Visibility = Visibility.Visible;

        private void BtnToggleRename_Unchecked(object sender, RoutedEventArgs e)
            => RenameBanner.Visibility = Visibility.Collapsed;

        // ── Preview Panel Toggle ──────────────────────────────────────────────

        private void BtnTogglePreview_Checked(object sender, RoutedEventArgs e)
        {
            if (PreviewPanel == null) return;
            PreviewPanel.Visibility = Visibility.Visible;
            PreviewColumn.Width = new GridLength(300);
            Settings.PreviewPanelVisible = true;
            App.SettingsService.Save(Settings);
        }

        private void BtnTogglePreview_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PreviewPanel == null) return;
            PreviewPanel.Visibility = Visibility.Collapsed;
            PreviewColumn.Width = new GridLength(0);
            Settings.PreviewPanelVisible = false;
            App.SettingsService.Save(Settings);
        }

        // ── Dosya Ekleme ──────────────────────────────────────────────────────

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.tiff"
            };
            if (dlg.ShowDialog() == true)
                AddImages(dlg.FileNames);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var images = files.Where(f =>
                    new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".tiff" }
                    .Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();
                AddImages(images);
            }
        }

        private void AddImages(string[] paths)
        {
            foreach (var path in paths)
            {
                if (_iImagePaths.Contains(path)) continue;
                _iImagePaths.Add(path);
                AddThumbnail(path);
            }
            RefreshDropZone();
            UpdateStatusBar();
        }

        private void AddThumbnail(string path)
        {
            var border = new Border
            {
                Width = 90,
                Height = 90,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(4),
                Background = (Brush)FindResource("ThumbnailBgBrush"),
                Cursor = Cursors.Hand,
                Tag = path,
                ToolTip = Path.GetFileName(path)
            };

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = 80;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            border.Child = new Image { Source = bmp, Stretch = Stretch.UniformToFill };
            border.MouseLeftButtonDown += Thumb_MouseDown;
            border.MouseMove += Thumb_MouseMove;
            border.MouseLeftButtonUp += Thumb_MouseUp;
            border.MouseRightButtonUp += Thumbnail_RightClick;
            ThumbPanel.Children.Add(border);
        }

        private Dialogs.WatermarkDialog? _watermarkDialog;

        private void SelectThumbnail(string path)
        {
            _selectedImagePath = path;
            if (PreviewPanel.Visibility == Visibility.Visible)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                ImgPreview.Source = bmp;
                ImgPreview.Visibility = Visibility.Visible;
                LblPreviewHint.Visibility = Visibility.Collapsed;
                LblPreviewFilename.Text = Path.GetFileName(path);
                RefreshWatermarkOverlay();
            }
        }

        private void Thumbnail_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            string path = (string)border.Tag;
            var menu = new ContextMenu();
            var cropItem = new MenuItem { Header = Lang == "TR" ? "✂ Kırp" : "✂ Crop" };
            cropItem.Click += (_, _) => OpenCropDialog(path);
            var removeItem = new MenuItem { Header = Lang == "TR" ? "🗑 Kaldır" : "🗑 Remove" };
            removeItem.Click += (_, _) => RemoveImage(path, border);
            menu.Items.Add(cropItem);
            menu.Items.Add(removeItem);
            border.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void RemoveImage(string path, Border border)
        {
            _iImagePaths.Remove(path);
            ThumbPanel.Children.Remove(border);
            if (_selectedImagePath == path)
            {
                _selectedImagePath = null;
                ImgPreview.Source = null;
                ImgPreview.Visibility = Visibility.Collapsed;
                LblPreviewHint.Visibility = Visibility.Visible;
                LblPreviewFilename.Text = "";
            }
            RefreshDropZone();
            UpdateStatusBar();
        }

        private void OpenCropDialog(string path)
        {
            var profile = BuildProfile();
            var dlg = new Dialogs.CropDialog(path, profile.Width, profile.Height, Lang) { Owner = this };
            if (dlg.ShowDialog() == true && dlg.CropResult.HasValue)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "PhotoConverter");
                Directory.CreateDirectory(tempDir);
                string tempPath = Path.Combine(tempDir,
                    Path.GetFileNameWithoutExtension(path) + "_crop" + Path.GetExtension(path));

                _ = Task.Run(async () =>
                {
                    await _imgService.ConvertSingleAsync(path, tempPath, profile, cropRect: dlg.CropResult.Value);
                    Dispatcher.Invoke(() =>
                    {
                        int idx = _iImagePaths.IndexOf(path);
                        if (idx >= 0)
                        {
                            _iImagePaths[idx] = tempPath;
                            if (ThumbPanel.Children[idx] is Border b)
                            {
                                var bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(tempPath);
                                bmp.DecodePixelWidth = 80;
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                bmp.Freeze();
                                if (b.Child is Image img) img.Source = bmp;
                                b.Tag = tempPath;
                            }
                            if (_selectedImagePath == path)
                                SelectThumbnail(tempPath);
                        }
                    });
                });
            }
        }

        private void RefreshDropZone()
        {
            bool hasImages = _iImagePaths.Count > 0;
            DropHint.Visibility = hasImages ? Visibility.Collapsed : Visibility.Visible;
            ThumbScroll.Visibility = hasImages ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PreviewIImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (_selectedImagePath == null) return;
            var preview = new Dialogs.FullscreenPreview(_selectedImagePath, Lang) { Owner = this };
            preview.ShowDialog();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            _iImagePaths.Clear();
            ThumbPanel.Children.Clear();
            _selectedImagePath = null;
            ImgPreview.Source = null;
            ImgPreview.Visibility = Visibility.Collapsed;
            LblPreviewHint.Visibility = Visibility.Visible;
            LblPreviewFilename.Text = "";
            RefreshDropZone();
            UpdateStatusBar();
        }

        // ── Convert ───────────────────────────────────────────────────────────

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            bool tr = Lang == "TR";

            if (_iImagePaths.Count == 0)
            {
                MessageBox.Show(tr ? "Lütfen önce görsel ekleyin." : "Please add images first.", "Photo Converter");
                return;
            }

            if (string.IsNullOrWhiteSpace(Settings.OutputFolder) || !Directory.Exists(Settings.OutputFolder))
            {
                MessageBox.Show(tr ? "Lütfen geçerli bir çıktı klasörü seçin." : "Please select a valid output folder.", "Photo Converter");
                return;
            }

            string selectedFormat = (CboFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "JPEG";
            bool hasPng = _iImagePaths.Any(p => Path.GetExtension(p).Equals(".png", StringComparison.OrdinalIgnoreCase));

            if (hasPng && selectedFormat == "JPEG")
            {
                var result = MessageBox.Show(
                    tr ? "Bazı girdiler PNG formatında. Çıktıyı JPEG olarak kaydetmek istiyor musunuz?" : "Some inputs are PNG. Save output as JPEG?",
                    "Photo Converter", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }

            var profile = BuildProfile();
            if (!await CheckAspectRatioAsync(profile)) return;

            BtnConvert.IsEnabled = false;
            PrgConvert.Visibility = Visibility.Visible;
            PrgConvert.Value = 0;

            bool useRename = BtnToggleRename.IsChecked == true;
            string baseName = TxtBaseName.Text.Trim();

            _cts = new CancellationTokenSource();
            var progress = new Progress<(int Done, int Total)>(p =>
            {
                PrgConvert.Value = (double)p.Done / p.Total * 100;
                LblStatus.Text = tr ? $"Dönüştürülüyor... {p.Done}/{p.Total}" : $"Converting... {p.Done}/{p.Total}";
            });

            (int ok, int fail) = (0, 0);
            try
            {
                (ok, fail) = await _imgService.ConvertAllAsync(
                    [.. _iImagePaths],
                    Settings.OutputFolder,
                    profile,
                    useRename,
                    baseName,
                    stripExif: Settings.StripExif,
                    watermark: GetActiveWatermark(),
                    onProgress: progress,
                    cancellationToken: _cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                BtnConvert.IsEnabled = true;
                PrgConvert.Visibility = Visibility.Collapsed;
                string msg = tr ? $"Tamamlandı: {ok} başarılı, {fail} başarısız" : $"Done: {ok} succeeded, {fail} failed";
                LblStatus.Text = msg;
                if (ok + fail > 0) MessageBox.Show(msg, "Photo Converter");
            }
        }

        private PlatformProfile BuildProfile()
        {
            _ = int.TryParse(TxtWidth.Text, out int w); w = Math.Max(100, w);
            _ = int.TryParse(TxtHeight.Text, out int h); h = Math.Max(100, h);
            _ = int.TryParse(TxtQuality.Text, out int q); q = Math.Clamp(q, 1, 100);
            return new PlatformProfile
            {
                Width = w,
                Height = h,
                Format = (CboFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "JPEG",
                JpegQuality = q,
                AspectRatioMode = (CboAspect.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Crop",
                PadColor = (CboPadColor.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "White"
            };
        }

        private async Task<bool> CheckAspectRatioAsync(PlatformProfile profile)
        {
            if (profile.AspectRatioMode != "Crop" || _iImagePaths.Count == 0) return true;
            double targetRatio = (double)profile.Width / profile.Height;
            bool mismatch = false;
            await Task.Run(() =>
            {
                foreach (var path in _iImagePaths)
                {
                    try
                    {
                        var info = SixLabors.ImageSharp.Image.Identify(path);
                        double srcRatio = (double)info.Width / info.Height;
                        if (Math.Abs(srcRatio - targetRatio) > 0.01) { mismatch = true; break; }
                    }
                    catch { }
                }
            });

            if (!mismatch) return true;
            bool tr = Lang == "TR";
            var r = MessageBox.Show(
                tr ? "Bazı görseller hedef oranla uyuşmuyor. Ortadan kırpılacak. Devam edilsin mi?"
                   : "Some images don't match the target ratio and will be cropped. Continue?",
                "Photo Converter", MessageBoxButton.YesNo);
            return r == MessageBoxResult.Yes;
        }

        private void UpdateStatusBar()
        {
            bool tr = Lang == "TR";
            int count = _iImagePaths.Count;
            LblStatus.Text = count == 0
                ? (tr ? "Hazır" : "Ready")
                : (tr ? $"{count} görsel yüklendi" : $"{count} image(s) loaded");
        }

        // ── Watermark ─────────────────────────────────────────────────────────

        private WatermarkPreset? GetActiveWatermark()
        {
            if (!Settings.WatermarkEnabled) return null;
            return Settings.WatermarkPresets
                .FirstOrDefault(p => p.Name == Settings.LastWatermarkPreset)
                ?? Settings.WatermarkPresets.FirstOrDefault();
        }

        private void RefreshWatermarkOverlay()
        {
            WatermarkOverlay.Children.Clear();
            if (!Settings.WatermarkEnabled) return;
            if (ImgPreview.Source == null) return;
            var preset = GetActiveWatermark();
            if (preset == null) return;

            double cw = WatermarkOverlay.ActualWidth;
            double ch = WatermarkOverlay.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            var bitmapSource = ImgPreview.Source as BitmapSource;
            if (bitmapSource == null) return;

            double imgNatW = bitmapSource.PixelWidth;
            double imgNatH = bitmapSource.PixelHeight;
            double scale = Math.Min(cw / imgNatW, ch / imgNatH);
            double renderW = imgNatW * scale;
            double renderH = imgNatH * scale;
            double offsetX = (cw - renderW) / 2;
            double offsetY = (ch - renderH) / 2;

            double px = offsetX + preset.PositionX * renderW;
            double py = offsetY + preset.PositionY * renderH;
            double logoPx = offsetX + preset.LogoPositionX * renderW;
            double logoPy = offsetY + preset.LogoPositionY * renderH;

            if (!string.IsNullOrEmpty(preset.Text))
            {
                string hex = preset.Color.TrimStart('#');
                byte r = Convert.ToByte(hex.Length >= 2 ? hex[0..2] : "FF", 16);
                byte g = Convert.ToByte(hex.Length >= 4 ? hex[2..4] : "FF", 16);
                byte b = Convert.ToByte(hex.Length >= 6 ? hex[4..6] : "FF", 16);
                var brush = new SolidColorBrush(Color.FromArgb((byte)(preset.TextOpacity * 255), r, g, b));
                var tb = new TextBlock
                {
                    Text = preset.Text,
                    FontSize = Math.Max(10, preset.FontSize * renderW / 600),
                    Foreground = brush
                };
                tb.Measure(new Size(cw, ch));
                Canvas.SetLeft(tb, px - tb.DesiredSize.Width / 2);
                Canvas.SetTop(tb, py - tb.DesiredSize.Height / 2);
                WatermarkOverlay.Children.Add(tb);
            }

            if (!string.IsNullOrEmpty(preset.LogoPath) && File.Exists(preset.LogoPath))
            {
                try
                {
                    var bmp = new BitmapImage(new Uri(preset.LogoPath));
                    double logoW = renderW * Math.Min(preset.LogoScale, 0.6f);
                    double logoH = logoW * bmp.PixelHeight / bmp.PixelWidth;
                    if (logoH > renderH * 0.25) { logoH = renderH * 0.25; logoW = logoH * bmp.PixelWidth / bmp.PixelHeight; }
                    var img = new Image
                    {
                        Source = bmp,
                        Width = logoW,
                        Height = logoH,
                        Opacity = preset.LogoOpacity,
                        Stretch = Stretch.Uniform
                    };
                    Canvas.SetLeft(img, logoPx - logoW / 2);
                    Canvas.SetTop(img, logoPy - logoH / 2);
                    WatermarkOverlay.Children.Add(img);
                }
                catch { }
            }
        }

        private void WatermarkOverlay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WatermarkOverlay.ActualWidth > 0 && WatermarkOverlay.ActualHeight > 0)
                RefreshWatermarkOverlay();
        }

        // ── Thumbnail Sürükle-Bırak ───────────────────────────────────────────

        private void Thumb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            _dragThumb = (Border)sender;
            _dragStartPoint = e.GetPosition(ThumbPanel);
            _thumbDragging = false;
        }

        private void Thumb_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragThumb == null || e.LeftButton != MouseButtonState.Pressed) return;
            Point pos = e.GetPosition(ThumbPanel);
            double dx = Math.Abs(pos.X - _dragStartPoint.X);
            double dy = Math.Abs(pos.Y - _dragStartPoint.Y);

            if (!_thumbDragging)
            {
                bool hOk = dx > SystemParameters.MinimumHorizontalDragDistance * 2;
                bool vOk = dy > SystemParameters.MinimumVerticalDragDistance * 2;
                if (!hOk && !vOk) return;
                _thumbDragging = true;
                _dragThumb.CaptureMouse();
                _dragThumb.Opacity = 0.6;
                _dragThumb.RenderTransformOrigin = new Point(0.5, 0.5);
                _dragThumb.RenderTransform = new ScaleTransform(1.05, 1.05);
            }
            ShowInsertionLine(GetInsertionIndex(pos));
        }

        private void Thumb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragThumb == null) return;
            if (_thumbDragging)
            {
                _dragThumb.ReleaseMouseCapture();
                PerformReorder(_dragThumb, GetInsertionIndex(e.GetPosition(ThumbPanel)));
            }
            else
            {
                SelectThumbnail((string)_dragThumb.Tag);
            }
            _dragThumb.Opacity = 1;
            _dragThumb.RenderTransform = null;
            HideInsertionLine();
            _dragThumb = null;
            _thumbDragging = false;
        }

        private int GetInsertionIndex(Point mousePos)
        {
            var thumbs = ThumbPanel.Children.OfType<Border>()
                .Where(b => b != _dragThumb).ToList();
            for (int i = 0; i < thumbs.Count; i++)
            {
                try
                {
                    var transform = thumbs[i].TransformToAncestor(ThumbPanel);
                    var center = transform.Transform(new Point(thumbs[i].ActualWidth / 2, thumbs[i].ActualHeight / 2));
                    if (mousePos.Y < center.Y + thumbs[i].ActualHeight / 2 && mousePos.X < center.X)
                        return ThumbPanel.Children.IndexOf(thumbs[i]);
                }
                catch { }
            }
            return ThumbPanel.Children.OfType<Border>().Count(b => b != _dragThumb);
        }

        private void ShowInsertionLine(int insertIdx)
        {
            HideInsertionLine();
            _iInsertionLine = new System.Windows.Shapes.Rectangle
            {
                Width = 3,
                Height = 90,
                Fill = (Brush)FindResource("AccentBrush"),
                Margin = new Thickness(0, 4, 0, 4),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            ThumbPanel.Children.Insert(Math.Clamp(insertIdx, 0, ThumbPanel.Children.Count), _iInsertionLine);
        }

        private void HideInsertionLine()
        {
            if (_iInsertionLine != null)
            {
                ThumbPanel.Children.Remove(_iInsertionLine);
                _iInsertionLine = null;
            }
        }

        private void PerformReorder(Border dragThumb, int insertIdx)
        {
            string path = (string)dragThumb.Tag;
            int srcPathIdx = _iImagePaths.IndexOf(path);
            int srcThumbIdx = ThumbPanel.Children.IndexOf(dragThumb);
            if (srcPathIdx < 0 || srcThumbIdx < 0) return;

            _iImagePaths.RemoveAt(srcPathIdx);
            _iImagePaths.Insert(Math.Clamp(insertIdx > srcPathIdx ? insertIdx - 1 : insertIdx, 0, _iImagePaths.Count), path);

            ThumbPanel.Children.Remove(dragThumb);
            ThumbPanel.Children.Insert(Math.Clamp(insertIdx > srcThumbIdx ? insertIdx - 1 : insertIdx, 0, ThumbPanel.Children.Count), dragThumb);
        }
    }
}