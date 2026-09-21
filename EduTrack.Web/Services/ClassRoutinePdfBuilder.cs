using System.Text;
using EduTrack.Web.Models;

namespace EduTrack.Web.Services;

public static class ClassRoutinePdfBuilder
{
    private static readonly string[] Days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday"];

    private record TimeSlot(TimeSpan Start, TimeSpan End, string Label);

    private static readonly TimeSlot[] TimeSlots = [
        new(new TimeSpan(8, 0, 0), new TimeSpan(8, 50, 0), "08:00 AM -\n08:50 AM"),
        new(new TimeSpan(8, 50, 0), new TimeSpan(9, 40, 0), "08:50 AM -\n09:40 AM"),
        new(new TimeSpan(9, 40, 0), new TimeSpan(10, 30, 0), "09:40 AM -\n10:30 AM"),
        new(new TimeSpan(10, 30, 0), new TimeSpan(11, 20, 0), "10:30 AM -\n11:20 AM"),
        new(new TimeSpan(11, 20, 0), new TimeSpan(12, 10, 0), "11:20 AM -\n12:10 PM"),
        new(new TimeSpan(12, 10, 0), new TimeSpan(13, 0, 0), "12:10 PM -\n01:00 PM"),
        new(new TimeSpan(13, 0, 0), new TimeSpan(13, 50, 0), "01:00 PM -\n01:50 PM"),
        new(new TimeSpan(13, 50, 0), new TimeSpan(14, 40, 0), "01:50 PM -\n02:40 PM"),
        new(new TimeSpan(14, 40, 0), new TimeSpan(15, 30, 0), "02:40 PM -\n03:30 PM"),
        new(new TimeSpan(15, 30, 0), new TimeSpan(16, 20, 0), "03:30 PM -\n04:20 PM"),
        new(new TimeSpan(16, 20, 0), new TimeSpan(17, 10, 0), "04:20 PM -\n05:10 PM"),
        new(new TimeSpan(17, 10, 0), new TimeSpan(18, 0, 0), "05:10 PM -\n06:00 PM")
    ];

