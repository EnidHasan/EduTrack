using System.Globalization;
using System.Text;
using EduTrack.Web.ViewModels;

namespace EduTrack.Web.Services;

/// <summary>Creates a dependency-free, standards-compliant PDF summary of the system report.</summary>
public static class SystemReportPdfBuilder
{
    private const int LinesPerPage = 48;

    public static byte[] Build(SystemReportViewModel report)
    {
        var lines = BuildLines(report);
        var pages = lines.Chunk(LinesPerPage).ToList();
        var objectCount = 3 + (pages.Count * 2);
        var objects = new byte[objectCount + 1][];

        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        var kids = string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{4 + (i * 2)} 0 R"));
        objects[2] = Bytes($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        for (var i = 0; i < pages.Count; i++)
        {
            var pageId = 4 + (i * 2);
            var contentId = pageId + 1;
            var content = BuildPageContent(pages[i], i + 1, pages.Count);
            objects[pageId] = Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            objects[contentId] = Bytes($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%EduTrack\n");
        var offsets = new long[objectCount + 1];
        for (var id = 1; id <= objectCount; id++)
        {
            offsets[id] = output.Position;
            Write(output, $"{id} 0 obj\n");
            output.Write(objects[id]);
            Write(output, "\nendobj\n");
        }

        var xrefOffset = output.Position;
        Write(output, $"xref\n0 {objectCount + 1}\n0000000000 65535 f \n");
        for (var id = 1; id <= objectCount; id++) Write(output, $"{offsets[id]:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return output.ToArray();
    }

    private static List<string> BuildLines(SystemReportViewModel r)
    {
        var lines = new List<string>
        {
            "EDUTRACK - SYSTEM ACADEMIC AND RISK SUMMARY",
            $"Generated: {r.GeneratedAt:dd MMM yyyy, hh:mm tt}",
            "",
            "INSTITUTIONAL OVERVIEW",
            $"Active students: {r.TotalStudentsCount}    Teachers: {r.TotalTeachersCount}    Courses: {r.TotalCoursesCount}",
            $"Enrollments: {r.TotalEnrollmentsCount}    Grades recorded: {r.TotalGradesCount}",
            $"Average GPA: {r.SystemAverageGpa:F2} / 4.00    Average attendance: {r.SystemAverageAttendance:F1}%",
            "",
            "RISK SUMMARY",
            $"At-risk students: {r.TotalAtRiskCount}    High: {r.HighRiskCount}    Medium: {r.MediumRiskCount}    Normal: {r.NormalCount}",
            $"Academic risk only: {r.AcademicRiskCount}    Attendance risk only: {r.AttendanceRiskCount}    Both: {r.BothRiskCount}",
            "",
            "DEPARTMENT PERFORMANCE",
            "Department | Students | At Risk | High | Medium | Avg GPA | Avg Attendance | Risk Rate"
        };

        lines.AddRange(r.DepartmentSummaries.Select(d =>
            $"{d.DepartmentName} | {d.TotalStudents} | {d.AtRiskCount} | {d.HighRiskCount} | {d.MediumRiskCount} | {d.AverageGpa:F2} | {d.AverageAttendance:F1}% | {d.AtRiskPercentage:F1}%"));
        lines.AddRange(["", "COURSE PERFORMANCE", "Course | Instructor | Credits | Enrolled | Graded | Avg Mark | Pass Rate | At Risk"]);
        lines.AddRange(r.CourseSummaries.Select(c =>
            $"{c.CourseCode} {c.CourseTitle} | {c.TeacherName} | {c.CreditHours:F1} | {c.TotalEnrolled} | {c.GradedCount} | {c.AverageTotalMark:F1} | {c.PassRatePercent:F1}% | {c.AtRiskEnrolledCount}"));
        return lines;
    }

    private static string BuildPageContent(IEnumerable<string> lines, int page, int totalPages)
    {
        var content = new StringBuilder("BT\n/F1 10 Tf\n44 798 Td\n14 TL\n");
        foreach (var line in lines)
            content.Append('(').Append(Escape(Trim(line, 102))).Append(") Tj\nT*\n");
        content.Append("T*\n(Page ").Append(page.ToString(CultureInfo.InvariantCulture)).Append(" of ")
            .Append(totalPages.ToString(CultureInfo.InvariantCulture)).Append(") Tj\nET");
        return content.ToString();
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..(max - 3)] + "...";
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Bytes(value));
}
