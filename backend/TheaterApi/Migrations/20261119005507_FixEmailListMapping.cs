using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheaterApi.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailListMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_list_person_PersonId1",
                table: "email_list");

            migrationBuilder.DropIndex(
                name: "IX_email_list_PersonId1",
                table: "email_list");

            migrationBuilder.DropColumn(
                name: "PersonId1",
                table: "email_list");

            migrationBuilder.AlterColumn<int>(
                name: "person_id",
                table: "email_list",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_email_list_person_person_id",
                table: "email_list",
                column: "person_id",
                principalTable: "person",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_list_person_person_id",
                table: "email_list");

            migrationBuilder.AlterColumn<int>(
                name: "person_id",
                table: "email_list",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "PersonId1",
                table: "email_list",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_email_list_PersonId1",
                table: "email_list",
                column: "PersonId1");

            migrationBuilder.AddForeignKey(
                name: "FK_email_list_person_PersonId1",
                table: "email_list",
                column: "PersonId1",
                principalTable: "person",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
