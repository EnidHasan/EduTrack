using System.Text;
using EduTrack.Web.Models;

namespace EduTrack.Web.Services;

public static class ExamRoutinePdfBuilder
{
    private const int RowsPerPage = 15;

    public static byte[] Build(IEnumerable<Course> courses, string title)
    {
        var ordered = courses
            .OrderBy(c => c.FinalExamDate.HasValue ? 0 : 1)
            .ThenBy(c => c.FinalExamDate)
            .ThenBy(c => c.CourseCode)
            .ToList();

        var pages = ordered.Chunk(RowsPerPage).ToList();
        if (pages.Count == 0) pages.Add([]);

        var count = 4 + pages.Count * 2;
        var objects = new byte[count + 1][];
        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Bytes($"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{5 + i * 2} 0 R"))}] /Count {pages.Count} >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        objects[4] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        for (var i = 0; i < pages.Count; i++)
        {
            var pageId = 5 + i * 2;
            var contentId = pageId + 1;
            var content = PageContent(pages[i], title, i + 1, pages.Count);
            objects[pageId] = Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            objects[contentId] = Bytes($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

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
        var xref = output.Position;
        Write(output, $"xref\n0 {count + 1}\n0000000000 65535 f \n");
        for (var id = 1; id <= count; id++) Write(output, $"{offsets[id]:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size {count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string PageContent(IReadOnlyList<Course> rows, string title, int page, int total)
    {
        var sb = new StringBuilder();

        // 1. Top Decorative Header Banner Background
        sb.AppendLine("0.08 0.38 0.74 rg"); // EduTrack Primary Blue Fill
        sb.AppendLine("35 525 772 45 re f");

        // 2. Title & Header Text (White text on blue banner)
        sb.AppendLine("BT");
        sb.AppendLine("/F1 16 Tf");
        sb.AppendLine("1 1 1 rg"); // White color
        sb.AppendLine("50 550 Td");
        sb.AppendLine($"({Escape("EDUTRACK - OFFICIAL SEMESTER FINAL EXAM ROUTINE")}) Tj");
        sb.AppendLine("ET");

        sb.AppendLine("BT");
        sb.AppendLine("/F2 11 Tf");
        sb.AppendLine("1 1 1 rg");
        sb.AppendLine("50 533 Td");
        sb.AppendLine($"({Escape(title + $"  |  Page {page} of {total}")}) Tj");
        sb.AppendLine("ET");

        // Metadata Subtext
        sb.AppendLine("BT");
        sb.AppendLine("/F2 9 Tf");
        sb.AppendLine("0.4 0.4 0.4 rg"); // Dark Gray
        sb.AppendLine("35 510 Td");
        sb.AppendLine($"({Escape($"Generated Date: {DateTime.Now:dd MMMM yyyy, hh:mm tt}")}) Tj");
        sb.AppendLine("ET");

        // 3. Table Dimensions & Layout
        // Page width 842, Table left 35, width 772 (Right 807)
        // Columns: SL (35), Date (70), Day (90), Course Code & Title (230), Credits (60), Exam Time & Venue (177), Instructor (110)
        float startX = 35;
        float startY = 490;
        float headerHeight = 26;
        float rowHeight = 24;

        float[] colWidths = [35, 90, 85, 230, 50, 162, 120];
        string[] headers = ["SL", "Exam Date", "Day", "Course Code & Title", "Credits", "Time & Venue", "Instructor"];

        // 4. Draw Header Row Background (Light Gray/Blue Fill)
        sb.AppendLine("0.90 0.94 0.98 rg");
        sb.AppendLine($"{startX} {startY - headerHeight} 772 {headerHeight} re f");

        // Header Borders
        sb.AppendLine("0.70 0.78 0.88 RG");
        sb.AppendLine("1 w");
        sb.AppendLine($"{startX} {startY - headerHeight} 772 {headerHeight} re s");

        // Header Labels
        float currentX = startX;
        for (int c = 0; c < headers.Length; c++)
        {
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("0.1 0.2 0.4 rg"); // Dark Navy text
            sb.AppendLine($"{currentX + 6} {startY - 17} Td");
            sb.AppendLine($"({Escape(headers[c])}) Tj");
            sb.AppendLine("ET");

            currentX += colWidths[c];
        }

        // 5. Draw Table Rows
        float currentY = startY - headerHeight;
        int sl = (page - 1) * RowsPerPage + 1;

        for (int r = 0; r < rows.Count; r++)
        {
            var course = rows[r];
            float rowTopY = currentY - (r + 1) * rowHeight;

            // Zebra Striping (Alternating row backgrounds)
            if (r % 2 == 1)
            {
                sb.AppendLine("0.97 0.98 1.00 rg");
                sb.AppendLine($"{startX} {rowTopY} 772 {rowHeight} re f");
            }

            // Outer Row Border
            sb.AppendLine("0.85 0.88 0.92 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{startX} {rowTopY} 772 {rowHeight} re s");

            // Format Cell Data
            var dateStr = course.FinalExamDate.HasValue ? course.FinalExamDate.Value.ToString("dd MMM yyyy") : "Not Set";
            var dayStr = course.FinalExamDate.HasValue ? course.FinalExamDate.Value.ToString("dddd") : "TBD";
            var courseStr = $"{course.CourseCode} - {course.CourseName}";
            var creditsStr = course.CreditHours.ToString("0.0");
            var timeVenueStr = string.IsNullOrWhiteSpace(course.FinalExamTime) ? "TBA" : course.FinalExamTime;
            var instructorStr = course.Teacher?.FullName ?? "TBD";

            string[] cellValues = [
                sl.ToString(),
                dateStr,
                dayStr,
                Fit(courseStr, 35),
                creditsStr,
                Fit(timeVenueStr, 24),
                Fit(instructorStr, 18)
            ];

            // Render Text in Cells
            currentX = startX;
            for (int c = 0; c < cellValues.Length; c++)
            {
                sb.AppendLine("BT");
                sb.AppendLine("/F2 10 Tf"); // Larger Helvetica 10pt Font
                sb.AppendLine("0.1 0.1 0.1 rg"); // Crisp Dark Text
                sb.AppendLine($"{currentX + 6} {rowTopY + 7} Td");
                sb.AppendLine($"({Escape(cellValues[c])}) Tj");
                sb.AppendLine("ET");

                currentX += colWidths[c];
            }

            sl++;
        }

        // 6. Draw Grid Vertical Lines for Clean Table Look
        currentX = startX;
        for (int c = 0; c <= colWidths.Length; c++)
        {
            float totalHeight = headerHeight + (rows.Count * rowHeight);
            sb.AppendLine("0.80 0.85 0.90 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{currentX} {startY - totalHeight} m {currentX} {startY} l s");

            if (c < colWidths.Length) currentX += colWidths[c];
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
