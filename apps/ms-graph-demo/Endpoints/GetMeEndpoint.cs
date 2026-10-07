using FastEndpoints;
using Microsoft.Graph;

namespace MsGraphDemo;

/// <summary>
/// Returns the caller's Graph profile. The API's <c>access_as_user</c> token is exchanged
/// on-behalf-of the user for a Graph token with the delegated <c>User.Read</c> scope
/// (the default scope configured under <c>DownstreamApis:MicrosoftGraph</c>).
/// </summary>
public class GetMeEndpoint(GraphServiceClient graph) : EndpointWithoutRequest<MeResponse>
{
  public override void Configure()
  {
    Get("/me");
    Policies(ApiPolicies.AccessAsUser);
    Description(d => d.WithName("GetMe").WithTags("Graph"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var me = await graph.Me.GetAsync(r => r.QueryParameters.Select = ["id", "displayName", "mail", "userPrincipalName"], ct);

    await Send.OkAsync(new MeResponse
    {
      DisplayName = me?.DisplayName ?? string.Empty,
      Email = me?.Mail ?? me?.UserPrincipalName ?? string.Empty,
      ObjectId = me?.Id ?? string.Empty,
    }, ct);
  }
}

public class MeResponse
{
  public string DisplayName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string ObjectId { get; set; } = string.Empty;
}
