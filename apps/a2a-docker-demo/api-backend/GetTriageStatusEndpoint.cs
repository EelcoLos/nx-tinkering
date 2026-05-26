namespace A2ADemo.ApiBackend;

public sealed class GetTriageStatusEndpoint(TriageStore store) : EndpointWithoutRequest<TriageRecord>
{
  public override void Configure()
  {
    Get("/api/triage/{id}");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var triageId = Route<string>("id") ?? string.Empty;
    var record = store.Get(triageId);
    if (record is null)
    {
      HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
      await HttpContext.Response.WriteAsJsonAsync(new { error = "Triage request not found" }, cancellationToken: ct);
      return;
    }

    await Send.OkAsync(record, ct);
  }
}
