using FastEndpoints;

namespace ApiDemo;

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
  public override void Configure()
  {
    Post("/api/user/create");
    AllowAnonymous();
  }

  public override async Task HandleAsync(MyRequest req, CancellationToken ct)
  {
    AgeEligibility ageEligibility = req.Age > 18 ? new Over18() : new NotOver18();
    await Send.OkAsync(new()
    {
      FullName = req.FirstName + " " + req.LastName,
      IsOver18 = ageEligibility switch
      {
        Over18 => true,
        NotOver18 => false,
      }
    }, cancellation: ct);
  }
}
