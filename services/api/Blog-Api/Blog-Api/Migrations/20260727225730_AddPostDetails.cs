using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPostDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "author_id",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "posts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reading_time",
                table: "posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "post_tag",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tags_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_tag", x => new { x.post_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_post_tag_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_tag_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_posts_author_id",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_tag_tags_id",
                table: "post_tag",
                column: "tags_id");

            migrationBuilder.AddForeignKey(
                name: "fk_posts_users_author_id",
                table: "posts",
                column: "author_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_users_author_id",
                table: "posts");

            migrationBuilder.DropTable(
                name: "post_tag");

            migrationBuilder.DropIndex(
                name: "ix_posts_author_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "author_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "description",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "reading_time",
                table: "posts");
        }
    }
}
