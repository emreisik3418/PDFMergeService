using Microsoft.Extensions.DependencyInjection;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Infrastructure.Services;

namespace PDFMergeService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfServices(this IServiceCollection services)
    {
        services.AddScoped<IPdfMergeService, PdfMergeService>();
        services.AddScoped<IPdfFooterService, PdfFooterService>();
        services.AddScoped<IFolderScanService, FolderScanService>();
        services.AddScoped<IPdfLogoDetectionService, PdfLogoDetectionService>();
        return services;
    }
}
