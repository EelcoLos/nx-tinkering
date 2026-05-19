# Security Considerations

This is a **demo application** for showcasing the A2A (Agent-to-Agent) protocol. It is NOT suitable for production use without significant security improvements.

## Known Security Issues (Demo Only)

### 1. Authentication
- **Plain text passwords**: Passwords are stored as plain text in memory. Production systems must use proper password hashing (bcrypt, Argon2).
- **Fixed demo users**: Only demo users (admin/demo123, user/user456) are hardcoded. Production systems require proper user management.

### 2. Agent Token Generation
- **No authentication**: The `/auth/agent/token` endpoint allows **any** caller to request tokens for **any** agent ID without authentication.
- **Production fix**: Require authentication for agent token generation using one of:
  - Client credentials (OAuth2)
  - API keys with rotation
  - Mutual TLS (mTLS)
  - Service-to-service authentication

### 3. CORS Configuration
- **AllowAnyOrigin**: The API backend allows requests from any origin, exposing the API to potential CSRF attacks.
- **Production fix**: Restrict CORS to specific trusted domains:
  ```csharp
  policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
  ```

### 4. Environment Variables
- **Weak JWT Secret**: The JWT secret key defaults to a weak value if not set. Always set `JWT_SECRET_KEY` to a strong, randomly generated value.
- **Production fix**: 
  - Generate a cryptographically secure key (32+ characters)
  - Set via environment variables or secret management systems
  - Rotate keys periodically
  - Validate that required secrets are set at startup (fail fast)

### 5. Exception Handling
- **Silent failures**: Empty catch blocks swallow exceptions without logging, making debugging difficult.
- **Production fix**: Implement comprehensive logging and structured error handling.

## Production Security Checklist

- [ ] Implement proper password hashing (ASP.NET Core Identity or similar)
- [ ] Secure agent token generation (require authentication)
- [ ] Restrict CORS to specific trusted origins
- [ ] Implement comprehensive logging and monitoring
- [ ] Use HTTPS/TLS for all communication
- [ ] Implement rate limiting and DDoS protection
- [ ] Add authentication/authorization to all API endpoints
- [ ] Use secret management systems (Vault, Key Vault, Secrets Manager)
- [ ] Implement audit logging for sensitive operations
- [ ] Regular security testing and vulnerability scanning

## Environment Variables (Required for Production)

```bash
# Security
JWT_SECRET_KEY=<your-cryptographically-secure-key-32-chars-minimum>
IDENTITY_SECRET=<random-secret-for-identity-service>

# Service Configuration
IDENTITY_SERVICE_URL=<your-identity-service-url>
DISCOVERY_SERVICE_URL=<your-discovery-service-url>
CLASSIFIER_SERVICE_URL=<your-classifier-service-url>
ASSESSOR_SERVICE_URL=<your-assessor-service-url>
ROUTER_SERVICE_URL=<your-router-service-url>
HANDLER_SERVICE_URL=<your-handler-service-url>
API_BACKEND_SERVICE_URL=<your-api-backend-url>

# CORS (set to production domain)
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
```

## Demo Usage

This application is intended for:
- Learning the A2A protocol concepts
- Local development and testing
- Docker/Kubernetes demonstrations
- Educational purposes

For production deployment, engage security experts and conduct thorough security audits.
