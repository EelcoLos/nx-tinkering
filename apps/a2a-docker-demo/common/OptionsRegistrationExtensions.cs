namespace A2ADemo.Common;

public static class OptionsRegistrationExtensions
{
  public static TSettings AddConfiguredSettings<TSettings>(
      this IServiceCollection services,
      Action<TSettings> configure)
      where TSettings : class, new()
  {
    services
        .AddOptionsWithValidateOnStart<TSettings>()
        .Configure(configure);

    return CreateConfiguredSettings(configure);
  }

  public static TSettings AddConfiguredIdentitySettings<TSettings>(
      this IServiceCollection services,
      Action<TSettings> configure)
      where TSettings : class, IIdentityServiceSettings, new()
  {
    var settings = services.AddConfiguredSettings(configure);
    services.AddSingleton<IIdentityServiceSettings, IdentityServiceSettingsAccessor<TSettings>>();
    return settings;
  }

  public static TSettings AddConfiguredToolServiceSettings<TSettings>(
      this IServiceCollection services,
      Action<TSettings> configure)
      where TSettings : class, IToolServiceSettings, new()
  {
    var settings = services.AddConfiguredIdentitySettings(configure);
    services.AddSingleton<IToolServiceSettings, ToolServiceSettingsAccessor<TSettings>>();
    return settings;
  }

  private static TSettings CreateConfiguredSettings<TSettings>(Action<TSettings> configure)
      where TSettings : class, new()
  {
    var settings = new TSettings();
    configure(settings);
    return settings;
  }
}

internal sealed class IdentityServiceSettingsAccessor<TSettings>(IOptionsMonitor<TSettings> settingsMonitor)
    : IIdentityServiceSettings
    where TSettings : class, IIdentityServiceSettings, new()
{
  private TSettings Current => settingsMonitor.CurrentValue;

  public string IdentityServiceUrl => Current.IdentityServiceUrl;

  public string AgentId => Current.AgentId;
}

internal sealed class ToolServiceSettingsAccessor<TSettings>(IOptionsMonitor<TSettings> settingsMonitor)
    : IToolServiceSettings
    where TSettings : class, IToolServiceSettings, new()
{
  private TSettings Current => settingsMonitor.CurrentValue;

  public string IdentityServiceUrl => Current.IdentityServiceUrl;

  public string AgentId => Current.AgentId;

  public string ServiceName => Current.ServiceName;

  public int Port => Current.Port;

  public string ServiceBaseUrl => Current.ServiceBaseUrl;

  public string JwtSecretKey => Current.JwtSecretKey;

  public bool OtelEnabled => Current.OtelEnabled;

  public string OtelExporterEndpoint => Current.OtelExporterEndpoint;

  public string OtelServiceNamespace => Current.OtelServiceNamespace;
}
