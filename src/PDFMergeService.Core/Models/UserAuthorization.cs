namespace PDFMergeService.Core.Models;

public class UserAuthorization
{
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? DisplayName { get; set; }
}
