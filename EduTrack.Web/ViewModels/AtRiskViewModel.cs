namespace EduTrack.Web.ViewModels;

public class StudentAtRiskItem
{
    public int StudentId { get; set; }
    public string RollNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Semester { get; set; }
    
    public decimal RunningGpa { get; set; }
    public decimal AttendancePercent { get; set; }
    
    public string RiskLevel { get; set; } = "Normal"; // High, Medium, Normal
    public string RiskType { get; set; } = "None"; // Academic, Attendance, Both, None
    public string Reason { get; set; } = string.Empty;
    
    public int EnrolledCoursesCount { get; set; }
    public int GradedCoursesCount { get; set; }
    public List<string> EnrolledCourseCodes { get; set; } = new();
}

public class AtRiskWarningPanelViewModel
{
    public int TotalStudentsEvaluated { get; set; }
    public int TotalAtRiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int NormalCount { get; set; }
    
    public int AcademicRiskCount { get; set; }
    public int AttendanceRiskCount { get; set; }
    public int BothRiskCount { get; set; }
    
    public decimal AverageAtRiskGpa { get; set; }
    public decimal AverageAtRiskAttendance { get; set; }
    
    public List<StudentAtRiskItem> AtRiskStudents { get; set; } = new();
    
    // Filter properties
    public string? SelectedDepartment { get; set; }
    public string? SelectedRiskLevel { get; set; }
    public string? SelectedRiskType { get; set; }
    public string? SearchTerm { get; set; }
    public List<string> AvailableDepartments { get; set; } = new();
    public bool IsTeacherScope { get; set; }
    public string? TeacherName { get; set; }
}
