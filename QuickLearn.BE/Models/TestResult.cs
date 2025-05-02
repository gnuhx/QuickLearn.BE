using System;

namespace QuickLearn.BE.Models;

public class TestResult
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public Test Test { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal TotalScore { get; set; }
    
    // Navigation properties
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
} 