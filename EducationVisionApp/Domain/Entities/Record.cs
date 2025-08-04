namespace EducationVisionApp.Domain.Entities;

public class Record
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long UserId { get; set; }

    /// <summary>
    /// Kayıt tarih aralıkları
    /// </summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    /// <summary>
    /// Duygudurum belirteçleri
    /// </summary>
    public float Distracted { get; set; }
    public float Focused { get; set; }
    public float Sleepy { get; set; }
    public int BlinkCount { get; set; }
    public int HeadTurn { get; set; }
    public int Confidence { get; set; }
}