using Framework.Endpoints;
using Framework.Middlewares;
using Serilog;

namespace EducationContentService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseCors(builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });

        app.UseExceptionMiddleware();
        app.UseRequestCorrelationId();
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapEndpoints();

        return app;
    }
}