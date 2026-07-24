using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services.Auth;

public class MockAdAuthenticationService : IAdAuthenticationService
{
    private readonly MockAuthSettings _settings;

    public MockAdAuthenticationService(IOptions<MockAuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<AdAuthenticationResult> ValidateCredentialsAsync(string username, string password)
    {
        bool isValid = string.Equals(username, _settings.Username, StringComparison.OrdinalIgnoreCase)
            && password == _settings.Password;

        return Task.FromResult(isValid
            ? AdAuthenticationResult.Ok()
            : AdAuthenticationResult.Fail("Kullanıcı adı veya şifre hatalı."));
    }
}
