namespace PDFMergeService.Core.Settings;

// Toplu yüklemede kurallardan önce denenen özel eşleştirme: dosya adı Contains metnini
// içeriyorsa dosya doğrudan TargetPath'e yüklenir. TargetPath'te {YIL} ve {CEYREK} kullanılabilir.
// Örnek: Contains = "YÖNETİM KURULU", TargetPath = "/Genel Müdürlük/Yönetim Kurulu/{YIL}"
public class BulkPathOverride
{
    public string Contains { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
}
