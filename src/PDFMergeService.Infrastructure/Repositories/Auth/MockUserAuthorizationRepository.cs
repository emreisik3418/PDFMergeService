using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Repositories.Auth;

public class MockUserAuthorizationRepository : IUserAuthorizationRepository
{
    private readonly MockAuthSettings _settings;

    public MockUserAuthorizationRepository(IOptions<MockAuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<UserAuthorization?> GetAuthorizedUserAsync(string username)
    {
        if (!string.Equals(username, _settings.Username, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<UserAuthorization?>(null);

        return Task.FromResult<UserAuthorization?>(new UserAuthorization
        {
            Username = username,
            Role = _settings.Role
        });
    }
}
