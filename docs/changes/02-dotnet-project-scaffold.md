# 02 — .NET 10 Web API Project Scaffold

## What
Created the `ForestLink.Api` ASP.NET Core Web API project in `src/ForestLink.Api/` and added all required NuGet packages.

## Files
- `src/ForestLink.Api/` — new project (dotnet new webapi --use-controllers)
- Removed template boilerplate: `WeatherForecastController.cs`, `WeatherForecast.cs`

## NuGet Packages Added

| Package | Version | Why |
|---------|---------|-----|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | EF Core provider for PostgreSQL |
| `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` | 10.0.0 | **Separate plugin** — adds `UseNetTopologySuite()` and PostGIS ↔ NTS geometry mapping |
| `NetTopologySuite.IO.ShapeFile` | 2.1.0 | Reads `.shp` files, yields NTS `Geometry` objects |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.3 | `dotnet ef migrations` CLI tooling (dev-time only) |

> **Note:** In Npgsql EF Core 10, NetTopologySuite support was moved to a **separate package**
> `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`. In older versions (≤9) it was
> in the base package. Always add both packages when using PostGIS geometry columns.

## Key Lesson
`UseNetTopologySuite()` is the bridge between .NET and PostGIS. Without it:
- Geometry columns come back as raw `byte[]` — unreadable
- You cannot insert NTS `Geometry` objects into PostGIS columns

## Commands
```bash
dotnet new webapi -n ForestLink.Api -o src/ForestLink.Api --use-controllers
cd src/ForestLink.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite
dotnet add package NetTopologySuite.IO.ShapeFile
dotnet add package Microsoft.EntityFrameworkCore.Design
```
