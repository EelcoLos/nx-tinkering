using Microsoft.AspNetCore.Diagnostics;

namespace A2ADemo.ApiBackend;

public sealed class TriageExceptionHandler(
    ILogger<TriageExceptionHandler> logger,
    TriageStore store) : IExceptionHandler
{
    private const string SubmittedInputItemKey = "submitted_triage_input";
    private const string GenericFailureMessage = "Triage processing failed.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Path.StartsWithSegments("/api/triage", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        logger.LogError(exception, "Triage request failed.");

        var input = httpContext.Items.TryGetValue(SubmittedInputItemKey, out var submittedInput)
            ? submittedInput as string ?? string.Empty
            : string.Empty;

        var failedRecord = new TriageRecord
        {
            Id = $"triage-{Guid.NewGuid():N}"[..19],
            Input = input,
            Status = "failed",
            Error = GenericFailureMessage,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        store.Save(failedRecord);

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        await httpContext.Response.WriteAsJsonAsync(failedRecord, cancellationToken: cancellationToken);
        return true;
    }

    public static void StoreSubmittedInput(HttpContext httpContext, string input) =>
        httpContext.Items[SubmittedInputItemKey] = input;
}