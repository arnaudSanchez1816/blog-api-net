using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPostTitleTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This trigram index helps speed up the title search
            // https://www.postgresql.org/docs/current/pgtrgm.html#PGTRGM-TEXT-SEARCH
            // We use gin instead of gist because we don't care about closest matching
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql(
                "CREATE INDEX ix_posts_title_trgm ON posts USING gin (title gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_posts_title_trgm;");
        }
    }
}
