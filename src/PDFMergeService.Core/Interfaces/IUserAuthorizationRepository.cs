using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IUserAuthorizationRepository
{
    /// <summary>
    /// Null döner: kullanıcı whitelist'te yok. Bağlantı/sorgu hatalarında exception fırlatır (null ile karıştırılmaz).
    /// </summary>
    Task<UserAuthorization?> GetAuthorizedUserAsync(string username);
}
