using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IDrivePathConfigService
{
    Task<DrivePathConfig> GetAsync();

    Task SaveAsync(DrivePathConfig config);
}
