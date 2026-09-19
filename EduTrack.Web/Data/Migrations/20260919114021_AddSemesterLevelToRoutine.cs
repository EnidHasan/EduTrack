using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduTrack.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSemesterLevelToRoutine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SemesterLevel",
                table: "ClassRoutines",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SemesterLevel",
                table: "ClassRoutines");
        }
    }
}
