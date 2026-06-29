using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

public class PdfLogoDetectionService : IPdfLogoDetectionService
{
    private readonly PdfSettings _settings;
    private readonly ILogger<PdfLogoDetectionService> _logger;

    // 64 bitlik hash'te maksimum farklılık toleransı
    private const int HashThreshold = 8;
    private const double AspectTolerance = 0.25;

    // İstek başına logo imzasını bir kez hesapla
    private string? _cachedLogoPath;
    private ulong _cachedLogoHash;
    private double _cachedLogoAspect;

    public PdfLogoDetectionService(IOptions<PdfSettings> settings, ILogger<PdfLogoDetectionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<LogoDetectionResult> DetectLogoPagesAsync(string pdfPath, string? customLogoPath = null)
    {
        var result = new LogoDetectionResult();

        var logoPath = string.IsNullOrWhiteSpace(customLogoPath)
            ? _settings.DefaultLogoPath
            : customLogoPath;

        if (!File.Exists(logoPath) || !File.Exists(pdfPath))
            return Task.FromResult(result);

        ulong logoHash;
        double logoAspect;

        try
        {
            (logoHash, logoAspect) = GetLogoSignature(logoPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logo imzası hesaplanamadı: {Path}", logoPath);
            return Task.FromResult(result);
        }

        try
        {
            using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.ReadOnly);
            result.TotalPages = doc.PageCount;

            for (int i = 0; i < doc.PageCount; i++)
            {
                if (PageContainsLogo(doc.Pages[i], logoHash, logoAspect))
                    result.LogoPages.Add(i + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF taranamadı: {Path}", pdfPath);
        }

        return Task.FromResult(result);
    }

    // Logoyu PdfSharpCore ile geçici bir PDF'e gömerek ham piksel hash'ini türetir.
    // Gerçek PDF'lerdeki embedding ile birebir aynı pipeline kullanıldığından
    // kıyaslama harici kütüphane gerektirmez.
    private (ulong hash, double aspect) GetLogoSignature(string logoPath)
    {
        if (_cachedLogoPath == logoPath)
            return (_cachedLogoHash, _cachedLogoAspect);

        using var xImg = XImage.FromFile(logoPath);
        double aspect = (double)xImg.PixelWidth / xImg.PixelHeight;

        using var tempDoc = new PdfDocument();
        var page = tempDoc.AddPage();
        using (var gfx = XGraphics.FromPdfPage(page))
            gfx.DrawImage(xImg, 0, 0, 80, 30);

        using var ms = new MemoryStream();
        tempDoc.Save(ms);
        ms.Position = 0;

        ulong hash = ExtractFirstImageHash(ms);

        _cachedLogoPath = logoPath;
        _cachedLogoHash = hash;
        _cachedLogoAspect = aspect;

        return (hash, aspect);
    }

    private ulong ExtractFirstImageHash(Stream pdfStream)
    {
        using var doc = PdfReader.Open(pdfStream, PdfDocumentOpenMode.ReadOnly);
        if (doc.PageCount == 0) return 0;

        var xObjDict = doc.Pages[0].Resources.Elements.GetDictionary("/XObject");
        if (xObjDict == null) return 0;

        foreach (var key in xObjDict.Elements.Keys)
        {
            var xobj = xObjDict.Elements.GetDictionary(key);
            if (xobj?.Elements.GetName("/Subtype") != "/Image") continue;

            int w = xobj.Elements.GetInteger("/Width");
            int h = xobj.Elements.GetInteger("/Height");
            var pixels = xobj.Stream?.UnfilteredValue;

            if (pixels != null && pixels.Length > 0 && w > 0 && h > 0)
                return ComputeAHash(pixels, w, h, GetBytesPerPixel(xobj));
        }

        return 0;
    }

    private bool PageContainsLogo(PdfPage page, ulong logoHash, double logoAspect)
    {
        try
        {
            var xObjDict = page.Resources.Elements.GetDictionary("/XObject");
            if (xObjDict == null) return false;

            foreach (var key in xObjDict.Elements.Keys)
            {
                var xobj = xObjDict.Elements.GetDictionary(key);
                if (xobj == null) continue;
                if (xobj.Elements.GetName("/Subtype") != "/Image") continue;

                int w = xobj.Elements.GetInteger("/Width");
                int h = xobj.Elements.GetInteger("/Height");
                int bpc = xobj.Elements.GetInteger("/BitsPerComponent");

                if (w <= 0 || h <= 0 || bpc != 8) continue;

                double aspect = (double)w / h;
                if (Math.Abs(aspect - logoAspect) / logoAspect > AspectTolerance) continue;

                var pixels = xobj.Stream?.UnfilteredValue;
                if (pixels == null || pixels.Length == 0) continue;

                int bpp = GetBytesPerPixel(xobj);
                ulong imgHash = ComputeAHash(pixels, w, h, bpp);

                if (HammingDistance(logoHash, imgHash) <= HashThreshold)
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sayfa XObject taraması başarısız");
        }

        return false;
    }

    private static int GetBytesPerPixel(PdfDictionary xobj)
    {
        var cs = xobj.Elements.GetName("/ColorSpace");
        if (cs == "/DeviceGray") return 1;
        if (cs == "/DeviceCMYK") return 4;
        return 3; // DeviceRGB ve varsayılan
    }

    // Average Hash (aHash): ham piksel baytlarını 8×8 grayscale'e küçült,
    // her piksel ortalamanın üstündeyse 1, altındaysa 0 → 64-bit hash
    private static ulong ComputeAHash(byte[] raw, int width, int height, int bpp)
    {
        var small = new byte[64];
        int blockW = Math.Max(1, width / 8);
        int blockH = Math.Max(1, height / 8);

        for (int by = 0; by < 8; by++)
        {
            for (int bx = 0; bx < 8; bx++)
            {
                long sum = 0;
                int count = 0;

                int yEnd = Math.Min((by + 1) * blockH, height);
                int xEnd = Math.Min((bx + 1) * blockW, width);

                for (int py = by * blockH; py < yEnd; py++)
                {
                    for (int px = bx * blockW; px < xEnd; px++)
                    {
                        int offset = (py * width + px) * bpp;
                        if (offset + bpp > raw.Length) continue;

                        byte luma = bpp == 1
                            ? raw[offset]
                            : (byte)(raw[offset] * 0.299 + raw[offset + 1] * 0.587 + raw[offset + 2] * 0.114);

                        sum += luma;
                        count++;
                    }
                }

                small[by * 8 + bx] = count > 0 ? (byte)(sum / count) : (byte)0;
            }
        }

        double avg = 0;
        for (int i = 0; i < 64; i++) avg += small[i];
        avg /= 64;

        ulong hash = 0;
        for (int i = 0; i < 64; i++)
            if (small[i] >= avg) hash |= 1UL << i;

        return hash;
    }

    private static int HammingDistance(ulong a, ulong b)
        => System.Numerics.BitOperations.PopCount(a ^ b);
}
