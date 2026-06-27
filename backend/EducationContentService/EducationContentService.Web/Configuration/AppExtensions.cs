using EducationContentService.Web.EndpointsSettings;
using Serilog;

namespace EducationContentService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        RouteGroupBuilder apiGroup = app.MapGroup("/api");
        app.MapEndpoints(apiGroup);

        return app;
    }
}