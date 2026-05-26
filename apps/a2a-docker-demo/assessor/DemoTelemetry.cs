namespace A2ADemo.Assessor;

public static class DemoTelemetry
{
  public const string ActivitySourceName = "a2a.tool.assessor";
  public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
