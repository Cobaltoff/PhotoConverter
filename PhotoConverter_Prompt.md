# 📋 PROJECT PROMPT — Photo Converter (WPF Desktop App)

## Genel Bakış

E-ticaret satıcıları için **masaüstü ürün fotoğrafı dönüştürme uygulaması** geliştir. Trendyol, N11, Hepsiburada gibi platformların teknik gereksinimlerine (boyut, format, oran, kalite) uygun şekilde fotoğrafları toplu olarak dönüştürmek için kullanılacak.

---

## Teknoloji Yığını

- **Dil/Framework:** C# + WPF (.NET 8)
- **IDE:** Visual Studio 2022/2026
- **Hedef Platform:** Windows
- **Çıktı:** Tek `.exe` dosyası (Self-contained, Single file publish — başka bilgisayarda kurulum gerektirmemeli)

### NuGet Paketleri
- `SixLabors.ImageSharp` — görüntü işleme
- `SixLabors.ImageSharp.Drawing` — kırpma/padding işlemleri
- `Newtonsoft.Json` — ayarları kalıcı kaydetmek
- `Microsoft.Xaml.Behaviors.Wpf` — UI davranışları

---

## Klasör/Dosya Yapısı

```
PhotoConverter/
├── Models/
│   ├── AppSettings.cs
│   └── PlatformProfile.cs
├── Services/
│   ├── SettingsService.cs
│   └── ImageProcessingService.cs
├── Dialogs/
│   ├── RenameDialog.xaml + .cs
│   ├── ProfileDialog.xaml + .cs
│   ├── CropDialog.xaml + .cs
│   └── FullscreenPreview.xaml + .cs
├── MainWindow.xaml + .cs
└── App.xaml
```

---

## Veri Modelleri

### `AppSettings`
Kullanıcı tercihlerini saklar — JSON olarak `%AppData%\PhotoConverter\settings.json` içine yazılır.
- `Theme` (Light/Dark) — varsayılan: Light
- `Language` (EN/TR) — **varsayılan: EN**
- `OutputFolder` (string) — son kullanılan çıktı klasörü
- `LastPlatform` (string) — son seçilen platform adı
- `PreviewPanelVisible` (bool) — önizleme paneli açık mı
- `Profiles` (List\<PlatformProfile\>) — yerleşik + kullanıcı profilleri

### `PlatformProfile`
- `Name` — profil adı
- `Width`, `Height` (int, px)
- `Format` (JPEG/PNG)
- `JpegQuality` (1-100)
- `AspectRatioMode` (Crop/Pad/Stretch)
- `PadColor` (White/Black)
- `IsBuiltIn` (bool) — yerleşik profiller silinemez

### Yerleşik Profiller (Built-in)
| Platform | Boyut | Format | Kalite | Oran Modu |
|---|---|---|---|---|
| Trendyol | 1200×1800 | JPEG | 90 | Crop |
| N11 | 1200×1200 | JPEG | 90 | Crop |
| Hepsiburada | 1500×1500 | JPEG | 90 | Crop |

---

## Fonksiyonel Gereksinimler

