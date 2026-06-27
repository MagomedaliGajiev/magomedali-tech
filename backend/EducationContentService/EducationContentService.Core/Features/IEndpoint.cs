using Microsoft.AspNetCore.Routing;

namespace EducationContentService.Core.Features;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}