using System.Diagnostics;

namespace AravisSharp.Utilities;

/// <summary>
/// Performance monitoring for camera acquisition
/// </summary>
public class AcquisitionStats
{
    private readonly Stopwatch _stopwatch = new();
    private long _frameCount;
    private long _successCount;
    private long _failureCount;
    private long _timeoutCount;
    private long _totalBytes;
    private double _minFrameTime = double.MaxValue;
    private double _maxFrameTime;
    private DateTime _lastFrameTime;

    /// <summary>
    /// Total number of frames attempted
    /// </summary>
    public long FrameCount => _frameCount;

    /// <summary>
    /// Number of successfully acquired frames
    /// </summary>
    public long SuccessCount => _successCount;

    /// <summary>
    /// Number of failed frames
    /// </summary>
    public long FailureCount => _failureCount;

    /// <summary>
    /// Number of timeouts
    /// </summary>
    public long TimeoutCount => _timeoutCount;

    /// <summary>
    /// Total bytes received
    /// </summary>
    public long TotalBytes => _totalBytes;

    /// <summary>
    /// Elapsed time in seconds
    /// </summary>
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

    /// <summary>
    /// Average frame rate in FPS
    /// </summary>
    public double AverageFps => _successCount / ElapsedSeconds;

    /// <summary>
    /// Average throughput in MB/s
    /// </summary>
    public double AverageMBps => (_totalBytes / (1024.0 * 1024.0)) / ElapsedSeconds;

    /// <summary>
    /// Minimum frame interval in milliseconds
    /// </summary>
    public double MinFrameIntervalMs => _minFrameTime;

    /// <summary>
    /// Maximum frame interval in milliseconds
    /// </summary>
    public double MaxFrameIntervalMs => _maxFrameTime;

    /// <summary>
    /// Success rate as percentage
    /// </summary>
    public double SuccessRate => _frameCount > 0 ? (_successCount * 100.0) / _frameCount : 0;

    /// <summary>
    /// Starts the statistics collection
    /// </summary>
    public void Start()
    {
        Reset();
        _stopwatch.Start();
        _lastFrameTime = DateTime.Now;
    }

    /// <summary>
    /// Stops the statistics collection
    /// </summary>
    public void Stop()
    {
        _stopwatch.Stop();
    }

    /// <summary>
    /// Resets all statistics
    /// </summary>
    public void Reset()
    {
        _stopwatch.Reset();
        _frameCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _timeoutCount = 0;
        _totalBytes = 0;
        _minFrameTime = double.MaxValue;
        _maxFrameTime = 0;
    }

    /// <summary>
    /// Records a successful frame acquisition
    /// </summary>
    public void RecordSuccess(int bufferSize)
    {
        _frameCount++;
        _successCount++;
        _totalBytes += bufferSize;
        UpdateFrameInterval();
    }

    /// <summary>
    /// Records a failed frame acquisition
    /// </summary>
    public void RecordFailure()
    {
        _frameCount++;
        _failureCount++;
    }

    /// <summary>
    /// Records a timeout
    /// </summary>
    public void RecordTimeout()
    {
        _frameCount++;
        _timeoutCount++;
    }

    private void UpdateFrameInterval()
    {
        var now = DateTime.Now;
        var interval = (now - _lastFrameTime).TotalMilliseconds;
        
        if (_successCount > 1) // Skip first frame
        {
            if (interval < _minFrameTime)
                _minFrameTime = interval;
            if (interval > _maxFrameTime)
                _maxFrameTime = interval;
        }
        
        _lastFrameTime = now;
    }

    /// <summary>
    /// Returns a formatted statistics summary
    /// </summary>
    public override string ToString()
    {
        return $"""
            Acquisition Statistics:
              Duration: {ElapsedSeconds:F2} seconds
              Frames: {SuccessCount}/{FrameCount} ({SuccessRate:F1}% success)
              Failures: {FailureCount}, Timeouts: {TimeoutCount}
              Average FPS: {AverageFps:F2}
              Frame Interval: {MinFrameIntervalMs:F2} - {MaxFrameIntervalMs:F2} ms
              Throughput: {AverageMBps:F2} MB/s
              Total Data: {TotalBytes / (1024.0 * 1024.0):F2} MB
            """;
    }

    /// <summary>
    /// Prints a real-time status line (overwrites previous line)
    /// </summary>
    public void PrintStatus()
    {
        Console.Write($"\rFrames: {SuccessCount} | FPS: {AverageFps:F1} | {AverageMBps:F1} MB/s | Failures: {FailureCount}   ");
    }
}
