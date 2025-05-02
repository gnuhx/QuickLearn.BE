using System;

namespace QuickLearn.BE.Models;

public class Question
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public Test Test { get; set; } = null!;
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    
    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}

public enum QuestionType
{
    MultipleChoice,
    SingleChoice,
    Essay
} 