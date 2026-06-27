using EducationContentService.Core.EndpointsSettings;

namespace EducationContentService.Core.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        RouteGroupBuilder apiGroup = app.MapGroup("/api");
        app.MapEndpoints(apiGroup);

        return app;
    }
}