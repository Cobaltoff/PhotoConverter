using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PhotoConverterV2.Models;
using PhotoConverterV2.Services;

namespace PhotoConverterV2.Dialogs
{
    public partial class WatermarkDialog : Window
    {
        private AppSettings _settings;
        private string _lang;
        private bool _suppressUpdate;

        public event Action? WatermarkChanged;

        public WatermarkDialog(AppSettings settings, string lang = "EN")
        {
            InitializeComponent();
            _settings = settings;
            _lang = lang;

            

            LoadPresets();

            BtnWatermarkEnabled.IsChecked = _settings.WatermarkEnabled;
            CboWatermarkScope.SelectedIndex = _settings.WatermarkApplyToAll ? 0 : 1;
        }

        public void UpdateLanguage(string lang)
        {
            _lang = lang;
            bool tr = lang == "TR";
            Title = tr ? "🎨 Watermark Ayarları" : "🎨 Watermark Settings";

            LblMetin.Text = tr ? "Metin" : "Text";
            LblOpaklık.Text = tr ? "Opaklık" : "Opacity";
            LblBoyut.Text = tr ? "Boyut (pt)" : "Size (pt)";
            LblRenk.Text = tr ? "Renk (#HEX)" : "Color (#HEX)";
            LblLogo.Text = tr ? "Logo" : "Logo";
            LblLogoOpaklık.Text = tr ? "Logo Opaklık" : "Logo Opacity";
            LblLogoOlcek.Text = tr ? "Logo Ölçek" : "Logo Scale";
            LblMetinKonumu.Text = tr ? "Metin Konumu" : "Text Position";
            LblLogoKonumu.Text = tr ? "Logo Konumu" : "Logo Position";
            LblYatayMetin.Text = tr ? "Yatay (X)" : "Horizontal (X)";
            LblDikeyMetin.Text = tr ? "Dikey (Y)" : "Vertical (Y)";
            LblYatayLogo.Text = tr ? "Yatay (X)" : "Horizontal (X)";
            LblDikeyLogo.Text = tr ? "Dikey (Y)" : "Vertical (Y)";
            LblAd.Text = tr ? "Ad:" : "Name:";

            CboWatermarkScope.Items.Clear();
            CboWatermarkScope.Items.Add(new ComboBoxItem { Content = tr ? "Tüm fotoğraflar" : "All photos" });
            CboWatermarkScope.Items.Add(new ComboBoxItem { Content = tr ? "Seçili fotoğraf" : "Selected photo" });
            CboWatermarkScope.SelectedIndex = _settings.WatermarkApplyToAll ? 0 : 1;
        }

        private void LoadPresets()
        {
            CboWatermarkPreset.Items.Clear();
            foreach (var p in _settings.WatermarkPresets)
                CboWatermarkPreset.Items.Add(new ComboBoxItem { Content = p.Name, Tag = p });

            if (!string.IsNullOrEmpty(_settings.LastWatermarkPreset))
                foreach (ComboBoxItem item in CboWatermarkPreset.Items)
                    if (item.Content?.ToString() == _settings.LastWatermarkPreset)
                    { CboWatermarkPreset.SelectedItem = item; break; }

            if (CboWatermarkPreset.SelectedIndex < 0 && CboWatermarkPreset.Items.Count > 0)
                CboWatermarkPreset.SelectedIndex = 0;
        }

