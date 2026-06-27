using EducationContentService.Core.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguration(builder.Configuration);

WebApplication app = builder.Build();

app.Configure();

app.Run();