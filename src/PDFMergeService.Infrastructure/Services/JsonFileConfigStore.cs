using System.Text.Encodings.Web;
using System.Text.Json;

namespace PDFMergeService.Infrastructure.Services;

// Ön yüzden düzenlenen ayarların App_Data altındaki JSON dosyasında tutulması için ortak taban.
// Singleton olarak kaydedilmeli: yazımlar tek instance'ta serileştirilir, okumalar bellekten karşılanır.
// Dosya yoksa ilk okumada CreateSeed() (genelde appsettings değerleri) ile oluşturulur.
public abstract class JsonFileConfigStore<T> where T : class, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private T? _cache;

    protected abstract string FilePath { get; }

    protected abstract T CreateSeed();

    // Eski sürümde yazılmış dosyayı güncel şemaya tamamlamak için; değişiklik yaptıysa true döner ve dosya yeniden yazılır.
    protected virtual bool Migrate(T config, JsonElement root) => false;

    public async Task<T> GetAsync()
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

    public async Task SaveAsync(T config)
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

    private async Task<T> LoadOrSeedAsync()
    {
        if (File.Exists(FilePath))
        {
            var json = await File.ReadAllTextAsync(FilePath);
            var config = JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();

            using var document = JsonDocument.Parse(json);
            if (Migrate(config, document.RootElement))
                await WriteAsync(config);

            return config;
        }

        var seed = Clone(CreateSeed());
        await WriteAsync(seed);
        return seed;
    }

    // Önce geçici dosyaya yazıp sonra yer değiştirerek yarım kalmış bir yazımın dosyayı bozmasını önler.
    private async Task WriteAsync(T config)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = FilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }

    // Önbellekteki nesnenin çağıranlar tarafından değiştirilmemesi için derin kopya.
    protected static TValue Clone<TValue>(TValue value) =>
        JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;
}
