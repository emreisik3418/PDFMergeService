using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

// Singleton: JSON dosyasına yazımı tek instance'ta serileştirip okumaları bellekten karşılamak için.
// Dosya yoksa ilk okumada appsettings içindeki SharePointSettings değerleriyle oluşturulur.
public class FileDrivePathConfigService : IDrivePathConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly DrivePathConfigSettings _settings;
    private readonly SharePointSettings _sharePointSettings;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DrivePathConfig? _cache;

    public FileDrivePathConfigService(
        IOptions<DrivePathConfigSettings> settings,
        IOptions<SharePointSettings> sharePointSettings)
    {
        _settings = settings.Value;
        _sharePointSettings = sharePointSettings.Value;
    }

    public async Task<DrivePathConfig> GetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _cache ??= await LoadOrSeedAsync();
            return Clone(_cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(DrivePathConfig config)
    {
        await _lock.WaitAsync();
        try
        {
            await WriteAsync(config);
            _cache = Clone(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<DrivePathConfig> LoadOrSeedAsync()
    {
        if (File.Exists(_settings.FilePath))
        {
            var json = await File.ReadAllTextAsync(_settings.FilePath);
            return JsonSerializer.Deserialize<DrivePathConfig>(json, JsonOptions) ?? new DrivePathConfig();
        }

        var seed = new DrivePathConfig
        {
            WebPathOptions = _sharePointSettings.WebPathOptions,
            BulkRootPathOptions = _sharePointSettings.BulkRootPathOptions,
            BulkWebPath = _sharePointSettings.BulkWebPath,
            RegionSuffixes = _sharePointSettings.RegionSuffixes
        };
        await WriteAsync(seed);
        return seed;
    }

    // Önce geçici dosyaya yazıp sonra yer değiştirerek yarım kalmış bir yazımın dosyayı bozmasını önler.
    private async Task WriteAsync(DrivePathConfig config)
    {
        var directory = Path.GetDirectoryName(_settings.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = _settings.FilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tempPath, _settings.FilePath, overwrite: true);
    }

    private static DrivePathConfig Clone(DrivePathConfig config) => new()
    {
        WebPathOptions = config.WebPathOptions.Select(o => new WebPathOption { Label = o.Label, Value = o.Value }).ToList(),
        BulkRootPathOptions = config.BulkRootPathOptions.Select(o => new WebPathOption { Label = o.Label, Value = o.Value }).ToList(),
        BulkWebPath = config.BulkWebPath,
        RegionSuffixes = config.RegionSuffixes.ToList()
    };
}
