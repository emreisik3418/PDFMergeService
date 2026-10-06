namespace PDFMergeService.Core.Settings;

// Örnek: "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3.Çeyrek.pdf"
//   FileSuffix         = "KURUMSAL ŞUBESİ"            (dosya adında aranır, önündeki "BAŞKENT" bölge adıdır)
//   FolderSuffix       = "KURUMSAL ŞUBELER"           (klasör adında ekin yerine geçer; boşsa dosya adındaki ek kullanılır)
//   RootPath           = "/performans/Kurumsal Şubeler"
//   PeriodFolderFormat = "{YIL} - {CEYREK}. Çeyrek"
//   → /performans/Kurumsal Şubeler/BAŞKENT KURUMSAL ŞUBELER/2026 - 3. Çeyrek
public class BulkUploadRule
{
    public string FileSuffix { get; set; } = string.Empty;
    public string FolderSuffix { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string PeriodFolderFormat { get; set; } = string.Empty;
}
