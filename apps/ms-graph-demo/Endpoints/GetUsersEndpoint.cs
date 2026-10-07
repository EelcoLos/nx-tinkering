using FastEndpoints;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace MsGraphDemo;

/// <summary>
/// Lists directory users with read-only Graph permissions:
///   - user callers (<c>access_as_user</c>): on-behalf-of with delegated <c>User.ReadBasic.All</c>
///   - daemon callers (<c>Users.List</c> app role): app-only with application <c>User.Read.All</c>
/// </summary>
public class GetUsersEndpoint(GraphServiceClient graph) : EndpointWithoutRequest<UsersResponse>
{
  public override void Configure()
  {
    Get("/users");
    Policies(ApiPolicies.ListUsers);
    Description(d => d.WithName("GetUsers").WithTags("Graph"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var isAppOnly = !User.HasClaim(c => c.Type is ClaimConstants.Scp or ClaimConstants.Scope);

    var users = await graph.Users.GetAsync(r =>
    {
      r.QueryParameters.Select = ["id", "displayName"];
      r.QueryParameters.Top = 25;
      if (isAppOnly)
        r.Options.WithAppOnly();
      else
        r.Options.WithScopes("User.ReadBasic.All");
    }, ct);

    await Send.OkAsync(new UsersResponse
    {
      Users = users?.Value?.Select(u => u.DisplayName ?? u.Id ?? string.Empty).ToList() ?? [],
    }, ct);
  }
}

public class UsersResponse
{
  public IReadOnlyList<string> Users { get; set; } = [];
}
