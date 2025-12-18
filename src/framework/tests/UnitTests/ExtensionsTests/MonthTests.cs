namespace UnitTests.ExtensionsTests
{
    public class MonthTests
    {
        [Theory]
        [InlineData(2, 2023, 28)]
        [InlineData(2, 2024, 29)]
        [InlineData(11, 2024, 30)]
        [InlineData(12, 2024, 31)]
        public void Should_Return_Correct_Month_Values(
            int monthTest,
            int yearTest,
            int totalDaysOfMonthTest)
        {
            var date = new DateTime(yearTest, monthTest, 15);

            var monthData = Month.ByDate(date);

            var firstDayOfMonth = new DateTime(yearTest, monthTest, 01);
            var lastDayOfMonth = new DateTime(yearTest, monthTest, totalDaysOfMonthTest);

            LightAssert.ShouldBe(firstDayOfMonth, monthData.FirstDay);
            LightAssert.ShouldBe(lastDayOfMonth, monthData.LastDay.Date);
            LightAssert.ShouldBe(totalDaysOfMonthTest, monthData.TotalDays);
        }
    }
}
