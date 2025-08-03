using EducationVisionApp.Application.DTOs.Record;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IRecordService
{
    public Task<bool> AddAsync(RecordCreateDto dto);
    public StudentRecordAverageDto GetAverageRecordsOfStudentsByLesson(long lessonId);
    public List<ClassLessonDto> GetAll();
}
