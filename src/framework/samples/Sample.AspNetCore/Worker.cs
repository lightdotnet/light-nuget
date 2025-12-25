using Light.Extensions;

namespace Sample.AspNetCore
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly Scheduler _scheduler;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _scheduler = new Scheduler(1)
            {
                //StartHour = 20,
                StartTime = new TimeSpan(16, 00, 00),
                EndTime = new TimeSpan(22, 35, 00)
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Task running at {v}", DateTime.Now);

                var rd = new Random();

                var seconds = rd.Next(1, 5);

                await Task.Delay(seconds * 1000, stoppingToken);

                _logger.LogInformation("Task processed in {v} seconds", seconds);

                _logger.LogInformation("Task end at {v}", DateTime.Now);

                var nextTime = _scheduler.NextTime();

                _logger.LogWarning("Next Task will run at {v}", nextTime);

                var diffSecond = nextTime - DateTime.Now;

                await Task.Delay(diffSecond, stoppingToken);
            }
        }
    }
}
