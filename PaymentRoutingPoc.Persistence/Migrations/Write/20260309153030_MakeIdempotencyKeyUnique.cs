using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentRoutingPoc.Persistence.Migrations.Write
{
    /// <inheritdoc />
    public partial class MakeIdempotencyKeyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_idempotency_key",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_key",
                table: "Events",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_idempotency_key",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_key",
                table: "Events",
                column: "IdempotencyKey");
        }
    }
}
