using System;

namespace QuickLearn.BE.Models;

public class AnswerResult
{
    public int Id { get; set; }
    public int TestResultId { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    // Navigation properties
    public TestResult TestResult { get; set; } = null!;
    public Question Question { get; set; } = null!;
} 