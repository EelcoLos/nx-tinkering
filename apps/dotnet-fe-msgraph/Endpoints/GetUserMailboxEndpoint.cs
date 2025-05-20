using FastEndpoints;
using Microsoft.Graph.Models;
using DotnetFeMsGraph.Services;
using DotnetFeMsGraph.Auth;

namespace DotnetFeMsGraph.Endpoints;

public class GetUserMailboxEndpointRequest 
{
    public int? Top { get; set; } = 5;
}

public class MessageDto
{
    public required string Subject { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

public class GetUserMailboxEndpointResponse
{
    public List<MessageDto> Messages { get; set; } = new();
}

public class GetUserMailboxEndpoint : Endpoint<GetUserMailboxEndpointRequest, GetUserMailboxEndpointResponse>
{
    private readonly IUserAuthGraphService _userAuthGraphService;

    public GetUserMailboxEndpoint(IUserAuthGraphService userAuthGraphService)
    {
        _userAuthGraphService = userAuthGraphService;
    }

    public override void Configure()
    {
        Get("/me/messages");
        Description(d => d.WithName("getusermailbox"));
        
        // Requires mail read permission
        Permissions(GraphPermissions.MailRead);
    }

    public override async Task HandleAsync(GetUserMailboxEndpointRequest req, CancellationToken ct)
    {
        try
        {
            var top = req.Top ?? 5;
            if (top < 1) top = 5;
            if (top > 25) top = 25;

            List<Message> messages = await _userAuthGraphService.GetMyMessagesAsync(top, ct);

            var response = new GetUserMailboxEndpointResponse
            {
                Messages = messages.Select(m => new MessageDto
                {
                    Subject = m.Subject ?? "(No subject)",
                    ReceivedDateTime = m.ReceivedDateTime?.DateTime ?? DateTime.MinValue,
                    FromEmail = m.From?.EmailAddress?.Address,
                    FromName = m.From?.EmailAddress?.Name
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