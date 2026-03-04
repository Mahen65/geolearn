using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GeoLearn.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "work_objects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    geom = table.Column<Geometry>(type: "geometry(Geometry,3006)", nullable: false),
                    area_ha = table.Column<double>(type: "double precision", nullable: true),
                    compartment_id = table.Column<string>(type: "text", nullable: true),
                    species_code = table.Column<string>(type: "text", nullable: true),
                    age_years = table.Column<int>(type: "integer", nullable: true),
                    source_srid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_objects", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_work_objects_geom",
                table: "work_objects",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_objects");
        }
    }
}
