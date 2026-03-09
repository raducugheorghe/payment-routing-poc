using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentRoutingPoc.Persistence.Migrations.Read
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MerchantPaymentStatistics",
                columns: table => new
                {
                    MerchantId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MerchantName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TotalPaymentsProcessed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SuccessfulPayments = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FailedPayments = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalVolumeProcessed = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    AverageTransactionAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    SuccessRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    LastPaymentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPaymentStatistics", x => x.MerchantId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEventLogs",
                columns: table => new
                {
                    EventLogId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    PaymentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEventLogs", x => x.EventLogId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionCheckpoints",
                columns: table => new
                {
                    ProjectionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastProcessedGlobalVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    LastCheckpointTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProjectionState = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionCheckpoints", x => x.ProjectionId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentsReadModel",
                columns: table => new
                {
                    PaymentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CardId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CardNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MerchantId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MerchantName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastEventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AggregateVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentsReadModel", x => x.PaymentId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_merchant_statistic_id",
                table: "MerchantPaymentStatistics",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "idx_event_log_occurred_at",
                table: "PaymentEventLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "idx_event_log_payment_id",
                table: "PaymentEventLogs",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "idx_event_log_payment_occurred",
                table: "PaymentEventLogs",
                columns: new[] { "PaymentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "idx_payment_amount",
                table: "PaymentsReadModel",
                column: "Amount");

            migrationBuilder.CreateIndex(
                name: "idx_payment_created_at",
                table: "PaymentsReadModel",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_payment_currency_status",
                table: "PaymentsReadModel",
                columns: new[] { "Currency", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_payment_merchant_id",
                table: "PaymentsReadModel",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_status",
                table: "PaymentsReadModel",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantPaymentStatistics");

            migrationBuilder.DropTable(
                name: "PaymentEventLogs");

            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints");

            migrationBuilder.DropTable(
                name: "PaymentsReadModel");
        }
    }
}
