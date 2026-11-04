using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Templates;
using LV.SignFlow.Infrastructure.Persistence;
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
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<ISignatureAssetStorage, LocalSignatureAssetStorage>();
            

            return services;
        }
    }
}
