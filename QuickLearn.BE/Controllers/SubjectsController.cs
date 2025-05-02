using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickLearn.BE.Data;
using QuickLearn.BE.Models;

namespace QuickLearn.BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SubjectsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects()
    {
        return await _context.Subjects.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Subject>> GetSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null)
        {
            return NotFound();
        }

        return subject;
    }

    [HttpPost]
    public async Task<ActionResult<Subject>> CreateSubject(Subject subject)
    {
        subject.CreatedDate = DateTime.UtcNow;
        subject.ModifiedDate = DateTime.UtcNow;
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubject(int id, Subject subject)
    {
        if (id != subject.Id)
        {
            return BadRequest();
        }

        var existingSubject = await _context.Subjects.FindAsync(id);
        if (existingSubject == null)
        {
            return NotFound();
        }

        existingSubject.Name = subject.Name;
        existingSubject.ModifiedDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubjectExists(id))
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
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
        {
            return NotFound();
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubjectExists(int id)
    {
        return _context.Subjects.Any(e => e.Id == id);
    }
} 