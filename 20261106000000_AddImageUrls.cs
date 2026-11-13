using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.API.Data;

namespace RaceDay.API.Migrations;

[DbContext(typeof(RaceDayContext))]
[Migration("20261106000000_AddImageUrls")]
public partial class AddImageUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "BannerImageUrl", table: "Event", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProfilePictureUrl", table: "User", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BannerImageUrl", table: "Event");
        migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "User");
    }
}
