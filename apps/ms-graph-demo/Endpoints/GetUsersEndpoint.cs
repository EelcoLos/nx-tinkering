using FastEndpoints;

namespace MsGraphDemo;

/// <summary>
/// Returns a list of users from the directory.
///
/// Uses <c>Policies("User.ReadWrite.All")</c> — the same approach as <see cref="GetMeEndpoint"/> —
/// to show that the dynamic policy pattern scales to any Graph permission.
/// The <see cref="PermissionRequirementHandler"/> will evaluate the "User.ReadWrite.All"
/// policy against both delegated (<c>scp</c>) and application (<c>roles</c>) token claims.
/// </summary>
public class GetUsersEndpoint : EndpointWithoutRequest<UsersResponse>
{
  public override void Configure()
  {
    Get("/users");
    Policies("User.ReadWrite.All");
    Description(d => d.WithName("GetUsers").WithTags("Graph"));
  }

  public override Task HandleAsync(CancellationToken ct)
      => Send.OkAsync(new UsersResponse { Users = [] }, ct);
}

public class UsersResponse
{
  public IReadOnlyList<string> Users { get; set; } = [];
}
