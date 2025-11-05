# Database Configuration Guide

## Overview

The Verdure MCP Platform supports multiple database providers through configuration. You can switch between different database systems by simply changing the `appsettings.json` file without modifying any code.

## Supported Providers

- **SQLite** - Default, suitable for development and small deployments
- **PostgreSQL** - Recommended for production environments

## Configuration

### Database Settings

```json
{
  "Database": {
    "TablePrefix": "verdure_",
    "Provider": "SQLite"
  },
  "ConnectionStrings": {
    "mcpdb": "Data Source=Data/mcpplatform.db",
    "identitydb": "Data Source=Data/identity.db"
  }
}
```

### Provider Options

| Provider | Value | Description |
|----------|-------|-------------|
| SQLite | `"SQLite"` | File-based database, no server required |
| PostgreSQL | `"PostgreSQL"` or `"Postgres"` | Full-featured relational database |

## Examples

### 1. SQLite Configuration (Development)

**Best for**: Development, testing, small deployments

```json
{
  "Database": {
    "TablePrefix": "verdure_",
    "Provider": "SQLite"
  },
  "ConnectionStrings": {
    "mcpdb": "Data Source=Data/mcpplatform.db",
    "identitydb": "Data Source=Data/identity.db"
  }
}
```

**Features**:
- ✅ No server setup required
- ✅ Single file per database
- ✅ Easy to backup and restore
- ✅ Perfect for development
- ⚠️ Limited concurrency
- ⚠️ Not suitable for high-traffic production

### 2. PostgreSQL Configuration (Production)

**Best for**: Production, high-traffic scenarios, multi-instance deployments

```json
{
  "Database": {
    "TablePrefix": "verdure_",
    "Provider": "PostgreSQL"
  },
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=your_password",
    "identitydb": "Host=localhost;Database=verdure_identity;Username=postgres;Password=your_password"
  }
}
```

**Features**:
- ✅ Production-ready
- ✅ Excellent concurrency
- ✅ ACID compliance
- ✅ Advanced features (JSON, full-text search, etc.)
- ✅ Horizontal scaling support
- ⚠️ Requires server setup

### 3. PostgreSQL with .NET Aspire

When using .NET Aspire orchestration, connection strings are automatically managed:

```json
{
  "Database": {
    "TablePrefix": "verdure_",
    "Provider": "PostgreSQL"
  }
}
```

Aspire will inject the `mcpdb` and `identitydb` connection strings automatically.

## Table Prefix Configuration

### What is Table Prefix?

The `TablePrefix` setting adds a prefix to all database table names. This is useful for:
- Sharing a database with other applications
- Multi-tenant deployments
- Avoiding naming conflicts
- Organizing tables by application/module

### Examples

#### With Prefix `"verdure_"`
```
verdure_xiaozhi_connections
verdure_mcp_service_configs
verdure_mcp_service_bindings
verdure_mcp_tools
```

#### Without Prefix (empty string `""`)
```
xiaozhi_connections
mcp_service_configs
mcp_service_bindings
mcp_tools
```

#### Custom Prefix `"prod_verdure_"`
```
prod_verdure_xiaozhi_connections
prod_verdure_mcp_service_configs
prod_verdure_mcp_service_bindings
prod_verdure_mcp_tools
```

### Configuration Example

```json
{
  "Database": {
    "TablePrefix": "",  // No prefix
    "Provider": "SQLite"
  }
}
```

## Environment-Specific Configuration

### Development (`appsettings.Development.json`)

```json
{
  "Database": {
    "TablePrefix": "dev_verdure_",
    "Provider": "SQLite"
  },
  "ConnectionStrings": {
    "mcpdb": "Data Source=Data/mcpplatform.db",
    "identitydb": "Data Source=Data/identity.db"
  }
}
```

### Staging (`appsettings.Staging.json`)

```json
{
  "Database": {
    "TablePrefix": "staging_verdure_",
    "Provider": "PostgreSQL"
  },
  "ConnectionStrings": {
    "mcpdb": "Host=staging-db.example.com;Database=verdure_mcp;Username=app_user;Password=secret",
    "identitydb": "Host=staging-db.example.com;Database=verdure_identity;Username=app_user;Password=secret"
  }
}
```

### Production (`appsettings.Production.json`)

