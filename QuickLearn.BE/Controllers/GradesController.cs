using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickLearn.BE.Data;
using QuickLearn.BE.Models;

namespace QuickLearn.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GradesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Grade>>> GetGrades()
    {
        return await _context.Grades.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Grade>> GetGrade(int id)
    {
        var grade = await _context.Grades.FindAsync(id);

        if (grade == null)
        {
            return NotFound();
        }

        return grade;
    }

    [HttpPost]
    public async Task<ActionResult<Grade>> CreateGrade(Grade grade)
    {
        grade.CreatedDate = DateTime.UtcNow;
        grade.ModifiedDate = DateTime.UtcNow;
        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGrade), new { id = grade.Id }, grade);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGrade(int id, Grade grade)
    {
        if (id != grade.Id)
        {
            return BadRequest();
        }

        var existingGrade = await _context.Grades.FindAsync(id);
        if (existingGrade == null)
        {
            return NotFound();
        }

        existingGrade.Name = grade.Name;
        existingGrade.ModifiedDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!GradeExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGrade(int id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade == null)
        {
            return NotFound();
        }

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool GradeExists(int id)
    {
        return _context.Grades.Any(e => e.Id == id);
    }
} 