using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenVersionTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "replaced_by_token_token",
                table: "refresh_tokens",
                type: "character varying(256)",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "refresh_tokens",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_replaced_by_token_token",
                table: "refresh_tokens",
                column: "replaced_by_token_token");

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_refresh_tokens_replaced_by_token_token",
                table: "refresh_tokens",
                column: "replaced_by_token_token",
                principalTable: "refresh_tokens",
                principalColumn: "token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_refresh_tokens_replaced_by_token_token",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_replaced_by_token_token",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_token_token",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "refresh_tokens");
        }
    }
}
