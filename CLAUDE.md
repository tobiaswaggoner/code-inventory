# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Code Inventory is a .NET 9 application designed to analyze and catalog all software projects across local file systems and remote repositories. The application creates a comprehensive timeline of development activities by extracting Git commit histories and project metadata.

## Architecture

The project follows a multi-tier architecture:

- **Backend**: .NET 9 Web API with Background Workers for crawling and analysis
- **Frontend**: .NET 9 Blazor Server for interactive web interface  
- **Database**: PostgreSQL for storing project metadata and commit histories
- **Data Access**: Entity Framework Core 9

## Key Concepts

### Data Model
- **Project**: Represents a unique software project identified by its initial commit SHA
- **ProjectLocation**: Tracks different locations (local paths, remote URLs) where the same project exists
- **Commit**: Stores Git commit metadata with global deduplication across all projects
- **Author**: Represents commit authors identified by email address

### Deduplication Strategy
The system uses commit SHA hashes as primary keys to ensure global deduplication across:
- Multiple local clones of the same repository
- Different machines containing the same project
- Local and remote versions of repositories

## Development Commands

Since the project is not yet implemented, standard .NET commands will be used:

```bash
# Build the solution
dotnet build

# Run the backend API
dotnet run --project CodeInventory.Backend

# Run the Blazor frontend
dotnet run --project CodeInventory.WebApp

# Create and apply EF migrations
dotnet ef migrations add [MigrationName] --startup-project CodeInventory.Backend
dotnet ef database update --startup-project CodeInventory.Backend

# Run with crawler execution
dotnet run --project CodeInventory.Backend -- -execute-crawl
```

## Infrastructure

### Database Setup
PostgreSQL runs via Docker Compose with configuration stored in `.env` files:
- Development database on standard port 5432
- Separate test database for testing scenarios

### Configuration
- Root directories to scan configured in `appsettings.json` under `CrawlSettings`
- Remote Git URLs also configured in the same section
- Environment-specific settings via `.env` files

## Implementation Phases

### Phase 1: Core Data Collection (MVP)
- Local file system crawler for Git repositories
- Git history extraction via command-line interface
- Basic project and commit storage with deduplication

### Phase 2: Visualization
- Blazor web interface for project overview
- Global timeline visualization of all commits
- Remote repository synchronization

### Phase 3: Code Analysis
- Technology stack detection (frameworks, languages)
- Project classification and metadata extraction
- AI-powered content analysis

### Phase 4: Time Tracking Integration
- ManicTime data integration for actual time spent analysis
- Correlation between tracked time and project activity

## Technical Notes

### Git Integration
The application uses command-line Git calls rather than libraries to maximize compatibility:
- `git log --all --pretty=format:"%H|||%an|||%ae|||%aI|||%s" --no-patch` for commit history
- `git status --porcelain` for uncommitted changes detection
- `git rev-list --max-parents=0 HEAD` for initial commit identification

### Idempotent Operations
All crawling and analysis operations are designed to be idempotent - they can be run multiple times without creating duplicates or corrupting existing data.

### Testing Strategy
- Unit tests (nUnit) for core logic like Git log parsing and deduplication
- Mocking framework (nSubstitute) for external dependencies
- Target 70%+ test coverage for critical components
- No UI or integration tests planned initially