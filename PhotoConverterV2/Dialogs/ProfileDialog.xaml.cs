using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhotoConverterV2.Models;

namespace PhotoConverterV2.Dialogs
{
    /// <summary>
    /// Platform profili ekleme / düzenleme / silme penceresi.
    /// Constructor'a mevcut profil listesi ve dil kodu verilir.
    /// Kapatıldıktan sonra <see cref="UpdatedProfiles"/> ile güncel liste alınır.
    /// </summary>
    public partial class ProfileDialog : Window
    {
        // ── Alanlar ──────────────────────────────────────────────────────────
        private readonly string _lang;
        private List<PlatformProfile> _profiles;
        private PlatformProfile? _selected;

        /// <summary>Dialog kapatıldıktan sonra dışarıdan okunur.</summary>
        public List<PlatformProfile> UpdatedProfiles => _profiles;

        // ── Constructor ──────────────────────────────────────────────────────
        public ProfileDialog(List<PlatformProfile> profiles, string lang = "EN")
        {
            InitializeComponent();
            _lang     = lang;
            _profiles = new List<PlatformProfile>(profiles); // kopya üzerinde çalış
            ApplyLanguage();
        }

        // ── Başlangıç ────────────────────────────────────────────────────────
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
            if (LstProfiles.Items.Count > 0)
                LstProfiles.SelectedIndex = 0;
        }

        // ── Listeyi Yenile ───────────────────────────────────────────────────
        private void RefreshList(string? selectName = null)
        {
            LstProfiles.Items.Clear();
            foreach (var p in _profiles)
                LstProfiles.Items.Add(p.Name);

            if (selectName != null)
            {
                int idx = _profiles.FindIndex(p => p.Name == selectName);
                if (idx >= 0) LstProfiles.SelectedIndex = idx;
            }
        }

        // ── Seçim Değişti ─────────────────────────────────────────────────────
        private void LstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = LstProfiles.SelectedIndex;
            if (idx < 0 || idx >= _profiles.Count) { ClearForm(); return; }

