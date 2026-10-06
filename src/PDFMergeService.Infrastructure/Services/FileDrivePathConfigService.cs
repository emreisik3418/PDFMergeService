using System.Text.Json;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

// Dosya yoksa ilk okumada appsettings içindeki SharePointSettings değerleriyle oluşturulur.
public class FileDrivePathConfigService : JsonFileConfigStore<DrivePathConfig>, IDrivePathConfigService
{
    private readonly DrivePathConfigSettings _settings;
    private readonly SharePointSettings _sharePointSettings;

    public FileDrivePathConfigService(
        IOptions<DrivePathConfigSettings> settings,
        IOptions<SharePointSettings> sharePointSettings)
    {
        _settings = settings.Value;
        _sharePointSettings = sharePointSettings.Value;
    }

    protected override string FilePath => _settings.FilePath;

    protected override DrivePathConfig CreateSeed() => new()
    {
        WebPathOptions = _sharePointSettings.WebPathOptions,
        BulkWebPath = _sharePointSettings.BulkWebPath,
        BulkUploadRules = _sharePointSettings.BulkUploadRules
    };

    // Kurallardan önceki sürümde oluşturulmuş dosya: kuralları appsettings'ten tamamla.
    protected override bool Migrate(DrivePathConfig config, JsonElement root)
    {
        if (root.TryGetProperty(nameof(DrivePathConfig.BulkUploadRules), out _))
            return false;

        config.BulkUploadRules = Clone(_sharePointSettings.BulkUploadRules);
        return true;
    }
}
