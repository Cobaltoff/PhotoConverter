using System.Windows;
using System.Windows.Input;

namespace PhotoConverterV2.Dialogs
{
    /// <summary>
    /// Toplu yeniden adlandırma için temel isim girişi dialog'u.
    /// Kullanım:
    ///   var dlg = new RenameDialog(lang: "EN", initialName: "product") { Owner = this };
    ///   if (dlg.ShowDialog() == true) baseName = dlg.BaseName;
    /// </summary>
    public partial class RenameDialog : Window
    {
        private readonly string _lang;

        /// <summary>Kullanıcının girdiği temel isim. ShowDialog() == true ise geçerlidir.</summary>
        public string BaseName { get; private set; } = "product";

        public RenameDialog(string lang = "EN", string initialName = "product")
        {
            InitializeComponent();
            _lang           = lang;
            BaseName        = initialName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();
            TxtBaseName.Text   = BaseName;
            TxtBaseName.Focus();
            TxtBaseName.SelectAll();
        }

        private void ApplyLanguage()
        {
            bool tr = _lang == "TR";
            Title            = tr ? "Yeniden Adlandır"                                  : "Rename";
            LblHint.Text     = tr ? "Dosyalar şu şekilde kaydedilir: isim_1.jpg ..."    : "Files will be saved as: name_1.jpg ...";
            LblBaseName.Text = tr ? "Temel isim:"                                        : "Base name:";
            BtnOk.Content    = "OK";
            BtnCancel.Content = tr ? "İptal" : "Cancel";
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtBaseName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            BaseName     = name;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void TxtBaseName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  { BtnOk_Click(sender, new RoutedEventArgs()); e.Handled = true; }
            if (e.Key == Key.Escape) { BtnCancel_Click(sender, new RoutedEventArgs()); e.Handled = true; }
        }
    }
}
