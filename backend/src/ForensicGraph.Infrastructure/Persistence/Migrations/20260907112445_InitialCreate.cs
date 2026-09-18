using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForensicGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crime_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    severity = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crime_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crime_events_occurred_at",
                table: "crime_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_crime_events_severity",
                table: "crime_events",
                column: "severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crime_events");
        }
    }
}
