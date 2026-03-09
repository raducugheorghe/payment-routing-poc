using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentRoutingPoc.Persistence.Migrations.Write
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EventStreamId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AggregateVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsCommitted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EventStreamId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AggregateVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    AggregateData = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_event_type",
                table: "Events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "idx_global_version",
                table: "Events",
                column: "GlobalVersion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recorded_at",
                table: "Events",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "idx_stream_id_version",
                table: "Events",
                columns: new[] { "EventStreamId", "AggregateVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_stream_aggregate",
                table: "Snapshots",
                columns: new[] { "EventStreamId", "AggregateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_stream_id",
                table: "Snapshots",
                column: "EventStreamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Snapshots");
        }
    }
}
