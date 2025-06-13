using Microsoft.Graph.Models;

namespace DotnetFeMsGraph.Services;

public interface IAppAuthGraphService
{
    Task<List<User>> GetAllUsersAsync(int top = 100, CancellationToken cancellationToken = default);
    Task<User> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<User> GetUserByPrincipalNameAsync(string userPrincipalName, CancellationToken cancellationToken = default);
}