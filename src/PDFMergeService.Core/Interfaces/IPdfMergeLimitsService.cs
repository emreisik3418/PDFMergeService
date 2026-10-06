using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IPdfMergeLimitsService
{
    Task<PdfMergeLimits> GetAsync();

    Task SaveAsync(PdfMergeLimits limits);
}
