using EducationVisionApp.Data;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Services.Jobs
{
    public class ClassMonitorJob
    {
        private readonly EducationDbContext _context;
        private readonly GeminiClient _geminiClient;

        public ClassMonitorJob(EducationDbContext context, GeminiClient geminiClient)
        {
            _context = context;
            _geminiClient = geminiClient;
        }

        public async Task CheckForFinishedLesson()
        {
            var now = DateTime.Now;
            // c.EndTime > now.AddMinutes(-1)
            var lastFinishedLesson = _context.Lessons
                .Where(c => c.EndTime <= now && !c.IsFinished)
                .OrderBy(c => c.StartTime)
                .FirstOrDefault();

            if (lastFinishedLesson == null) return;
            
            var previousLesson = _context.Lessons
                .Where(c => c.ClassId == lastFinishedLesson.ClassId && c.Id != lastFinishedLesson.Id && c.EndTime <= lastFinishedLesson.StartTime)
                .OrderByDescending(c => c.EndTime)
                .FirstOrDefault();
            
            // Dersi bitti işaretle
            lastFinishedLesson.IsFinished = true;
            var lastLessonRecords = _context.Records
                .Where(x => x.LessonId == lastFinishedLesson.Id)
                .GroupBy(x => x.UserId)
                .ToList();

            var userRecords = new List<UserLesson>();
            foreach (var lessonRecord in lastLessonRecords)
            {
                var userId = lessonRecord.Key;
                var records = lessonRecord.ToList();

                var ul = new UserLesson()
                {
                    UserId = userId,
                    LessonId = lastFinishedLesson.Id,
                    AvgDistracted = records.Average(x => x.Distracted),
                    AvgFocused = records.Average(x => x.Focused),
                    AvgSleepy = records.Average(x => x.Sleepy)
                };
                userRecords.Add(ul);
                _context.UserLessons.AddAsync(ul);
            }
            
            var prompt =
                $"Bir ders sırasında odak durumu, uykusuzluk ve dikkat dağınıklığı olarak üç parametremiz var. Bu parametreler [0,1] aralığında. Sana bir sınıftaki insanların göndereceğim bu üç parmaetrelerinin ortalama verisinden sınıf hakkında yazılı bir analizde bulun max. 150 kelime olsun. Sakın sayısal bir değerden bahsetme sadece yazılı yorumunu yap. Eğer sana geçmiş dersin verisini gönderdiysem onunla da karşılaştırma yapabilirsin." +
                $"Ort. Dikkat dağınıklığı: {userRecords.Average(x => x.AvgDistracted)}" +
                $"Ort. Odaklanma durumu {userRecords.Average(x => x.AvgFocused)}" +
                $"Ort. Uykulu olma durumu {userRecords.Average(x => x.AvgSleepy)}";
            
            var pastUserRecords = new List<UserLesson>();

            if (previousLesson != null)
            {
                var prevlessonRecords = _context.Records
                    .Where(x => x.LessonId == previousLesson.Id)
                    .GroupBy(x => x.UserId)
                    .ToList();

                foreach (var record in prevlessonRecords)
                {
                    var records = record.ToList();
                    var ul = new UserLesson()
                    {
                        UserId = record.Key,
                        LessonId = previousLesson.Id,
                        AvgDistracted = records.Average(x => x.Distracted),
                        AvgFocused = records.Average(x => x.Focused),
                        AvgSleepy = records.Average(x => x.Sleepy)
                    };
                    pastUserRecords.Add(ul);
                }

                prompt += $"Ayrıca bir önceki derse ait veriler de şöyledir;" +
                          $"Ort. Dikkat dağınıklığı: {pastUserRecords.Average(x => x.AvgDistracted)}" +
                          $"Ort. Odaklanma durumu {pastUserRecords.Average(x => x.AvgFocused)}" +
                          $"Ort. Uykulu olma durumu {pastUserRecords.Average(x => x.AvgSleepy)}";

            }

            var result = await _geminiClient.GenerateContentAsync(prompt, new CancellationToken() {});
            lastFinishedLesson.Comment = result;
            
            await _context.SaveChangesAsync();

            var nextLesson = _context.Lessons
                .Where(c => !c.IsFinished && c.StartTime >= lastFinishedLesson.EndTime)
                .OrderBy(c => c.StartTime)
                .FirstOrDefault();

            if (nextLesson != null)
            {
                // İşlemler...
                Console.WriteLine(nextLesson.Name);
            }
        }
    }
}
