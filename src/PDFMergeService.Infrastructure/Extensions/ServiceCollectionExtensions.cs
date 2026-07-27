using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Infrastructure.Repositories.Auth;
using PDFMergeService.Infrastructure.Services;
using PDFMergeService.Infrastructure.Services.Auth;

namespace PDFMergeService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfServices(this IServiceCollection services)
    {
        services.AddScoped<IPdfMergeService, PdfMergeService>();
        services.AddScoped<IPdfFooterService, PdfFooterService>();
        services.AddScoped<IFolderScanService, FolderScanService>();
        services.AddScoped<IDriveTransferService, DriveTransferService>();
        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        bool useMockProviders = configuration.GetValue<bool>("Authentication:UseMockProviders");
        bool useConfigAuthorization = configuration.GetValue<bool>("Authentication:UseConfigAuthorization");

        if (useMockProviders)
        {
            services.AddScoped<IAdAuthenticationService, MockAdAuthenticationService>();
        }
        else
        {
            services.AddScoped<IAdAuthenticationService, ActiveDirectoryAuthenticationService>();
        }

        if (useConfigAuthorization)
        {
            services.AddScoped<IUserAuthorizationRepository, ConfigUserAuthorizationRepository>();
        }
        else if (useMockProviders)
        {
            services.AddScoped<IUserAuthorizationRepository, MockUserAuthorizationRepository>();
        }
        else
        {
            services.AddScoped<IUserAuthorizationRepository, OracleUserAuthorizationRepository>();
        }

        return services;
    }
}
