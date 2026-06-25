using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IPdfMergeService
{
    Task<byte[]> MergeAsync(PdfMergeRequest request);
}
