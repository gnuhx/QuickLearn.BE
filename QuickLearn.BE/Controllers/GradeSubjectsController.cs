using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickLearn.BE.Data;
using QuickLearn.BE.Models;

namespace QuickLearn.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradeSubjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GradeSubjectsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("grade/{gradeId}")]
    public async Task<ActionResult<IEnumerable<Subject>>> GetSubjectsForGrade(int gradeId)
    {
        var subjects = await _context.GradeSubjects
            .Where(gs => gs.GradeId == gradeId)
            .Include(gs => gs.Subject)
            .Select(gs => gs.Subject)
            .ToListAsync();

        return subjects;
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<IEnumerable<Grade>>> GetGradesForSubject(int subjectId)
    {
        var grades = await _context.GradeSubjects
            .Where(gs => gs.SubjectId == subjectId)
            .Include(gs => gs.Grade)
            .Select(gs => gs.Grade)
            .ToListAsync();

        return grades;
    }

    [HttpPost]
    public async Task<ActionResult> MapGradeToSubject(GradeSubject gradeSubject)
    {
        var exists = await _context.GradeSubjects
            .AnyAsync(gs => gs.GradeId == gradeSubject.GradeId && gs.SubjectId == gradeSubject.SubjectId);

        if (exists)
        {
            return BadRequest("This grade-subject mapping already exists.");
        }

        _context.GradeSubjects.Add(gradeSubject);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{gradeId}/{subjectId}")]
    public async Task<ActionResult> UnmapGradeFromSubject(int gradeId, int subjectId)
    {
        var gradeSubject = await _context.GradeSubjects
            .FirstOrDefaultAsync(gs => gs.GradeId == gradeId && gs.SubjectId == subjectId);

        if (gradeSubject == null)
        {
            return NotFound();
        }

        _context.GradeSubjects.Remove(gradeSubject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<ActionResult> BulkMapGradeToSubjects([FromBody] BulkGradeSubjectMapping request)
    {
        foreach (var subjectId in request.SubjectIds)
        {
            var exists = await _context.GradeSubjects
                .AnyAsync(gs => gs.GradeId == request.GradeId && gs.SubjectId == subjectId);

            if (!exists)
            {
                _context.GradeSubjects.Add(new GradeSubject
                {
                    GradeId = request.GradeId,
                    SubjectId = subjectId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class BulkGradeSubjectMapping
{
    public int GradeId { get; set; }
    public List<int> SubjectIds { get; set; } = new List<int>();
} 