using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoLearn.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "work_objects",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "country_configs",
                columns: table => new
                {
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    main_category_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sub_category_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_configs", x => x.country_code);
                });

            // Seed Sri Lanka configuration.
            migrationBuilder.Sql("""
                INSERT INTO country_configs (country_code, country_name, main_category_label, sub_category_label)
                VALUES ('LK', 'Sri Lanka', 'Forest Type', 'Division');
                """);

            // Back-fill existing work_objects rows — all current data is from Sri Lanka NSDI.
            migrationBuilder.Sql("UPDATE work_objects SET country_code = 'LK' WHERE country_code IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_configs");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "work_objects");
        }
    }
}
