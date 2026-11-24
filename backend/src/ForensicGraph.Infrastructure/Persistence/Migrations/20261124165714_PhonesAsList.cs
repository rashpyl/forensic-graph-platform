using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForensicGraph.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces the comma-separated <c>phone</c> text column with a <c>phones</c> array,
    /// the same shape as <c>passport_numbers</c>. Existing values are split, trimmed and
    /// de-duplicated in their original order; <see cref="Down"/> joins them back.
    /// </summary>
    public partial class PhonesAsList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "phones",
                table: "persons",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.Sql("""
                UPDATE persons SET phones = ARRAY(
                    SELECT d.number
                    FROM (
                        SELECT DISTINCT ON (btrim(t.part)) btrim(t.part) AS number, t.pos
                        FROM unnest(string_to_array(phone, ',')) WITH ORDINALITY AS t(part, pos)
                        WHERE btrim(t.part) <> ''
                        ORDER BY btrim(t.part), t.pos
                    ) d
                    ORDER BY d.pos
                )
                WHERE phone IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "phone",
                table: "persons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "persons",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE persons SET phone = array_to_string(phones, ', ')
                WHERE cardinality(phones) > 0;
                """);

            migrationBuilder.DropColumn(
                name: "phones",
                table: "persons");
        }
    }
}
