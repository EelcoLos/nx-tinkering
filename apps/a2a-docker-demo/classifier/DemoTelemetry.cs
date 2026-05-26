namespace A2ADemo.Classifier;

public static class DemoTelemetry
{
  public const string ActivitySourceName = "a2a.tool.classifier";
  public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
