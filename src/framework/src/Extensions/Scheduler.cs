using System;

namespace Light.Extensions
{
    public class Scheduler
    {
        public Scheduler(int runEveryMins)
        {
            if (runEveryMins <= 0)
                throw new ArgumentOutOfRangeException(nameof(runEveryMins), "RunEveryMins must be greater than 0.");

            RunEveryMins = runEveryMins;
        }

        private TimeSpan _start = TimeSpan.Zero;            // 00:00
        private TimeSpan _end = new TimeSpan(23, 59, 59);   // 23:59:59

        public int RunEveryMins { get; }

        public TimeSpan StartTime
        {
            get => _start;
            set
            {
                if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
                    throw new ArgumentOutOfRangeException(nameof(StartTime), "StartTime must be within 00:00–23:59.");
                if (value >= EndTime)
                    throw new ArgumentException("StartTime must be less than EndTime.");
                _start = value;
            }
        }

        public TimeSpan EndTime
        {
            get => _end;
            set
            {
                if (value <= StartTime)
                    throw new ArgumentException("EndTime must be greater than StartTime.");
                if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
                    throw new ArgumentOutOfRangeException(nameof(EndTime), "EndTime must be within 00:00–23:59.");
                _end = value;
            }
        }

        public DateTime NextTime()
        {
            var now = DateTime.Now;
            var todayStart = now.Date + StartTime;
            var todayEnd = now.Date + EndTime;

            // Case 1: Too early → wait until today's StartTime
            if (now < todayStart)
                return todayStart;

            // Case 2: Too late → schedule tomorrow at StartTime
            if (now > todayEnd)
                return todayStart.AddDays(1);

            // Case 3: Inside window → add interval
            var next = now.AddMinutes(RunEveryMins);

            // If still inside window, return it. Otherwise, roll over to tomorrow's StartTime.
            return next <= todayEnd ? next : todayStart.AddDays(1);
        }
    }
}