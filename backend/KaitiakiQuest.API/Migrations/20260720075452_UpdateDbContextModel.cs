using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KaitiakiQuest.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbContextModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Description", "IconUrl", "IsActive", "Name", "UnlockXP" },
                values: new object[,]
                {
                    { 1, "Complete your first mission", null, true, "🌱 Green Sprout", 10 },
                    { 2, "Reach a total of 100 XP", null, true, "🌿 Eco Guardian", 100 },
                    { 3, "Reach a total of 500 XP", null, true, "♻️ Recycling Master", 500 },
                    { 4, "Reach a total of 1000 XP", null, true, "🌟 Protector", 1000 },
                    { 5, "Complete missions for 7 consecutive days", null, true, "🔥 Combo King", 500 }
                });
        }
    }
}
