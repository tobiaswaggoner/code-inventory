# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Code Inventory is a .NET 8 application designed to analyze and catalog all software projects across local file systems and remote repositories. The application creates a comprehensive timeline of development activities by extracting Git commit histories and project metadata.

## Architecture

The project follows a multi-tier architecture:

- **Backend**: .NET 8 Web API with Background Workers for crawling and analysis
- **Frontend**: .NET 8 Blazor Server for interactive web interface  
- **Database**: PostgreSQL for storing project metadata and commit histories
- **Data Access**: Entity Framework Core 8

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

## Service Architecture

### Git Services
- **IGitCommandService**: Executes Git commands with timeout and error handling
- **IGitLogParser**: Parses Git command output into domain models
- **IRepositoryScanner**: Recursively scans directories for Git repositories
- **IGitIntegrationService**: High-level orchestration of Git operations
- **IRepositoryDataService**: Database operations with deduplication logic

### Background Services
- **DirectoryCrawlerService**: Background service that processes crawl triggers
- **ICrawlTriggerService**: Manages crawl execution triggers
- **IDelayProvider**: Testable delay abstraction for better unit testing

### Dependency Injection
All services are registered with interfaces for testability and maintainability.

## Development Commands

### Basic .NET Commands
```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the backend API
dotnet run --project src/CodeInventory.Backend

# Run the Blazor frontend
dotnet run --project src/CodeInventory.WebApp

# Run with crawler execution
dotnet run --project src/CodeInventory.Backend -- -execute-crawl
```

### Database Commands
```bash
# Start PostgreSQL databases
docker compose -f infrastructure/docker/docker-compose.yml up -d

# Stop databases
docker compose -f infrastructure/docker/docker-compose.yml down

# View database logs
docker compose -f infrastructure/docker/docker-compose.yml logs postgres

# Create and apply EF migrations
dotnet ef migrations add [MigrationName] --startup-project src/CodeInventory.Backend
dotnet ef database update --startup-project src/CodeInventory.Backend
```

## Infrastructure

### Database Setup
PostgreSQL runs via Docker Compose in `infrastructure/docker/`:
- **Development database**: Port 5432 (codeinventory/dev123456)
- **Test database**: Port 5433 (codeinventory_test/test123456)
- **Configuration**: Environment variables in `.env` file
- **Initialization**: SQL scripts in `init-scripts/` directory

### Configuration
- Root directories to scan configured in `appsettings.json` under `CrawlSettings`
- Remote Git URLs also configured in the same section
- Environment-specific settings via `.env` files

## Testing Framework

### Test Structure
- **NUnit** for test framework
- **NSubstitute** for mocking dependencies
- **Backend Tests**: Comprehensive coverage of services and Git operations
- **WebApp Tests**: Basic template tests (to be expanded in Phase 2)

### Test Projects
- `CodeInventory.Backend.Tests` - Core business logic tests
- `CodeInventory.WebApp.Tests` - Web interface tests

### Testing Patterns
- All dependencies are mocked using interfaces
- Focus on testing core functionality without logger outputs
- Comprehensive test coverage for Git parsing and integration logic

## Implementation Phases

### Phase 1: Core Data Collection (MVP) ✅ **COMPLETED**
- ✅ Local file system crawler for Git repositories
- ✅ Git history extraction via command-line interface
- ✅ Complete project and commit storage with deduplication
- ✅ Background service architecture with trigger mechanism
- ✅ HTTP API endpoints for crawl management
- ✅ Comprehensive testing framework
- ✅ Database migrations and entity relationships
- ✅ Service-oriented architecture with dependency injection

### Phase 2: Repository Analysis ✅ **COMPLETED**
- ✅ Repomix integration for code consolidation
- ✅ Gemini API integration for AI-powered analysis
- ✅ Automated project description generation
- ✅ Hero image generation for projects
- ✅ One-liner headline generation
- ✅ Extended data model with analysis fields
- ✅ CLI commands for repository analysis
- ✅ Debug file output for manual inspection

### Phase 3: Visualization (IN PROGRESS)
- ✅ Blazor web interface for project overview
- ✅ Display of AI-generated project descriptions and images
- Add Tags to the project data (many tags per project like C#, Unity, .NET, etc.)
- Add Category to the project (One Category per project: Private, Work, etc.)
- Add State to the (One State per project: Experiment, In Progress, Usable, Obsolete, etc.)
- Add start end end date to project - extract from commit history + latest file change date
- Display Tags, Categoriesm State, start, end within the tiles of the project overview
- Add filter and grouping to the project overview. 
- Group by either state, Category or Tag
- Filter: One search bar with fulltext search and syntax like "tag:xxx AND state:yyy" (keywords and selectors)
- Global timeline visualization of all commits. Apply the same filter as within the overview page
- Add commit overview (list view, endless scroller) as overlay when clicking on a project in the overview
- Dashboard with statistics and insights

### Phase 4: Include more data (PLANNED)
- ManicTime data integration for actual time spent analysis
- Correlation between tracked time and project activity
- Create code history by checking out each commit squentially and running cloc (lines of code per type per commit plus delta to previous commit - do not use git LOC count - we want to split by type (c#/ ts, ...))

## Technical Notes

### Git Integration
The application uses command-line Git calls rather than libraries to maximize compatibility:
- `git log --all --pretty=format:"%H|||%an|||%ae|||%aI|||%s" --no-patch` for commit history
- `git status --porcelain` for uncommitted changes detection
- `git rev-list --max-parents=0 HEAD` for initial commit identification

### Idempotent Operations
All crawling and analysis operations are designed to be idempotent - they can be run multiple times without creating duplicates or corrupting existing data.

### Error Handling
- Comprehensive exception handling in all services
- Graceful handling of inaccessible directories
- Detailed logging for debugging and monitoring
- Service continues processing despite individual repository failures

### Performance Considerations
- Directory scanning with smart skip logic for common build/cache directories
- Small delays between repository processing to avoid system overload
- Scoped service resolution for proper resource management

### HTTP API Endpoints
The backend provides RESTful endpoints (via Swagger in development):
- Standard Web API controllers for future dashboard integration
- Background service management through command-line triggers

## Current Status

**Phase 1 is complete** and the system can successfully:
1. Scan configured directories for Git repositories
2. Extract complete Git history from discovered repositories
3. Store project metadata with global deduplication
4. Handle errors gracefully without service interruption
5. Provide detailed logging and statistics
6. Execute crawls via command-line triggers
7. Maintain data integrity through proper entity relationships

The foundation is solid for Phase 2 implementation (web visualization) and beyond.