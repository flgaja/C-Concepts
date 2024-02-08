using Xunit.Abstractions;

namespace SemaphoreSlimSampleTest
{
    internal class TaskThrottlingHelper<T>
    {
        private readonly ITestOutputHelper _logger;
        private readonly int _maxConcurrency = 1;

        private readonly IEnumerable<T> _collectionToLoop;
        private readonly Func<T, Task> _actionToMoq;

        private int _maxConcurrencyAcheived;
        private int _numberOfParallelTasks;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="collectionToLoop">Collection to Loop through asynchronously</param>
        /// <param name="actionToMoq">Action to be performed on each item in the collection</param>
        /// <param name="maxConcurrency">Number of Tasks that can execute concurrently</param>

        public TaskThrottlingHelper(ITestOutputHelper logger, IEnumerable<T> collectionToLoop, Func<T, Task> actionToMoq, int maxConcurrency)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _collectionToLoop = collectionToLoop ?? throw new ArgumentNullException(nameof(collectionToLoop));
            _actionToMoq = actionToMoq ?? throw new ArgumentNullException(nameof(actionToMoq));
            _maxConcurrency = (maxConcurrency == 0) ? throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Value cannot be zero or less") : maxConcurrency;
        }

        public int MaxConcurrencyAcheived => _maxConcurrencyAcheived;

        public async Task ExecuteWithSemaphoreAsync()
        {
            _maxConcurrencyAcheived = 0;
            //Initialize a Semaphore Slim to control the number of tasks that can run parallely
            using (var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency))
            {
                _logger.WriteLine($"{DateTime.Now} - Initialized semaphore with capacity of {semaphore.CurrentCount} tasks");
                var tasks = _collectionToLoop.Select(async item =>
                {
                    _logger.WriteLine($"{DateTime.Now} - Processing Item {item} - available slots in semaphore {semaphore.CurrentCount}");

                    // semaphore wait will block the task until a slot opens up for this task to continue.
                    await semaphore.WaitAsync().ConfigureAwait(false);

                    // Perform desired action
                    try
                    {
                        // ++ operator & -- operator are not thread safe, Use Interlocked.Increment() instead;
                        // ++_numberOfParallelTasks; 
                        Interlocked.Increment(ref _numberOfParallelTasks);
                        _maxConcurrencyAcheived = _numberOfParallelTasks > _maxConcurrencyAcheived ? _numberOfParallelTasks : _maxConcurrencyAcheived;
                        _logger.WriteLine($"{DateTime.Now} - Item {item} - Running {_numberOfParallelTasks} in parallel");
                        await _actionToMoq(item);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _numberOfParallelTasks);
                        semaphore.Release(); // release an occupied slot for next waiting task;
                    }
                }).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                _logger.WriteLine($"{DateTime.Now} - Max concurrency acheived = {_maxConcurrencyAcheived}");

            }
        }

        public async Task ExecuteWithoutSemaphoreAsync()
        {
            _maxConcurrencyAcheived = 0;
            var tasks = _collectionToLoop.Select(async item =>
            {
                _logger.WriteLine($"{DateTime.Now} - Processing Item {item}");

                // Perform desired action
                try
                {
                    // ++ operator & -- operator are not thread safe, Use Interlocked.Increment() instead;
                    // ++_numberOfParallelTasks; 
                    Interlocked.Increment(ref _numberOfParallelTasks);
                    _maxConcurrencyAcheived = _numberOfParallelTasks > _maxConcurrencyAcheived ? _numberOfParallelTasks : _maxConcurrencyAcheived;
                    _logger.WriteLine($"{DateTime.Now} - Item {item} - Running {_numberOfParallelTasks} in parallel");
                    await _actionToMoq(item);
                }
                finally
                {
                    Interlocked.Decrement(ref _numberOfParallelTasks);
                }
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            _logger.WriteLine($"{DateTime.Now} - Max concurrency acheived = {_maxConcurrencyAcheived}");


        }
    }
}
