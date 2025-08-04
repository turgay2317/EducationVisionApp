using System.Text.Json;
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
            var userIds = _context.Records
                .Where(x => x.LessonId == lastFinishedLesson.Id)
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var userRecords = new List<UserLesson>();
            foreach (var uid in userIds)
            {
                var records = _context.Records
                    .Where(x => x.StudentId == uid && x.LessonId == lastFinishedLesson.Id)
                    .ToList();

                var ul = new UserLesson()
                {
                    StudentId = uid,
                    LessonId = lastFinishedLesson.Id,
                    AvgDistracted = records.Average(x => x.Distracted),
                    AvgFocused = records.Average(x => x.Focused),
                    AvgSleepy = records.Average(x => x.Sleepy),
                    TotalBlinkCount = records.Sum(x => x.BlinkCount),
                    TotalHeadTurn = records.Sum(x => x.HeadTurn),
                    AvgConfidence = (int)records.Average(x => x.Confidence),
                    IsProcessed = false
                };
                userRecords.Add(ul);
                //_context.UserLessons.AddAsync(ul);
            }

            var processedRecords = await _geminiClient.GetProcessedLessonDatas($"Bana sadece ve sadece json olarak cevap ver hiçbir şey yazma." +
                                                                               $"Elimde {userRecords.Count} öğrencinin " +
                                                                               $"{lastFinishedLesson.StartTime} - {lastFinishedLesson.EndTime} tarihleri arasındaki " +
                                                                               $"[0,1] aralığında ortalama dikkat dağınıklığı, ortalama odaklanma ve ortalama uykululuk " +
                                                                               $"hali parametreleri var. Bu parametrelere ek olarak bunları kayıt altına alan" +
                                                                               $"Görüntü işleme algoritmasının ortalama güven yüzdesi (confidence)," +
                                                                               $"Bu zaman aralığında toplam kaç kez göz kırptığı (blinkCount) " +
                                                                               $"ve toplam kaç kez kafasını sağa sola çevirdiği verisi var. (headTurn)" +
                                                                               $"Bu (blinkCount, headTurn, confidence) doğrultusunda senden;" +
                                                                               $"AvgDistracted, AvgFocused, AvgSleepy verilerini tekrardan  " +
                                                                               $"ve bu parametrelerle beraber yeniden hesaplamanı istiyorum." +
                                                                               $"Bu [0,1] aralığındaki parametrelere maksimum [-0.15, +0.15] aralığında etki edebilirsin." +
                                                                               $"Bu değerler minimum 0 maksimum 1 olabilir bunu unutma." +
                                                                               $"Ve sonuç olarak bana tamamen aynı formatta sana ilettiğim JSON'un " +
                                                                               $"güncellenmiş halini vermeni istiyorum. IsProcessed sütunu 1 olacak." +
                                                                               $"{JsonSerializer.Serialize(userRecords)}", new CancellationToken());
            
            _context.UserLessons.UpdateRange(processedRecords);
            
            var prompt =
                $"Bir ders sırasında odak durumu, uykusuzluk ve dikkat dağınıklığı olarak üç parametremiz var. Bu parametreler [0,1] aralığında. Sana bir sınıftaki insanların göndereceğim bu üç parmaetrelerinin ortalama verisinden sınıf hakkında yazılı bir analizde bulun max. 80-90 kelime olsun. Sakın sayısal bir değerden bahsetme sadece yazılı yorumunu yap. Eğer sana geçmiş dersin verisini gönderdiysem onunla da karşılaştırma yapabilirsin." +
                $"Ort. Dikkat dağınıklığı: {userRecords.Average(x => x.AvgDistracted)}" +
                $"Ort. Odaklanma durumu {userRecords.Average(x => x.AvgFocused)}" +
                $"Ort. Uykulu olma durumu {userRecords.Average(x => x.AvgSleepy)}";
            
            var pastUserRecords = new List<UserLesson>();

            if (previousLesson != null)
            {
                var prevlessonRecords = _context.Records
                    .Where(x => x.LessonId == previousLesson.Id)
                    .GroupBy(x => x.StudentId)
                    .ToList();

                foreach (var record in prevlessonRecords)
                {
                    var records = record.ToList();
                    var ul = new UserLesson()
                    {
                        StudentId = record.Key,
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
            
            var messageForNextLesson = await _geminiClient.GenerateContentAsync($"{prompt} verilerini analiz edip, bundan sonraki derse girecek öğretmene bazı tespitlerini ve önerilerini sunan maksimum 60 kelimelik bir blgilendiri yazı yaz. Çocukların {lastFinishedLesson.Name} dersinde nasıl performans gösterdiklerini nasıl davrandıklarını belirt. Kısa olsun kısa!", new CancellationToken() {});
            lastFinishedLesson.CommentForNextTeacher = messageForNextLesson;
            
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
