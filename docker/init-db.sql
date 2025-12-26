-- Create multiple databases for Verdure MCP Platform
-- This file is automatically executed by PostgreSQL docker-entrypoint-initdb.d

-- Create MCP database
CREATE DATABASE verdure_mcp;
GRANT ALL PRIVILEGES ON DATABASE verdure_mcp TO postgres;

-- Create Identity database
CREATE DATABASE verdure_identity;
GRANT ALL PRIVILEGES ON DATABASE verdure_identity TO postgres;

-- Create Keycloak database
CREATE DATABASE verdure_keycloak;
GRANT ALL PRIVILEGES ON DATABASE verdure_keycloak TO postgres;

-- Log success
DO $$
BEGIN
    RAISE NOTICE 'Databases created successfully: verdure_mcp, verdure_identity, verdure_keycloak';
END
$$;
