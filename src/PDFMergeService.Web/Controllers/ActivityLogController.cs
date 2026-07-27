using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Web.ViewModels.ActivityLog;

namespace PDFMergeService.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ActivityLogController : Controller
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [HttpGet("/activity-logs")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? username)
    {
        var entries = await _activityLogService.GetLogsAsync(from, to, username);

        return View(new ActivityLogViewModel
        {
            From = from,
            To = to,
            Username = username,
            Entries = entries.ToList()
        });
    }
}
