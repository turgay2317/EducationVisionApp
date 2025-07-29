using EducationVisionApp.Application.DTOs.Record;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Services.Concrete;

public class RecordService : IRecordService
{
    private readonly EducationDbContext _context;

    public RecordService(EducationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(long id, RecordCreateDto dto)
    {
        var userClassRecords = dto.Records.Select(r =>
        {
            var createRecord = _context.Records.Add(new Record
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Adhd = r.Adhd,
                Hyperactivity = r.Hyperactivity,
                Sadness = r.Sadness,
                Stress = r.Stress,
            });

            return new UserClassRecord
            {
                UserClassId = id,
                Record = createRecord.Entity
            };
        });

        await _context.UserClassRecords.AddRangeAsync(userClassRecords);
        return await _context.SaveChangesAsync() > 0;
    }
}
