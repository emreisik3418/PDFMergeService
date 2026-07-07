using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Web.ViewModels.PdfMerge;

namespace PDFMergeService.Web.Controllers;

public class PdfMergeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View(new PdfMergeViewModel());
    }
}
