using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize(Roles = "Admin")]
public class RoutineController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string semester = "1.1")
    {
        ViewBag.CurrentSemester = semester;
        var routines = await db.ClassRoutines
            .Include(r => r.Course)
            .Where(r => r.SemesterLevel == semester)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.StartTime)
            .ToListAsync();
        return View(routines);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CourseId"] = new SelectList(await db.Courses.ToListAsync(), "Id", "CourseName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseId,DayOfWeek,StartTime,EndTime,RoomNumber,Section,SemesterLevel,ClassType")] ClassRoutine routine)
    {
        if (ModelState.IsValid)
        {
            db.Add(routine);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { semester = routine.SemesterLevel });
        }
        ViewData["CourseId"] = new SelectList(await db.Courses.ToListAsync(), "Id", "CourseName", routine.CourseId);
        return View(routine);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var routine = await db.ClassRoutines.FindAsync(id);
        if (routine == null) return NotFound();

        ViewData["CourseId"] = new SelectList(await db.Courses.ToListAsync(), "Id", "CourseName", routine.CourseId);
        return View(routine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,DayOfWeek,StartTime,EndTime,RoomNumber,Section,SemesterLevel,ClassType")] ClassRoutine routine)

    {
        if (id != routine.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                db.Update(routine);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!db.ClassRoutines.Any(e => e.Id == routine.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index), new { semester = routine.SemesterLevel });
        }
        ViewData["CourseId"] = new SelectList(await db.Courses.ToListAsync(), "Id", "CourseName", routine.CourseId);
        return View(routine);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var routine = await db.ClassRoutines.Include(r => r.Course).FirstOrDefaultAsync(m => m.Id == id);
        if (routine == null) return NotFound();
        return View(routine);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var routine = await db.ClassRoutines.FindAsync(id);
        if (routine != null) db.ClassRoutines.Remove(routine);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
