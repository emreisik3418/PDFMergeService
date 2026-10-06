namespace PDFMergeService.Core.Settings;

// FileSuffix dosya adında aranan kalıptır; {AD} şube/bölge adını yakalar.
// {AD} yazılmamışsa ad ekin önündedir ("KURUMSAL ŞUBESİ" = "{AD} KURUMSAL ŞUBESİ").
//
// Örnek 1: "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3. Çeyrek.pdf"
//   FileSuffix         = "KURUMSAL ŞUBESİ"
//   FolderSuffix       = "KURUMSAL ŞUBELER"      ({AD} yoksa adın arkasına eklenir; boşsa dosya adındaki ifade aynen kullanılır)
//   RootPath           = "/Kurumsal Şubeler"
//   PeriodFolderFormat = "{YIL} - {CEYREK}. Çeyrek" (boşsa dönem klasörü açılmaz)
//   → /Kurumsal Şubeler/BAŞKENT KURUMSAL ŞUBELER/2026 - 3. Çeyrek
//
// Örnek 2: "ÖZEL BAŞKENT ŞUBESİ 2026 - 3. Çeyrek.pdf"
//   FileSuffix = "ÖZEL {AD} ŞUBESİ", FolderSuffix = "" → /<RootPath>/ÖZEL BAŞKENT ŞUBESİ/2026 - 3. Çeyrek
public class BulkUploadRule
{
    public string FileSuffix { get; set; } = string.Empty;
    public string FolderSuffix { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string PeriodFolderFormat { get; set; } = string.Empty;
}
