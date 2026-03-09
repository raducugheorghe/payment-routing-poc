using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentRoutingPoc.Persistence.Migrations.Write
{
    public partial class AddIdempotencyKeyColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Events",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_key",
                table: "Events",
                column: "IdempotencyKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_idempotency_key",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Events");
        }
    }
}
