using FastEndpoints;

namespace ApiDemo.Test;

public class MyEndpointTests
{
  [Theory]
  [InlineData(18, false)]
  [InlineData(19, true)]
  public async Task HandleAsync_returns_full_name_and_age_check(int age, bool isOver18)
  {
    var ep = Factory.Create<MyEndpoint>();

    await ep.HandleAsync(new MyRequest { FirstName = "Ada", LastName = "Lovelace", Age = age }, TestContext.Current.CancellationToken);

    Assert.Equal("Ada Lovelace", ep.Response.FullName);
    Assert.Equal(isOver18, ep.Response.IsOver18);
  }
}
