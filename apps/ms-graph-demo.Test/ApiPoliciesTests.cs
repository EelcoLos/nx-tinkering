using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Xunit;

namespace MsGraphDemo.Test;

public class ApiPoliciesTests
{
  private static readonly IAuthorizationService Authorization = CreateAuthorizationService();

  // Same registration as Program.cs: Microsoft.Identity.Web provides the scope/app-permission handlers.
  private static IAuthorizationService CreateAuthorizationService()
  {
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
          ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
          ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
        })
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(config);
    services.AddLogging();
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(config.GetSection("AzureAd"));
    services.AddAuthorization(ApiPolicies.Configure);

    return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
  }

  private static ClaimsPrincipal Caller(params Claim[] claims) => new(new ClaimsIdentity(claims, "Bearer"));

  [Theory]
  [InlineData(ApiPolicies.AccessAsUser)]
  [InlineData(ApiPolicies.ListUsers)]
  public async Task User_with_api_scope_is_allowed(string policy)
  {
    var result = await Authorization.AuthorizeAsync(Caller(new Claim("scp", "access_as_user")), policy);

    Assert.True(result.Succeeded);
  }

  [Theory]
  [InlineData(ApiPolicies.AccessAsUser)]
  [InlineData(ApiPolicies.ListUsers)]
  public async Task Graph_scopes_in_api_token_are_not_enough(string policy)
  {
    var result = await Authorization.AuthorizeAsync(Caller(new Claim("scp", "User.Read User.ReadBasic.All")), policy);

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task Daemon_with_app_role_can_list_users_but_has_no_me()
  {
    var daemon = Caller(new Claim("roles", ApiPolicies.UsersListAppRole));

    Assert.True((await Authorization.AuthorizeAsync(daemon, ApiPolicies.ListUsers)).Succeeded);
    Assert.False((await Authorization.AuthorizeAsync(daemon, ApiPolicies.AccessAsUser)).Succeeded);
  }
}
