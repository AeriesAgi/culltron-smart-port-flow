-- SmartPort PostgreSQL initialisation script
-- Runs once on first container start.
-- EF Core migrations handle the actual schema creation.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

SET timezone = 'UTC';
