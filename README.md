# Projects in this Workspace
## Angular and dotnet fastendpoint combination
Considering https://github.com/EelcoLos/nx-tinkering/issues/240, there is a 2 part project, namely `dotnet-fe-auth` and `angular-auth-example` that represent the option that would normally be a dotnet new creation, namely `dotnet new angular -o Project`. However, this is a project that would be intertwined with **controllers** and an old angular version, which makes it hard to maintain both. Second, there is no real interaction between the two itself... you have to still create the services to call the controller api.
This combination has `nswag` in between, creating said interaction.

### dotnet-fe-auth

`dotnet-fe-auth` is a .NET backend project that provides authentication services using JWT tokens. It includes endpoints for validating tokens and integrates with FastEndpoints for rapid API development. The project is configured to use symmetric key encryption for JWT tokens.

#### Key Features:
- JWT Authentication
- Token Validation Endpoint
- FastEndpoints Integration
- Swagger Documentation

#### How to Run:
1. Navigate to the `dotnet-fe-auth` directory.
2. Run `dotnet build` to build the project.
3. Run `dotnet run` to start the server.
4. The server will be available at `https://localhost:5001`.

### dotnet-fe-msgraph

`dotnet-fe-msgraph` is a .NET backend project that integrates Microsoft Graph API with FastEndpoints. It demonstrates how to implement dynamic permission scopes and provides both user authentication (delegated permissions) and application authentication flows.

#### Key Features:
- Microsoft Graph API Integration
- Dynamic Permission Scopes
- User Authentication (Delegated Permissions)
- Application Authentication
- Multiple API Endpoints (User Profile, Mail, Calendar)
- Swagger Documentation

#### How to Run:
1. Register an application in Azure AD and configure the necessary permissions.
2. Update the `appsettings.json` with your Azure AD application details.
3. Navigate to the `dotnet-fe-msgraph` directory.
4. Run `dotnet build` to build the project.
5. Run `dotnet run` to start the server.
6. The server will be available at `https://localhost:7001`.

### angular-auth-example

`angular-auth-example` is an Angular frontend project that demonstrates authentication using JWT tokens. It includes a login form, protected routes, and integration with the `dotnet-fe-auth` backend for token validation.

#### Key Features:
- Login Form
- JWT Token Storage in LocalStorage
- Protected Routes with Angular Guards
- HTTP Interceptor for Adding JWT Token to Requests

#### How to Run:
1. Before ever running, make sure you ran the `npm run dev-cert` command in the root of the repository to generate a development certificate.
2. Run `npx nx serve angular-auth-example` to start the development server.
3. The application will be available at `https://localhost:4200`.

This app is dependant on the `dotnet-fe-auth` backend project to be running in order to authenticate users and validate tokens.
