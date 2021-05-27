using System;

namespace UpdateStressTest.Settings
{
    public class StressTestSettings
    {
        public TimeSpan SleepBetweenTasks { get; set; } = TimeSpan.FromSeconds(0);
        public int ConcurrentTasks { get; set; } = 1;		
        public TimeSpan SlowThreshold { get; set; } = TimeSpan.FromSeconds(1);

        public override string ToString() {
            return $"{nameof(SleepBetweenTasks)}: {SleepBetweenTasks}, {nameof(ConcurrentTasks)}: {ConcurrentTasks}, {nameof(SlowThreshold)}: {SlowThreshold}";
        }
    }
}