namespace PDFMergeService.Core.Models;

public class AdAuthenticationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static AdAuthenticationResult Ok() => new() { Success = true };
    public static AdAuthenticationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
