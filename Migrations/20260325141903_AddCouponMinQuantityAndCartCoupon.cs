using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruitables.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponMinQuantityAndCartCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinQuantity",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Carts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscount",
                table: "Carts",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinQuantity",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "CouponDiscount",
                table: "Carts");
        }
    }
}