```json
{
  "Database": {
    "TablePrefix": "verdure_",
    "Provider": "PostgreSQL"
  },
  "ConnectionStrings": {
    "mcpdb": "Host=prod-db.example.com;Database=verdure_mcp;Username=app_user;Password=secret;SSL Mode=Require",
    "identitydb": "Host=prod-db.example.com;Database=verdure_identity;Username=app_user;Password=secret;SSL Mode=Require"
  }
}
```

## Switching Providers

### From SQLite to PostgreSQL

1. **Update Configuration**:
   ```json
   {
     "Database": {
       "TablePrefix": "verdure_",
       "Provider": "PostgreSQL"
     },
     "ConnectionStrings": {
       "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=your_password",
       "identitydb": "Host=localhost;Database=verdure_identity;Username=postgres;Password=your_password"
     }
   }
   ```

2. **Create Migration** (if needed):
   ```powershell
   dotnet ef migrations add SwitchToPostgreSQL `
     --project src/Verdure.McpPlatform.Infrastructure `
     --startup-project src/Verdure.McpPlatform.Api
   ```

3. **Apply Migration**:
   ```powershell
   dotnet ef database update `
     --project src/Verdure.McpPlatform.Infrastructure `
     --startup-project src/Verdure.McpPlatform.Api
   ```

### From PostgreSQL to SQLite

1. **Update Configuration**:
   ```json
   {
     "Database": {
       "TablePrefix": "verdure_",
       "Provider": "SQLite"
     },
     "ConnectionStrings": {
       "mcpdb": "Data Source=Data/mcpplatform.db",
       "identitydb": "Data Source=Data/identity.db"
     }
   }
   ```

2. **Export Data** (if migrating existing data):
   - Use database export/import tools
   - Or use Entity Framework data seeding

3. **Create Migration** (if needed):
   ```powershell
   dotnet ef migrations add SwitchToSQLite `
     --project src/Verdure.McpPlatform.Infrastructure `
     --startup-project src/Verdure.McpPlatform.Api
   ```

4. **Apply Migration**:
   ```powershell
   dotnet ef database update `
     --project src/Verdure.McpPlatform.Infrastructure `
     --startup-project src/Verdure.McpPlatform.Api
   ```

## Connection String Examples

### PostgreSQL

#### Local Development
```
Host=localhost;Port=5432;Database=verdure_mcp;Username=postgres;Password=postgres
```

#### With SSL
```
Host=prod-db.example.com;Database=verdure_mcp;Username=app_user;Password=secret;SSL Mode=Require
```

#### With Connection Pooling
```
Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100
```

### SQLite

#### Relative Path
```
Data Source=Data/mcpplatform.db
```

#### Absolute Path
```
Data Source=C:\Data\mcpplatform.db
```

#### In-Memory (Testing)
```
Data Source=:memory:
```

## Best Practices

1. **Use Environment Variables for Sensitive Data**:
   ```json
   "ConnectionStrings": {
     "mcpdb": "${DB_CONNECTION_STRING}"
   }
   ```

2. **Use Different Prefixes per Environment**:
   - Development: `dev_verdure_`
   - Staging: `staging_verdure_`
   - Production: `verdure_`

3. **Enable Connection Pooling** (PostgreSQL):
   ```
   Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100
   ```

4. **Use SSL in Production** (PostgreSQL):
   ```
   SSL Mode=Require
   ```

5. **Regular Backups**:
   - SQLite: Copy the `.db` file
   - PostgreSQL: Use `pg_dump`

## Troubleshooting

### Error: "Unsupported database provider"

**Solution**: Check the `Database:Provider` value in `appsettings.json`. Ensure it's one of: `SQLite`, `PostgreSQL`, or `Postgres`.

### Error: "Connection string is required"

**Solution**: When using PostgreSQL, ensure the connection string is provided in `ConnectionStrings:mcpdb` and `ConnectionStrings:identitydb`.

### Tables Not Found

**Solution**: Run database migrations:
```powershell
dotnet ef database update --project src/Verdure.McpPlatform.Infrastructure --startup-project src/Verdure.McpPlatform.Api
```

### Wrong Table Names

**Solution**: Check the `Database:TablePrefix` setting. If you changed it after creating tables, you need to create a new migration.

## See Also

- [Entity Framework Core Documentation](https://learn.microsoft.com/ef/core/)
- [PostgreSQL .NET Driver](https://www.npgsql.org/)
- [SQLite EF Core Provider](https://learn.microsoft.com/ef/core/providers/sqlite/)
