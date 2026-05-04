using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PhotoConverterV2.Dialogs
{
    /// <summary>
    /// Tam ekran görüntü önizleme penceresi.
    /// ESC tuşu veya fareyle tıklanarak kapatılır.
    /// </summary>
    public partial class FullscreenPreview : Window
    {
        public FullscreenPreview(string imagePath, string lang = "EN")
        {
            InitializeComponent();

            // Görüntüyü yükle
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource   = new Uri(imagePath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            ImgFull.Source = bmp;

            // Dil
            LblHint.Text = lang == "TR"
                ? "Kapatmak için tıklayın veya ESC'ye basın"
                : "Click or press ESC to close";
        }

        private void Window_Click(object sender, MouseButtonEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
