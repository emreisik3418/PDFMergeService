using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Repositories.Auth;

public class ConfigUserAuthorizationRepository : IUserAuthorizationRepository
{
    private readonly AuthorizedUsersSettings _settings;

    public ConfigUserAuthorizationRepository(IOptions<AuthorizedUsersSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<UserAuthorization?> GetAuthorizedUserAsync(string username)
    {
        var match = _settings.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return Task.FromResult<UserAuthorization?>(null);

        return Task.FromResult<UserAuthorization?>(new UserAuthorization
        {
            Username = username,
            Role = match.Role,
            DisplayName = match.DisplayName
        });
    }
}
