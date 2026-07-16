using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_TimestampUtc",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "RoomName",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RoomName_TimestampUtc",
                table: "Messages",
                columns: new[] { "RoomName", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_RoomName_TimestampUtc",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "RoomName",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TimestampUtc",
                table: "Messages",
                column: "TimestampUtc");
        }
    }
}
