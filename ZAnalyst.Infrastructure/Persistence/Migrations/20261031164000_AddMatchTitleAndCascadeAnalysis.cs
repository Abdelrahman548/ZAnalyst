using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZAnalyst.Infrastructure.Persistence;

#nullable disable

namespace ZAnalyst.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZAnalystDbContext))]
[Migration("20261031164000_AddMatchTitleAndCascadeAnalysis")]
public partial class AddMatchTitleAndCascadeAnalysis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Title",
            table: "Matches",
            type: "TEXT",
            nullable: false,
            defaultValue: "Match");

        RebuildAnalysisTables(migrationBuilder, ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RebuildAnalysisTables(migrationBuilder, ReferentialAction.Restrict);

        migrationBuilder.DropColumn(
            name: "Title",
            table: "Matches");
    }

    private static void RebuildAnalysisTables(
        MigrationBuilder migrationBuilder,
        ReferentialAction matchDeleteBehavior)
    {
        var matchDeleteSql = matchDeleteBehavior == ReferentialAction.Cascade ? "CASCADE" : "RESTRICT";
        migrationBuilder.Sql($"""
            CREATE TABLE "Actions_new" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Actions" PRIMARY KEY,
                "MatchId" TEXT NOT NULL,
                "ButtonId" TEXT NOT NULL,
                "PlayerId" TEXT NOT NULL,
                "Rate" INTEGER NOT NULL,
                "StartTime" TEXT NOT NULL,
                "EndTime" TEXT NOT NULL,
                "Note" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Actions_Buttons_ButtonId" FOREIGN KEY ("ButtonId")
                    REFERENCES "Buttons" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Actions_Matches_MatchId" FOREIGN KEY ("MatchId")
                    REFERENCES "Matches" ("Id") ON DELETE {matchDeleteSql},
                CONSTRAINT "FK_Actions_Players_PlayerId" FOREIGN KEY ("PlayerId")
                    REFERENCES "Players" ("Id") ON DELETE RESTRICT
            );
            INSERT INTO "Actions_new" ("Id", "MatchId", "ButtonId", "PlayerId", "Rate", "StartTime", "EndTime", "Note", "CreatedAt", "UpdatedAt")
                SELECT "Id", "MatchId", "ButtonId", "PlayerId", "Rate", "StartTime", "EndTime", "Note", "CreatedAt", "UpdatedAt"
                FROM "Actions";
            DROP TABLE "Actions";
            ALTER TABLE "Actions_new" RENAME TO "Actions";
            CREATE INDEX "IX_Actions_ButtonId" ON "Actions" ("ButtonId");
            CREATE INDEX "IX_Actions_MatchId" ON "Actions" ("MatchId");
            CREATE INDEX "IX_Actions_PlayerId" ON "Actions" ("PlayerId");

            CREATE TABLE "Events_new" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Events" PRIMARY KEY,
                "MatchId" TEXT NOT NULL,
                "ButtonId" TEXT NOT NULL,
                "StartTime" TEXT NOT NULL,
                "EndTime" TEXT NOT NULL,
                "Note" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Events_Buttons_ButtonId" FOREIGN KEY ("ButtonId")
                    REFERENCES "Buttons" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Events_Matches_MatchId" FOREIGN KEY ("MatchId")
                    REFERENCES "Matches" ("Id") ON DELETE {matchDeleteSql}
            );
            INSERT INTO "Events_new" ("Id", "MatchId", "ButtonId", "StartTime", "EndTime", "Note", "CreatedAt", "UpdatedAt")
                SELECT "Id", "MatchId", "ButtonId", "StartTime", "EndTime", "Note", "CreatedAt", "UpdatedAt"
                FROM "Events";
            DROP TABLE "Events";
            ALTER TABLE "Events_new" RENAME TO "Events";
            CREATE INDEX "IX_Events_ButtonId" ON "Events" ("ButtonId");
            CREATE INDEX "IX_Events_MatchId" ON "Events" ("MatchId");
            """);
    }
}
