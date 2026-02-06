# Verdure MCP Platform - UI Guide

This guide provides information about the Blazor WebAssembly UI implementation.

## Overview

The Verdure MCP Platform features a modern, professional UI built with:
- **Blazor WebAssembly** - Client-side single-page application
- **MudBlazor 8.0** - Material Design component library
- **JWT Authentication** - Token-based authentication with LocalStorage

## Features

### 1. Dashboard
- Real-time statistics cards showing:
  - Total MCP Servers
  - Total Bindings
  - Active Bindings
  - Inactive Bindings
- Recent servers list
- Quick action buttons

### 2. MCP Servers Management
- **List View**:
  - Search functionality
  - Server name, address, and binding count
  - Edit, view bindings, and delete actions
- **Create/Edit**:
  - Form validation
  - Server name, address, and description fields
  - Server information display (for edit mode)

### 3. MCP Bindings Management
- **List View**:
  - Search and filter functionality
  - Service name, node address, and status
  - Activate/deactivate, edit, and delete actions
  - View by server or all bindings
- **Create/Edit**:
  - Server selection dropdown
  - Service name and node address fields
  - Form validation

### 4. Authentication
- **Login Page**:
  - JWT token input (for development/testing)
  - Can be extended with Keycloak OIDC flow
- **Logout**: Clears authentication state
- **Settings**: View user claims and account information

### 5. Support Page
- **Support Us**:
  - Bilibili follow and charge links
  - QR code donation dialog (WeChat and Alipay)
  - Other ways to support: Star, share, report issues, contribute code

## Design System

### Color Palette
- **Primary**: Purple gradient (#667eea to #764ba2)
- **Secondary**: Purple shade (#764ba2)
- **Success**: Teal (#06d6a0)
- **Info**: Blue (#4cc9f0)
- **Warning**: Yellow (#ffd60a)
- **Error**: Red (#ef476f)
- **Background**: Light gray (#f5f7fa)

### Typography
- **Font Family**: Inter, Segoe UI, Roboto, Helvetica, Arial, sans-serif
- **Headings**: Various Typo levels (h3-h6)
- **Body**: Typo.body1 and Typo.body2

### Components
- **MudCard**: Elevation 2 for consistent depth
- **MudTable**: Dense layout with hover effects
- **MudButton**: Filled primary, outlined secondary
- **MudChip**: Size small for status indicators
- **MudTextField**: Outlined variant with dense margin

## Configuration

### Web Project (Blazor WebAssembly)

Create or update `wwwroot/appsettings.json`:

```json
{
  "ApiBaseAddress": "https://localhost:5000"
}
```

For development, use `appsettings.Development.json`:

```json
{
  "ApiBaseAddress": "http://localhost:5000"
}
```

### API Project (Backend)

Update `appsettings.json` to configure JWT/Keycloak:

```json
{
  "Jwt": {
    "Key": "your-secret-key-here-min-32-chars",
    "Issuer": "verdure-mcp-platform",
    "Audience": "verdure-mcp-api"
  },
  "Keycloak": {
    "Realm": "verdure",
    "AuthServerUrl": "http://localhost:8080",
    "ClientId": "verdure-mcp-platform",
    "ClientSecret": "your-client-secret"
  }
}
```

## Authentication Flow

### Current Implementation (JWT Token)

1. User navigates to Login page
2. User pastes a JWT token (obtained from Keycloak or other provider)
3. Token is stored in LocalStorage
4. Token is automatically included in all API requests via Authorization header
5. API validates the token and extracts user identity

### Keycloak OIDC Flow (To be configured)

1. Configure Keycloak client with appropriate redirect URIs
2. Update `appsettings.json` with Keycloak details
3. Implement OIDC authentication flow in Blazor
4. Users will be redirected to Keycloak for authentication
5. JWT tokens will be automatically managed

## Running the Application

### Development Mode

1. **Start the API**:
   ```bash
   cd src/Verdure.McpPlatform.Api
   dotnet run
   ```

2. **Start the Web UI**:
   ```bash
   cd src/Verdure.McpPlatform.Web
   dotnet run
   ```

3. Navigate to `http://localhost:5001` (or the port shown)

### Using Aspire (Recommended)

```bash
cd src/Verdure.McpPlatform.AppHost
dotnet run
```

This will start both API and Web projects with service orchestration.

## Development Notes

### Adding New Pages

1. Create a new `.razor` file in `Pages/`
2. Add `@page` directive with route
3. Add `@attribute [Authorize]` if authentication is required
4. Inject required services
5. Add navigation link in `Layout/NavMenu.razor`

### Adding New Services

1. Create interface in `Services/I{ServiceName}.cs`
2. Implement service in `Services/{ServiceName}.cs`
3. Register in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IServiceName, ServiceName>();
   ```

### Styling Guidelines

- Use MudBlazor components for consistency
- Follow the established color palette
- Use consistent spacing (Class="mb-4", "mt-4", etc.)
- Apply `Elevation="2"` for cards
- Use `Dense="true"` for forms and tables

## API Integration

All API calls go through service classes:
- `IMcpServerClientService` - Server operations
- `IMcpBindingClientService` - Binding operations
- `IAuthenticationService` - Authentication management

Services automatically include JWT tokens in requests through the `CustomAuthenticationStateProvider`.

## Troubleshooting

### Token Not Working
- Verify the JWT token is valid and not expired
- Check that API is configured to accept the token
- Ensure CORS is properly configured

### API Connection Issues
- Verify `ApiBaseAddress` in appsettings.json
- Check that API is running
- Verify CORS allows requests from the Web UI origin

### Build Errors
- Run `dotnet clean` and `dotnet build`
- Check for package version conflicts
- Verify all required packages are restored

## Future Enhancements

- [ ] Implement full Keycloak OIDC flow
- [ ] Add user profile management
- [ ] Implement real-time updates with SignalR
- [ ] Add export/import functionality
- [ ] Implement advanced filtering and sorting
- [ ] Add dark mode toggle
- [ ] Implement pagination for large datasets
- [ ] Add audit logging UI

## Resources

- [MudBlazor Documentation](https://mudblazor.com/)
- [Blazor WebAssembly Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
