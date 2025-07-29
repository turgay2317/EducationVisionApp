namespace EducationVisionApp.Application.DTOs.Record;

public class RecordCreateDto
{
    public class RecordSingleDto
    {
        public long UserClassId { get; set; }
        public float Stress { get; set; }
        public float Hyperactivity { get; set; }
        public float Sadness { get; set; }
        public float Adhd { get; set; }
    }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public List<RecordSingleDto> Records { get; set; }
}
