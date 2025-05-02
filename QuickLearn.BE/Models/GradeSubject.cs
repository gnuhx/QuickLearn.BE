namespace QuickLearn.BE.Models;

public class GradeSubject
{
    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
    
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
} 