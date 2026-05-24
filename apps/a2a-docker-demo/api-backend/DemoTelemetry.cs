namespace A2ADemo.ApiBackend;

public static class DemoTelemetry
{
    public const string ActivitySourceName = "a2a.triage";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}