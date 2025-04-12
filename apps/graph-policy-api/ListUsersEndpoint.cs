namespace GraphPolicyApi;

public class ListUsersRequest
{
    [FromQuery]
    public int? Top { get; set; } = 10;
    public string? SearchTerm { get; set; }
}

public class UserDto
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? JobTitle { get; set; }
}

public class ListUsersResponse
{
    public List<UserDto> Users { get; set; } = new();
    public string? NextPageLink { get; set; }
}

public class ListUsersEndpoint(GraphServiceClient graphServiceClient) : Endpoint<ListUsersRequest, ListUsersResponse>
{
    public override void Configure()
    {
        Get("/users");
        Policies("GraphDirectory.Read");
        Description(x => x.WithName("ListUsers"));
        Summary(s => s.Summary = "Lists users from Microsoft Graph with directory access");
    }

    public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
    {
        try
        {
            var userRequest = graphServiceClient.Users.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select = ["displayName", "userPrincipalName", "id", "jobTitle"];
                requestConfig.QueryParameters.Top = req.Top;

                if (!string.IsNullOrEmpty(req.SearchTerm))
                {
                    requestConfig.QueryParameters.Filter = $"startswith(displayName, '{req.SearchTerm}') or startswith(userPrincipalName, '{req.SearchTerm}')";
                }
            }, ct);

            var usersPage = await userRequest;

            var response = new ListUsersResponse();

            if (usersPage?.Value != null)
            {
                response.Users = usersPage.Value.Select(u => new UserDto
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserPrincipalName = u.UserPrincipalName,
                    JobTitle = u.JobTitle
                }).ToList();

                // Handle pagination if available
                if (usersPage.OdataNextLink != null)
                {
                    response.NextPageLink = usersPage.OdataNextLink;
                }
            }

            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            ThrowError(ex.Message);
        }
    }
}