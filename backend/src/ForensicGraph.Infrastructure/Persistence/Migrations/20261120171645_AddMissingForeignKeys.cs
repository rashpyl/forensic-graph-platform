using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForensicGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows left dangling by the missing constraints would make AddForeignKey fail.
            // The number of removed rows is reported so the cleanup is not silent.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE removed bigint;
                BEGIN
                    DELETE FROM event_links l
                    WHERE NOT EXISTS (SELECT 1 FROM crime_events e WHERE e.id = l.to_event_id);
                    GET DIAGNOSTICS removed = ROW_COUNT;
                    RAISE WARNING 'AddMissingForeignKeys: removed % orphaned row(s) from event_links', removed;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE removed bigint;
                BEGIN
                    DELETE FROM event_persons ep
                    WHERE NOT EXISTS (SELECT 1 FROM persons p WHERE p.id = ep.person_id);
                    GET DIAGNOSTICS removed = ROW_COUNT;
                    RAISE WARNING 'AddMissingForeignKeys: removed % orphaned row(s) from event_persons', removed;
                END $$;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_event_links_crime_events_to_event_id",
                table: "event_links",
                column: "to_event_id",
                principalTable: "crime_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_event_persons_persons_person_id",
                table: "event_persons",
                column: "person_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_event_links_crime_events_to_event_id",
                table: "event_links");

            migrationBuilder.DropForeignKey(
                name: "FK_event_persons_persons_person_id",
                table: "event_persons");
        }
    }
}
