using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MsGraphDemo;
using Xunit;

namespace MsGraphDemo.Test;

public class PermissionRequirementHandlerTests
{
  private static AuthorizationHandlerContext CreateContext(
      PermissionRequirement requirement,
      IEnumerable<Claim> claims)
  {
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);
    return new AuthorizationHandlerContext([requirement], principal, null);
  }

  [Fact]
  public async Task Succeeds_when_delegated_scope_matches()
  {
    var requirement = new PermissionRequirement("User.Read");
    var context = CreateContext(requirement, [new Claim("scp", "User.Read User.ReadWrite")]);

    await new PermissionRequirementHandler().HandleAsync(context);

    Assert.True(context.HasSucceeded);
  }

  [Fact]
  public async Task Succeeds_when_app_role_matches()
  {
    var requirement = new PermissionRequirement("User.ReadWrite.All");
    var context = CreateContext(requirement, [new Claim("roles", "User.ReadWrite.All")]);

    await new PermissionRequirementHandler().HandleAsync(context);

    Assert.True(context.HasSucceeded);
  }

  [Fact]
  public async Task Fails_when_neither_scp_nor_roles_contain_permission()
  {
    var requirement = new PermissionRequirement("User.ReadWrite.All");
    var context = CreateContext(requirement, [new Claim("scp", "User.Read")]);

    await new PermissionRequirementHandler().HandleAsync(context);

    Assert.False(context.HasSucceeded);
  }

  [Fact]
  public async Task Succeeds_with_case_insensitive_scope_match()
  {
    var requirement = new PermissionRequirement("user.read");
    var context = CreateContext(requirement, [new Claim("scp", "User.Read")]);

    await new PermissionRequirementHandler().HandleAsync(context);

    Assert.True(context.HasSucceeded);
  }

  [Fact]
  public async Task Fails_when_user_has_no_claims()
  {
    var requirement = new PermissionRequirement("User.Read");
    var context = CreateContext(requirement, []);

    await new PermissionRequirementHandler().HandleAsync(context);

    Assert.False(context.HasSucceeded);
  }
}
