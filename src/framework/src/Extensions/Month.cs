using System;

namespace Light.Extensions
{
    public readonly struct Month
    {
        private Month(DateTime date)
        {
            // first day of month
            FirstDay = new DateTime(date.Year, date.Month, 01);
        }

        public DateTime FirstDay { get; }

        // first next month day -1 minute = last day of month at 23:59:59
        public DateTime LastDay => FirstDay.AddMonths(1).AddTicks(-1);

        public int TotalDays => LastDay.Day - FirstDay.Day + 1;

        public static Month ByDate(DateTime date)
        {
            return new Month(date);
        }
    }
}
