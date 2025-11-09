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

    -- Log success
    SELECT 'Databases created successfully: verdure_mcp, verdure_identity' AS status;
EOSQL

echo "Database initialization completed"
