using FastEndpoints;
using Microsoft.Graph.Models;
using DotnetFeMsGraph.Services;
using DotnetFeMsGraph.Auth;

namespace DotnetFeMsGraph.Endpoints;

public class GetUserCalendarEndpointRequest 
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CalendarEventDto
{
    public required string Subject { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
}

public class GetUserCalendarEndpointResponse
{
    public List<CalendarEventDto> Events { get; set; } = new();
}

public class GetUserCalendarEndpoint : Endpoint<GetUserCalendarEndpointRequest, GetUserCalendarEndpointResponse>
{
    private readonly IUserAuthGraphService _userAuthGraphService;

    public GetUserCalendarEndpoint(IUserAuthGraphService userAuthGraphService)
    {
        _userAuthGraphService = userAuthGraphService;
    }

    public override void Configure()
    {
        Get("/me/calendar");
        Description(d => d.WithName("getusercalendar"));
        
        // Requires calendar read permission
        Permissions(GraphPermissions.CalendarRead);
    }

    public override async Task HandleAsync(GetUserCalendarEndpointRequest req, CancellationToken ct)
    {
        try
        {
            List<Event> events = await _userAuthGraphService.GetMyCalendarEventsAsync(
                req.StartDate, 
                req.EndDate,
                ct);

            var response = new GetUserCalendarEndpointResponse
            {
                Events = events.Select(e => new CalendarEventDto
                {
                    Subject = e.Subject ?? "(No subject)",
                    StartTime = e.Start?.ToDateTime(),
                    EndTime = e.End?.ToDateTime(),
                    Location = e.Location?.DisplayName
                }).ToList()
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            ThrowError(ex.Message);
        }
    }
}

public static class DateTimeTimeZoneExtension
{
    public static DateTime? ToDateTime(this DateTimeTimeZone? dateTimeTimeZone)
    {
        if (dateTimeTimeZone?.DateTime == null) return null;
        
        if (DateTime.TryParse(dateTimeTimeZone.DateTime, out var result))
            return result;
            
        return null;
    }
}