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

    public async Task<bool> AddAsync(RecordCreateDto dto)
    {
        var now = DateTime.Now;
        var isClassOnGoing = _context.Classes.Any(c =>
            c.Id == dto.Id &&
            c.StartTime <= now &&
            c.EndTime >= now &&
            !c.IsFinished
        );

        if (!isClassOnGoing)
            throw new Exception("İlgili ders devam etmediği için veri eklenemez.");

        var userClassRecords = dto.Records.Select(r =>
        {
            var createRecord = _context.Records.Add(new Record
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Distracted = r.Distracted,
                Focused = r.Focused,
                Sleepy = r.Sleepy,
            });

            return new UserClassRecord
            {
                UserClassId = r.UserClassId,
                Record = createRecord.Entity
            };
        });

        await _context.UserClassRecords.AddRangeAsync(userClassRecords);
        return await _context.SaveChangesAsync() > 0;
    }
}
