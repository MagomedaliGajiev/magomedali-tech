using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using EducationContentService.IntegrationTests.Infrastructure;
using FileService.Contracts;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
        Assert.Equal(2, lessonsResponse.Value.Page);
        Assert.Equal(2, lessonsResponse.Value.PageSize);
        Assert.Equal(3, lessonsResponse.Value.TotalPages);
        Assert.Equal(2, lessonsResponse.Value.Items.Count);
        Assert.All(lessonsResponse.Value.Items, lesson => Assert.NotNull(lesson.Video));
        Assert.Equal(
            expectedVideoIds,
            lessonsResponse.Value.Items.Select(lesson => lesson.Video!.Id));

        await ExecuteInDb(async dbContext =>
        {
            Lesson lessonToDelete = await dbContext.Lessons
                .SingleAsync(lesson => lesson.Id == lessons[0].Id, cancellationToken);
            lessonToDelete.SoftDelete();
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        PaginationLessonResponse activeSearchResponse = await GetLessons(
            "  LESSON 2  ",
            false,
            cancellationToken);
        PaginationLessonResponse deletedSearchResponse = await GetLessons(
            "Lesson 1",
            true,
            cancellationToken);

        Assert.Equal(1, activeSearchResponse.TotalCount);
        Assert.Equal(lessons[1].Id, Assert.Single(activeSearchResponse.Items).Id);
        Assert.Equal(1, deletedSearchResponse.TotalCount);
        Assert.Equal(lessons[0].Id, Assert.Single(deletedSearchResponse.Items).Id);
    }

    private async Task<PaginationLessonResponse> GetLessons(
        string search,
        bool isDeleted,
        CancellationToken cancellationToken)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["search"] = search,
            ["isDeleted"] = isDeleted.ToString(),
            ["page"] = "1",
            ["pageSize"] = "10",
        };

        string url = QueryHelpers.AddQueryString("api/lessons", queryParams);
        HttpResponseMessage response = await AppHttpClient.GetAsync(url, cancellationToken);
        Result<PaginationLessonResponse, Error> result = await response
            .HandleResponseAsync<PaginationLessonResponse>(cancellationToken);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}