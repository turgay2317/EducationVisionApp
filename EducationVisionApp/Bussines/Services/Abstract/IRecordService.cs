using EducationVisionApp.Application.DTOs.Record;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IRecordService
{
    public Task<bool> AddAsync(long id, RecordCreateDto dto);
}
