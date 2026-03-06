# HackerNews Story Browser

A full-stack application for browsing the newest stories from Hacker News. Built with a .NET 9 API backend and Angular 18 frontend in a monorepo structure.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)

## Getting Started

```bash
# Install root tooling (concurrently)
npm install

# Install Angular dependencies
npm run install:frontend

# Start both API and frontend
npm start
```

This launches the API on `http://localhost:5000` and the Angular dev server on `http://localhost:4200`.

## Running Tests

```bash
# Run all tests (backend + frontend)
npm test

# Backend only
dotnet test api/HackerNewsApi.Tests

# Frontend only
cd frontend && npx ng test
```

## Project Structure

```
.
├── api/
│   ├── HackerNewsApi/             # .NET 9 Web API
│   │   ├── Controllers/           # StoriesController (GET /api/stories/newest)
│   │   ├── Models/                # Story model
│   │   ├── Services/              # IHackerNewsService / HackerNewsService
│   │   └── Program.cs             # App configuration & DI setup
│   └── HackerNewsApi.Tests/       # xUnit + Moq tests
│       ├── StoriesControllerTests.cs
│       └── HackerNewsServiceTests.cs
├── frontend/                      # Angular 18 SPA
│   ├── src/app/
│   │   ├── components/story-list/ # Story list component + spec
│   │   ├── models/                # Story TypeScript interface
│   │   └── services/              # HackerNewsService + spec
│   └── proxy.conf.json            # Dev proxy to API
└── package.json                   # Root scripts (start, test, build)
```

## Key Concepts

### In-Memory Caching

The API caches fetched stories using `IMemoryCache` with a 5-minute TTL. This avoids hammering the HackerNews Firebase API on every request. See `HackerNewsService.cs` for the cache key `"newest_stories"` and `TimeSpan.FromMinutes(5)` duration.

### Dependency Injection

`IHackerNewsService` is registered in `Program.cs` and injected into `StoriesController`. This decouples the controller from the concrete implementation, making the controller independently testable with a mock service.

### Typed HttpClient Factory

```csharp
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>();
```

Registers `HackerNewsService` with a typed `HttpClient` via the built-in HttpClient factory. This provides automatic `HttpClient` lifetime management and avoids socket exhaustion issues.

### Parallel Async Fetching

Story details are fetched concurrently using `Task.WhenAll`:

```csharp
var tasks = storyIds.Take(200).Select(id => FetchStoryAsync(id));
var stories = await Task.WhenAll(tasks);
```

This fetches up to 200 story details in parallel rather than sequentially, significantly reducing response time.

### Server-Side Pagination and Search

The `GET /api/stories/newest` endpoint supports `search`, `page`, and `pageSize` query parameters. Filtering by title (case-insensitive) and pagination are performed server-side, returning a response with `stories`, `totalCount`, `page`, and `pageSize`.

### Angular Standalone Components

The frontend uses Angular 18's standalone component architecture -- no `NgModule` declarations. Components declare their imports directly via the `@Component` decorator's `imports` array.

### RxJS Observables for HTTP

The Angular `HackerNewsService` uses `HttpClient` which returns RxJS `Observable`s. Components subscribe to these observables to reactively handle API responses.

### Proxy Configuration

During development, `frontend/proxy.conf.json` routes `/api` requests from the Angular dev server (`localhost:4200`) to the .NET API (`localhost:5000`), avoiding CORS issues in development.

### CORS Policy

The API configures a named CORS policy `"AllowAngular"` permitting requests from `http://localhost:4200` with any headers and methods. This is applied as middleware via `app.UseCors("AllowAngular")`.

### Testing Strategies

**Backend (xUnit + Moq):** `StoriesControllerTests` mock `IHackerNewsService` to test controller logic in isolation. `HackerNewsServiceTests` verify caching and HTTP behavior.

**Frontend (Jasmine + Karma):** Component and service specs use Angular's `TestBed` for dependency injection setup, with `HttpClientTestingModule` for mocking HTTP calls.
