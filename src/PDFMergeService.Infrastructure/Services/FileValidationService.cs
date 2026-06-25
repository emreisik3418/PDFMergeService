using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

public class FileValidationService : IFileValidationService
{
    private readonly PdfSettings _settings;
    private static readonly string[] AllowedExtensions = { ".pdf" };
    private static readonly string[] AllowedMimeTypes = { "application/pdf", "application/x-pdf" };

    public FileValidationService(IOptions<PdfSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<ValidationResult> ValidateAsync(IEnumerable<IFormFile> files)
    {
        var fileList = files.ToList();

        if (fileList.Count == 0)
            return Task.FromResult(ValidationResult.Fail("En az bir PDF dosyası seçmelisiniz."));

        if (fileList.Count > _settings.MaxFileCount)
            return Task.FromResult(ValidationResult.Fail($"En fazla {_settings.MaxFileCount} dosya yükleyebilirsiniz."));

        var errors = new List<string>();
        long maxBytes = (long)_settings.MaxFileSizeMB * 1024 * 1024;

        foreach (var file in fileList)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add($"'{file.FileName}' geçersiz dosya türü. Sadece PDF kabul edilir.");
                continue;
            }

            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                errors.Add($"'{file.FileName}' geçersiz içerik türü.");
                continue;
            }

            if (file.Length > maxBytes)
                errors.Add($"'{file.FileName}' dosyası {_settings.MaxFileSizeMB} MB limitini aşıyor.");

            if (file.Length == 0)
                errors.Add($"'{file.FileName}' boş dosya.");
        }

        return errors.Count > 0
            ? Task.FromResult(ValidationResult.Fail(errors.ToArray()))
            : Task.FromResult(ValidationResult.Success());
    }
}