            _selected = _profiles[idx];
            LoadProfileToForm(_selected);
        }

        // ── Forma Yükle ───────────────────────────────────────────────────────
        private void LoadProfileToForm(PlatformProfile p)
        {
            TxtName.Text    = p.Name;
            TxtWidth.Text   = p.Width.ToString();
            TxtHeight.Text  = p.Height.ToString();
            TxtQuality.Text = p.JpegQuality.ToString();

            SelectCombo(CboFormat, p.Format);
            SelectCombo(CboAspect, p.AspectRatioMode);
            SelectCombo(CboPadColor, p.PadColor);

            PadColorSection.Visibility = p.AspectRatioMode == "Pad"
                ? Visibility.Visible : Visibility.Collapsed;

            // Built-in uyarısı
            BuiltInWarning.Visibility = p.IsBuiltIn ? Visibility.Visible : Visibility.Collapsed;
            BtnDelete.IsEnabled       = !p.IsBuiltIn;

            // Built-in profillerde ad ve boyut düzenlenemez
            TxtName.IsReadOnly   = p.IsBuiltIn;
            TxtWidth.IsReadOnly  = p.IsBuiltIn;
            TxtHeight.IsReadOnly = p.IsBuiltIn;
        }

        private void ClearForm()
        {
            TxtName.Text    = "";
            TxtWidth.Text   = "";
            TxtHeight.Text  = "";
            TxtQuality.Text = "";
            _selected = null;
        }

        // ── + Add ────────────────────────────────────────────────────────────
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new PlatformProfile
            {
                Name           = "New Profile",
                Width          = 1200,
                Height         = 1200,
                Format         = "JPEG",
                JpegQuality    = 90,
                AspectRatioMode = "Crop",
                PadColor       = "White",
                IsBuiltIn      = false
            };
            _profiles.Add(newProfile);
            RefreshList(newProfile.Name);
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            bool tr = _lang == "TR";

            if (_selected.IsBuiltIn)
            {
                MessageBox.Show(
                    tr ? "Yerleşik profiller silinemez." : "Built-in profiles cannot be deleted.",
                    Title);
                return;
            }

            _profiles.Remove(_selected);
            _selected = null;
            RefreshList();
            if (LstProfiles.Items.Count > 0)
                LstProfiles.SelectedIndex = 0;
        }

        // ── Save Profile ──────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;
            bool tr = _lang == "TR";

            // Doğrulama
            string name = TxtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(tr ? "Profil adı boş olamaz." : "Profile name cannot be empty.", Title);
                return;
            }

            if (!int.TryParse(TxtWidth.Text, out int w) || w < 100)
            {
                MessageBox.Show(tr ? "Geçerli bir genişlik girin (min 100)." : "Enter a valid width (min 100).", Title);
                return;
            }

            if (!int.TryParse(TxtHeight.Text, out int h) || h < 100)
            {
                MessageBox.Show(tr ? "Geçerli bir yükseklik girin (min 100)." : "Enter a valid height (min 100).", Title);
                return;
            }

            if (!int.TryParse(TxtQuality.Text, out int q) || q < 1 || q > 100)
            {
                MessageBox.Show(tr ? "JPEG kalitesi 1-100 arasında olmalı." : "JPEG quality must be between 1-100.", Title);
                return;
            }

            // Built-in profillerde ad/boyut değiştirilemez
            if (!_selected.IsBuiltIn)
            {
                _selected.Name   = name;
                _selected.Width  = w;
                _selected.Height = h;
            }

            _selected.JpegQuality     = q;
            _selected.Format          = (CboFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "JPEG";
            _selected.AspectRatioMode = (CboAspect.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Crop";
            _selected.PadColor        = (CboPadColor.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "White";

            RefreshList(_selected.Name);
            MessageBox.Show(tr ? "Profil kaydedildi." : "Profile saved.", Title);
        }

        // ── Format / Aspect Değişikliği ───────────────────────────────────────
        private void CboFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void CboAspect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string mode = (CboAspect.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Crop";
            if (PadColorSection != null)
                PadColorSection.Visibility = mode == "Pad" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Kapat ────────────────────────────────────────────────────────────
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Dil ──────────────────────────────────────────────────────────────
        private void ApplyLanguage()
        {
            bool tr = _lang == "TR";
            Title                          = tr ? "Platform Profilleri"    : "Platform Profiles";
            LblProfilesTitle.Text          = tr ? "Platform Profilleri"    : "Platform Profiles";
            LblDetailTitle.Text            = tr ? "Profil Detayları"       : "Profile Details";
            LblNameField.Text              = tr ? "Profil Adı"             : "Profile Name";
            LblWidthField.Text             = tr ? "Genişlik (px)"          : "Width (px)";
            LblHeightField.Text            = tr ? "Yükseklik (px)"         : "Height (px)";
            LblFormatField.Text            = tr ? "Format"                 : "Format";
            LblQualityField.Text           = tr ? "JPEG Kalitesi (1-100)"  : "JPEG Quality (1-100)";
            LblAspectField.Text            = tr ? "Oran Modu"              : "Aspect Ratio Mode";
            LblPadColorField.Text          = tr ? "Dolgu Rengi"            : "Pad Color";
            LblBuiltInWarning.Text         = tr ? "Yerleşik profiller silinemez." : "Built-in profiles cannot be deleted.";
            BtnAdd.Content                 = tr ? "+ Ekle"                 : "+ Add";
            BtnDelete.Content              = tr ? "Sil"                    : "Delete";
            BtnSave.Content                = tr ? "Profili Kaydet"         : "Save Profile";
            BtnClose.Content               = tr ? "Kapat"                  : "Close";
        }

        // ── Yardımcı: ComboBox seçimi ─────────────────────────────────────────
        private static void SelectCombo(ComboBox cbo, string value)
        {
            foreach (ComboBoxItem item in cbo.Items)
                if (item.Content?.ToString() == value) { cbo.SelectedItem = item; return; }
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
        }
    }
}
