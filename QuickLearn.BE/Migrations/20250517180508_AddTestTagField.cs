using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickLearn.BE.Migrations
{
    /// <inheritdoc />
    public partial class AddTestTagField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TestTag",
                table: "Tests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestTag",
                table: "Tests");
        }
    }
}
