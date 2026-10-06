using System.Text;
using System.Text.Json;
using PDFMergeService.Web.Models;

namespace PDFMergeService.Web.Services;

// Data/changelog.json dosyası uygulamayla birlikte yayınlanır ve yalnızca deploy'da değişir;
// bu yüzden ilk okumada belleğe alınır (Singleton).
public class ChangelogService
{
    private readonly Lazy<IReadOnlyList<ChangelogEntry>> _entries;

    public ChangelogService(IWebHostEnvironment environment, ILogger<ChangelogService> logger)
    {
        _entries = new Lazy<IReadOnlyList<ChangelogEntry>>(() =>
        {
            var path = Path.Combine(environment.ContentRootPath, "Data", "changelog.json");
            try
            {
                var entries = JsonSerializer.Deserialize<List<ChangelogEntry>>(ReadText(path, logger)) ?? new();
                return entries.OrderByDescending(e => e.Date).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Yenilikler dosyası okunamadı: {Path}", path);
                return Array.Empty<ChangelogEntry>();
            }
        });
    }

    // Admin olmayanlar için yalnızca yöneticilere yönelik maddeler çıkarılır; maddesi kalmayan kayıt da gizlenir.
    public IReadOnlyList<ChangelogEntry> GetEntries(bool isAdmin) =>
        _entries.Value
            .Select(e => new ChangelogEntry
            {
                Id = e.Id,
                Date = e.Date,
                Title = e.Title,
                Items = e.Items.Where(i => isAdmin || !i.AdminOnly).ToList()
            })
            .Where(e => e.Items.Count > 0)
            .ToList();

    public string? GetLatestId(bool isAdmin) => GetEntries(isAdmin).FirstOrDefault()?.Id;

    // Dosya UTF-8 olmalı; bir editörde ANSI (Windows-1254) olarak kaydedilmişse Türkçe karakterler
    // "�" olarak görünmesin diye o kod sayfasıyla okunur ve uyarı loglanır.
    private static string ReadText(string path, ILogger logger)
    {
        var bytes = File.ReadAllBytes(path);
        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes)
                .TrimStart('﻿');
        }
        catch (DecoderFallbackException)
        {
            logger.LogWarning("Yenilikler dosyası UTF-8 değil, Windows-1254 olarak okunuyor; dosyayı UTF-8 olarak kaydedin: {Path}", path);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1254).GetString(bytes);
        }
    }
}
