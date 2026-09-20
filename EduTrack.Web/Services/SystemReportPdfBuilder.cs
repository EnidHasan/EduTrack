using System.Globalization;
using System.Text;
using EduTrack.Web.ViewModels;

namespace EduTrack.Web.Services;

/// <summary>Creates a publication-grade, beautifully formatted PDF report of the academic and risk analytics summary.</summary>
public static class SystemReportPdfBuilder
{
    private const int DepartmentRowsPerPage = 12;

    public static byte[] Build(SystemReportViewModel report)
    {
        // 1 Page Report Layout (Landscape A4: 842 x 595 pt)
        var count = 6;
        var objects = new byte[count + 1][];

        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Bytes("<< /Type /Pages /Kids [5 0 R] /Count 1 >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        objects[4] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var content = PageContent(report);
        objects[5] = Bytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 6 0 R >>");
        objects[6] = Bytes($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%EduTrack\n");

        var offsets = new long[count + 1];
        for (var id = 1; id <= count; id++)
        {
            offsets[id] = output.Position;
            Write(output, $"{id} 0 obj\n");
            output.Write(objects[id]);
            Write(output, "\nendobj\n");
        }

        var xrefOffset = output.Position;
        Write(output, $"xref\n0 {count + 1}\n0000000000 65535 f \n");
        for (var id = 1; id <= count; id++) Write(output, $"{offsets[id]:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size {count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return output.ToArray();
    }

    private static string PageContent(SystemReportViewModel r)
    {
        var sb = new StringBuilder();

        // -------------------------------------------------------------
        // 1. TOP HEADER BANNER (EduTrack Navy/Indigo Background)
        // -------------------------------------------------------------
        sb.AppendLine("0.10 0.20 0.45 rg"); // Deep Navy Blue
        sb.AppendLine("30 525 782 45 re f");

        sb.AppendLine("BT");
        sb.AppendLine("/F1 16 Tf");
        sb.AppendLine("1 1 1 rg"); // White Text
        sb.AppendLine("45 550 Td");
        sb.AppendLine($"({Escape("EDUTRACK - SYSTEM ACADEMIC AND RISK SUMMARY REPORT")}) Tj");
        sb.AppendLine("ET");

        sb.AppendLine("BT");
        sb.AppendLine("/F2 9.5 Tf");
        sb.AppendLine("0.85 0.90 1.00 rg"); // Light Cyan Subtitle
        sb.AppendLine("45 534 Td");
        sb.AppendLine($"({Escape($"Generated Date: {r.GeneratedAt:dd MMMM yyyy, hh:mm tt}  |  Official Academic Performance Snapshot")}) Tj");
        sb.AppendLine("ET");

        // -------------------------------------------------------------
        // 2. INSTITUTIONAL OVERVIEW CARDS (4 Summary Metric Cards)
        // -------------------------------------------------------------
        float cardY = 465;
        float cardWidth = 185;
        float cardHeight = 48;
        float gap = 14;
        float startX = 30;

        string[] cardTitles = ["INSTITUTION TOTALS", "SYSTEM ACADEMICS", "ACADEMIC RISK STATUS", "RISK DISTRIBUTIONS"];
        string[] cardLine1 = [
            $"Students: {r.TotalStudentsCount}  |  Teachers: {r.TotalTeachersCount}",
            $"Average GPA: {r.SystemAverageGpa:F2} / 4.00",
            $"At-Risk Students: {r.TotalAtRiskCount}",
            $"High Risk: {r.HighRiskCount}  |  Med Risk: {r.MediumRiskCount}"
        ];
        string[] cardLine2 = [
            $"Courses: {r.TotalCoursesCount}  |  Enrollments: {r.TotalEnrollmentsCount}",
            $"Avg Attendance: {r.SystemAverageAttendance:F1}%",
            $"Normal Status: {r.NormalCount}",
            $"Acad Risk: {r.AcademicRiskCount}  |  Att Risk: {r.AttendanceRiskCount}"
        ];

        for (int i = 0; i < 4; i++)
        {
            float cX = startX + i * (cardWidth + gap);

            // Card Background
            sb.AppendLine("0.94 0.96 0.99 rg");
            sb.AppendLine($"{cX} {cardY} {cardWidth} {cardHeight} re f");

            // Card Top Border Accent
            sb.AppendLine(i switch
            {
                0 => "0.15 0.45 0.85 rg", // Blue
                1 => "0.05 0.60 0.40 rg", // Green
                2 => r.TotalAtRiskCount > 0 ? "0.85 0.25 0.20 rg" : "0.05 0.60 0.40 rg", // Red or Green
                _ => "0.80 0.50 0.10 rg"  // Orange
            });
            sb.AppendLine($"{cX} {cardY + cardHeight - 3} {cardWidth} 3 re f");

            // Card Border
            sb.AppendLine("0.80 0.85 0.90 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{cX} {cardY} {cardWidth} {cardHeight} re s");

            // Card Title
            sb.AppendLine("BT");
            sb.AppendLine("/F1 8 Tf");
            sb.AppendLine("0.2 0.3 0.5 rg");
            sb.AppendLine($"{cX + 8} {cardY + 32} Td");
            sb.AppendLine($"({Escape(cardTitles[i])}) Tj");
            sb.AppendLine("ET");

            // Card Metric Line 1
            sb.AppendLine("BT");
            sb.AppendLine("/F1 8.5 Tf");
            sb.AppendLine("0.1 0.1 0.1 rg");
            sb.AppendLine($"{cX + 8} {cardY + 18} Td");
            sb.AppendLine($"({Escape(cardLine1[i])}) Tj");
            sb.AppendLine("ET");

            // Card Metric Line 2
            sb.AppendLine("BT");
            sb.AppendLine("/F2 8 Tf");
            sb.AppendLine("0.3 0.3 0.3 rg");
            sb.AppendLine($"{cX + 8} {cardY + 6} Td");
            sb.AppendLine($"({Escape(cardLine2[i])}) Tj");
            sb.AppendLine("ET");
        }

        // -------------------------------------------------------------
        // 3. DEPARTMENT PERFORMANCE TABLE
        // -------------------------------------------------------------
        sb.AppendLine("BT");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine("0.10 0.20 0.45 rg");
        sb.AppendLine("30 448 Td");
        sb.AppendLine($"({Escape("DEPARTMENT PERFORMANCE BREAKDOWN")}) Tj");
        sb.AppendLine("ET");

        float deptTableY = 440;
        float deptHeaderH = 20;
        float deptRowH = 18;
        float[] deptColWidths = [150, 75, 75, 75, 75, 95, 110, 107];
        string[] deptHeaders = ["Department", "Students", "At Risk", "High", "Medium", "Avg GPA", "Avg Attendance", "Risk Rate"];

        // Draw Department Header
        sb.AppendLine("0.88 0.92 0.97 rg");
        sb.AppendLine($"30 {deptTableY - deptHeaderH} 782 {deptHeaderH} re f");
        sb.AppendLine("0.70 0.78 0.88 RG");
        sb.AppendLine("0.75 w");
        sb.AppendLine($"30 {deptTableY - deptHeaderH} 782 {deptHeaderH} re s");

        float curX = 30;
        for (int c = 0; c < deptHeaders.Length; c++)
        {
            sb.AppendLine("BT");
            sb.AppendLine("/F1 8.5 Tf");
            sb.AppendLine("0.1 0.2 0.4 rg");
            sb.AppendLine($"{curX + 5} {deptTableY - 14} Td");
            sb.AppendLine($"({Escape(deptHeaders[c])}) Tj");
            sb.AppendLine("ET");
            curX += deptColWidths[c];
        }

        // Draw Department Rows
        for (int d = 0; d < r.DepartmentSummaries.Count; d++)
        {
            var dept = r.DepartmentSummaries[d];
            float rY = deptTableY - deptHeaderH - (d + 1) * deptRowH;

            if (d % 2 == 1)
            {
                sb.AppendLine("0.96 0.98 1.00 rg");
                sb.AppendLine($"30 {rY} 782 {deptRowH} re f");
            }

            sb.AppendLine("0.85 0.88 0.92 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"30 {rY} 782 {deptRowH} re s");

            string[] deptVals = [
                dept.DepartmentName,
                dept.TotalStudents.ToString(),
                dept.AtRiskCount.ToString(),
                dept.HighRiskCount.ToString(),
                dept.MediumRiskCount.ToString(),
                dept.AverageGpa.ToString("F2"),
                $"{dept.AverageAttendance:F1}%",
                $"{dept.AtRiskPercentage:F1}%"
            ];

            curX = 30;
            for (int c = 0; c < deptVals.Length; c++)
            {
                sb.AppendLine("BT");
                sb.AppendLine("/F2 8.5 Tf");
                sb.AppendLine("0.1 0.1 0.1 rg");
                sb.AppendLine($"{curX + 5} {rY + 5} Td");
                sb.AppendLine($"({Escape(deptVals[c])}) Tj");
                sb.AppendLine("ET");
                curX += deptColWidths[c];
            }
        }

        // Draw Department Vertical Lines
        curX = 30;
        float deptTotalH = deptHeaderH + (r.DepartmentSummaries.Count * deptRowH);
        for (int c = 0; c <= deptColWidths.Length; c++)
        {
            sb.AppendLine("0.78 0.83 0.88 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{curX} {deptTableY - deptTotalH} m {curX} {deptTableY} l s");
            if (c < deptColWidths.Length) curX += deptColWidths[c];
        }

        // -------------------------------------------------------------
        // 4. COURSE PERFORMANCE TABLE
        // -------------------------------------------------------------
        float courseSectionTopY = deptTableY - deptTotalH - 18;

        sb.AppendLine("BT");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine("0.10 0.20 0.45 rg");
        sb.AppendLine($"30 {courseSectionTopY} Td");
        sb.AppendLine($"({Escape("COURSE PERFORMANCE OVERVIEW")}) Tj");
        sb.AppendLine("ET");

        float courseTableY = courseSectionTopY - 6;
        float courseHeaderH = 20;
        float courseRowH = 17;
        float[] courseColWidths = [210, 160, 55, 65, 65, 75, 75, 77];
        string[] courseHeaders = ["Course Code & Title", "Instructor", "Credits", "Enrolled", "Graded", "Avg Mark", "Pass Rate", "At Risk"];

        // Draw Course Header
        sb.AppendLine("0.88 0.92 0.97 rg");
        sb.AppendLine($"30 {courseTableY - courseHeaderH} 782 {courseHeaderH} re f");
        sb.AppendLine("0.70 0.78 0.88 RG");
        sb.AppendLine("0.75 w");
        sb.AppendLine($"30 {courseTableY - courseHeaderH} 782 {courseHeaderH} re s");

        curX = 30;
        for (int c = 0; c < courseHeaders.Length; c++)
        {
            sb.AppendLine("BT");
            sb.AppendLine("/F1 8.5 Tf");
            sb.AppendLine("0.1 0.2 0.4 rg");
            sb.AppendLine($"{curX + 5} {courseTableY - 14} Td");
            sb.AppendLine($"({Escape(courseHeaders[c])}) Tj");
            sb.AppendLine("ET");
            curX += courseColWidths[c];
        }

        // Draw Course Rows (Display top 10 courses cleanly on page 1)
        var courseRowsToDisplay = r.CourseSummaries.Take(10).ToList();

        for (int cr = 0; cr < courseRowsToDisplay.Count; cr++)
        {
            var course = courseRowsToDisplay[cr];
            float rY = courseTableY - courseHeaderH - (cr + 1) * courseRowH;

            if (cr % 2 == 1)
            {
                sb.AppendLine("0.96 0.98 1.00 rg");
                sb.AppendLine($"30 {rY} 782 {courseRowH} re f");
            }

            sb.AppendLine("0.85 0.88 0.92 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"30 {rY} 782 {courseRowH} re s");

            string[] courseVals = [
                Fit($"{course.CourseCode} {course.CourseTitle}", 33),
                Fit(course.TeacherName, 24),
                course.CreditHours.ToString("F1"),
                course.TotalEnrolled.ToString(),
                course.GradedCount.ToString(),
                course.AverageTotalMark.ToString("F1"),
                $"{course.PassRatePercent:F1}%",
                course.AtRiskEnrolledCount.ToString()
            ];

            curX = 30;
            for (int c = 0; c < courseVals.Length; c++)
            {
                sb.AppendLine("BT");
                sb.AppendLine("/F2 8 Tf");
                sb.AppendLine("0.1 0.1 0.1 rg");
                sb.AppendLine($"{curX + 5} {rY + 5} Td");
                sb.AppendLine($"({Escape(courseVals[c])}) Tj");
                sb.AppendLine("ET");
                curX += courseColWidths[c];
            }
        }

        // Draw Course Vertical Lines
        curX = 30;
        float courseTotalH = courseHeaderH + (courseRowsToDisplay.Count * courseRowH);
        for (int c = 0; c <= courseColWidths.Length; c++)
        {
            sb.AppendLine("0.78 0.83 0.88 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{curX} {courseTableY - courseTotalH} m {curX} {courseTableY} l s");
            if (c < courseColWidths.Length) curX += courseColWidths[c];
        }

        return sb.ToString();
    }

    private static string Fit(string? value, int width)
    {
        var clean = new string((value ?? "").Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        return clean.Length <= width ? clean : clean[..(width - 3)] + "...";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Bytes(value));
}
