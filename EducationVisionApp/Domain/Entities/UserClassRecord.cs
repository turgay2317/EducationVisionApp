namespace EducationVisionApp.Domain.Entities;

public class UserClassRecord
{
    public long Id { get; set; }
    public long UserClassId { get; set; }
    public long RecordId { get; set; }
    public UserClass UserClass { get; set; }
    public Record Record { get; set; }
}