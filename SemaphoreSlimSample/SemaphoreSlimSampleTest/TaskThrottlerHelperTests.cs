using Xunit.Abstractions;

namespace SemaphoreSlimSampleTest
{
    public class TaskThrottlerHelperTests
    {
        private readonly ITestOutputHelper _logger;

        public TaskThrottlerHelperTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(100)]
        [InlineData(150)]
        public async Task TaskThrottlerHelper_ExecuteWithSemaphoreAsync_DoesNotExceedMaxAllowedConcurrency(int maxAllowedConcurrency)
        {
            // Arrance 
            var sampleList = Enumerable.Range(0, 100).Select(i => i);
            var throttledTask = new TaskThrottlingHelper<int>(_logger, sampleList, i => Task.Delay(i), maxAllowedConcurrency);
            
            // Act
            await throttledTask.ExecuteWithSemaphoreAsync().ConfigureAwait(false);

            // Assert
            Assert.True (throttledTask.MaxConcurrencyAcheived <=  maxAllowedConcurrency);
        }

        [Fact]
        public async Task TaskThrottlerHelper_ExecuteWithOutSemaphoreAsync()
        {
            // Arrance 
            var sampleList = Enumerable.Range(0, 100).Select(i => i);
            var throttledTask = new TaskThrottlingHelper<int>(_logger, sampleList, i => Task.Delay(i), 1);

            // Act
            await throttledTask.ExecuteWithoutSemaphoreAsync().ConfigureAwait(false);

            // Assert
            Assert.True(throttledTask.MaxConcurrencyAcheived > 1);
        }
    }
}
