namespace GraphPolicyApi;

public class GetCurrentUserResponse
{
    public string? DisplayName { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? Id { get; set; }
}

public class UserEndpoint(GraphServiceClient graphServiceClient) : EndpointWithoutRequest<GetCurrentUserResponse>
{
    public override void Configure()
    {
        Get("/users/me");
        Policies("GraphUser.Read");
        Description(x => x.WithName("GetCurrentUser"));
        Summary(s => s.Summary = "Gets the current user's profile information from Microsoft Graph");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var user = await graphServiceClient.Me
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = ["displayName", "userPrincipalName", "id"];
                }, ct);

            if (user == null)
            {
                ThrowError("User not found");
                return;
            }

            await SendAsync(new GetCurrentUserResponse
            {
                DisplayName = user.DisplayName,
                UserPrincipalName = user.UserPrincipalName,
                Id = user.Id
            }, cancellation: ct);
        }
        catch (Exception ex)
        {
            ThrowError(ex.Message);
        }
    }
}