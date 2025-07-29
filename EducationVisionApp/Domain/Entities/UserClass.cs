namespace EducationVisionApp.Domain.Entities;

public class UserClass
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
    public Class Class { get; set; }
}