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
        
        while (repeatCount < 0 || count < repeatCount)
        {
            yield return new WaitForSeconds(interval);
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
    /// Formats seconds into a readable time string (MM:SS)
    /// </summary>
    public static string FormatTimeMMSS(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
    
    /// <summary>
    /// Formats seconds into a readable time string (HH:MM:SS)
    /// </summary>
    public static string FormatTimeHHMMSS(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);
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
    /// Creates a timer that updates a callback with remaining time (for UI updates)
    /// </summary>
    public Coroutine StartUITimer(float duration, Action<string> onUpdate, string format = "MM:SS", Action onComplete = null)
    {
        return StartCoroutine(UITimerCoroutine(duration, onUpdate, format, onComplete));
    }
    
    private IEnumerator UITimerCoroutine(float duration, Action<string> onUpdate, string format, Action onComplete)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            
            if (onUpdate != null)
            {
                string timeString = "";
                switch (format.ToUpper())
                {
                    case "MM:SS":
                        timeString = FormatTimeMMSS(remaining);
                        break;
                    case "HH:MM:SS":
                        timeString = FormatTimeHHMMSS(remaining);
                        break;
                    case "READABLE":
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
