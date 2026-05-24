using System.Diagnostics;

namespace A2ADemo.Handler;

public static class DemoTelemetry
{
    public const string ActivitySourceName = "a2a.tool.handler";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}