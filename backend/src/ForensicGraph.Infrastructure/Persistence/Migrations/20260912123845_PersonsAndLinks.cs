using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForensicGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonsAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "crime_events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_links",
                columns: table => new
                {
                    from_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_links", x => new { x.from_event_id, x.to_event_id });
                    table.ForeignKey(
                        name: "FK_event_links_crime_events_from_event_id",
                        column: x => x.from_event_id,
                        principalTable: "crime_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_persons",
                columns: table => new
                {
                    crime_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_persons", x => new { x.crime_event_id, x.person_id, x.role });
                    table.ForeignKey(
                        name: "FK_event_persons_crime_events_crime_event_id",
                        column: x => x.crime_event_id,
                        principalTable: "crime_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    physical_description = table.Column<string>(type: "text", nullable: true),
                    citizenships = table.Column<List<string>>(type: "text[]", nullable: false),
                    passport_numbers = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_links_from_event_id",
                table: "event_links",
                column: "from_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_links_to_event_id",
                table: "event_links",
                column: "to_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_persons_crime_event_id",
                table: "event_persons",
                column: "crime_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_persons_person_id",
                table: "event_persons",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_persons_last_first_name",
                table: "persons",
                columns: new[] { "last_name", "first_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_links");

            migrationBuilder.DropTable(
                name: "event_persons");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropColumn(
                name: "address",
                table: "crime_events");
        }
    }
}
