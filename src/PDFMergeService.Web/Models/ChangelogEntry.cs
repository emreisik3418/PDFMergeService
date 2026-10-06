namespace PDFMergeService.Web.Models;

// Data/changelog.json kayıtları; Yenilikler sayfasında gösterilir.
public class ChangelogEntry
{
    // Kullanıcının bu kaydı görüp görmediğini tarayıcıda izlemek için kullanılır; her kayıtta benzersiz olmalı.
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<ChangelogItem> Items { get; set; } = new();
}

public class ChangelogItem
{
    // "new" | "improvement" | "fix"
    public string Type { get; set; } = "new";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Yalnızca yöneticileri ilgilendiren maddeler (ör. ayar sayfaları) diğer kullanıcılara gösterilmez.
    public bool AdminOnly { get; set; }
}
