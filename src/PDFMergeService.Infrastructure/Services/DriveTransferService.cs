using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;
using SharePointService;

namespace PDFMergeService.Infrastructure.Services;

public class DriveTransferService : IDriveTransferService
{
    private readonly SharePointSettings _settings;
    private readonly ILogger<DriveTransferService> _logger;

    public DriveTransferService(IOptions<SharePointSettings> settings, ILogger<DriveTransferService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DriveUploadResult> UploadAsync(DriveUploadRequest request)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
        {
            MaxReceivedMessageSize = 100 * 1024 * 1024,
            MaxBufferSize = 100 * 1024 * 1024
        };
        var endpoint = new EndpointAddress(_settings.ServiceUrl);
        var client = new DocumentServiceClient(binding, endpoint);

        // Kimlik doğrulama şekli SharePoint ekibiyle netleştikten sonra biri seçilir:
        // client.ClientCredentials.Windows.ClientCredential = new NetworkCredential(user, pass, domain);
        // client.ClientCredentials.UserName.UserName = _settings.Username;
        // client.ClientCredentials.UserName.Password = _settings.Password;

        try
        {
            bool success = await client.UploadFileToPathAsync(
                _settings.UserId,
                request.ExtraParams,
                _settings.SiteUrl,
                request.WebPath,
                request.Path,
                request.FileName,
                request.FileBytes);

            client.Close();

            return new DriveUploadResult
            {
                Success = success,
                Message = success ? "Dosya SharePoint'e aktarıldı." : "SharePoint servisi aktarımı reddetti."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SharePoint'e yükleme hatası: {FileName}", request.FileName);
            client.Abort();
            return new DriveUploadResult { Success = false, Message = "SharePoint servisine bağlanırken hata oluştu." };
        }
    }
}
