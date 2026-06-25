using Microsoft.AspNetCore.Http;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IFileValidationService
{
    Task<ValidationResult> ValidateAsync(IEnumerable<IFormFile> files);
}
