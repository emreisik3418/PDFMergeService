using Microsoft.AspNetCore.Http;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IPdfInfoService
{
    Task<PdfFileInfo> GetFileInfoAsync(IFormFile file);
}
