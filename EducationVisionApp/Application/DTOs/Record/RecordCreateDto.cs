namespace EducationVisionApp.Application.DTOs.Record;

public class RecordCreateDto
{
    public class RecordSingleDto
    {
        public long UserClassId { get; set; }
        public float Distracted { get; set; }
        public float Focused { get; set; }
        public float Sleepy { get; set; }
    }
    public long Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public List<RecordSingleDto> Records { get; set; }
}
