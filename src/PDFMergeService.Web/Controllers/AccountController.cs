using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.Account;

namespace PDFMergeService.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAdAuthenticationService _adAuthenticationService;
    private readonly IUserAuthorizationRepository _userAuthorizationRepository;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAdAuthenticationService adAuthenticationService,
        IUserAuthorizationRepository userAuthorizationRepository,
        ILogger<AccountController> logger)
    {
        _adAuthenticationService = adAuthenticationService;
        _userAuthorizationRepository = userAuthorizationRepository;
        _logger = logger;
    }

    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var adResult = await _adAuthenticationService.ValidateCredentialsAsync(model.Username, model.Password);
        if (!adResult.Success)
        {
            _logger.LogWarning("Başarısız giriş denemesi: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        UserAuthorization? authorizedUser;
        try
        {
            authorizedUser = await _userAuthorizationRepository.GetAuthorizedUserAsync(model.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yetki sorgusu sırasında hata: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Sistem hatası, lütfen daha sonra tekrar deneyin.");
            return View(model);
        }

        if (authorizedUser == null)
        {
            _logger.LogWarning("Yetkisiz kullanıcı giriş denemesi: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Bu uygulamayı kullanma yetkiniz bulunmamaktadır.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, model.Username),
            new(ClaimTypes.Role, authorizedUser.Role ?? "User")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        _logger.LogInformation("Başarılı giriş: {Username}", model.Username);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "PdfMerge");
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
