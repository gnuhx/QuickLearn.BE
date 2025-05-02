using System;

namespace QuickLearn.BE.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    
    // Navigation properties
    public ICollection<GradeSubject> GradeSubjects { get; set; } = new List<GradeSubject>();
    public ICollection<Test> Tests { get; set; } = new List<Test>();
} 