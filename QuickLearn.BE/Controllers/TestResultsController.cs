using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickLearn.BE.Data;
using QuickLearn.BE.Models;

namespace QuickLearn.BE.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TestResultsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TestResultsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestResult>>> GetUserTestResults()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        return await _context.TestResults
            .Where(tr => tr.UserId == userId.ToString())
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Subject)
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Grade)
            .OrderByDescending(tr => tr.StartedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TestResult>> GetTestResult(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var testResult = await _context.TestResults
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Subject)
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Grade)
            .Include(tr => tr.UserAnswers)
                .ThenInclude(ua => ua.Question)
            .Include(tr => tr.UserAnswers)
                .ThenInclude(ua => ua.SelectedAnswers)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.ToString());

        if (testResult == null)
        {
            return NotFound();
        }

        return testResult;
    }

    [HttpGet("chart-data")]
    public async Task<ActionResult<object>> GetChartData()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var testResults = await _context.TestResults
            .Where(tr => tr.UserId == userId.ToString() && tr.SubmittedAt != null)
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Subject)
            .OrderBy(tr => tr.SubmittedAt)
            .ToListAsync();

        var chartData = new
        {
            labels = testResults.Select(tr => tr.Test.Subject.Name).ToList(),
            scores = testResults.Select(tr => tr.TotalScore).ToList(),
            dates = testResults.Select(tr => tr.SubmittedAt!.Value.ToString("yyyy-MM-dd")).ToList()
        };

        return chartData;
    }

    [HttpPost]
    public async Task<ActionResult<TestResult>> StartTest(int testId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var test = await _context.Tests.FindAsync(testId);
        if (test == null)
        {
            return NotFound("Test not found");
        }

        var testResult = new TestResult
        {
            TestId = testId,
            UserId = userId.ToString(),
            StartedAt = DateTime.UtcNow,
            TotalScore = 0
        };

        _context.TestResults.Add(testResult);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTestResult), new { id = testResult.Id }, testResult);
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitTest(int id, List<UserAnswerSubmission> answers)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var testResult = await _context.TestResults
            .Include(tr => tr.Test)
                .ThenInclude(t => t.Questions)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.ToString());

        if (testResult == null)
        {
            return NotFound();
        }

        if (testResult.SubmittedAt != null)
        {
            return BadRequest("Test already submitted");
        }

        decimal totalScore = 0;
        var userAnswers = new List<UserAnswer>();

        foreach (var answer in answers)
        {
            var question = testResult.Test.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var userAnswer = new UserAnswer
            {
                TestResultId = testResult.Id,
                QuestionId = answer.QuestionId,
                EssayText = answer.EssayText
            };

            if (question.Type == QuestionType.Essay)
            {
                // Essay questions need manual grading
                userAnswer.IsCorrect = false;
            }
            else
            {
                var correctAnswers = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList();
                var selectedAnswers = answer.SelectedAnswerIds;

                userAnswer.IsCorrect = correctAnswers.Count == selectedAnswers.Count &&
                                     correctAnswers.All(id => selectedAnswers.Contains(id));

                if (userAnswer.IsCorrect)
                {
                    totalScore += 1;
                }
            }

            if (answer.SelectedAnswerIds.Any())
            {
                userAnswer.SelectedAnswers = await _context.Answers
                    .Where(a => answer.SelectedAnswerIds.Contains(a.Id))
                    .ToListAsync();
            }

            userAnswers.Add(userAnswer);
        }

        testResult.UserAnswers = userAnswers;
        testResult.TotalScore = totalScore;
        testResult.SubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { totalScore });
    }
}

public class UserAnswerSubmission
{
    public int QuestionId { get; set; }
    public List<int> SelectedAnswerIds { get; set; } = new List<int>();
    public string? EssayText { get; set; }
} 