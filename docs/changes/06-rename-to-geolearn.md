# 06 — Rename ForestLink → GeoLearn

## What
Renamed all project identifiers from `ForestLink` / `forestlink` to `GeoLearn` / `geolearn`.

## Changes

| What | Before | After |
|------|--------|-------|
| Solution file | `forest-link.sln` | `GeoLearn.sln` |
| Project folder | `src/ForestLink.Api/` | `src/GeoLearn.Api/` |
| Project file | `ForestLink.Api.csproj` | `GeoLearn.Api.csproj` |
| Root namespace | `ForestLink.Api` | `GeoLearn.Api` |
| HTTP test file | `ForestLink.Api.http` | `GeoLearn.Api.http` |
| Docker DB name | `forestlink` | `geolearn` |
| Docker DB user | `forestlink` | `geolearn` |
| Docker DB password | `forestlink` | `geolearn` |
| Connection string | `Database=forestlink;...` | `Database=geolearn;...` |

## Also fixed in this change
Switched `GET /workobjects` and `GET /workobjects/{id}` from `db.Database.SqlQueryRaw<T>()`
to `db.WorkObjectRows.FromSqlRaw()` / `FromSqlInterpolated()`.

**Root cause:** EF Core 10.0.3 has a bug in `NavigationExpandingExpressionVisitor` that
throws `IndexOutOfRangeException` when `SqlQueryRaw<T>` is called with a type that is not
registered as an EF Core entity. The fix is to register `WorkObjectRow` as a **keyless entity**
(`HasNoKey()`) and use `FromSqlRaw` on `db.WorkObjectRows` instead.

### What is a keyless entity?
```csharp
modelBuilder.Entity<WorkObjectRow>(entity =>
{
    entity.HasNoKey(); // no PK, no table, never appears in migrations
    // column name mappings...
});
```
`HasNoKey()` tells EF Core: "I know about this type for query mapping, but don't create a table."
`FromSqlRaw` / `FromSqlInterpolated` then work correctly because the type is registered.

## Note on Docker volume
After changing the Docker credentials, the old volume (`postgis_data`) must be wiped —
the Postgres image only initialises credentials on first run:
```bash
docker compose down -v   # removes the volume
docker compose up -d     # reinitialises with new credentials
```
