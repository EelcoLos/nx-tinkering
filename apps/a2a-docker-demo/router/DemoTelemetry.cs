namespace A2ADemo.Router;

public static class DemoTelemetry
{
    public const string ActivitySourceName = "a2a.tool.router";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}