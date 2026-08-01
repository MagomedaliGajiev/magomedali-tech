using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using EducationContentService.IntegrationTests.Infrastructure;
using FileService.Contracts;
using Microsoft.AspNetCore.WebUtilities;
using Shared.SharedKernel;

namespace EducationContentService.IntegrationTests.Features.Lessons;

public class GetLessonsTests : EducationTestsBase
{
    public GetLessonsTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Get_ReturnsPagedLessonsWithVideos()
    {
        // arrange
        CancellationToken cancellationToken = CancellationToken.None;

        Guid[] videoIds =
        [
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
        ];

        Lesson[] lessons = videoIds
            .Select((videoId, index) => new Lesson(
                Guid.NewGuid(),
                Title.Create($"Lesson {index + 1}").Value,
                Description.Create($"Description {index + 1}").Value,
                videoId))
            .ToArray();

        await ExecuteInDb(async dbContext =>
        {
            await dbContext.Lessons.AddRangeAsync(lessons, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        var getLessonsRequest = new GetLessonsRequest(null, 2, 2);

        Guid[] expectedVideoIds = lessons
            .OrderBy(lesson => lesson.CreatedAt)
            .ThenBy(lesson => lesson.Id)
            .Skip(2)
            .Take(2)
            .Select(lesson => lesson.VideoId!.Value)
            .ToArray();

        var queryParams = new Dictionary<string, string?>
        {
            {
                "page", getLessonsRequest.Page.ToString()
            },
            {
                "pageSize", getLessonsRequest.PageSize.ToString()
            }
        };

        string url = QueryHelpers.AddQueryString("api/lessons", queryParams);

        HttpResponseMessage getLessonsResponse =
            await AppHttpClient.GetAsync(url, cancellationToken);

        // act
        Result<PaginationLessonResponse, Error> lessonsResponse = await getLessonsResponse
            .HandleResponseAsync<PaginationLessonResponse>(cancellationToken);

        // assert
        Assert.True(lessonsResponse.IsSuccess);
        Assert.Equal(5, lessonsResponse.Value.TotalCount);
        Assert.Equal(2, lessonsResponse.Value.Lessons.Count);
        Assert.All(lessonsResponse.Value.Lessons, lesson => Assert.NotNull(lesson.Video));
        Assert.Equal(
            expectedVideoIds,
            lessonsResponse.Value.Lessons.Select(lesson => lesson.Video!.Id));
    }
}