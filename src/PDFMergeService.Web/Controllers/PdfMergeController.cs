using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Enums;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.PdfMerge;

namespace PDFMergeService.Web.Controllers;

public class PdfMergeController : Controller
{
    private readonly IActivityLogService _activityLogService;
    private readonly IPdfMergeLimitsService _pdfMergeLimitsService;

    public PdfMergeController(IActivityLogService activityLogService, IPdfMergeLimitsService pdfMergeLimitsService)
    {
        _activityLogService = activityLogService;
        _pdfMergeLimitsService = pdfMergeLimitsService;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var limits = await _pdfMergeLimitsService.GetAsync();
        return View(new PdfMergeViewModel
        {
            MaxFileCount = limits.MaxFileCount,
            MaxFileSizeMB = limits.MaxFileSizeMB
        });
    }

    [HttpPost("/pdf-merge/log")]
    public async Task<IActionResult> LogMerge([FromBody] SingleMergeLogRequest request)
    {
        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.SingleMerge,
            Detail = $"{request.FileCount} dosya, {request.PageCount} sayfa",
            Success = true
        });

        return Ok();
    }
}

public class SingleMergeLogRequest
{
    public int FileCount { get; set; }
    public int PageCount { get; set; }
}
