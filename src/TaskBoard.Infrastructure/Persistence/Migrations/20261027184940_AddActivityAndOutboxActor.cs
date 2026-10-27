using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityAndOutboxActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Detail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityEntries_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityEntries_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEntries_ActorId",
                table: "ActivityEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEntries_BoardId_OccurredAt",
                table: "ActivityEntries",
                columns: new[] { "BoardId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEntries_CardId_OccurredAt",
                table: "ActivityEntries",
                columns: new[] { "CardId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEntries");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "OutboxMessages");
        }
    }
}
