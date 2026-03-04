# 08 — CORS for Web Frontend

## What changed
Added a CORS policy to `src/GeoLearn.Api/Program.cs` so the React dev
server running at `http://localhost:5173` can call the API at
`http://localhost:8080` without being blocked by the browser's same-origin
policy.

## Files modified
- `src/GeoLearn.Api/Program.cs`

## Details

```csharp
// builder section
builder.Services.AddCors(options =>
    options.AddPolicy("DevFrontend", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader().AllowAnyMethod()));

// middleware, before MapControllers
app.UseCors("DevFrontend");
```

`WithOrigins` is intentionally narrow — only the local Vite dev server is
whitelisted. In production the policy would be replaced by an environment-
specific origin or removed entirely if the frontend is served from the same
origin.

## Why CORS is needed
A browser enforces the same-origin policy: JavaScript running on
`localhost:5173` is not allowed to read responses from `localhost:8080`
unless that server explicitly opts in via `Access-Control-Allow-Origin`
headers. `UseCors` adds those headers before the response leaves the API.
