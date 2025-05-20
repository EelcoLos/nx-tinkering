using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace DotnetFeMsGraph.Services;

public static class GraphServiceClientFactory
{
    public static GraphServiceClient Create(Func<ClientOptions, Task> configureOptions)
    {
        var options = new ClientOptions();
        configureOptions(options).GetAwaiter().GetResult();
        return new GraphServiceClient(options.AuthenticationProvider);
    }

    public class ClientOptions
    {
        public IAuthenticationProvider? AuthenticationProvider { get; set; }
    }
}