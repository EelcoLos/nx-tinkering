using FastEndpoints;
using Microsoft.Graph.Models;
using DotnetFeMsGraph.Services;
using DotnetFeMsGraph.Auth;

namespace DotnetFeMsGraph.Endpoints;

public class GetMeEndpointRequest { }

public class GetMeEndpointResponse
{
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public required string Id { get; set; }
    public string? UserPrincipalName { get; set; }
}

public class GetMeEndpoint : Endpoint<GetMeEndpointRequest, GetMeEndpointResponse>
{
    private readonly IUserAuthGraphService _userAuthGraphService;

    public GetMeEndpoint(IUserAuthGraphService userAuthGraphService)
    {
        _userAuthGraphService = userAuthGraphService;
    }

    public override void Configure()
    {
        Get("/me");
        Description(d => d.WithName("getme"));
        
        // Use the dynamic permissions approach
        Permissions(GraphPermissions.UserRead);
    }

    public override async Task HandleAsync(GetMeEndpointRequest req, CancellationToken ct)
    {
        try
        {
            User user = await _userAuthGraphService.GetMeAsync(ct);

            var response = new GetMeEndpointResponse
            {
                DisplayName = user.DisplayName ?? "Unknown",
                Email = user.Mail,
                Id = user.Id ?? string.Empty,
                UserPrincipalName = user.UserPrincipalName
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            ThrowError(ex.Message);
        }
    }
}