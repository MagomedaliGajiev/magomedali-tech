using EducationContentService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace EducationContentService.IntegrationTests.Infrastructure;

public class EducationTestsBase : IClassFixture<IntegrationTestsWebFactory>
{
    public const string TEST_FILE_NAME = "test-file.mp4";
    protected EducationTestsBase(IntegrationTestsWebFactory factory)
    {
        AppHttpClient = factory.CreateClient();
        HttpClient = new HttpClient();
        Services = factory.Services;
    }

    protected IServiceProvider Services { get; init; }

    protected HttpClient AppHttpClient { get; init; }

    protected HttpClient HttpClient { get; init; }

    protected async Task ExecuteInDb(Func<EducationDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        EducationDbContext dbContext = scope.ServiceProvider.GetRequiredService<EducationDbContext>();

        await action(dbContext);
    }
}