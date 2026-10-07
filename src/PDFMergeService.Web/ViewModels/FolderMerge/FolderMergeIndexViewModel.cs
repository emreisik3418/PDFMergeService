using PDFMergeService.Core.Settings;

namespace PDFMergeService.Web.ViewModels.FolderMerge;

public class FolderMergeIndexViewModel
{
    // Birleştirilmiş dosya adı önerisi için (Drive toplu yükleme kurallarının tersi, bkz. drive-path-resolver.js).
    public List<BulkUploadRule> BulkUploadRules { get; set; } = new();
}
