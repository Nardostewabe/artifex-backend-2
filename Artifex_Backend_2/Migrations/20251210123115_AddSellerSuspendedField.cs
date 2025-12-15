using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Artifex_Backend_2.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerSuspendedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "Sellers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "Sellers");
        }
    }
}
