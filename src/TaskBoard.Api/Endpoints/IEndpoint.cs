namespace TaskBoard.Api.Endpoints;

/// <summary>
/// One implementation per module. They are discovered and mapped at startup by EndpointExtensions.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
