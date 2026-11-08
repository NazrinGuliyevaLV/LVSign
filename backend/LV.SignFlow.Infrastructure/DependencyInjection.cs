using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Finalization;
using LV.SignFlow.Application.Templates;
using LV.SignFlow.Infrastructure.Pdf;
using LV.SignFlow.Infrastructure.Persistence;
using LV.SignFlow.Infrastructure.Persistence.Finalization;
using LV.SignFlow.Infrastructure.Persistence.Repositories;
using LV.SignFlow.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped(typeof(IRepository<>),typeof(Repository<>));
            services.AddScoped<IUnitOfWork,UnitOfWork>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<IFinalPdfRenderer, PdfSharpFinalPdfRenderer>();
            services.AddScoped<ICompletionCertificateGenerator, PdfSharpCompletionCertificateGenerator>();
            services.AddScoped<IEnvelopeFinalizationProcessor,EnvelopeFinalizationProcessor>();
            services.AddScoped<IEnvelopeFinalizationQueue,EfEnvelopeFinalizationQueue>();
            services.AddScoped<ICompletionCertificateDataBuilder,CompletionCertificateDataBuilder>();
            services.Configure<StorageOptions>(options =>
            {
                options.Provider =
                    configuration["Storage:Provider"]
                    ?? "Local";

                options.LocalRootPath =
                    configuration["Storage:LocalRootPath"];

                options.ServiceUri =
                    configuration["Storage:ServiceUri"];

                options.ContainerName =
                    configuration["Storage:ContainerName"]
                    ?? "lv-signflow";

                options.ManagedIdentityClientId =
                    configuration[
                        "Storage:ManagedIdentityClientId"];
            });

            var storageProvider =
                configuration["Storage:Provider"]
                ?? "Local";

            if (storageProvider.Equals(
                    "Local",
                    StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<
                    IFileStorage,
                    LocalFileStorage>();
            }
            else if (storageProvider.Equals(
                         "AzureBlob",
                         StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<
                    IFileStorage,
                    AzureBlobFileStorage>();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported storage provider: {storageProvider}");
            }

            services.AddScoped<
                ISignatureAssetStorage,
                SignatureAssetStorage>();

            return services;
        }
    }
}
