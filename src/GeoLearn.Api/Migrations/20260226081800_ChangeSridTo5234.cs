using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GeoLearn.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSridTo5234 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear any existing data before changing the geometry column CRS.
            // Rows inserted under EPSG:3006 (SWEREF99 TM) cannot be safely
            // re-projected here because their SRID is already set to 3006 in the column.
            // After this migration the project stores geometry in EPSG:5234 (Sri Lanka Grid 1999).
            migrationBuilder.Sql("TRUNCATE TABLE work_objects RESTART IDENTITY");

            // Drop the spatial index before changing the column type.
            // PostgreSQL requires this because GiST indexes are type-specific.
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_work_objects_geom");

            // Change the geometry column from SWEREF99 TM (3006) to Sri Lanka Grid 1999 (5234).
            // USING ST_SetSRID(geom, 5234) is required syntax; since the table is empty
            // after TRUNCATE, no rows are actually transformed.
            migrationBuilder.Sql(
                "ALTER TABLE work_objects " +
                "ALTER COLUMN geom TYPE geometry(Geometry,5234) " +
                "USING ST_SetSRID(geom, 5234)");

            // Recreate the GiST spatial index on the updated column.
            migrationBuilder.Sql(
                "CREATE INDEX idx_work_objects_geom ON work_objects USING GIST (geom)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE TABLE work_objects RESTART IDENTITY");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_work_objects_geom");
            migrationBuilder.Sql(
                "ALTER TABLE work_objects " +
                "ALTER COLUMN geom TYPE geometry(Geometry,3006) " +
                "USING ST_SetSRID(geom, 3006)");
            migrationBuilder.Sql(
                "CREATE INDEX idx_work_objects_geom ON work_objects USING GIST (geom)");
        }
    }
}
