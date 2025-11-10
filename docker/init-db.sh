#!/bin/bash
set -e

# Create multiple databases for Verdure MCP Platform
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
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
    SELECT 'Databases created successfully: verdure_mcp, verdure_identity, verdure_keycloak' AS status;
EOSQL

echo "Database initialization completed"
