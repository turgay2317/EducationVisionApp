namespace EducationVisionApp.Domain.Entities;

public class Record
{
    public long Id { get; set; }
    /// <summary>
    /// Kayıt tarih aralıkları
    /// </summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    /// <summary>
    /// Duygudurum belirteçleri
    /// </summary>
    public float Stress { get; set; }
    public float Hyperactivity { get; set; }
    public float Sadness { get; set; }
    public float Adhd { get; set; }
}