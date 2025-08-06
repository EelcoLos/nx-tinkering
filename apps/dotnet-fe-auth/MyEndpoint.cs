using FastEndpoints;

namespace DotnetFeAuth;

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
  public override void Configure()
  {
    Post("/api/user/create");
    Description(d => d.WithName("createuser"));
  }

  public override async Task HandleAsync(MyRequest req, CancellationToken ct)
  {
    await Send.OkAsync(new MyResponse
    {
      FullName = req.FirstName + " " + req.LastName,
      IsOver18 = req.Age > 18
    }, cancellation: ct);
  }
}
