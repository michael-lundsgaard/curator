using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Curator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            // Expression indexes aren't part of the EF model. They serve lower(...) LIKE '%word%' (ADR 0005).
            migrationBuilder.Sql(
                "CREATE INDEX ix_assets_title_trgm ON assets USING gin (lower(title) gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX ix_assets_description_trgm ON assets USING gin (lower(description) gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ix_assets_description_trgm;");
            migrationBuilder.Sql("DROP INDEX ix_assets_title_trgm;");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
