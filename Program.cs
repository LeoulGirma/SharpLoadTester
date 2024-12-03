using System.Diagnostics;
using System.Threading.Channels;
using System.Text.Json;
using static SharpLoadTester.Stats;
using System.CommandLine;
using System.CommandLine.Invocation;
namespace SharpLoadTester
{
    public class Options
    {
        public string Url { get; set; } = string.Empty;
        public int? RequestsPerSecond { get; set; }
        public int TotalRequests { get; set; } = 2000000;
        public int Concurrency { get; set; } = 200;
        public int? StartId { get; set; }
        public int? EndId { get; set; }
        public string Method { get; set; } = "GET";
        public string? Headers { get; set; }
        public string? Body { get; set; }
        public string? Output { get; set; }
        public int? Threshold { get; set; }
        public string LogLevel { get; set; } = "info";
    }
    public class Stats
    {
        public int Successes { get; set; }
        public int Failures { get; set; }
        public double Rps { get; set; }
        public List<double> RpsValues { get; set; } = new();
        public RpsPercentiles RpsPercentiles { get; set; } = new();
        public RpsStatistics RpsStats { get; set; } = new();
        public class RpsStatistics
        {
            public double Minimum { get; set; }
            public double Median { get; set; }
            public double Average { get; set; }
            public double StdDev { get; set; }
            public double OnePercentLow { get; set; }
            public double FivePercentLow { get; set; }
            public double TenPercentLow { get; set; }
        }
        public double ElapsedTimeSeconds { get; set; }
        public ResponseTimeStats ResponseTimes { get; set; } = new();
        public CpuUsageStats CpuUsage { get; set; } = new();
        public MemoryUsageStats MemoryUsage { get; set; } = new();
        public DataTransferStats DataTransfer { get; set; } = new();
        public ThresholdStats? ThresholdStats { get; set; }
    }
    public class RpsPercentiles
    {
        public double P50 { get; set; }
        public double P75 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
    }
    public class ResponseTimeStats
    {
        public double Average { get; set; }
        public double Median { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public double P50 { get; set; }
        public double P75 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
    }
    public class CpuUsageStats
    {
        public float Min { get; set; }
        public float Median { get; set; }
        public float Average { get; set; }
        public float Max { get; set; }
    }
    public class MemoryUsageStats
    {
        public double Min { get; set; }
        public double Average { get; set; }
        public double Max { get; set; }
    }
    public class DataTransferStats
    {
        public double SentMb { get; set; }
        public double ReceivedMb { get; set; }
    }
    public class ThresholdStats
    {
        public int ThresholdMs { get; set; }
        public int BelowThreshold { get; set; }
        public int AboveThreshold { get; set; }
        public double BelowThresholdPercentage { get; set; }
        public double AboveThresholdPercentage { get; set; }
    }
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var urlOption = new Option<string>("--url", "Target URL") { IsRequired = true };
            var rpsOption = new Option<int?>("--requests-per-second", "Number of requests per second");
            var totalRequestsOption = new Option<int>("--total-requests", () => 200, "Total number of requests");
            var concurrencyOption = new Option<int>("--concurrency", () => 20, "Number of concurrent tasks");
            var startIdOption = new Option<int?>("--start-id", "Start ID to append to the URL");
            var endIdOption = new Option<int?>("--end-id", "End ID to append to the URL");
            var methodOption = new Option<string>("--method", () => "GET", "HTTP method to use");
            var headersOption = new Option<string?>("--headers", "Custom headers in 'Key:Value' format, separated by semicolons");
            var bodyOption = new Option<string?>("--body", "Request body for POST/PUT requests");
            var outputOption = new Option<string?>("--output", "Output file to save results (JSON format)");
            var thresholdOption = new Option<int?>("--threshold", "Response time threshold in milliseconds");
            var logLevelOption = new Option<string>("--log-level", () => "info", "Log level (debug, info, warning, error)");
            var rootCommand = new RootCommand("Load Testing Tool")
        {
            urlOption,
            rpsOption,
            totalRequestsOption,
            concurrencyOption,
            startIdOption,
            endIdOption,
            methodOption,
            headersOption,
            bodyOption,
            outputOption,
            thresholdOption,
            logLevelOption
        };
            rootCommand.SetHandler(async (Options options) =>
            {
                await RunLoadTestAsync(options);
            }, new OptionsBinder(
                urlOption,
                rpsOption,
                totalRequestsOption,
                concurrencyOption,
                startIdOption,
                endIdOption,
                methodOption,
                headersOption,
                bodyOption,
                outputOption,
                thresholdOption,
                logLevelOption));
            return await rootCommand.InvokeAsync(args);
        }
        static async Task RunLoadTestAsync(Options options)
        {
            Console.WriteLine($"URL: {options.Url}");
            Console.WriteLine($"Requests Per Second: {options.RequestsPerSecond}");
            Console.WriteLine($"Total Requests: {options.TotalRequests}");
            Console.WriteLine($"Concurrency: {options.Concurrency}");
            Console.WriteLine($"Method: {options.Method}");
            Console.WriteLine($"Log Level: {options.LogLevel}");
            Logger.SetLogLevel(options.LogLevel);
            if ((options.StartId.HasValue && !options.EndId.HasValue) ||
                (!options.StartId.HasValue && options.EndId.HasValue))
            {
                Logger.LogError("Error: Both --start-id and --end-id must be specified together.");
                return;
            }
            var totalSuccessCount = 0;
            var totalFailureCount = 0;
            var totalDataSent = 0L;
            var totalDataReceived = 0L;
            var totalBelowThresholdCount = 0;
            var totalAboveThresholdCount = 0;
            var responseTimesList = new List<long>();
            var failureLog = new List<string>();
            var cpuUsageStatsList = new List<float>();
            var memoryUsageStatsList = new List<double>();
            var rpsValues = new List<double>();
            var startTime = Stopwatch.StartNew();
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Logger.LogInfo("\nInterrupt received, generating final report...");
                cancellationTokenSource.Cancel();
                eventArgs.Cancel = true; // Prevent the process from terminating immediately
            };
            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = int.MaxValue,
                EnableMultipleHttp2Connections = true,
                PooledConnectionLifetime = Timeout.InfiniteTimeSpan, // Keep connections indefinitely
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                UseCookies = false, // Disable cookies if not needed
                AllowAutoRedirect = false // Disable auto-redirects if not needed
            };
            var client = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = Timeout.InfiniteTimeSpan // Remove timeout to let CancellationToken handle it
            };
            cancellationTokenSource.Token.Register(() =>
            {
                client?.Dispose();
                handler?.Dispose();
            });
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(options.Headers))
            {
                var headerPairs = options.Headers.Split(';');
                foreach (var header in headerPairs)
                {
                    var kvp = header.Split(':', 2);
                    if (kvp.Length == 2)
                    {
                        var key = kvp[0].Trim();
                        var value = kvp[1].Trim();
                        headers[key] = value;
                    }
                }
            }
            int? startId = options.StartId;
            int? endId = options.EndId;
            Channel<Token> rateLimitChannel = null;
            if (options.RequestsPerSecond.HasValue && options.RequestsPerSecond > 0)
            {
                rateLimitChannel = Channel.CreateBounded<Token>(new BoundedChannelOptions(options.RequestsPerSecond.Value * 2)
                {
                    SingleReader = false,
                    SingleWriter = true
                });
                _ = Task.Run(async () =>
                {
                    var delayBetweenTokens = 1000.0 / options.RequestsPerSecond.Value;
                    var delay = TimeSpan.FromMilliseconds(delayBetweenTokens);
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await rateLimitChannel.Writer.WriteAsync(new Token(), cancellationTokenSource.Token);
                        await Task.Delay(delay, cancellationTokenSource.Token);
                    }
                }, cancellationTokenSource.Token);
            }
            int totalRequestsPerTask = options.TotalRequests / options.Concurrency;
            var tasks = new List<Task>();
            for (int i = 0; i < options.Concurrency; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var localResponseTimes = new List<long>();
                    var localFailureLog = new List<string>();
                    for (int j = 0; j < totalRequestsPerTask; j++)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;
                        if (rateLimitChannel != null)
                        {
                            try
                            {
                                await rateLimitChannel.Reader.ReadAsync(cancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                        string fullUrl = options.Url;
                        if (startId.HasValue && endId.HasValue)
                        {
                            int id = Random.Shared.Next(startId.Value, endId.Value + 1);
                            fullUrl = $"{options.Url}/{id}";
                        }
                        try
                        {
                            var (elapsedMs, bytesSentLocal, bytesReceivedLocal) =
                            await SendRequestWithRetriesAsync(
                                client,
                                fullUrl,
                                options.Method,
                                headers,
                                options.Body,
                                retries: 3,
                                cancellationTokenSource.Token);
                            Interlocked.Increment(ref totalSuccessCount);
                            localResponseTimes.Add(elapsedMs);
                            Interlocked.Add(ref totalDataSent, bytesSentLocal);
                            Interlocked.Add(ref totalDataReceived, bytesReceivedLocal);
                            if (options.Threshold.HasValue)
                            {
                                if (elapsedMs <= options.Threshold.Value)
                                    Interlocked.Increment(ref totalBelowThresholdCount);
                                else
                                    Interlocked.Increment(ref totalAboveThresholdCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref totalFailureCount);
                            localFailureLog.Add($"URL: {fullUrl}, Error: {ex.Message}");
                            Logger.LogDebug($"Request to {fullUrl} failed: {ex.Message}");
                        }
                        await Task.Yield();
                    }
                    lock (responseTimesList)
                    {
                        responseTimesList.AddRange(localResponseTimes);
                    }
                    lock (failureLog)
                    {
                        failureLog.AddRange(localFailureLog);
                    }
                }, cancellationTokenSource.Token));
            }
            var progressReportingTask = Task.Run(async () =>
            {
                var reportInterval = TimeSpan.FromSeconds(10);
                var warmUpDuration = TimeSpan.FromSeconds(3); // Warm-up period to exclude initial low RPS values
                DateTime startTimeUtc = DateTime.UtcNow;
                DateTime previousTime = DateTime.UtcNow;
                int previousSuccesses = 0;
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(reportInterval, cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    var currentTime = DateTime.UtcNow;
                    var currentSuccesses = totalSuccessCount;
                    var intervalSeconds = (currentTime - previousTime).TotalSeconds;
                    var intervalSuccesses = currentSuccesses - previousSuccesses;
                    var intervalRps = intervalSuccesses / intervalSeconds;
                    var elapsedTime = currentTime - startTimeUtc;
                    if (elapsedTime >= warmUpDuration && intervalRps > 0)
                    {
                        rpsValues.Add(intervalRps);
                    }
                    previousSuccesses = currentSuccesses;
                    previousTime = currentTime;
                    var cpuUsage = PerformanceUtils.GetCpuUsageForProcess();
                    var memoryUsage = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0); // MB
                    lock (cpuUsageStatsList)
                    {
                        cpuUsageStatsList.Add(cpuUsage);
                    }
                    lock (memoryUsageStatsList)
                    {
                        memoryUsageStatsList.Add(memoryUsage);
                    }
                    var successes = totalSuccessCount;
                    var failures = totalFailureCount;
                    var elapsed = startTime.Elapsed.TotalSeconds;
                    var rps = elapsed > 0 ? successes / elapsed : 0;
                    Logger.LogInfo($"Progress - Successes: {successes}, Failures: {failures}, RPS: {rps:F2}, Interval RPS: {intervalRps:F2}, CPU Usage: {cpuUsage:F2}%, Memory Usage: {memoryUsage:F2} MB");
                }
            }, cancellationTokenSource.Token);
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }
            cancellationTokenSource.Cancel();
            await progressReportingTask;
            await DisplayReportAsync(
                startTime,
                totalSuccessCount,
                totalFailureCount,
                responseTimesList,
                failureLog,
                cpuUsageStatsList,
                memoryUsageStatsList,
                totalDataSent,
                totalDataReceived,
                options,
                totalBelowThresholdCount,
                totalAboveThresholdCount,
                rpsValues);
        }
        static async Task<(long elapsedMs, long bytesSent, long bytesReceived)> SendRequestWithRetriesAsync(
            HttpClient client,
            string url,
            string method,
            Dictionary<string, string> headers,
            string? body,
            int retries,
            CancellationToken cancellationToken)
        {
            int attempts = 0;
            Exception lastException = null;
            while (attempts < retries)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), url);
                foreach (var kvp in headers)
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body);
                }
                var start = Stopwatch.StartNew();
                try
                {
                    using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    var bytesReceived = responseBody.Length;
                    var bytesSent = PerformanceUtils.EstimateRequestSize(request);
                    return (start.ElapsedMilliseconds, bytesSent, bytesReceived);
                }
                catch (Exception ex) when (attempts < retries - 1 && !cancellationToken.IsCancellationRequested)
                {
                    attempts++;
                    lastException = ex;
                    Logger.LogWarning($"Attempt {attempts} failed for {url}: {ex.Message}");
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }
            throw new Exception("Request failed after retries", lastException);
        }
        static async Task DisplayReportAsync(
            Stopwatch startTime,
            int successCount,
            int failureCount,
            List<long> responseTimesList,
            List<string> failureLog,
            List<float> cpuUsageStatsList,
            List<double> memoryUsageStatsList,
            long dataSent,
            long dataReceived,
            Options options,
            int belowThresholdCount,
            int aboveThresholdCount,
            List<double> rpsValues)
        {
            try
            {
                Logger.LogInfo("Generating final report... This may take a few moments for large datasets.");
                var elapsedSeconds = startTime.Elapsed.TotalSeconds;
                var elapsedTime = startTime.Elapsed;
                var rps = elapsedSeconds > 0 ? successCount / elapsedSeconds : 0;
                double avgResponseTime = 0, medianResponseTime = 0, minResponseTime = 0, maxResponseTime = 0;
                double p50ResponseTime = 0, p75ResponseTime = 0, p90ResponseTime = 0, p95ResponseTime = 0, p99ResponseTime = 0;
                float minCpu = 0, maxCpu = 0, avgCpu = 0, medianCpu = 0;
                double minMem = 0, maxMem = 0, avgMem = 0;
                double totalDataSentMB = 0, totalDataReceivedMB = 0;
                double minRps = 0, medianRps = 0, rpsStdDev = 0, onePercentLowRps = 0, fivePercentLowRps = 0, tenPercentLowRps = 0;
                var tasks = new List<Task>();
                int totalCalculations = 4; // Number of calculation groups
                int calculationsCompleted = 0;
                tasks.Add(Task.Run(() =>
                {
                    Logger.LogInfo("Calculating response time statistics...");
                    if (responseTimesList.Any())
                    {
                        avgResponseTime = responseTimesList.Average();
                        medianResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 50);
                        minResponseTime = responseTimesList.Min();
                        maxResponseTime = responseTimesList.Max();
                        p50ResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 50);
                        p75ResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 75);
                        p90ResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 90);
                        p95ResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 95);
                        p99ResponseTime = PerformanceUtils.CalculateApproximatePercentile(responseTimesList, 99);
                    }
                    Interlocked.Increment(ref calculationsCompleted);
                    Logger.LogInfo($"Response time statistics calculated. ({calculationsCompleted}/{totalCalculations})");
                }));
                tasks.Add(Task.Run(() =>
                {
                    Logger.LogInfo("Calculating CPU and memory usage statistics...");
                    if (cpuUsageStatsList.Any())
                    {
                        minCpu = cpuUsageStatsList.Min();
                        maxCpu = cpuUsageStatsList.Max();
                        avgCpu = cpuUsageStatsList.Average();
                        medianCpu = PerformanceUtils.CalculateMedian(cpuUsageStatsList);
                    }
                    if (memoryUsageStatsList.Any())
                    {
                        minMem = memoryUsageStatsList.Min();
                        maxMem = memoryUsageStatsList.Max();
                        avgMem = memoryUsageStatsList.Average();
                    }
                    totalDataSentMB = dataSent / (1024.0 * 1024.0);
                    totalDataReceivedMB = dataReceived / (1024.0 * 1024.0);
                    Interlocked.Increment(ref calculationsCompleted);
                    Logger.LogInfo($"CPU and memory usage statistics calculated. ({calculationsCompleted}/{totalCalculations})");
                }));
                tasks.Add(Task.Run(() =>
                {
                    Logger.LogInfo("Calculating RPS statistics...");
                    var validRpsValues = rpsValues.Where(rv => rv > 0).ToList();
                    if (rpsValues.Any())
                    {
                        if (validRpsValues.Any())
                        {
                            minRps = validRpsValues.Min();
                            medianRps = PerformanceUtils.CalculateApproximatePercentile(validRpsValues, 50);
                            rpsStdDev = rpsValues.Any() ? PerformanceUtils.CalculateStandardDeviation(rpsValues) : 0;
                            medianRps = PerformanceUtils.CalculateApproximatePercentile(rpsValues, 50);
                            onePercentLowRps = PerformanceUtils.CalculateLowPercentAverage(validRpsValues, 1);
                            fivePercentLowRps = PerformanceUtils.CalculateLowPercentAverage(validRpsValues, 5);
                            tenPercentLowRps = PerformanceUtils.CalculateLowPercentAverage(validRpsValues, 10);
                        }
                        else
                        {
                            minRps = 0;
                            medianRps = 0;
                            onePercentLowRps = 0;
                        }
                    }
                    Interlocked.Increment(ref calculationsCompleted);
                    Logger.LogInfo($"RPS statistics calculated. ({calculationsCompleted}/{totalCalculations})");
                }));
                tasks.Add(Task.Run(() =>
                {
                    Logger.LogInfo("Processing failure logs...");
                    Interlocked.Increment(ref calculationsCompleted);
                    Logger.LogInfo($"Failure logs processed. ({calculationsCompleted}/{totalCalculations})");
                }));
                await Task.WhenAll(tasks);
                Logger.LogInfo("Finalizing report...");
                Logger.LogInfo("\n🛠️ Test completed.");
                Logger.LogInfo("==========================================");
                Logger.LogInfo("📊 Report Summary");
                Logger.LogInfo("------------------------------------------");
                Logger.LogInfo($"⏳ Elapsed Time:          {elapsedTime}");
                Logger.LogInfo($"✅ Total Successes:       {successCount}");
                Logger.LogInfo($"❌ Total Failures:        {failureCount}");
                Logger.LogInfo($"✔️ Status 200 OK Rate:   {(successCount + failureCount > 0 ? 100.0 * successCount / (successCount + failureCount) : 0):F2}%");
                Logger.LogInfo($"⚡ Average RPS:           {rps:F2} RPS");
                Logger.LogInfo("------------------------------------------");
                Logger.LogInfo("📈 Throughput Statistics:");
                Logger.LogInfo($"   - Minimum RPS:        {minRps:F2} RPS");
                Logger.LogInfo($"   - Median RPS:         {medianRps:F2} RPS");
                Logger.LogInfo($"   - Average RPS:        {rps:F2} RPS");
                Logger.LogInfo($"   - RPS Standard Deviation: {rpsStdDev:F2} RPS");
                Logger.LogInfo($"   - 1% Low RPS:         {onePercentLowRps:F2} RPS");
                Logger.LogInfo($"   - 5% Low RPS:         {fivePercentLowRps:F2} RPS");
                Logger.LogInfo($"   - 10% Low RPS:        {tenPercentLowRps:F2} RPS");
                Logger.LogInfo("------------------------------------------");
                Logger.LogInfo("📊 Response Times (ms):");
                Logger.LogInfo($"   - Average:           {avgResponseTime:F2} ms");
                Logger.LogInfo($"   - Median:            {medianResponseTime:F2} ms");
                Logger.LogInfo($"   - Minimum:           {minResponseTime} ms");
                Logger.LogInfo($"   - Maximum:           {maxResponseTime} ms");
                Logger.LogInfo($"   - P50:               {p50ResponseTime:F2} ms");
                Logger.LogInfo($"   - P75:               {p75ResponseTime:F2} ms");
                Logger.LogInfo($"   - P90:               {p90ResponseTime:F2} ms");
                Logger.LogInfo($"   - P95:               {p95ResponseTime:F2} ms");
                Logger.LogInfo($"   - P99:               {p99ResponseTime:F2} ms");
                Logger.LogInfo("------------------------------------------");
                Logger.LogInfo("💻 Tool CPU Usage (%):");
                Logger.LogInfo($"   - Minimum:           {minCpu:F2}%");
                Logger.LogInfo($"   - Median:            {medianCpu:F2}%");
                Logger.LogInfo($"   - Average:           {avgCpu:F2}%");
                Logger.LogInfo($"   - Maximum:           {maxCpu:F2}%");
                Logger.LogInfo("🧠 Tool Memory Usage (MB):");
                Logger.LogInfo($"   - Minimum:           {minMem:F2} MB");
                Logger.LogInfo($"   - Average:           {avgMem:F2} MB");
                Logger.LogInfo($"   - Maximum:           {maxMem:F2} MB");
                Logger.LogInfo("------------------------------------------");
                Logger.LogInfo("📤 Data Transfer:");
                Logger.LogInfo($"   - Data Sent:         {totalDataSentMB:F2} MB");
                Logger.LogInfo($"   - Data Received:     {totalDataReceivedMB:F2} MB");
                if (options.Threshold.HasValue)
                {
                    Logger.LogInfo("------------------------------------------");
                    Logger.LogInfo($"⏱️¸ Response Time Threshold: {options.Threshold.Value} ms");
                    Logger.LogInfo($"   - Below Threshold:    {belowThresholdCount} ({(successCount > 0 ? 100.0 * belowThresholdCount / successCount : 0):F2}%)");
                    Logger.LogInfo($"   - Above Threshold:    {aboveThresholdCount} ({(successCount > 0 ? 100.0 * aboveThresholdCount / successCount : 0):F2}%)");
                }
                Logger.LogInfo("==========================================");
                if (failureLog.Any())
                {
                    Logger.LogInfo("\n ❗ Failed Requests:");
                    foreach (var entry in failureLog)
                    {
                        Logger.LogInfo(entry);
                    }
                }
                if (!string.IsNullOrEmpty(options.Output))
                {
                    Logger.LogInfo("Saving detailed results to file...");
                    var stats = new Stats
                    {
                        Successes = successCount,
                        Failures = failureCount,
                        Rps = rps,
                        RpsValues = rpsValues,
                        ElapsedTimeSeconds = elapsedSeconds,
                        RpsStats = new RpsStatistics
                        {
                            Minimum = minRps,
                            Median = medianRps,
                            Average = rps,
                            StdDev = rpsStdDev,
                            OnePercentLow = onePercentLowRps,
                            FivePercentLow = fivePercentLowRps,
                            TenPercentLow = tenPercentLowRps
                        },
                        ResponseTimes = new ResponseTimeStats
                        {
                            Average = avgResponseTime,
                            Median = medianResponseTime,
                            Min = (long)minResponseTime,
                            Max = (long)maxResponseTime,
                            P50 = p50ResponseTime,
                            P75 = p75ResponseTime,
                            P90 = p90ResponseTime,
                            P95 = p95ResponseTime,
                            P99 = p99ResponseTime
                        },
                        CpuUsage = new CpuUsageStats
                        {
                            Min = minCpu,
                            Median = medianCpu,
                            Average = avgCpu,
                            Max = maxCpu
                        },
                        MemoryUsage = new MemoryUsageStats
                        {
                            Min = minMem,
                            Average = avgMem,
                            Max = maxMem
                        },
                        DataTransfer = new DataTransferStats
                        {
                            SentMb = totalDataSentMB,
                            ReceivedMb = totalDataReceivedMB
                        },
                        ThresholdStats = options.Threshold.HasValue ? new ThresholdStats
                        {
                            ThresholdMs = options.Threshold.Value,
                            BelowThreshold = belowThresholdCount,
                            AboveThreshold = aboveThresholdCount,
                            BelowThresholdPercentage = successCount > 0 ? 100.0 * belowThresholdCount / successCount : 0,
                            AboveThresholdPercentage = successCount > 0 ? 100.0 * aboveThresholdCount / successCount : 0
                        } : null
                    };
                    var json = JsonSerializer.Serialize(stats, MyJsonContext.Default.Stats);// for native AOT
                    try
                    {
                        await File.WriteAllTextAsync(options.Output, json, cancellationToken: CancellationToken.None);
                        Logger.LogInfo($"Detailed results have been saved to {options.Output}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to write to output file: {ex.Message}");
                    }
                }
                Logger.LogInfo("Report generation complete.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while generating the report: {ex}");
            }
        }
    }
    public class Token { }
}
