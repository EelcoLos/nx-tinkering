# Microsoft Graph FastEndpoints Application

This project demonstrates how to integrate Microsoft Graph API with FastEndpoints, providing both user-based (delegated) and application-based permissions.

## Features

- Integration with Microsoft Graph API using FastEndpoints
- Dynamic permission-based access control
- Both user authentication (delegated permissions) and app-only authentication
- Multiple API endpoints demonstrating various Graph API capabilities

## Endpoints

- `GET /api/me` - Get the current user's profile (requires `User.Read` permission)
- `GET /api/me/messages` - Get the current user's emails (requires `Mail.Read` permission)
- `GET /api/me/calendar` - Get the current user's calendar events (requires `Calendars.Read` permission)
- `GET /api/users` - Get all users in the directory (requires `User.Read.All` application permission)

## Configuration

Before running the application, you need to configure Azure Active Directory settings in `appsettings.json`:

```json
"AzureAd": {
  "TenantId": "YOUR_TENANT_ID",
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET"
}
```

## Microsoft Graph Permissions

The application uses the following Microsoft Graph permissions:

### Delegated Permissions (User)
- `User.Read` - Read current user's profile
- `Mail.Read` - Read user's email
- `Calendars.Read` - Read user's calendar events

### Application Permissions
- `User.Read.All` - Read all users' profiles in the organization

## How to Run

1. Register an application in the Azure Active Directory portal
2. Configure the required permissions (both delegated and application)
3. Update `appsettings.json` with your application's details
4. Run the application:

```bash
dotnet run
```

## Permission Handling

FastEndpoints provides a customizable way to handle permissions. In this application, we use:

```csharp
public override void Configure()
{
    Get("/api/me");
    Permissions("User.Read"); // Specify the required permission
}
```

This ensures that the endpoint requires the specified Microsoft Graph permission scope.