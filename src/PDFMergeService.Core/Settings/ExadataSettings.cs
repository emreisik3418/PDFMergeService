namespace PDFMergeService.Core.Settings;

public class ExadataSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string AuthorizationQuery { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 15;
}
