namespace EducationVisionApp.Application.DTOs.Record;

public class StudentRecordAverageDto
{
    public class LessonAverageDto
    {
        public long LessonId { get; set; }
        public string Name { get; set; }
        public float AvgDistracted { get; set; }
        public float AvgFocused { get; set; }
        public float AvgSleepy { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public List<StudentAverageDto> Students { get; set; }
    }

    public class StudentAverageDto
    {
        public float AvgDistracted { get; set; }
        public float AvgFocused { get; set; }
        public float AvgSleepy { get; set; }
        public DateTime LastSeen { get; set; }
    }
    public LessonAverageDto Lesson { get; set; }
    public List<LessonAverageDto> History { get; set; }
}