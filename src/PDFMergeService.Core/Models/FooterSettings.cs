using PDFMergeService.Core.Enums;

namespace PDFMergeService.Core.Models;

public class FooterSettings
{
    public bool PageNumberEnabled { get; set; } = true;
    public int StartFromPage { get; set; } = 2;
    public FooterPosition PageNumberPosition { get; set; } = FooterPosition.BottomLeft;
    public int FontSize { get; set; } = 10;
    public string FontColor { get; set; } = "#FF0000";

    public bool LogoEnabled { get; set; } = true;
    public string? CustomLogoPath { get; set; }
    public FooterPosition LogoPosition { get; set; } = FooterPosition.BottomRight;
    public double LogoWidth { get; set; } = 80;
    public double LogoHeight { get; set; } = 30;
    public double MarginBottom { get; set; } = 10;
    public double MarginHorizontal { get; set; } = 15;
}
