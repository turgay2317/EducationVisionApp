using EducationVisionApp.Data.Context;

namespace EducationVisionApp.Bussines.Services.Jobs
{
    public class ClassMonitorJob
    {
        private readonly EducationDbContext _context;

        public ClassMonitorJob(EducationDbContext context)
        {
            _context = context;
        }

        public void CheckForFinishedClass()
        {
            var now = DateTime.Now;
            var lastFinishedClass = _context.Classes
                .Where(c => c.EndTime <= now && c.EndTime > now.AddMinutes(-1) && !c.IsFinished)
                .FirstOrDefault();

            if (lastFinishedClass == null) return;
            // Dersi bitti işaretle
            lastFinishedClass.IsFinished = true;
            var userClasses = _context.UserClasses
                 .Where(x => x.ClassId == lastFinishedClass.Id)
                 .ToList();

            var userClassIds = userClasses.Select(x => x.Id).ToList();

            // Bütün record'ları tek seferde al
            var userClassRecords = _context.UserClassRecords
                .Where(x => userClassIds.Contains(x.UserClassId))
                .ToList();

            var allRecordIds = userClassRecords.Select(x => x.RecordId).Distinct().ToList();

            // Tüm Record'ları da tek seferde al
            var allRecords = _context.Records
                .Where(r => allRecordIds.Contains(r.Id))
                .ToList();

            // Gruplayıp hesapla
            var grouped = userClassRecords.GroupBy(x => x.UserClassId);

            foreach (var group in grouped)
            {
                var records = allRecords
                    .Where(r => group.Select(g => g.RecordId).Contains(r.Id))
                    .ToList();

                int count = records.Count;
                if (count == 0) continue;

                float sleepy = records.Sum(r => r.Sleepy);
                float distracted = records.Sum(r => r.Distracted);
                float focused = records.Sum(r => r.Focused);

                sleepy /= count;
                distracted /= count;
                focused /= count;

                var userClass = userClasses.FirstOrDefault(x => x.Id == group.Key);
                if (userClass != null)
                {
                    userClass.AvgSleepy = sleepy;
                    userClass.AvgDistracted = distracted;
                    userClass.AvgFocused = focused;
                }
            }

            _context.SaveChanges();

            var nextClass = _context.Classes
                .Where(c => !c.IsFinished && c.StartTime >= lastFinishedClass.EndTime)
                .OrderBy(c => c.StartTime)
                .FirstOrDefault();

            if (nextClass != null)
            {
                // İşlemler...
                Console.WriteLine(nextClass.Name);
            }
        }
    }
}
