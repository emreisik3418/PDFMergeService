using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

// Dosya yoksa ilk okumada appsettings içindeki PdfSettings değerleriyle oluşturulur.
public class FilePdfMergeLimitsService : JsonFileConfigStore<PdfMergeLimits>, IPdfMergeLimitsService
{
    private readonly PdfSettings _settings;

    public FilePdfMergeLimitsService(IOptions<PdfSettings> settings)
    {
        _settings = settings.Value;
    }

    protected override string FilePath => _settings.LimitsFilePath;

    protected override PdfMergeLimits CreateSeed() => new()
    {
        MaxFileCount = _settings.MaxFileCount,
        MaxFileSizeMB = _settings.MaxFileSizeMB
    };
}
