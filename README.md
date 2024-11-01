## Projects in this Workspace

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
