using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IAdAuthenticationService
{
    Task<AdAuthenticationResult> ValidateCredentialsAsync(string username, string password);
}
