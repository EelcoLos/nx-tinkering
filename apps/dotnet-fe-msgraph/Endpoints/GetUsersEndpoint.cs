using FastEndpoints;
using Microsoft.Graph.Models;
using DotnetFeMsGraph.Services;
using DotnetFeMsGraph.Auth;

namespace DotnetFeMsGraph.Endpoints;

public class GetUsersEndpointRequest 
{
    public int? Top { get; set; } = 10;
}

public class UserDto
{
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public required string Id { get; set; }
    public string? UserPrincipalName { get; set; }
}

public class GetUsersEndpointResponse
{
    public List<UserDto> Users { get; set; } = new();
}

public class GetUsersEndpoint : Endpoint<GetUsersEndpointRequest, GetUsersEndpointResponse>
{
    private readonly IAppAuthGraphService _appAuthGraphService;

    public GetUsersEndpoint(IAppAuthGraphService appAuthGraphService)
    {
        _appAuthGraphService = appAuthGraphService;
    }

    public override void Configure()
    {
        Get("/users");
        Description(d => d.WithName("getusers"));
        
        // Requires application-level permission to read all users
        Permissions(GraphPermissions.UserReadAll); 
    }

    public override async Task HandleAsync(GetUsersEndpointRequest req, CancellationToken ct)
    {
        try
        {
            var top = req.Top ?? 10;
            if (top < 1) top = 10;
            if (top > 100) top = 100;

            List<User> users = await _appAuthGraphService.GetAllUsersAsync(top, ct);

            var response = new GetUsersEndpointResponse
            {
                Users = users.Select(u => new UserDto
                {
                    DisplayName = u.DisplayName ?? "Unknown",
                    Email = u.Mail,
                    Id = u.Id ?? string.Empty,
                    UserPrincipalName = u.UserPrincipalName
                }).ToList()
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            ThrowError(ex.Message);
        }
    }
}