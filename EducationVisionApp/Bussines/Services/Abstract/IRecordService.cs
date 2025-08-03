using EducationVisionApp.Application.DTOs.Record;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IRecordService
{
    public Task<bool> AddAsync(RecordCreateDto dto);
    public StudentRecordAverageDto GetAverageRecordsOfStudentsByLesson(long lessonId);
}
