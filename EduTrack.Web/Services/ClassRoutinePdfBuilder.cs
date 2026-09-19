using System.Text;
using EduTrack.Web.Models;

namespace EduTrack.Web.Services;

public static class ClassRoutinePdfBuilder
{
    private const int RowsPerPage = 30;
    private static readonly string[] Days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public static byte[] Build(IEnumerable<ClassRoutine> routines, string title)
    {
        var ordered = routines.OrderBy(r => Array.IndexOf(Days, r.DayOfWeek))
            .ThenBy(r => r.StartTime).ToList();
        var pages = ordered.Chunk(RowsPerPage).ToList();
        if (pages.Count == 0) pages.Add([]);

        var count = 3 + pages.Count * 2;
        var objects = new byte[count + 1][];
        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Bytes($"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, pages.Count).Select(i => $"{4 + i * 2} 0 R"))}] /Count {pages.Count} >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

        for (var i = 0; i < pages.Count; i++)
        {
            var pageId = 4 + i * 2;
            var contentId = pageId + 1;
            var content = PageContent(pages[i], title, i + 1, pages.Count);
            objects[pageId] = Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
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

    private static string PageContent(IReadOnlyList<ClassRoutine> rows, string title, int page, int total)
    {
        var content = new StringBuilder("BT\n/F1 10 Tf\n42 550 Td\n14 TL\n");
        AddLine(content, "EDUTRACK - CLASS ROUTINE");
        AddLine(content, title);
        AddLine(content, $"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}                         Page {page} of {total}");
        AddLine(content, "");
        AddLine(content, $"{"DAY",-11} {"TIME",-13} {"COURSE",-12} {"SECTION",-10} {"ROOM",-12} INSTRUCTOR");
        AddLine(content, new string('-', 106));
        if (rows.Count == 0) AddLine(content, "No classes scheduled.");
        foreach (var r in rows)
        {
            var time = $"{r.StartTime:hh\\:mm}-{r.EndTime:hh\\:mm}";
            var line = $"{Fit(r.DayOfWeek, 11),-11} {time,-13} {Fit(r.Course?.CourseCode, 12),-12} {Fit(r.Section, 10),-10} {Fit(r.RoomNumber, 12),-12} {Fit(r.Course?.Teacher?.FullName, 28)}";
            AddLine(content, line);
        }
        content.Append("ET");
        return content.ToString();
    }

    private static void AddLine(StringBuilder content, string line) =>
        content.Append('(').Append(Escape(line)).Append(") Tj\nT*\n");
    private static string Fit(string? value, int width)
    {
        var clean = new string((value ?? "").Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        return clean.Length <= width ? clean : clean[..(width - 3)] + "...";
    }
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);
    private static void Write(Stream stream, string value) => stream.Write(Bytes(value));
}
