using GeoLearn.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoLearn.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkObject>    WorkObjects    => Set<WorkObject>();
    public DbSet<Country>       Countries      => Set<Country>();
    public DbSet<MainCategory>  MainCategories => Set<MainCategory>();
    public DbSet<SubCategory>   SubCategories  => Set<SubCategory>();

    /// <summary>
    /// Keyless entity — used with FromSqlRaw for PostGIS GeoJSON queries.
    /// ToView(null) tells EF Core: no backing table, never appears in migrations.
    /// </summary>
    public DbSet<WorkObjectRow> WorkObjectRows => Set<WorkObjectRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<WorkObject>(entity =>
        {
            entity.ToTable("work_objects");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.AreaHa).HasColumnName("area_ha");
            entity.Property(e => e.CompartmentId).HasColumnName("compartment_id");
            entity.Property(e => e.SpeciesCode).HasColumnName("species_code");
            entity.Property(e => e.AgeYears).HasColumnName("age_years");
            entity.Property(e => e.SourceSrid).HasColumnName("source_srid");
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);

            // Geometry column stored in Sri Lanka Grid 1999 (EPSG:5234)
            entity.Property(e => e.Geom)
                .HasColumnName("geom")
                .HasColumnType("geometry(Geometry,5234)");

            // GiST spatial index — without this every spatial query is a full table scan
            entity.HasIndex(e => e.Geom)
                .HasMethod("gist")
                .HasDatabaseName("idx_work_objects_geom");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("countries");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Lat).HasColumnName("lat");
            entity.Property(e => e.Lng).HasColumnName("lng");
            entity.Property(e => e.Zoom).HasColumnName("zoom");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("uq_countries_code");
        });

        modelBuilder.Entity<MainCategory>(entity =>
        {
            entity.ToTable("main_categories");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
            entity.Property(e => e.FieldName).HasColumnName("field_name").HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Country)
                  .WithMany(c => c.MainCategories)
                  .HasForeignKey(e => e.CountryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.ToTable("sub_categories");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MainCategoryId).HasColumnName("main_category_id");
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.MainCategory)
                  .WithMany(m => m.SubCategories)
                  .HasForeignKey(e => e.MainCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WorkObjectRow — keyless, no backing table.
        modelBuilder.Entity<WorkObjectRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.AreaHa).HasColumnName("area_ha");
            entity.Property(e => e.CompartmentId).HasColumnName("compartment_id");
            entity.Property(e => e.SpeciesCode).HasColumnName("species_code");
            entity.Property(e => e.AgeYears).HasColumnName("age_years");
            entity.Property(e => e.SourceSrid).HasColumnName("source_srid");
            entity.Property(e => e.GeoJson).HasColumnName("geojson");
            entity.Property(e => e.AreaHaLive).HasColumnName("area_ha_live");
        });
    }
}
