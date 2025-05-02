namespace QuickLearn.BE.Models;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    
    // Navigation properties
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
} 