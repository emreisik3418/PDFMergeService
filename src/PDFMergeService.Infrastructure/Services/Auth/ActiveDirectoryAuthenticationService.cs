using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services.Auth;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryAuthenticationService : IAdAuthenticationService
{
    private readonly LdapSettings _settings;
    private readonly ILogger<ActiveDirectoryAuthenticationService> _logger;

    public ActiveDirectoryAuthenticationService(
        IOptions<LdapSettings> settings,
        ILogger<ActiveDirectoryAuthenticationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<AdAuthenticationResult> ValidateCredentialsAsync(string username, string password)
    {
        return Task.Run(() =>
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain, _settings.Domain, _settings.ContainerDn);
                bool isValid = context.ValidateCredentials(username, password, ContextOptions.Negotiate);

                if (!isValid)
                {
                    _logger.LogWarning("AD kimlik doğrulama başarısız: {Username}", username);
                    return AdAuthenticationResult.Fail("Kullanıcı adı veya şifre hatalı.");
                }

                return AdAuthenticationResult.Ok();
            }
            catch (PrincipalServerDownException ex)
            {
                _logger.LogError(ex, "AD sunucusuna ulaşılamadı. Domain: {Domain}", _settings.Domain);
                return AdAuthenticationResult.Fail("Kimlik doğrulama servisine ulaşılamıyor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD kimlik doğrulama sırasında beklenmeyen hata: {Username}", username);
                return AdAuthenticationResult.Fail("Kimlik doğrulama sırasında bir hata oluştu.");
            }
        });
    }
}
