using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PDFMergeService.Core.Enums;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

public class PdfFooterService : IPdfFooterService
{
    private readonly PdfSettings _settings;
    private readonly ILogger<PdfFooterService> _logger;

    public PdfFooterService(IOptions<PdfSettings> settings, ILogger<PdfFooterService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<byte[]> ApplyFooterAsync(byte[] pdfBytes, FooterSettings settings)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var doc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

        XImage? logoImage = null;

        if (settings.LogoEnabled)
        {
            var logoPath = string.IsNullOrWhiteSpace(settings.CustomLogoPath)
                ? _settings.DefaultLogoPath
                : settings.CustomLogoPath;

            if (File.Exists(logoPath))
            {
                try { logoImage = XImage.FromFile(logoPath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Logo yüklenemedi: {Path}", logoPath); }
            }
            else
            {
                _logger.LogWarning("Logo dosyası bulunamadı: {Path}", logoPath);
            }
        }

        for (int pageIndex = 0; pageIndex < doc.PageCount; pageIndex++)
        {
            var page = doc.Pages[pageIndex];
            var pageNumber = pageIndex + 1;

            using var gfx = XGraphics.FromPdfPage(page);

            if (settings.PageNumberEnabled && pageNumber >= settings.StartFromPage)
            {
                int displayNumber = pageNumber - settings.StartFromPage + 1;
                DrawPageNumber(gfx, page, settings, displayNumber);
            }

            if (settings.LogoEnabled && logoImage != null && !settings.LogoSkipPages.Contains(pageNumber))
                DrawLogo(gfx, page, settings, logoImage);
        }

        logoImage?.Dispose();

        using var outputStream = new MemoryStream();
        doc.Save(outputStream);
        return Task.FromResult(outputStream.ToArray());
    }

    private static void DrawPageNumber(XGraphics gfx, PdfPage page, FooterSettings settings, int pageNumber)
    {
        var color = ParseColor(settings.FontColor);
        var font = new XFont("Arial", settings.FontSize);
        var text = pageNumber.ToString();
        var size = gfx.MeasureString(text, font);

        var (x, y) = CalculateTextPosition(
            page.Width.Point, page.Height.Point,
            size.Width, size.Height,
            settings.PageNumberPosition,
            settings.MarginBottom, settings.MarginHorizontal);

        gfx.DrawString(text, font, new XSolidBrush(color), x, y);
    }

    private static void DrawLogo(XGraphics gfx, PdfPage page, FooterSettings settings, XImage logo)
    {
        double logoW = settings.LogoWidth;
        double logoH = settings.LogoHeight;

        double ratio = logo.PixelWidth / (double)logo.PixelHeight;
        if (logoW / logoH > ratio)
            logoW = logoH * ratio;
        else
            logoH = logoW / ratio;

        var (x, y) = CalculateImagePosition(
            page.Width.Point, page.Height.Point,
            logoW, logoH,
            settings.LogoPosition,
            settings.MarginBottom, settings.MarginHorizontal);

        gfx.DrawImage(logo, x, y, logoW, logoH);
    }

    private static (double x, double y) CalculateTextPosition(
        double pageW, double pageH, double textW, double textH,
        FooterPosition position, double marginBottom, double marginH)
    {
        double y = pageH - marginBottom - textH;
        double x = position switch
        {
            FooterPosition.BottomLeft => marginH,
            FooterPosition.BottomCenter => (pageW - textW) / 2,
            FooterPosition.BottomRight => pageW - textW - marginH,
            _ => marginH
        };
        return (x, y);
    }

    private static (double x, double y) CalculateImagePosition(
        double pageW, double pageH, double imgW, double imgH,
        FooterPosition position, double marginBottom, double marginH)
    {
        double y = pageH - marginBottom - imgH;
        double x = position switch
        {
            FooterPosition.BottomLeft => marginH,
            FooterPosition.BottomCenter => (pageW - imgW) / 2,
            FooterPosition.BottomRight => pageW - imgW - marginH,
            _ => pageW - imgW - marginH
        };
        return (x, y);
    }

    private static XColor ParseColor(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                return XColor.FromArgb(r, g, b);
            }
        }
        catch { }
        return XColors.Black;
    }
}
