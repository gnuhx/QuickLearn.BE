using System;

namespace QuickLearn.BE.Models;

public class UserAnswer
{
    public int Id { get; set; }
    public int TestResultId { get; set; }
    public TestResult TestResult { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string? EssayText { get; set; }
    public bool IsCorrect { get; set; }
    
    // Navigation properties
    public ICollection<Answer> SelectedAnswers { get; set; } = new List<Answer>();
} 