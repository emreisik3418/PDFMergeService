using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Enums;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Web.Controllers;

[Authorize(Roles = "Admin")]
public class PdfSettingsController : Controller
{
    private readonly IPdfMergeLimitsService _pdfMergeLimitsService;
    private readonly IActivityLogService _activityLogService;

    public PdfSettingsController(
        IPdfMergeLimitsService pdfMergeLimitsService,
        IActivityLogService activityLogService)
    {
        _pdfMergeLimitsService = pdfMergeLimitsService;
        _activityLogService = activityLogService;
    }

    [HttpGet("/pdf-settings")]
    public async Task<IActionResult> Index() => View(await _pdfMergeLimitsService.GetAsync());

    [HttpPost("/pdf-settings")]
    public async Task<IActionResult> Save([FromBody] PdfMergeLimits? model)
    {
        if (model == null)
            return BadRequest(new { error = "Geçersiz istek." });

        if (model.MaxFileCount < PdfMergeLimits.MinFileCount || model.MaxFileCount > PdfMergeLimits.MaxFileCountLimit)
            return BadRequest(new { error = $"En fazla dosya sayısı {PdfMergeLimits.MinFileCount} ile {PdfMergeLimits.MaxFileCountLimit} arasında olmalı." });

        if (model.MaxFileSizeMB < PdfMergeLimits.MinFileSizeMB || model.MaxFileSizeMB > PdfMergeLimits.MaxFileSizeMBLimit)
            return BadRequest(new { error = $"Dosya başına boyut sınırı {PdfMergeLimits.MinFileSizeMB} ile {PdfMergeLimits.MaxFileSizeMBLimit} MB arasında olmalı." });

        await _pdfMergeLimitsService.SaveAsync(model);

        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.PdfSettingsUpdate,
            Detail = $"En fazla {model.MaxFileCount} dosya, dosya başına {model.MaxFileSizeMB} MB",
            Success = true
        });

        return Ok(new { message = "PDF ayarları kaydedildi." });
    }
}