    public static byte[] Build(IEnumerable<ClassRoutine> routines, string title)
    {
        var routineList = routines.ToList();
        var count = 5;
        var objects = new byte[count + 1][];

        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Bytes("<< /Type /Pages /Kids [5 0 R] /Count 1 >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        objects[4] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var content = PageContentGrid(routineList, title);
        objects[5] = Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 6 0 R >>");

        var contentObj = Bytes($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%EduTrack\n");
        
        // 6 objects total: 1..5 plus content stream object 6
        var offsets = new long[7];
        for (var id = 1; id <= 5; id++)
        {
            offsets[id] = output.Position;
            Write(output, $"{id} 0 obj\n");
            output.Write(objects[id]);
            Write(output, "\nendobj\n");
        }

        offsets[6] = output.Position;
        Write(output, "6 0 obj\n");
        output.Write(contentObj);
        Write(output, "\nendobj\n");

        var xref = output.Position;
        Write(output, $"xref\n0 7\n0000000000 65535 f \n");
        for (var id = 1; id <= 6; id++) Write(output, $"{offsets[id]:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");

        return output.ToArray();
    }

    private static string PageContentGrid(List<ClassRoutine> routines, string title)
    {
        var sb = new StringBuilder();

        // 1. Top Decorative Header Banner
        sb.AppendLine("0.06 0.54 0.38 rg"); // Emerald Green
        sb.AppendLine("30 535 782 40 re f");

        sb.AppendLine("BT");
        sb.AppendLine("/F1 15 Tf");
        sb.AppendLine("1 1 1 rg");
        sb.AppendLine("45 553 Td");
        sb.AppendLine($"({Escape("Class Routine for Semester: Fall, 2026")}) Tj");
        sb.AppendLine("ET");

        sb.AppendLine("BT");
        sb.AppendLine("/F2 10 Tf");
        sb.AppendLine("1 1 1 rg");
        sb.AppendLine("45 540 Td");
        sb.AppendLine($"({Escape(title + $"  |  Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}")}) Tj");
        sb.AppendLine("ET");

        // 2. Table Dimensions & Layout
        // Landscape A4 Page Width = 842, Height = 595
        // Left margin = 30, Total Grid Width = 782
        // Column 0 (Day/Time header) width = 74
        // 12 Time Slot Columns width = 59 each (12 * 59 = 708). Total width = 74 + 708 = 782.
        float startX = 30;
        float startY = 515;
        float dayColWidth = 74;
        float slotColWidth = 59;
        float headerHeight = 32;
        float rowHeight = 65; // Height for each day row to fit multi-line class info

        // 3. Draw Header Cell Backgrounds (Light Ice Blue/Gray Fill)
        sb.AppendLine("0.93 0.95 0.98 rg");
        sb.AppendLine($"{startX} {startY - headerHeight} 782 {headerHeight} re f");

        // Outer Header Border
        sb.AppendLine("0.70 0.76 0.85 RG");
        sb.AppendLine("1 w");
        sb.AppendLine($"{startX} {startY - headerHeight} 782 {headerHeight} re s");

        // Time / Day Corner Header Label
        sb.AppendLine("BT");
        sb.AppendLine("/F1 7 Tf");
        sb.AppendLine("0.1 0.2 0.4 rg");
        sb.AppendLine($"{startX + 6} {startY - 13} Td");
        sb.AppendLine($"({Escape("TIME ->")}) Tj");
        sb.AppendLine("ET");

        sb.AppendLine("BT");
        sb.AppendLine("/F1 7 Tf");
        sb.AppendLine("0.1 0.2 0.4 rg");
        sb.AppendLine($"{startX + 6} {startY - 25} Td");
        sb.AppendLine($"({Escape("DAY  v")}) Tj");
        sb.AppendLine("ET");

        // Time Slot Column Headers (08:00 AM - 08:50 AM, etc.)
        for (int t = 0; t < TimeSlots.Length; t++)
        {
            float colX = startX + dayColWidth + (t * slotColWidth);
            var slot = TimeSlots[t];
            var parts = slot.Label.Split('\n');

            sb.AppendLine("BT");
            sb.AppendLine("/F1 6.5 Tf");
            sb.AppendLine("0.1 0.2 0.4 rg");
            sb.AppendLine($"{colX + 3} {startY - 14} Td");
            sb.AppendLine($"({Escape(parts[0])}) Tj");
            sb.AppendLine("ET");

            if (parts.Length > 1)
            {
                sb.AppendLine("BT");
                sb.AppendLine("/F1 6.5 Tf");
                sb.AppendLine("0.1 0.2 0.4 rg");
                sb.AppendLine($"{colX + 3} {startY - 24} Td");
                sb.AppendLine($"({Escape(parts[1])}) Tj");
                sb.AppendLine("ET");
            }
        }

        // 4. Draw 5 Day Rows (Sunday to Thursday)
        float currentY = startY - headerHeight;

        for (int d = 0; d < Days.Length; d++)
        {
            var day = Days[d];
            float rowTopY = currentY - (d + 1) * rowHeight;

            // Row Outer Outline
            sb.AppendLine("0.82 0.86 0.90 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{startX} {rowTopY} 782 {rowHeight} re s");

            // Day Label Background Fill (Leftmost column)
            sb.AppendLine("0.95 0.97 0.99 rg");
            sb.AppendLine($"{startX} {rowTopY} {dayColWidth} {rowHeight} re f");

            // Day Title Text
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("0.1 0.2 0.4 rg");
            sb.AppendLine($"{startX + 8} {rowTopY + (rowHeight / 2) - 3} Td");
            sb.AppendLine($"({Escape(day)}) Tj");
            sb.AppendLine("ET");

            // Draw Empty Grid Cells for all 12 time slots in this day row
            for (int t = 0; t < TimeSlots.Length; t++)
            {
                float colX = startX + dayColWidth + (t * slotColWidth);
                sb.AppendLine("0.85 0.88 0.92 RG");
                sb.AppendLine("0.5 w");
                sb.AppendLine($"{colX} {rowTopY} {slotColWidth} {rowHeight} re s");
            }

            // Find and render classes scheduled for this day
            var classesForDay = routines.Where(m => m.DayOfWeek == day).ToList();

            for (int t = 0; t < TimeSlots.Length; t++)
            {
                var slot = TimeSlots[t];
                var matchedClass = classesForDay.FirstOrDefault(c => c.StartTime == slot.Start);

                if (matchedClass != null)
                {
                    // Calculate colspan for duration (e.g. 1.5 hr or 3 hr labs spanning multiple 50-min slots)
                    int colSpan = 1;
                    var currentEnd = matchedClass.EndTime;
                    while (t + colSpan < TimeSlots.Length && TimeSlots[t + colSpan].Start < currentEnd)
                    {
                        colSpan++;
                    }

                    float cellX = startX + dayColWidth + (t * slotColWidth);
                    float cellWidth = colSpan * slotColWidth;

                    bool isLab = matchedClass.ClassType == "Lab";

                    // Class Block Card Background Fill (Soft Peach for Lab, Soft Blue for Theory)
                    if (isLab) sb.AppendLine("1.00 0.94 0.88 rg");
                    else sb.AppendLine("0.88 0.94 1.00 rg");
                    sb.AppendLine($"{cellX + 1} {rowTopY + 1} {cellWidth - 2} {rowHeight - 2} re f");

                    // Bottom Accent Line (Orange for Lab, Blue for Theory)
                    if (isLab) sb.AppendLine("0.95 0.45 0.05 rg");
                    else sb.AppendLine("0.10 0.35 0.85 rg");
                    sb.AppendLine($"{cellX + 1} {rowTopY + 1} {cellWidth - 2} 3 re f");

                    // Class Card Border Outline
                    if (isLab) sb.AppendLine("0.95 0.65 0.35 RG");
                    else sb.AppendLine("0.60 0.75 0.95 RG");
                    sb.AppendLine("0.75 w");
                    sb.AppendLine($"{cellX + 1} {rowTopY + 1} {cellWidth - 2} {rowHeight - 2} re s");

                    // Render Class Content Inside Card
                    string secPrefix = isLab ? "Grp " : "Sec ";
                    string courseCodeSec = (isLab ? "[LAB] " : "") + $"{matchedClass.Course?.CourseCode} ({secPrefix}{matchedClass.Section})";
                    string roomStr = matchedClass.RoomNumber ?? "";
                    string teacherStr = Fit(matchedClass.Course?.Teacher?.FullName, 22);

                    // Line 1: Course & Section (Bold 8pt)
                    sb.AppendLine("BT");
                    sb.AppendLine("/F1 8 Tf");
                    if (isLab) sb.AppendLine("0.70 0.25 0.00 rg");
                    else sb.AppendLine("0.05 0.20 0.60 rg");
                    sb.AppendLine($"{cellX + 4} {rowTopY + rowHeight - 14} Td");
                    sb.AppendLine($"({Escape(Fit(courseCodeSec, 35))}) Tj");
                    sb.AppendLine("ET");


                    // Line 2: Room Number (Bold 8pt)
                    if (!string.IsNullOrWhiteSpace(roomStr))
                    {
                        sb.AppendLine("BT");
                        sb.AppendLine("/F1 8 Tf");
                        sb.AppendLine("0.1 0.1 0.1 rg");
                        sb.AppendLine($"{cellX + 4} {rowTopY + rowHeight - 28} Td");
                        sb.AppendLine($"({Escape(Fit(roomStr, 24))}) Tj");
                        sb.AppendLine("ET");
                    }

                    // Line 3: Instructor Name (Regular 7pt)
                    if (!string.IsNullOrWhiteSpace(teacherStr))
                    {
                        sb.AppendLine("BT");
                        sb.AppendLine("/F2 7 Tf");
                        sb.AppendLine("0.3 0.3 0.3 rg");
                        sb.AppendLine($"{cellX + 4} {rowTopY + rowHeight - 42} Td");
                        sb.AppendLine($"({Escape($"({teacherStr})")}) Tj");
                        sb.AppendLine("ET");
                    }

                    t += (colSpan - 1); // Skip spanned slots
                }
            }
        }

        // 5. Draw Complete Grid Vertical Dividers
        float gridStartX = startX;
        for (int c = 0; c <= TimeSlots.Length + 1; c++)
        {
            float totalGridHeight = headerHeight + (Days.Length * rowHeight);
            sb.AppendLine("0.78 0.82 0.88 RG");
            sb.AppendLine("0.5 w");
            sb.AppendLine($"{gridStartX} {startY - totalGridHeight} m {gridStartX} {startY} l s");

            if (c == 0) gridStartX += dayColWidth;
            else gridStartX += slotColWidth;
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
