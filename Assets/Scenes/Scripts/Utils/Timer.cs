using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Utility class for timer functionality.
/// Supports countdown timers, interval timers, and one-shot timers.
/// </summary>
public class Timer : MonoBehaviour
{
    private static Timer _instance;
    public static Timer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Timer>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Timer");
                    _instance = go.AddComponent<Timer>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Starts a countdown timer that calls a callback when finished
    /// </summary>
    /// <param name="duration">Duration in seconds</param>
    /// <param name="onComplete">Callback when timer completes</param>
    /// <param name="onUpdate">Optional callback called every frame with remaining time</param>
    /// <returns>Coroutine handle</returns>
    public Coroutine StartCountdown(float duration, Action onComplete, Action<float> onUpdate = null)
    {
        return StartCoroutine(CountdownCoroutine(duration, onComplete, onUpdate));
    }
    
    private IEnumerator CountdownCoroutine(float duration, Action onComplete, Action<float> onUpdate)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            
            onUpdate?.Invoke(remaining);
            
            yield return null;
        }
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Starts an interval timer that calls a callback at regular intervals
    /// </summary>
    /// <param name="interval">Interval in seconds</param>
    /// <param name="onTick">Callback called at each interval</param>
    /// <param name="repeatCount">Number of times to repeat (-1 for infinite)</param>
    /// <returns>Coroutine handle</returns>
    public Coroutine StartInterval(float interval, Action onTick, int repeatCount = -1)
    {
        return StartCoroutine(IntervalCoroutine(interval, onTick, repeatCount));
    }
    
    private IEnumerator IntervalCoroutine(float interval, Action onTick, int repeatCount)
    {
        int count = 0;
        // Perf: hoist yield instruction out of the loop (was new'd every tick).
        var wait = new WaitForSeconds(interval);
        
        while (repeatCount < 0 || count < repeatCount)
        {
            yield return wait;
            onTick?.Invoke();
            count++;
        }
    }
    
    /// <summary>
    /// Starts a one-shot timer that calls a callback after a delay
    /// </summary>
    /// <param name="delay">Delay in seconds</param>
    /// <param name="onComplete">Callback when timer completes</param>
    /// <returns>Coroutine handle</returns>
    public Coroutine StartDelayed(float delay, Action onComplete)
    {
        return StartCoroutine(DelayedCoroutine(delay, onComplete));
    }
    
    private IEnumerator DelayedCoroutine(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Formats seconds into a readable time string (MM:SS).
    /// Perf: manual two-digit formatting via string.Concat — avoids
    /// string.Format boxing of value types (GC per call in UI loops).
    /// </summary>
    public static string FormatTimeMMSS(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        if (totalSeconds < 0) totalSeconds = 0;
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        string m = minutes < 10 ? string.Concat("0", minutes.ToString()) : minutes.ToString();
        return string.Concat(m, ":", TwoDigits(secs));
    }
    
    /// <summary>
    /// Formats seconds into a readable time string (HH:MM:SS).
    /// Perf: same boxing-free formatting as FormatTimeMMSS.
    /// </summary>
    public static string FormatTimeHHMMSS(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        if (totalSeconds < 0) totalSeconds = 0;
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        return string.Concat(TwoDigits(hours), ":", TwoDigits(minutes), ":", TwoDigits(secs));
    }

    private static string TwoDigits(int v)
    {
        v %= 100;
        if (v < 0) v += 100;
        // string.Concat(char, char) overload avoids intermediate strings/boxing.
        return string.Concat((char)('0' + v / 10), (char)('0' + v % 10));
    }
    
    /// <summary>
    /// Formats seconds into a readable time string with units (e.g., "5m 30s", "2h 15m")
    /// </summary>
    public static string FormatTimeReadable(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        
        if (totalSeconds < 60)
        {
            return $"{totalSeconds}s";
        }
        else if (totalSeconds < 3600)
        {
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;
            if (secs > 0)
            {
                return $"{minutes}m {secs}s";
            }
            return $"{minutes}m";
        }
        else
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            if (minutes > 0)
            {
                return $"{hours}h {minutes}m";
            }
            return $"{hours}h";
        }
    }
    
    /// <summary>
    /// Formats a TimeSpan into a readable string
    /// </summary>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            return $"{timeSpan.Seconds}s";
        }
    }
    
    /// <summary>
    /// Creates a timer that updates a callback with remaining time (for UI updates).
    /// NOTE: callers must keep the returned Coroutine handle and call
    /// StopTimer(handle) (or StopCoroutine) when the UI is destroyed/hidden,
    /// otherwise the per-frame loop leaks after scene unload.
    /// </summary>
    public Coroutine StartUITimer(float duration, Action<string> onUpdate, string format = "MM:SS", Action onComplete = null)
    {
        return StartCoroutine(UITimerCoroutine(duration, onUpdate, format, onComplete));
    }

    /// <summary>
    /// Stops a timer started with StartCountdown/StartInterval/StartDelayed/StartUITimer.
    /// Call this from OnDestroy/OnDisable of the owning UI.
    /// </summary>
    public void StopTimer(Coroutine handle)
    {
        if (handle != null) StopCoroutine(handle);
    }
    
    private IEnumerator UITimerCoroutine(float duration, Action<string> onUpdate, string format, Action onComplete)
    {
        float elapsed = 0f;

        // Perf: hoist format.ToUpper() out of the per-frame loop — compute once.
        // 0 = MM:SS, 1 = HH:MM:SS, 2 = READABLE.
        int formatMode = 0;
        if (!string.IsNullOrEmpty(format))
        {
            if (format.Length == 5) formatMode = 0; // "MM:SS" fast path, avoids ToUpper alloc
            else
            {
                string f = format.ToUpper();
                if (f == "HH:MM:SS") formatMode = 1;
                else if (f == "READABLE") formatMode = 2;
                else if (f == "MM:SS") formatMode = 0;
                else formatMode = 0;
            }
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            
            if (onUpdate != null)
            {
                string timeString;
                switch (formatMode)
                {
                    case 1:
                        timeString = FormatTimeHHMMSS(remaining);
                        break;
                    case 2:
                        timeString = FormatTimeReadable(remaining);
                        break;
                    default:
                        timeString = FormatTimeMMSS(remaining);
                        break;
                }
                onUpdate(timeString);
            }
            
            yield return null;
        }
        
        if (onUpdate != null)
        {
            onUpdate("00:00");
        }
        
        onComplete?.Invoke();
    }
}
