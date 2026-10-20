# User Management API — TechHive Solutions

A beginner-friendly ASP.NET Core **.NET 10** API for Coursera's **Building a Simple API with Copilot** peer-graded assignment. The API provides CRUD user management, DataAnnotations validation, centralized JSON error responses, request/response logging, and a simple Bearer-token demonstration.

## Prerequisites

- .NET 10 SDK (`dotnet --version`)
- VS Code + C# extension (optional)
- curl or Postman

## Start

```bash
cd UserManagementAPI
export ApiToken='replace-this-with-a-long-random-secret'
dotnet restore
dotnet run --launch-profile http
```

The API listens on `http://localhost:5180` with this launch profile. Development permits HTTP for local testing. Outside Development, protected routes reject HTTP requests with `400`; configure HTTPS directly or configure trusted forwarded headers when TLS terminates at a reverse proxy. The public `/health` endpoint remains available over HTTP. **Do not commit a real token.** The server intentionally rejects protected requests if `ApiToken` is unset. On macOS/Linux you can generate a random token using `openssl rand -hex 32`. In PowerShell use `$env:ApiToken="..."`.

## Routes

| Method | Path | Behavior | Success |
| --- | --- | --- | --- |
| GET | `/health` | Public health check | 200 |
| GET | `/api/users` | List all users | 200 |
| GET | `/api/users/{id}` | Get one user | 200 or 404 |
| POST | `/api/users` | Create a user | 201 |
| PUT | `/api/users/{id}` | Update user | 200 or 404 |
| DELETE | `/api/users/{id}` | Delete user | 204 or 404 |

All `/api/users` routes require `Authorization: Bearer <ApiToken>`; otherwise 401. Invalid input returns 400 with validation details. Unhandled exceptions return 500 with a safe JSON message. API data is stored **in memory** and resets on restart; no database is required for this assignment.

## Sample requests

```bash
export API_TOKEN="$ApiToken"
curl -i http://localhost:5180/health
curl -i http://localhost:5180/api/users -H "Authorization: Bearer $API_TOKEN"
curl -i -X POST http://localhost:5180/api/users \
  -H "Authorization: Bearer $API_TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Jane Doe","email":"jane@example.com","department":"HR"}'
curl -i -X PUT http://localhost:5180/api/users/1 \
  -H "Authorization: Bearer $API_TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Jane Smith","email":"jane.smith@example.com","department":"IT"}'
curl -i -X DELETE http://localhost:5180/api/users/1 -H "Authorization: Bearer $API_TOKEN"
```

Run `API_TOKEN="$ApiToken" bash scripts/smoke-test.sh` in a **second terminal** while the server runs. The test starts with an empty in-memory user store; restart the API before rerunning if necessary.

## Middleware

Execution order: `RequestLoggingMiddleware` -> `ApiExceptionMiddleware` -> `TokenAuthenticationMiddleware`.

1. `RequestLoggingMiddleware` wraps the rest of the pipeline and logs method, path, final status code and elapsed time using `ILogger`.
2. `TokenAuthenticationMiddleware` validates a configured Bearer token and rejects unauthorized requests with JSON 401 responses.
3. `ApiExceptionMiddleware` catches unhandled exceptions and returns safe JSON 500 responses; request-abort cancellations are logged at debug level.

The order follows the course activity. Rejected requests are logged by the authentication middleware itself, because they do not reach the downstream logging middleware. The token approach is an **educational shared-token example**, not a production identity system. For production use standard ASP.NET Core JWT Bearer authentication, HTTPS, a persistent database, and rate limiting.

## Validation and debugging improvements

- Required `Name`, `Email`, `Department`; trims values before applying email and length validation; rejects whitespace-only fields.
- Returns 404 instead of throwing when a user ID is missing.
- Centralized exception handling prevents internal exception details from leaking.
- `ConcurrentDictionary` and atomic ID generation avoid simple concurrency bugs in the in-memory example.
- Uses structured logging templates rather than printing raw messages.

The paragraph above is historical and describes the original assignment order; it does not reflect the current pipeline. The current execution order is listed at the top of this section.

## Copilot debugging record

This review, implementation, and verification were completed in this Copilot SDK session in VS Code; this does not claim a separate GitHub Copilot Chat UI session.

- Prompt used: `Review my ASP.NET Core 10 User Management API project for bugs, security vulnerabilities, missing validation, improper exception handling, and middleware ordering issues. Examine Program.cs, the Models folder, and all middleware classes. Identify specific issues, recommend improvements, and explain how to test the fixes.`
- Findings: production HTTP requests could carry bearer tokens without transport protection; validation checked string lengths before trimming, allowing values such as `" A "` to be stored as `"A"`; request logging ran inside exception handling and could log a stale status code.
- Fixes applied: require HTTPS for protected routes outside Development; trim before validation; place request logging outside exception handling; log request-abort cancellations at debug level.
- Tests: `dotnet build --no-restore` passed with 0 warnings/errors; `BASE=http://localhost:5182 API_TOKEN=local-test-token bash scripts/smoke-test.sh` passed all 10 checks, including the whitespace-padded short-name case; Production-mode `GET /api/users` over HTTP returned 400 and public `/health` returned 200.

## Peer review checklist

- [ ] Public GitHub repository created and link submitted
- [x] CRUD routes GET, POST, PUT, DELETE implemented
- [x] Copilot used for a genuine debugging step and evidence added
- [x] Validation implemented
- [x] Logging, authentication and exception middleware implemented
- [x] Project built and endpoints tested locally

## GitHub publishing

Create a **public empty** repository named `UserManagementAPI` on GitHub, then run from this folder:

```bash
git init
git add .
git commit -m "Build user management API with validation and middleware"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/UserManagementAPI.git
git push -u origin main
```

Replace `YOUR_USERNAME` with your GitHub account name. Copy the repository link into the Coursera peer-graded assignment submission. A GitHub repository is source hosting; it does not itself run the API.
