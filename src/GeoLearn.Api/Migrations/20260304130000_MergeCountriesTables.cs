using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GeoLearn.Api.Data;

#nullable disable

namespace GeoLearn.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260304130000_MergeCountriesTables")]
public partial class MergeCountriesTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Add columns nullable first so existing rows don't violate NOT NULL
        migrationBuilder.AddColumn<double>(
            name: "lat", table: "countries", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(
            name: "lng", table: "countries", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "zoom", table: "countries", type: "integer", nullable: true);

        // 2. Upsert all 20 countries (sets coords for LK/SE already in table, inserts the rest)
        migrationBuilder.Sql("""
            INSERT INTO countries (code, name, lat, lng, zoom) VALUES
                ('AU', 'Australia',      -25.2744,  133.7751,  4),
                ('BD', 'Bangladesh',      23.6850,   90.3563,  7),
                ('BR', 'Brazil',         -14.2350,  -51.9253,  4),
                ('CA', 'Canada',          56.1304, -106.3468,  4),
                ('CN', 'China',           35.8617,  104.1954,  4),
                ('FR', 'France',          46.2276,    2.2137,  6),
                ('DE', 'Germany',         51.1657,   10.4515,  6),
                ('IN', 'India',           20.5937,   78.9629,  5),
                ('ID', 'Indonesia',       -0.7893,  113.9213,  5),
                ('JP', 'Japan',           36.2048,  138.2529,  5),
                ('LB', 'Lebanon',         33.8938,   35.5018,  8),
                ('NP', 'Nepal',           28.3949,   84.1240,  7),
                ('NG', 'Nigeria',          9.0820,    8.6753,  6),
                ('RW', 'Rwanda',          -1.9403,   29.8739,  7),
                ('ZA', 'South Africa',   -30.5595,   22.9375,  5),
                ('LK', 'Sri Lanka',        6.9271,   79.8612, 12),
                ('SE', 'Sweden',          62.0000,   15.0000,  5),
                ('AE', 'UAE',             23.4241,   53.8478,  7),
                ('GB', 'United Kingdom',  55.3781,   -3.4360,  5),
                ('US', 'United States',   37.0902,  -95.7129,  4)
            ON CONFLICT (code) DO UPDATE
                SET name = EXCLUDED.name,
                    lat  = EXCLUDED.lat,
                    lng  = EXCLUDED.lng,
                    zoom = EXCLUDED.zoom;
            """);

        // 3. Now enforce NOT NULL
        migrationBuilder.AlterColumn<double>(
            name: "lat", table: "countries", type: "double precision",
            nullable: false, oldClrType: typeof(double), oldType: "double precision", oldNullable: true);
        migrationBuilder.AlterColumn<double>(
            name: "lng", table: "countries", type: "double precision",
            nullable: false, oldClrType: typeof(double), oldType: "double precision", oldNullable: true);
        migrationBuilder.AlterColumn<int>(
            name: "zoom", table: "countries", type: "integer",
            nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);

        // 4. Drop the old shapefile-tool table
        migrationBuilder.Sql("DROP TABLE IF EXISTS focus_countries;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS focus_countries (
                id   SERIAL PRIMARY KEY,
                name TEXT             NOT NULL,
                lat  DOUBLE PRECISION NOT NULL,
                lng  DOUBLE PRECISION NOT NULL,
                zoom INTEGER          NOT NULL
            );
            INSERT INTO focus_countries (name, lat, lng, zoom)
                SELECT name, lat, lng, zoom FROM countries;
            """);

        migrationBuilder.DropColumn(name: "lat",  table: "countries");
        migrationBuilder.DropColumn(name: "lng",  table: "countries");
        migrationBuilder.DropColumn(name: "zoom", table: "countries");
    }
}
