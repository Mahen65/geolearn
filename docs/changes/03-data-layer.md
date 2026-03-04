# 03 — Data Layer: Model, DbContext, Migration

## What
Defined the `WorkObject` entity, configured `AppDbContext` with PostGIS geometry column and GiST index, and generated the initial EF Core migration.

## Files
- `src/ForestLink.Api/Models/WorkObject.cs` — new
- `src/ForestLink.Api/Data/AppDbContext.cs` — new
- `src/ForestLink.Api/Migrations/` — new (auto-generated)

## WorkObject Entity

```csharp
public Geometry Geom { get; set; }  // NTS Geometry — stored as geometry(Geometry,3006) in PostGIS
```

All geometry is stored in **SWEREF99 TM (EPSG:3006)** — a meter-based projected CRS.
This allows accurate area calculations directly in SQL: `ST_Area(geom) / 10000.0` → hectares.

Normalised attributes captured during shapefile import:
- `AreaHa` — mapped from `AREAL`, `area_ha`, `AREAL_HA`, etc.
- `CompartmentId` — mapped from `AVDNR`, `Avdelning`, etc.
- `SpeciesCode` — mapped from `TRAD`, `tradslag`, etc.
- `AgeYears` — mapped from `ALDER`, `Alder_ar`, etc.
- `SourceSrid` — the EPSG code from the `.prj` file at import time (for traceability)

## AppDbContext Configuration

```csharp
// Declares the postgis extension (applied via AlterDatabase in migration)
modelBuilder.HasPostgresExtension("postgis");

// Typed geometry column with SRID constraint
entity.Property(e => e.Geom)
    .HasColumnType("geometry(Geometry,3006)");

// GiST spatial index — essential for any spatial query performance
entity.HasIndex(e => e.Geom)
    .HasMethod("gist");
```

## What the Migration Creates

```sql
-- Enables PostGIS in the database
CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TABLE work_objects (
    id          SERIAL PRIMARY KEY,
    name        TEXT NOT NULL,
    geom        geometry(Geometry, 3006),   -- SWEREF99 TM
    area_ha     DOUBLE PRECISION,
    compartment_id TEXT,
    species_code   TEXT,
    age_years   INTEGER,
    source_srid INTEGER NOT NULL
);

-- GiST index — without this, ST_Intersects/ST_Within scan every row
CREATE INDEX idx_work_objects_geom ON work_objects USING GIST (geom);
```

## Commands
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update   # run after docker compose up -d
```
