using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Casino.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008100000_AdminManagement")]
public partial class AdminManagement : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        new ApplicationDbContextModelSnapshot().BuildSnapshot(modelBuilder);

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("INSERT INTO \"Games\" (\"Id\", \"Nombre\", \"ApuestaMinima\", \"ApuestaMaxima\", \"isActive\") VALUES ('a941f3ab-6ca8-4b9c-9af5-b5f6033d1d39', 'Ruleta europea', 1, 10000, TRUE) ON CONFLICT (\"Id\") DO NOTHING");

        migrationBuilder.CreateTable(
            name: "RouletteConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                BettingDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                ChipValuesJson = table.Column<string>(type: "jsonb", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_RouletteConfigurations", x => x.Id));

        migrationBuilder.Sql("INSERT INTO \"RouletteConfigurations\" (\"Id\", \"BettingDurationSeconds\", \"ChipValuesJson\", \"UpdatedAtUtc\") VALUES (1, 30, '[10,25,50,100]'::jsonb, CURRENT_TIMESTAMP)");

        migrationBuilder.CreateTable(
            name: "AdminAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "text", nullable: false),
                DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                table.ForeignKey("FK_AdminAuditLogs_Users_ActorUserId", x => x.ActorUserId,
                    principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AdminAuditLogs_Users_TargetUserId", x => x.TargetUserId,
                    principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_AdminAuditLogs_ActorUserId", "AdminAuditLogs", "ActorUserId");
        migrationBuilder.CreateIndex("IX_AdminAuditLogs_TargetUserId", "AdminAuditLogs", "TargetUserId");
        migrationBuilder.CreateIndex("IX_AdminAuditLogs_CreatedAtUtc_Action", "AdminAuditLogs", new[] { "CreatedAtUtc", "Action" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AdminAuditLogs");
        migrationBuilder.DropTable("RouletteConfigurations");
    }
}
