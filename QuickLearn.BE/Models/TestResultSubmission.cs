using System;
using System.Collections.Generic;

namespace QuickLearn.BE.Models;

public class TestResultSubmission
{
    public string UserId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string SubjectTag { get; set; } = string.Empty;
    public string GradeTag { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public List<AnswerSubmission> Answers { get; set; } = new List<AnswerSubmission>();
    public DateTime CompletedAt { get; set; }
}

public class AnswerSubmission
{
    public string QuestionTag { get; set; } = string.Empty;
    public object UserAnswer { get; set; } = null!; // Can be string for single choice or string[] for multiple choice
    public bool IsCorrect { get; set; }
} 