namespace PDFMergeService.Core.Settings;

public class AuthorizedUsersSettings
{
    public List<AuthorizedUserEntry> Users { get; set; } = new();
}

public class AuthorizedUserEntry
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? DisplayName { get; set; }
}
