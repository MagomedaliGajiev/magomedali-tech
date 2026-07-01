using System.Globalization;
using EducationContentService.Core.Features.Lessons;
using EducationContentService.Infrastructure.Postgres;
using EducationContentService.Web.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddConfiguration(builder.Configuration);

    builder.Services.AddScoped<ILessonsRepository, LessonsRepository>();

    builder.Services.AddScoped<CreateHandler>();

    WebApplication app = builder.Build();

    app.Configure();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}