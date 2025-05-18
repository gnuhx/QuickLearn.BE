using System;

namespace QuickLearn.BE.Models;

public class Test
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TestTag { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
} 