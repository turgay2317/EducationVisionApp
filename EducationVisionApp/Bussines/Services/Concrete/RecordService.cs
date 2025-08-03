using EducationVisionApp.Application.DTOs.Record;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        var isClassOnGoing = await _context.Lessons.AnyAsync(c =>
            c.Id == dto.LessonId &&
            c.StartTime <= now &&
            c.EndTime >= now &&
            !c.IsFinished
        );
/*
        if (!isClassOnGoing)
            throw new Exception("İlgili ders devam etmediği için veri eklenemez.");
            */

        var insertingRecords = dto.Records.Select(r => new Record
        {
            UserId = r.UserId,
            LessonId = dto.LessonId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Distracted = r.Distracted,
            Focused = r.Focused,
            Sleepy = r.Sleepy,
        });
        
        await _context.Records.AddRangeAsync(insertingRecords);

        return await _context.SaveChangesAsync() > 0;
    }

    public StudentRecordAverageDto GetAverageRecordsOfStudentsByLesson(long lessonId)
    {
        var currentLesson = _context.Lessons.Find(lessonId);

        var userLessons = _context.UserLessons
            .Include(x => x.Lesson)
            .Where(x => x.LessonId == lessonId)
            .GroupBy(x => x.LessonId)
            .ToList();

        var lessonIds = _context.Lessons
            .Include(x => x.Records)
            .Where(x => 
                x.ClassId == currentLesson.ClassId && 
                x.Id != currentLesson.Id &&
                x.EndTime <= currentLesson.StartTime)
            .Select(x => x.Id)
            .ToList();

        var classLessions = _context.UserLessons
            .Include(x => x.Lesson)
            .Where(x => lessonIds.Contains(x.LessonId));
            
            var classLessionsHistory = classLessions
            .GroupBy(x => x.LessonId)
            .ToList();

        var res = userLessons.ToList().Select(l =>
        {
            var list = l.ToList();
            return new StudentRecordAverageDto
            {
                Lesson = new StudentRecordAverageDto.LessonAverageDto()
                {
                    LessonId = currentLesson.Id,
                    Name = currentLesson.Name,
                    AvgFocused = list.Average(x => x.AvgFocused) * 100,
                    AvgDistracted = list.Average(x => x.AvgDistracted) * 100,
                    AvgSleepy = list.Average(x => x.AvgSleepy) * 100,
                    Date = currentLesson.StartTime,
                    Comment = currentLesson.Comment,
                    Students = l.Select(st => new StudentRecordAverageDto.StudentAverageDto()
                    {
                        AvgDistracted = (float)Math.Round(st.AvgDistracted * 100,2),
                        AvgFocused = (float)Math.Round(st.AvgFocused * 100, 2),
                        AvgSleepy = (float)Math.Round(st.AvgSleepy * 100, 2),
                        LastSeen = _context.Records.Where(r => r.LessonId == currentLesson.Id).OrderBy(x => x.EndDate).First().EndDate,
                    }).ToList(),
                },
                History = classLessionsHistory.ToList().Select(h =>
                {
                    var ul = h.ToList();
                    var firstEntry = ul.FirstOrDefault(); 

                    return new StudentRecordAverageDto.LessonAverageDto
                    {
                        LessonId = h.Key,
                        Name = firstEntry?.Lesson.Name,
                        AvgFocused = ul.Average(x => x.AvgFocused) * 100,
                        AvgDistracted = ul.Average(x => x.AvgDistracted) * 100,
                        AvgSleepy = ul.Average(x => x.AvgSleepy) * 100,
                        Date = firstEntry?.Lesson.StartTime ?? DateTime.MinValue,
                        Comment = classLessions.Where(l => l.LessonId == firstEntry.LessonId).FirstOrDefault().Lesson.Comment,
                        Students = l.Select(st => new StudentRecordAverageDto.StudentAverageDto()
                        {
                            AvgDistracted = (float)Math.Round(st.AvgDistracted * 100,2),
                            AvgFocused = (float)Math.Round(st.AvgFocused * 100, 2),
                            AvgSleepy = (float)Math.Round(st.AvgSleepy * 100,2),
                            LastSeen =  _context.Records.Where(r => r.LessonId == h.Key).OrderBy(x => x.EndDate).First().EndDate,

                        }).ToList(),
                    };
                }).ToList(), };
        });
        return res.FirstOrDefault();
    }
}
