using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IDriveTransferService
{
    Task<DriveUploadResult> UploadAsync(DriveUploadRequest request);
}
