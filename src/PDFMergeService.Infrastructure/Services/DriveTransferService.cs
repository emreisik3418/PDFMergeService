using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

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

    public Task<DriveUploadResult> UploadAsync(DriveUploadRequest request)
    {
        // DocumentService.svc?wsdl servis referansı bu ortamda oluşturulamadı.
        // Visual Studio'da bu projeye
        // "Connected Service" (Microsoft WCF Web Service Reference Provider) ekleyip
        // DocumentService.svc?wsdl adresini vererek DocumentServiceClient sınıfını
        // ürettikten sonra bu metodu şu şekilde tamamlayın:
        //
        // var client = new DocumentServiceClient(
        //     DocumentServiceClient.EndpointConfiguration.DocumentServiceSoap, _settings.ServiceUrl);
        //
        // // Kimlik doğrulama şekli SharePoint ekibiyle netleştikten sonra biri seçilir:
        // // client.ClientCredentials.Windows.ClientCredential = new NetworkCredential(user, pass, domain);
        // // client.ClientCredentials.UserName.UserName = _settings.Username;
        // // client.ClientCredentials.UserName.Password = _settings.Password;
        //
        // var response = await client.UploadFileToPathAsync(
        //     _settings.UserId, request.ExtraParams, _settings.SiteUrl,
        //     request.WebPath, request.Path, request.FileName, request.FileBytes);
        //
        // return new DriveUploadResult { Success = true, Message = response?.ToString() };

        _logger.LogWarning(
            "SharePoint aktarımı denendi ancak DocumentService.svc servis referansı henüz eklenmedi: {FileName}",
            request.FileName);

        return Task.FromResult(new DriveUploadResult
        {
            Success = false,
            Message = "SharePoint servis bağlantısı henüz yapılandırılmadı (DocumentService.svc referansı eksik)."
        });
    }
}