### 1. Fotoğraf Ekleme
- **Dosya seç butonu** ile çoklu dosya seçimi
- **Sürükle & bırak** ile pencereye fotoğraf bırakma
- Desteklenen formatlar: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.webp`, `.tiff`
- Eklenen fotoğraflar drop-zone bölgesinde **küçük thumbnail** olarak gösterilir (80×80 px)
- Thumbnail'a tıklanırsa o fotoğraf önizleme panelinde gösterilir
- Thumbnail üzerinde sağ tık → "Crop" ve "Remove" seçenekleri

### 2. Platform & Ayarlar
- **Platform ComboBox'u (isteğe bağlı):** Profil seçilirse ayarlar otomatik dolar. Kullanıcı manuel de girebilir.
- **Width / Height** (px) — TextBox
- **Output Format** — JPEG / PNG
- **JPEG Quality** — 1-100 arası
- **Aspect Mode** — Crop / Pad / Stretch
- **⚙ Profiles** butonu → Profil yönetimi popup'ı açar
- Tüm değerler değiştirilene kadar **kalıcı** (settings.json'a kaydedilir)

### 3. Profil Yönetimi (ProfileDialog)
- Sol tarafta profil listesi
- Sağ tarafta seçili profilin detayları (ad, boyut, format, kalite, oran modu, pad rengi)
- **+ Add** ile yeni profil eklenebilir
- **Delete** ile sadece kullanıcının eklediği profiller silinir (yerleşikler silinemez)
- **Save Profile** ile değişiklikler uygulanır
- Tüm metinler dil ayarına göre çevrilmeli

### 4. Çıktı Klasörü
- 📁 butonu ile klasör seçilir (`OpenFolderDialog`)
- Seçili klasör TextBox'ta gösterilir (read-only)
- Kalıcı olarak hatırlanır

### 5. Toplu Yeniden Adlandırma (Rename — İsteğe Bağlı)
- Toggle butonla aktif/pasif
- Aktifse: Aşağıda bir banner açılır, kullanıcı **temel isim** girer (örn: "product")
- Çıktılar: `product_1.jpg`, `product_2.jpg` ... şeklinde kaydedilir
- Pasifse: Orijinal dosya isimleri korunur

### 6. Önizleme Paneli (Toggle)
- Sağ tarafta sabit panel — toggle butonla açılıp kapanabilir
- Seçili thumbnail'ın büyük önizlemesini gösterir
- Resme **tıklanınca tam ekran** modunda açılır
- Tam ekranda ESC veya tıklama ile kapanır
- Açık/kapalı durumu kalıcı

### 7. Kırpma (Crop)
- Thumbnail üzerine sağ tıklayınca açılır
- Görselin üzerine **mouse ile sürükleyerek** kırpma alanı çizilir
- Hedef boyutu (Width×Height) ekranda gösterilir
- "Apply Crop" → kırpılmış görsel temp klasörüne kaydedilir, listedeki yolu güncellenir
- "Reset" → seçimi sıfırlar
- "Cancel" → değişiklik yapmaz
- **Oran uyumsuzluğunda zorunlu uyarı:** Convert sırasında giriş ve hedef oranı uyuşmazsa kullanıcıya "Crop?" diye sorulur

### 8. Dönüştürme (Convert)
- En az 1 fotoğraf ve geçerli çıktı klasörü gerekir
- **PNG girdi varsa ve format JPEG seçiliyse** kullanıcıya şu sorulur:
  > "Bazı girdiler PNG formatında. Çıktıyı JPEG olarak mı kaydetmek istiyorsunuz?"
- Aspect Mode kontrolü:
  - **Crop:** Hedef orana göre ortadan kırparak yeniden boyutlandırır
  - **Pad:** Hedef orana göre boş alanları beyaz/siyah ile doldurur
  - **Stretch:** Oranı görmezden gelir, doğrudan boyutlandırır
- Dönüştürme **async** çalışır, alt barda ilerleme gösterilir
- Sonunda "Done: X succeeded, Y failed" mesajı

### 9. Tema (Light/Dark)
- Sağ üstte 🌙 / ☀ butonu
- Tüm UI elemanları (background, text, border, input alanlar) tema ile değişmeli
- `Resources["BrushName"] = ...` doğrudan atama ile uygulanır (`MergedDictionaries.Clear()` KULLANMA — tüm kontrolleri güncellemiyor)
- Tema seçimi kalıcı

### 10. Dil (EN/TR)
- Sağ üstte tema butonunun **solunda** TR/EN butonu
- **Varsayılan: EN**
- Tüm metinler (label, buton, mesaj kutusu, dialog başlıkları) dile göre değişmeli
- Dil seçimi kalıcı

### 11. Pencere
- **Varsayılan boyut:** 1100×700 (tam ekran değil)
- **Min boyut:** 900×600
- Yeniden boyutlandırılabilir, tam ekran yapılabilir
- Açılışta ekranın ortasında
- `UseLayoutRounding="True"` eklenecek

---

## UI Layout

```
┌──────────────────────────────────────────────────────┐
│ 📷 Photo Converter                    [TR/EN] [🌙]  │  ← Header
├──────────────────────────────────────────────────────┤
│ ┌──────────────────────────────┐  ┌───────────────┐ │
│ │ Platform (Optional) | Format │  │               │ │
│ │ [⚙ Profiles]                │  │   PREVIEW     │ │
│ │ W(px) | H(px) | Q | Aspect  │  │   PANEL       │ │
│ ├──────────────────────────────┤  │  (toggle ile  │ │
│ │                              │  │   açılır)     │ │
│ │  DROP ZONE / THUMBNAILS      │  │               │ │
│ │  [thumb][thumb][thumb]...    │  │  [tıkla →     │ │
│ │                              │  │  fullscreen]  │ │
│ ├──────────────────────────────┤  │               │ │
│ │ Output: [_________] [📁]    │  │   filename    │ │
│ │ [✏ Rename] [👁 Preview]     │  │               │ │
│ │ ┌── Rename Banner ─────────┐ │  └───────────────┘ │
│ │ │ Base name: [product    ] │ │                    │
│ │ └──────────────────────────┘ │                    │
│ └──────────────────────────────┘                    │
├──────────────────────────────────────────────────────┤
│ 0 image(s) loaded          [Clear All] [✓ Convert]  │  ← Footer
└──────────────────────────────────────────────────────┘
```

---

## Kritik Teknik Notlar

### 1. PreviewPanel NullReferenceException
`Window_Loaded` içinde `BtnTogglePreview.IsChecked` set edilirken event'ler geçici kaldırılmalı:
```csharp
BtnTogglePreview.Checked -= BtnTogglePreview_Checked;
BtnTogglePreview.Unchecked -= BtnTogglePreview_Unchecked;
BtnTogglePreview.IsChecked = previewVisible;
PreviewPanel.Visibility = previewVisible ? Visibility.Visible : Visibility.Collapsed;
PreviewColumn.Width = previewVisible ? new GridLength(300) : new GridLength(0);
BtnTogglePreview.Checked += BtnTogglePreview_Checked;
BtnTogglePreview.Unchecked += BtnTogglePreview_Unchecked;
```

### 2. Tema Uygulaması
`MergedDictionaries` yerine doğrudan `Resources` atama kullan:
```csharp
Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 18));
Resources["PanelBgBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
// ... diğerleri
```

### 3. Rectangle Tipi Çakışması
`SixLabors.ImageSharp.Rectangle` kullan, `System.Drawing.Rectangle` ile karıştırma:
```csharp
// DOĞRU:
public SixLabors.ImageSharp.Rectangle CropResult { get; private set; }

// YANLIŞ (CS1503 hatası verir):
public System.Drawing.Rectangle CropResult { get; private set; }
```

### 4. StackPanel Spacing
WPF'te `StackPanel` öğesinin `Spacing` özelliği yoktur (UWP'ye özgü). Yerine:
```xml
<StackPanel Orientation="Horizontal">
    <StackPanel.Resources>
        <Style TargetType="Button">
            <Setter Property="Margin" Value="5,0,0,0"/>
        </Style>
    </StackPanel.Resources>
```

### 5. ResourceDictionary x:Key Hatası
`Window.Resources` içindeki `ResourceDictionary`'ye `x:Key` ekleme:
```xml
<!-- YANLIŞ: -->
<ResourceDictionary x:Key="LightTheme">

<!-- DOĞRU: -->
<ResourceDictionary>
```

### 6. Thumbnail Bellek Optimizasyonu
```csharp
bmp.DecodePixelWidth = 80; // Belleği küçültür
```

### 7. ProfileDialog Dil Desteği
`ProfileDialog` constructor'ı `lang` parametresi almalı:
```csharp
public ProfileDialog(List<PlatformProfile> profiles, string lang = "EN")
{
    InitializeComponent();
    _lang = lang;
    // ...
    ApplyLanguage();
}
```

---

## Renk Paleti

### Light Tema
| Key | Renk |
|-----|------|
| BackgroundBrush | `#f5f5f5` |
| PanelBgBrush | `#ffffff` |
| ForegroundBrush | `#1a1a1a` |
| SubtleBrush | `#666666` |
| BorderBrush2 | `#dddddd` |
| InputBgBrush | `#f9f9f9` |
| AccentBrush | `#0078d4` |
| DropZoneBrush | `#e8f4fd` |
| ThumbnailBgBrush | `#eeeeee` |

### Dark Tema
| Key | Renk |
|-----|------|
| BackgroundBrush | `#121212` |
| PanelBgBrush | `#1e1e1e` |
| ForegroundBrush | `#f0f0f0` |
| SubtleBrush | `#a0a0a0` |
| BorderBrush2 | `#373737` |
| InputBgBrush | `#282828` |
| AccentBrush | `#0078d4` |
| DropZoneBrush | `#142332` |
| ThumbnailBgBrush | `#2d2d2d` |

---

## Tüm Çevrilecek Metinler (EN → TR)

| EN | TR |
|----|----|
| Platform (Optional) | Platform (İsteğe Bağlı) |
| Output Format | Çıktı Formatı |
| Width (px) | Genişlik (px) |
| Height (px) | Yükseklik (px) |
| JPEG Quality | JPEG Kalitesi |
| Aspect Mode | Oran Modu |
| Output: | Çıktı: |
| Preview | Önizleme |
| ✏ Rename | ✏ Yeniden Adlandır |
| 👁 Preview | 👁 Önizleme |
| ✓ Convert | ✓ Dönüştür |
| Clear All | Temizle |
| Drop images here or | Görselleri buraya bırakın veya |
| browse | gözat |
| ⚙ Profiles | ⚙ Profiller |
| Platform Profiles | Platform Profilleri |
| Profile Name | Profil Adı |
| Format | Format |
| JPEG Quality (1-100) | JPEG Kalitesi (1-100) |
| Aspect Ratio Mode | Oran Modu |
| Pad Color | Dolgu Rengi |
| + Add | + Ekle |
| Delete | Sil |
| Save Profile | Profili Kaydet |
| ✂ Crop | ✂ Kırp |
| 🗑 Remove | 🗑 Kaldır |
| Ready | Hazır |
| X image(s) loaded | X görsel yüklendi |
| Converting... | Dönüştürülüyor... |
| Done: X succeeded, Y failed | Tamamlandı: X başarılı, Y başarısız |
| Please add images first. | Lütfen önce görsel ekleyin. |
| Please select a valid output folder. | Lütfen geçerli bir çıktı klasörü seçin. |
| Built-in profiles cannot be deleted. | Yerleşik profiller silinemez. |
| Profile name cannot be empty. | Profil adı boş olamaz. |
| Enter a valid width (min 100). | Geçerli bir genişlik girin (min 100). |
| Enter a valid height (min 100). | Geçerli bir yükseklik girin (min 100). |
| JPEG quality must be between 1-100. | JPEG kalitesi 1-100 arasında olmalı. |
| Profile saved. | Profil kaydedildi. |
| Click or press ESC to close | Kapatmak için tıklayın veya ESC'ye basın |
| Click and drag to select crop area | Kırpma alanı seçmek için sürükleyin |
| Files will be saved as: name_1.jpg ... | Dosyalar şu şekilde kaydedilir: isim_1.jpg ... |
| Base name: | Temel isim: |
| Select output folder | Çıktı klasörü seçin |

---

## Publish Ayarları (Tek EXE)

Visual Studio'da projeye sağ tıkla → **Publish** → **Folder** seç:
- **Configuration:** Release
- **Target Framework:** net8.0-windows
- **Deployment Mode:** Self-contained
- **Target Runtime:** win-x64
- **Produce single file:** ✅ İşaretle
- **Enable ReadyToRun:** ✅ İşaretle (opsiyonel, daha hızlı başlangıç)

Ya da `.csproj` dosyasına ekle:
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

---

*Bu prompt ile projeyi sıfırdan hatasız oluşturabilirsin. Mevcut projeyi geliştirmek için başına şunu ekle:*
> "Aşağıdaki spec'e göre yazılmış mevcut bir WPF projem var. Şu sorunları gidermeni istiyorum: [sorunları listele]."
