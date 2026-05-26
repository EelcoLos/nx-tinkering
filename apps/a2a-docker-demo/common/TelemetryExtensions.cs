using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace A2ADemo.Common;

public static class TelemetryExtensions
{
  public static IServiceCollection AddServiceTelemetry(
      this IServiceCollection services,
      bool enabled,
      string serviceName,
      string serviceNamespace,
      string otlpEndpoint,
      string activitySourceName,
      params string[] excludedAspNetCorePathPrefixes)
  {
    if (!enabled)
    {
      return services;
    }

    var excludedPrefixes = Array.FindAll(
        excludedAspNetCorePathPrefixes,
        static prefix => !string.IsNullOrWhiteSpace(prefix));

    services
        .AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName, serviceNamespace: serviceNamespace))
        .WithTracing(tracing =>
        {
          tracing
                  .AddSource(activitySourceName)
                  .AddAspNetCoreInstrumentation(options =>
                  {
                  if (excludedPrefixes.Length == 0)
                  {
                    return;
                  }

                  options.Filter = httpContext => !IsExcludedAspNetCorePath(httpContext.Request.Path.Value, excludedPrefixes);
                })
                  .AddHttpClientInstrumentation();

          if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
          {
            tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
          }
        });

    return services;
  }

  private static bool IsExcludedAspNetCorePath(string? path, string[] excludedPrefixes)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return false;
    }

    foreach (var prefix in excludedPrefixes)
    {
      if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
          || path.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  public static Activity? StartToolActivity(ActivitySource activitySource, string toolName)
  {
    var activity = activitySource.StartActivity($"execute_tool {toolName}", ActivityKind.Internal);
    ApplyToolTags(activity, toolName);
    return activity;
  }

  public static Activity? StartWorkflowActivity(ActivitySource activitySource, string workflowName)
  {
    var activity = activitySource.StartActivity($"invoke_workflow {workflowName}", ActivityKind.Internal);
    ApplyWorkflowTags(activity, workflowName);
    return activity;
  }

  public static string? GetCorrelationId(Activity? activity) => activity?.TraceId.ToString();

  private static void ApplyToolTags(Activity? activity, string toolName)
  {
    if (activity is null)
    {
      return;
    }

    activity.SetTag("gen_ai.system", "fastendpoints-a2a");
    activity.SetTag("gen_ai.operation.name", "execute_tool");
    activity.SetTag("gen_ai.tool.name", toolName);
    activity.SetTag("gen_ai.output.type", "json");
    activity.SetTag("correlationId", activity.TraceId.ToString());
    activity.SetTag("a2a.correlation_id", activity.TraceId.ToString());
  }

  private static void ApplyWorkflowTags(Activity? activity, string workflowName)
  {
    if (activity is null)
    {
      return;
    }

    activity.SetTag("gen_ai.system", "fastendpoints-a2a");
    activity.SetTag("gen_ai.operation.name", "invoke_workflow");
    activity.SetTag("gen_ai.workflow.name", workflowName);
    activity.SetTag("gen_ai.output.type", "json");
    activity.SetTag("correlationId", activity.TraceId.ToString());
    activity.SetTag("a2a.correlation_id", activity.TraceId.ToString());
  }
}
