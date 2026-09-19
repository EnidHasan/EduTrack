using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduTrack.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherEvaluationPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherEvaluationPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TeacherName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Clarity = table.Column<int>(type: "int", nullable: false),
                    Preparation = table.Column<int>(type: "int", nullable: false),
                    Fairness = table.Column<int>(type: "int", nullable: false),
                    Communication = table.Column<int>(type: "int", nullable: false),
                    Overall = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherEvaluations_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherEvaluations_TeacherEvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "TeacherEvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEvaluationPeriods_AcademicYear_Term",
                table: "TeacherEvaluationPeriods",
                columns: new[] { "AcademicYear", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEvaluations_EnrollmentId",
                table: "TeacherEvaluations",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEvaluations_PeriodId_EnrollmentId",
                table: "TeacherEvaluations",
                columns: new[] { "PeriodId", "EnrollmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherEvaluations");

            migrationBuilder.DropTable(
                name: "TeacherEvaluationPeriods");
        }
    }
}
