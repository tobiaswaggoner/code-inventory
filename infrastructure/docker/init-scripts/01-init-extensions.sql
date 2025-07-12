-- Initialize PostgreSQL extensions for CodeInventory database
-- This script runs automatically when the PostgreSQL container starts for the first time

-- Enable UUID extension for generating UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable pgcrypto extension for cryptographic functions (if needed later)
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create a simple health check function
CREATE OR REPLACE FUNCTION health_check() RETURNS TEXT AS $$
BEGIN
    RETURN 'CodeInventory Database is healthy';
END;
$$ LANGUAGE plpgsql;