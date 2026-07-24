using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Repositories.Auth;

public class OracleUserAuthorizationRepository : IUserAuthorizationRepository
{
    private readonly ExadataSettings _settings;
    private readonly ILogger<OracleUserAuthorizationRepository> _logger;

    public OracleUserAuthorizationRepository(
        IOptions<ExadataSettings> settings,
        ILogger<OracleUserAuthorizationRepository> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<UserAuthorization?> GetAuthorizedUserAsync(string username)
    {
        try
        {
            await using var connection = new OracleConnection(_settings.ConnectionString);
            await connection.OpenAsync();

            await using var command = new OracleCommand(_settings.AuthorizationQuery, connection)
            {
                BindByName = true,
                CommandTimeout = _settings.CommandTimeoutSeconds
            };
            command.Parameters.Add(new OracleParameter("Username", username));

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new UserAuthorization
            {
                Username = username,
                Role = TryGetString(reader, "ROLE"),
                DisplayName = TryGetString(reader, "DISPLAY_NAME")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exadata yetki sorgusu sırasında hata: {Username}", username);
            throw;
        }
    }

    private static string? TryGetString(System.Data.Common.DbDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }
}
