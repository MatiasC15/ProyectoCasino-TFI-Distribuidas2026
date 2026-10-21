using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Casino.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261002090000_AgregaRuleta")]
public partial class AgregaRuleta : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        new ApplicationDbContextModelSnapshot().BuildSnapshot(modelBuilder);

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RouletteRounds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BettingClosesAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                WinningNumber = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_RouletteRounds", x => x.Id));

        migrationBuilder.AddColumn<Guid>(
            name: "RouletteRoundId", table: "Bets", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "IdempotencyKey", table: "Bets", type: "uuid", nullable: true);

        migrationBuilder.DropIndex(name: "IX_Bets_UserId", table: "Bets");

        migrationBuilder.CreateIndex(
            name: "IX_Bets_RouletteRoundId", table: "Bets", column: "RouletteRoundId");
        migrationBuilder.CreateIndex(
            name: "IX_Bets_UserId_IdempotencyKey", table: "Bets", columns: new[] { "UserId", "IdempotencyKey" }, unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_RouletteRounds_State_BettingClosesAtUtc", table: "RouletteRounds", columns: new[] { "State", "BettingClosesAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_RouletteRounds_OnlyOneOpen", table: "RouletteRounds", column: "State", unique: true, filter: "\"State\" = 1");
        migrationBuilder.AddForeignKey(
            name: "FK_Bets_RouletteRounds_RouletteRoundId", table: "Bets", column: "RouletteRoundId",
            principalTable: "RouletteRounds", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Bets_RouletteRounds_RouletteRoundId", table: "Bets");
        migrationBuilder.DropTable(name: "RouletteRounds");
        migrationBuilder.DropIndex(name: "IX_Bets_RouletteRoundId", table: "Bets");
        migrationBuilder.DropIndex(name: "IX_Bets_UserId_IdempotencyKey", table: "Bets");
        migrationBuilder.DropColumn(name: "RouletteRoundId", table: "Bets");
        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "Bets");
        migrationBuilder.CreateIndex(name: "IX_Bets_UserId", table: "Bets", column: "UserId");
    }
}