        private void CboWatermarkPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboWatermarkPreset.SelectedItem is not ComboBoxItem { Tag: WatermarkPreset p }) return;
            _settings.LastWatermarkPreset = p.Name;
            App.SettingsService.Save(_settings);
            PopulateFields(p);
            WatermarkChanged?.Invoke();
        }

        private void PopulateFields(WatermarkPreset p)
        {
            _suppressUpdate = true;
            TxtWmText.Text = p.Text ?? "";
            SldWmTextOpacity.Value = p.TextOpacity;
            SldWmFontSize.Value = p.FontSize;
            TxtWmColor.Text = p.Color;
            TxtWmLogoPath.Text = p.LogoPath ?? "";
            SldWmLogoOpacity.Value = p.LogoOpacity;
            SldWmLogoScale.Value = p.LogoScale;
            SldWmPosX.Value = p.PositionX;
            SldWmPosY.Value = p.PositionY;
            SldWmLogoPosX.Value = p.LogoPositionX;
            SldWmLogoPosY.Value = p.LogoPositionY;
            TxtPresetName.Text = p.Name;

            bool hasLogo = !string.IsNullOrEmpty(p.LogoPath) && File.Exists(p.LogoPath);
            SldWmLogoOpacity.IsEnabled = hasLogo;
            SldWmLogoScale.IsEnabled = hasLogo;
            _suppressUpdate = false;
        }

        private void WmField_Changed(object sender, object e)
        {
            if (_suppressUpdate) return;
            if (CboWatermarkPreset.SelectedItem is not ComboBoxItem { Tag: WatermarkPreset p }) return;

            p.Text = TxtWmText.Text.Trim().Length > 0 ? TxtWmText.Text.Trim() : null;
            p.TextOpacity = (float)SldWmTextOpacity.Value;
            p.FontSize = (float)SldWmFontSize.Value;
            p.Color = TxtWmColor.Text.Trim().Length > 0 ? TxtWmColor.Text.Trim() : "#FFFFFF";
            p.LogoPath = TxtWmLogoPath.Text.Trim().Length > 0 ? TxtWmLogoPath.Text.Trim() : null;
            p.LogoOpacity = (float)SldWmLogoOpacity.Value;
            p.LogoScale = (float)SldWmLogoScale.Value;
            p.PositionX = SldWmPosX.Value;
            p.PositionY = SldWmPosY.Value;
            p.LogoPositionX = SldWmLogoPosX.Value;
            p.LogoPositionY = SldWmLogoPosY.Value;

            bool hasLogo = !string.IsNullOrEmpty(p.LogoPath) && File.Exists(p.LogoPath);
            SldWmLogoOpacity.IsEnabled = hasLogo;
            SldWmLogoScale.IsEnabled = hasLogo;

            App.SettingsService.Save(_settings);
            WatermarkChanged?.Invoke();
        }

        private void WatermarkEnabled_Changed(object sender, RoutedEventArgs e)
        {
            _settings.WatermarkEnabled = BtnWatermarkEnabled.IsChecked == true;
            App.SettingsService.Save(_settings);
            WatermarkChanged?.Invoke();
        }

        private void WatermarkScope_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            _settings.WatermarkApplyToAll = CboWatermarkScope.SelectedIndex == 0;
            App.SettingsService.Save(_settings);
        }

        private void BtnNewWatermark_Click(object sender, RoutedEventArgs e)
        {
            var preset = new WatermarkPreset
            {
                Name = $"Watermark {_settings.WatermarkPresets.Count + 1}"
            };
            _settings.WatermarkPresets.Add(preset);
            App.SettingsService.Save(_settings);
            LoadPresets();
            foreach (ComboBoxItem item in CboWatermarkPreset.Items)
                if (item.Content?.ToString() == preset.Name)
                { CboWatermarkPreset.SelectedItem = item; break; }
        }

        private void BtnSaveWatermark_Click(object sender, RoutedEventArgs e)
        {
            App.SettingsService.Save(_settings);
            MessageBox.Show(
                _lang == "TR" ? "Watermark kaydedildi." : "Watermark saved.",
                "Photo Converter", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TxtPresetName_Changed(object sender, TextChangedEventArgs e)
        {
            if (_suppressUpdate) return;
            if (CboWatermarkPreset.SelectedItem is not ComboBoxItem { Tag: WatermarkPreset p }) return;
            if (string.IsNullOrWhiteSpace(TxtPresetName.Text)) return;
            p.Name = TxtPresetName.Text;
            if (CboWatermarkPreset.SelectedItem is ComboBoxItem item)
                item.Content = p.Name;
            App.SettingsService.Save(_settings);
        }

        private void BtnDeleteWatermark_Click(object sender, RoutedEventArgs e)
        {
            if (CboWatermarkPreset.SelectedItem is not ComboBoxItem { Tag: WatermarkPreset p }) return;
            _settings.WatermarkPresets.Remove(p);
            App.SettingsService.Save(_settings);
            LoadPresets();
            WatermarkChanged?.Invoke();
        }

        private void BtnWmLogo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
                Title = _lang == "TR" ? "Logo seçin" : "Select logo"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtWmLogoPath.Text = dlg.FileName;
                WmField_Changed(sender, e);
            }
        }

        private void BtnWmLogoClear_Click(object sender, RoutedEventArgs e)
        {
            TxtWmLogoPath.Text = "";
            WmField_Changed(sender, e);
        }
    }
}