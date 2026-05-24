using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A2ADemo.Common;

public static class OptionsRegistrationExtensions
{
    public static TSettings AddConfiguredSettings<TSettings>(
        this IServiceCollection services,
        Action<TSettings> configure)
        where TSettings : class, new()
    {
        services.AddOptions<TSettings>().Configure(configure);

        var settings = new TSettings();
        configure(settings);
        return settings;
    }

    public static TSettings AddConfiguredIdentitySettings<TSettings>(
        this IServiceCollection services,
        Action<TSettings> configure)
        where TSettings : class, IIdentityServiceSettings, new()
    {
        var settings = services.AddConfiguredSettings(configure);
        services.AddSingleton<IIdentityServiceSettings>(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);
        return settings;
    }

    public static TSettings AddConfiguredToolServiceSettings<TSettings>(
        this IServiceCollection services,
        Action<TSettings> configure)
        where TSettings : class, IToolServiceSettings, new()
    {
        var settings = services.AddConfiguredIdentitySettings(configure);
        services.AddSingleton<IToolServiceSettings>(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);
        return settings;
    }
}