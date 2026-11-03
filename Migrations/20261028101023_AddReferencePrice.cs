using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JajkoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddReferencePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReferencePrice",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferencePrice",
                table: "Items");
        }
    }
}
