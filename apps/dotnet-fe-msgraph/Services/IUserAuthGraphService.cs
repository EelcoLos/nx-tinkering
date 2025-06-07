using Microsoft.Graph.Models;

namespace DotnetFeMsGraph.Services;

public interface IUserAuthGraphService
{
    Task<User> GetMeAsync(CancellationToken cancellationToken = default);
    Task<List<Message>> GetMyMessagesAsync(int top = 10, CancellationToken cancellationToken = default);
    Task<List<Event>> GetMyCalendarEventsAsync(DateTime? startDateTime = null, DateTime? endDateTime = null, CancellationToken cancellationToken = default);
}