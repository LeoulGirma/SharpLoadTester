using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
public static class PerformanceUtils
{
    private static DateTime _lastCpuTime = DateTime.MinValue;
    private static TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    public static float GetCpuUsageForProcess()
    {
        var process = Process.GetCurrentProcess();
        if (_lastCpuTime == DateTime.MinValue)
        {
            _lastCpuTime = DateTime.UtcNow;
            _lastTotalProcessorTime = process.TotalProcessorTime;
            return 0;
        }
        var currentCpuTime = DateTime.UtcNow;
        var currentTotalProcessorTime = process.TotalProcessorTime;
        var cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
        var totalTimePassedMs = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
        if (totalTimePassedMs <= 0)
        {
            return 0;
        }
        var cpuUsageTotal = (float)(cpuUsedMs / (Environment.ProcessorCount * totalTimePassedMs));
        _lastCpuTime = currentCpuTime;
        _lastTotalProcessorTime = currentTotalProcessorTime;
        return cpuUsageTotal * 100;
    }
    public static long EstimateRequestSize(HttpRequestMessage request)
    {
        var method = request.Method.Method;
        var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
        var httpVersion = "HTTP/1.1\r\n";
        var headersSize = request.Headers.ToString().Length;
        var contentHeadersSize = request.Content?.Headers.ToString().Length ?? 0;
        var bodySize = request.Content?.Headers?.ContentLength ?? 0;
        var totalSize = method.Length + 1 + url.Length + 1 + httpVersion.Length + headersSize + contentHeadersSize + 2 + bodySize;
        return totalSize;
    }
    public static double CalculatePercentile(List<long> sequence, double percentile)
    {
        if (!sequence.Any())
            return 0;
        var arr = sequence.ToArray();
        int N = arr.Length;
        double n = (N - 1) * percentile / 100.0 + 1;
        if (n == 1d) return arr.Min();
        else if (n == N) return arr.Max();
        else
        {
            int k = (int)n - 1;
            double d = n - (k + 1);
            double lower = QuickSelect(arr, k);
            double upper = QuickSelect(arr, k + 1);
            return lower + d * (upper - lower);
        }
    }
    public static double CalculatePercentile(List<double> sequence, double percentile)
    {
        if (!sequence.Any())
            return 0;
        var arr = sequence.ToArray();
        int N = arr.Length;
        double n = (N - 1) * percentile / 100.0 + 1;
        if (n == 1d) return arr.Min();
        else if (n == N) return arr.Max();
        else
        {
            int k = (int)n - 1;
            double d = n - (k + 1);
            double lower = QuickSelect(arr, k);
            double upper = QuickSelect(arr, k + 1);
            return lower + d * (upper - lower);
        }
    }
    public static double CalculateApproximatePercentile(List<long> sequence, double percentile, int numberOfBins = 1000)
    {
        if (!sequence.Any())
            return 0;
        var min = sequence.Min();
        var max = sequence.Max();
        if (min == max)
            return min;
        var binSize = (max - min) / (double)numberOfBins;
        var bins = new int[numberOfBins];
        foreach (var value in sequence)
        {
            int binIndex = (int)((value - min) / binSize);
            if (binIndex >= numberOfBins)
                binIndex = numberOfBins - 1;
            bins[binIndex]++;
        }
        var total = sequence.Count;
        var cumulative = 0;
        var target = percentile / 100.0 * total;
        for (int i = 0; i < numberOfBins; i++)
        {
            cumulative += bins[i];
            if (cumulative >= target)
            {
                var binStart = min + i * binSize;
                return binStart;
            }
        }
        return max;
    }
    public static double CalculateOnePercentLow(List<double> rpsValues)
    {
        if (rpsValues.Count < 100)
        {
            return rpsValues.Min();
        }
        int onePercentCount = (int)(rpsValues.Count * 0.01);
        if (onePercentCount == 0)
            onePercentCount = 1; // Ensure at least one value
        var sortedRps = rpsValues.OrderBy(r => r).ToList();
        var lowestOnePercentRps = sortedRps.Take(onePercentCount);
        return lowestOnePercentRps.Average();
    }
    public static double CalculateLowPercentAverage(List<double> values, double percent)
    {
        if (values == null || values.Count == 0)
            return 0;
        if (percent <= 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100");
        int count = values.Count;
        int numValues = (int)Math.Ceiling(count * (percent / 100.0));
        numValues = Math.Max(numValues, 1); // Ensure at least one value
        var sortedValues = values.OrderBy(v => v).ToList();
        var lowestValues = sortedValues.Take(numValues);
        return lowestValues.Average();
    }
    public static double CalculateApproximatePercentile(List<double> sequence, double percentile, int numberOfBins = 1000)
    {
        if (!sequence.Any())
            return 0;
        var min = sequence.Min();
        var max = sequence.Max();
        if (min == max)
            return min;
        var binSize = (max - min) / numberOfBins;
        var bins = new int[numberOfBins];
        foreach (var value in sequence)
        {
            int binIndex = (int)((value - min) / binSize);
            if (binIndex >= numberOfBins)
                binIndex = numberOfBins - 1;
            bins[binIndex]++;
        }
        var total = sequence.Count;
        var cumulative = 0;
        var target = percentile / 100.0 * total;
        for (int i = 0; i < numberOfBins; i++)
        {
            cumulative += bins[i];
            if (cumulative >= target)
            {
                var binStart = min + i * binSize;
                return binStart;
            }
        }
        return max;
    }
    public static double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count <= 1)
            return 0;
        var avg = values.Average();
        var sumOfSquares = values.Sum(value => Math.Pow(value - avg, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
    private static double QuickSelect(long[] arr, int k)
    {
        return QuickSelect(arr, 0, arr.Length - 1, k);
    }
    private static double QuickSelect(long[] arr, int left, int right, int k)
    {
        if (left == right)
            return arr[left];
        int pivotIndex = Partition(arr, left, right);
        if (k == pivotIndex)
            return arr[k];
        else if (k < pivotIndex)
            return QuickSelect(arr, left, pivotIndex - 1, k);
        else
            return QuickSelect(arr, pivotIndex + 1, right, k);
    }
    private static int Partition(long[] arr, int left, int right)
    {
        long pivot = arr[right];
        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (arr[i] < pivot)
            {
                Swap(arr, i, storeIndex);
                storeIndex++;
            }
        }
        Swap(arr, storeIndex, right);
        return storeIndex;
    }
    private static void Swap(long[] arr, int i, int j)
    {
        long temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }
    private static double QuickSelect(double[] arr, int k)
    {
        return QuickSelect(arr, 0, arr.Length - 1, k);
    }
    private static double QuickSelect(double[] arr, int left, int right, int k)
    {
        if (left == right)
            return arr[left];
        int pivotIndex = Partition(arr, left, right);
        if (k == pivotIndex)
            return arr[k];
        else if (k < pivotIndex)
            return QuickSelect(arr, left, pivotIndex - 1, k);
        else
            return QuickSelect(arr, pivotIndex + 1, right, k);
    }
    private static int Partition(double[] arr, int left, int right)
    {
        double pivot = arr[right];
        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (arr[i] < pivot)
            {
                Swap(arr, i, storeIndex);
                storeIndex++;
            }
        }
        Swap(arr, storeIndex, right);
        return storeIndex;
    }
    private static void Swap(double[] arr, int i, int j)
    {
        double temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }
    public static float CalculateMedian(List<float> values)
    {
        if (!values.Any())
            return 0;
        var arr = values.ToArray();
        int N = arr.Length;
        int k = N / 2;
        if (N % 2 == 1)
        {
            return QuickSelect(arr, k);
        }
        else
        {
            float lower = QuickSelect(arr, k - 1);
            float upper = QuickSelect(arr, k);
            return (lower + upper) / 2.0f;
        }
    }
    private static float QuickSelect(float[] arr, int k)
    {
        return QuickSelect(arr, 0, arr.Length - 1, k);
    }
    private static float QuickSelect(float[] arr, int left, int right, int k)
    {
        if (left == right)
            return arr[left];
        int pivotIndex = Partition(arr, left, right);
        if (k == pivotIndex)
            return arr[k];
        else if (k < pivotIndex)
            return QuickSelect(arr, left, pivotIndex - 1, k);
        else
            return QuickSelect(arr, pivotIndex + 1, right, k);
    }
    private static int Partition(float[] arr, int left, int right)
    {
        float pivot = arr[right];
        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (arr[i] < pivot)
            {
                Swap(arr, i, storeIndex);
                storeIndex++;
            }
        }
        Swap(arr, storeIndex, right);
        return storeIndex;
    }
    private static void Swap(float[] arr, int i, int j)
    {
        float temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }
}
