using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IPdfFooterService
{
    Task<byte[]> ApplyFooterAsync(byte[] pdfBytes, FooterSettings settings);
}
