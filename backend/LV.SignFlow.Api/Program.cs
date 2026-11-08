
using LV.SignFlow.Api.Authentification;
using LV.SignFlow.Api.BackgroundServices;
using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Envelopes;
using LV.SignFlow.Application.Signing;
using LV.SignFlow.Application.Templates;
using LV.SignFlow.Infrastructure;
using LV.SignFlow.Infrastructure.Pdf;
using LV.SignFlow.Infrastructure.Persistence;
using LV.SignFlow.Infrastructure.Persistence.Seed;
using LV.SignFlow.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);
PdfFontBootstrapper.Initialize();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IEnvelopeService, EnvelopeService>();
builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddScoped<IFileStorage,LocalFileStorage>();


const string FrontentCorsPolicy = "FrontendCors";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy(FrontentCorsPolicy,
        policy =>
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();
}
builder.Services.AddOpenApi();
builder.Services.Configure<
    EnvelopeFinalizationWorkerOptions>(
        builder.Configuration.GetSection(
            EnvelopeFinalizationWorkerOptions.SectionName));

builder.Services.AddHostedService<
    EnvelopeFinalizationBackgroundService>();
builder.Services.AddHostedService<
    EnvelopeExpirationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

   
    await DevelopmentDataSeeder.SeedAsync(dbContext);
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseCors(FrontentCorsPolicy);
app.Run();


