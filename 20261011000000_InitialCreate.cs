using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.API.Data;

namespace RaceDay.API.Migrations;

[DbContext(typeof(RaceDayContext))]
[Migration("20261011000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Role",
            columns: table => new
            {
                RoleId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Role", x => x.RoleId));

        migrationBuilder.CreateTable(
            name: "User",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                RoleId = table.Column<int>(type: "int", nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_User", x => x.UserId);
                table.ForeignKey("FK_User_Role", x => x.RoleId, "Role", "RoleId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Event",
            columns: table => new
            {
                EventId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrganiserId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Run"),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Event", x => x.EventId);
                table.ForeignKey("FK_Event_Organiser", x => x.OrganiserId, "User", "UserId", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_Event_EventType", "[EventType] IN ('Run','Walk','Cycle')");
                table.CheckConstraint("CK_Event_Status", "[Status] IN ('Draft','Published','Completed','Cancelled')");
            });

        migrationBuilder.CreateTable(
            name: "Category",
            columns: table => new
            {
                CategoryId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DistanceKm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                MaxParticipants = table.Column<int>(type: "int", nullable: true),
                EntryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Category", x => x.CategoryId);
                table.ForeignKey("FK_Category_Event", x => x.EventId, "Event", "EventId", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_Category_DistanceKm", "[DistanceKm] > 0");
                table.CheckConstraint("CK_Category_EntryFee", "[EntryFee] >= 0");
            });

        migrationBuilder.CreateTable(
            name: "Enrolment",
            columns: table => new
            {
                EnrolmentId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<int>(type: "int", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                ParticipantId = table.Column<int>(type: "int", nullable: false),
                EnrolmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Enrolment", x => x.EnrolmentId);
                table.ForeignKey("FK_Enrolment_Event", x => x.EventId, "Event", "EventId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Enrolment_Category", x => x.CategoryId, "Category", "CategoryId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Enrolment_Participant", x => x.ParticipantId, "User", "UserId", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_Enrolment_Status", "[Status] IN ('Pending','Confirmed','Cancelled','Completed')");
            });

        migrationBuilder.CreateTable(
            name: "Result",
            columns: table => new
            {
                ResultId = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EnrolmentId = table.Column<int>(type: "int", nullable: false),
                FinishTime = table.Column<TimeSpan>(type: "time", nullable: true),
                Position = table.Column<int>(type: "int", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Result", x => x.ResultId);
                table.ForeignKey("FK_Result_Enrolment", x => x.EnrolmentId, "Enrolment", "EnrolmentId", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_Result_Position", "[Position] IS NULL OR [Position] > 0");
            });

        migrationBuilder.CreateIndex("IX_Role_RoleName", "Role", "RoleName", unique: true);
        migrationBuilder.CreateIndex("IX_User_Email", "User", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_User_RoleId", "User", "RoleId");
        migrationBuilder.CreateIndex("IX_Event_OrganiserId", "Event", "OrganiserId");
        migrationBuilder.CreateIndex("IX_Category_EventId", "Category", "EventId");
        migrationBuilder.CreateIndex("IX_Enrolment_EventId_ParticipantId", "Enrolment", new[] { "EventId", "ParticipantId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Enrolment_CategoryId", "Enrolment", "CategoryId");
        migrationBuilder.CreateIndex("IX_Enrolment_ParticipantId", "Enrolment", "ParticipantId");
        migrationBuilder.CreateIndex("IX_Result_EnrolmentId", "Result", "EnrolmentId", unique: true);

        migrationBuilder.InsertData("Role", new[] { "RoleId", "RoleName" }, new object[,] { { 1, "Organiser" }, { 2, "Participant" } });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Result");
        migrationBuilder.DropTable("Enrolment");
        migrationBuilder.DropTable("Category");
        migrationBuilder.DropTable("Event");
        migrationBuilder.DropTable("User");
        migrationBuilder.DropTable("Role");
    }
}
