namespace PDFMergeService.Core.Settings;

public class LdapSettings
{
    public string Domain { get; set; } = string.Empty;
    public string? Server { get; set; }
    public int? Port { get; set; }
    public string? ContainerDn { get; set; }
    public bool UseSsl { get; set; } = false;
}
