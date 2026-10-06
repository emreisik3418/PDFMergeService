using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Web.Services;

namespace PDFMergeService.Web.Controllers;

public class ChangelogController : Controller
{
    private readonly ChangelogService _changelogService;

    public ChangelogController(ChangelogService changelogService)
    {
        _changelogService = changelogService;
    }

    [HttpGet("/whats-new")]
    public IActionResult Index() => View(_changelogService.GetEntries(User.IsInRole("Admin")));
}
