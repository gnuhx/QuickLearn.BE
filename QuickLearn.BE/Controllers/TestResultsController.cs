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

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitTest([FromBody] TestResultSubmission submission)
    {
        try
        {
            // Check if a Subject with the given SubjectTag exists
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Name == submission.SubjectTag);
            if (subject == null)
            {
                subject = new Subject
                {
                    Name = submission.SubjectTag,
                    // Add other required properties if needed
                };
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
            }

            // Check if a Grade with the given GradeTag exists
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Name == submission.GradeTag);
            if (grade == null)
            {
                grade = new Grade
                {
                    Name = submission.GradeTag,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();
            }

            // Check if a Test with the given TestTag, SubjectId, and GradeId exists
            var test = await _context.Tests.FirstOrDefaultAsync(
                t => t.TestTag == submission.TestId && t.SubjectId == subject.Id && t.GradeId == grade.Id);

            if (test == null)
            {
                // Create a new Test
                test = new Test
                {
                    Name = "Test " + submission.TestId,
                    TestTag = submission.TestId,
                    SubjectId = subject.Id,
                    GradeId = grade.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Tests.Add(test);
                await _context.SaveChangesAsync();
            }

            // Create new test result
            var testResult = new TestResult
            {
                TestId = test.Id,
                UserId = submission.UserId,
                StartedAt = submission.CompletedAt.AddMinutes(-30), // Assuming 30 minutes test duration
                SubmittedAt = submission.CompletedAt,
                TotalScore = submission.Score
            };

            // Add test result to get the ID
            _context.TestResults.Add(testResult);
            await _context.SaveChangesAsync();

            // Create user answers
            var userAnswers = new List<UserAnswer>();
            foreach (var answer in submission.Answers)
            {
                // Check if a Question with the given QuestionTag exists
                var question = await _context.Questions
                    .FirstOrDefaultAsync(q => q.QuestionText == answer.QuestionTag && q.TestId == test.Id);

                if (question == null)
                {
                    // Create a new Question if it doesn't exist
                    question = new Question
                    {
                        TestId = test.Id,
                        QuestionText = answer.QuestionTag,
                        Type = answer.UserAnswer is System.Text.Json.JsonElement jsonElement &&
                               jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? QuestionType.MultipleChoice
                            : QuestionType.SingleChoice
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();
                }

                var userAnswer = new UserAnswer
                {
                    TestResultId = testResult.Id,
                    QuestionId = question.Id,
                    IsCorrect = answer.IsCorrect
                };

                if (answer.UserAnswer is string singleAnswer)
                {
                    userAnswer.EssayText = singleAnswer;
                }
                else if (answer.UserAnswer is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var selectedAnswers = jsonElement.EnumerateArray().Select(x => x.GetString()).ToList();

                    // Create Answer entities for the selected answers if they don't exist
                    var answers = new List<Answer>();
                    foreach (var answerText in selectedAnswers)
                    {
                        var existingAnswer = await _context.Answers
                            .FirstOrDefaultAsync(a => a.AnswerText == answerText && a.QuestionId == question.Id);

                        if (existingAnswer == null)
                        {
                            existingAnswer = new Answer
                            {
                                QuestionId = question.Id,
                                AnswerText = answerText,
                                IsCorrect = false // Default to false, can be updated later if needed
                            };
                            _context.Answers.Add(existingAnswer);
                            await _context.SaveChangesAsync();
                        }
                        answers.Add(existingAnswer);
                    }
                    userAnswer.SelectedAnswers = answers;
                }

                userAnswers.Add(userAnswer);
            }

            _context.UserAnswers.AddRange(userAnswers);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Test result submitted successfully",
                testResultId = testResult.Id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("by-test/{testId}")]
    public async Task<ActionResult<TestResultSubmission>> GetTestResultByTestId(string testId)
    {
        try
        {
            var test = await _context.Tests
                .Include(t => t.Subject)
                .Include(t => t.Grade)
                .FirstOrDefaultAsync(t => t.TestTag == testId);

            if (test == null)
            {
                return NotFound($"Test with tag {testId} not found");
            }

            var testResult = await _context.TestResults
                .Include(tr => tr.UserAnswers)
                    .ThenInclude(ua => ua.Question)
                .Include(tr => tr.UserAnswers)
                    .ThenInclude(ua => ua.SelectedAnswers)
                .FirstOrDefaultAsync(tr => tr.TestId == test.Id);

            if (testResult == null)
            {
                return NotFound($"No test result found for test {testId}");
            }

            var submission = new TestResultSubmission
            {
                TestId = test.TestTag,
                SubjectTag = test.Subject.Name,
                GradeTag = test.Grade.Name,
                UserId = testResult.UserId,
                Score = testResult.TotalScore,
                TotalQuestions = testResult.UserAnswers.Count,
                Percentage = (testResult.TotalScore / testResult.UserAnswers.Count) * 100,
                CompletedAt = testResult.SubmittedAt ?? testResult.StartedAt,
                Answers = testResult.UserAnswers.Select(ua => new AnswerSubmission
                {
                    QuestionTag = ua.Question.QuestionText,
                    IsCorrect = ua.IsCorrect,
                    UserAnswer = ua.SelectedAnswers.Any() 
                        ? ua.SelectedAnswers.Select(a => a.AnswerText).ToList()
                        : ua.EssayText ?? string.Empty
                }).ToList()
            };

            return Ok(submission);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("by-result/{testResultId}")]
    public async Task<ActionResult<TestResultSubmission>> GetTestResultById(int testResultId)
    {
        try
        {
            var testResult = await _context.TestResults
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.Subject)
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.Grade)
                .Include(tr => tr.UserAnswers)
                    .ThenInclude(ua => ua.Question)
                .Include(tr => tr.UserAnswers)
                    .ThenInclude(ua => ua.SelectedAnswers)
                .FirstOrDefaultAsync(tr => tr.Id == testResultId);

            if (testResult == null)
            {
                return NotFound($"Test result with ID {testResultId} not found");
            }

            var submission = new TestResultSubmission
            {
                TestId = testResult.Test.TestTag,
                SubjectTag = testResult.Test.Subject.Name,
                GradeTag = testResult.Test.Grade.Name,
                UserId = testResult.UserId,
                Score = testResult.TotalScore,
                TotalQuestions = testResult.UserAnswers.Count,
                Percentage = (testResult.TotalScore / testResult.UserAnswers.Count) * 100,
                CompletedAt = testResult.SubmittedAt ?? testResult.StartedAt,
                Answers = testResult.UserAnswers.Select(ua => new AnswerSubmission
                {
                    QuestionTag = ua.Question.QuestionText,
                    IsCorrect = ua.IsCorrect,
                    UserAnswer = ua.SelectedAnswers.Any() 
                        ? ua.SelectedAnswers.Select(a => a.AnswerText).ToList()
                        : ua.EssayText ?? string.Empty
                }).ToList()
            };

            return Ok(submission);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    public class TestResultSummary
    {
        public int TestResultId { get; set; }
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string SubjectName { get; set; }
        public string GradeName { get; set; }
        public decimal TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    [HttpGet("by-user/{userId}/summary")]
    public async Task<ActionResult<PaginatedResponse<TestResultSummary>>> GetTestResultsSummaryByUserId(
        string userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        try
        {
            // Ensure page number is at least 1
            pageNumber = Math.Max(1, pageNumber);
            
            // Get total count for pagination
            var totalCount = await _context.TestResults
                .Where(tr => tr.UserId == userId)
                .CountAsync();

            var testResults = await _context.TestResults
                .Where(tr => tr.UserId == userId)
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.Subject)
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.Grade)
                .Include(tr => tr.UserAnswers)
                .OrderByDescending(tr => tr.SubmittedAt ?? tr.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(tr => new TestResultSummary
                {
                    TestResultId = tr.Id,
                    TestId = tr.TestId,
                    TestName = tr.Test.Name,
                    SubjectName = tr.Test.Subject.Name,
                    GradeName = tr.Test.Grade.Name,
                    TotalScore = tr.TotalScore,
                    TotalQuestions = tr.UserAnswers.Count,
                    StartedAt = tr.StartedAt,
                    SubmittedAt = tr.SubmittedAt
                })
                .ToListAsync();

            if (!testResults.Any())
            {
                return NotFound($"No test results found for user {userId}");
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new PaginatedResponse<TestResultSummary>
            {
                Items = testResults,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    public enum TimePeriod
    {
        Week,
        Month,
        ThreeMonths,
        SixMonths,
        TwelveMonths
    }

    public class TestResultChartData
    {
        public string Date { get; set; }
        public decimal Score { get; set; }
        public string TestName { get; set; }
        public int TotalQuestions { get; set; }
    }

    [HttpGet("chart-data/{userId}")]
    public async Task<ActionResult<IEnumerable<TestResultChartData>>> GetTestResultChartData(
        string userId,
        [FromQuery] string subjectNameTag,
        [FromQuery] TimePeriod timePeriod = TimePeriod.TwelveMonths)
    {
        try
        {
            var startDate = DateTime.UtcNow;
            switch (timePeriod)
            {
                case TimePeriod.Week:
                    startDate = startDate.AddDays(-7);
                    break;
                case TimePeriod.Month:
                    startDate = startDate.AddMonths(-1);
                    break;
                case TimePeriod.ThreeMonths:
                    startDate = startDate.AddMonths(-3);
                    break;
                case TimePeriod.SixMonths:
                    startDate = startDate.AddMonths(-6);
                    break;
                case TimePeriod.TwelveMonths:
                    startDate = startDate.AddMonths(-12);
                    break;
            }

            var testResults = await _context.TestResults
                .Where(tr => tr.UserId == userId && 
                            tr.SubmittedAt >= startDate &&
                            tr.Test.Subject.Name == subjectNameTag)
                .Include(tr => tr.Test)
                .Include(tr => tr.UserAnswers)
                .OrderBy(tr => tr.SubmittedAt)
                .Select(tr => new TestResultChartData
                {
                    Date = tr.SubmittedAt!.Value.ToString("yyyy-MM-dd"),
                    Score = tr.TotalScore,
                    TestName = tr.Test.Name,
                    TotalQuestions = tr.UserAnswers.Count
                })
                .ToListAsync();

            if (!testResults.Any())
            {
                return NotFound($"No test results found for user {userId} in subject {subjectNameTag} for the selected time period");
            }

            return Ok(testResults);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

public class UserAnswerSubmission
{
    public int QuestionId { get; set; }
    public List<int> SelectedAnswerIds { get; set; } = new List<int>();
    public string? EssayText { get; set; }
} 