using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// PIN gate for the teacher/parent admin panel.
///
/// IMPORTANT SECURITY NOTE
/// This is a parental gate, not a security boundary. The salt and the derived
/// hash live in Application.persistentDataPath and the algorithm ships inside
/// the binary, so anyone with the APK can lift the hash out and brute force a
/// short PIN offline. It reliably stops a curious child and a well-meaning
/// classmate; it does not stop a motivated attacker. Real authentication would
/// have to move server side, which the current GooglePlayGamesService stub
/// cannot provide.
///
/// There is deliberately NO default PIN in the build. The first person to open
/// the admin panel creates one, so every installation has its own secret.
///
/// The PIN is stored in its own file so that exporting the admin configuration
/// (which teachers share between devices) never leaks it.
/// </summary>
public static class AdminSecurity
{
    public const int MinPinLength = 6;
    public const int MaxPinLength = 12;
    public const int MaxFailedAttempts = 5;
    public const int LockoutMinutes = 5;
    public const int Iterations = 10000;

    public const string FileName = "admin_pin.json";

    [Serializable]
    private class PinRecord
    {
        public string salt = "";
        public string hash = "";
        public int iterations = Iterations;
        public int failedAttempts = 0;
        public string lockoutUntilUtc = "";
    }

    private static PinRecord _record;
    private static bool _loaded;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasPin
    {
        get
        {
            Load();
            return _record != null && !string.IsNullOrEmpty(_record.hash);
        }
    }

    public static int FailedAttempts
    {
        get
        {
            Load();
            return _record != null ? _record.failedAttempts : 0;
        }
    }

    /// <summary>True while a failed-attempt lockout is still active.</summary>
    public static bool IsLockedOut
    {
        get
        {
            Load();
            if (_record == null || string.IsNullOrEmpty(_record.lockoutUntilUtc)) return false;
            if (!DateTime.TryParse(_record.lockoutUntilUtc, out DateTime until)) return false;
            return DateTime.UtcNow < until;
        }
    }

    /// <summary>How long the lockout lasts, or zero when it is not locked.</summary>
    public static int LockoutSecondsRemaining
    {
        get
        {
            Load();
            if (_record == null || string.IsNullOrEmpty(_record.lockoutUntilUtc)) return 0;
            if (!DateTime.TryParse(_record.lockoutUntilUtc, out DateTime until)) return 0;
            double seconds = (until - DateTime.UtcNow).TotalSeconds;
            return seconds <= 0 ? 0 : (int)Math.Ceiling(seconds);
        }
    }

    // ------------------------------------------------------------------
    // Storage
    // ------------------------------------------------------------------

    private static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            string path = FilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                {
                    _record = JsonUtility.FromJson<PinRecord>(json);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminSecurity] Could not read the PIN file: " + e.Message);
            _record = null;
        }

        if (_record == null) _record = new PinRecord();
    }

    private static void Save()
    {
        try
        {
            string path = FilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(_record, true));
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminSecurity] Failed to save the PIN file: " + e.Message);
        }
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Creates the first PIN. Fails if a PIN already exists.</summary>
    public static bool CreatePin(string pin, out string error)
    {
        Load();
        error = ValidateFormat(pin);
        if (error.Length > 0) return false;

        if (HasPin)
        {
            error = "A PIN already exists on this device.";
            return false;
        }

        byte[] salt = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        _record.salt = Convert.ToBase64String(salt);
        _record.iterations = Iterations;
        _record.hash = Convert.ToBase64String(Derive(pin, salt, Iterations));
        _record.failedAttempts = 0;
        _record.lockoutUntilUtc = "";
        Save();
        return true;
    }

    /// <summary>Checks a PIN. Failed attempts count towards a lockout.</summary>
    public static bool VerifyPin(string pin, out string error)
    {
        Load();
        error = "";

        if (!HasPin)
        {
            error = "No PIN has been created on this device yet.";
            return false;
        }

        if (IsLockedOut)
        {
            error = "Too many attempts. Try again in " + LockoutSecondsRemaining + " seconds.";
            return false;
        }

        if (string.IsNullOrEmpty(pin))
        {
            RegisterFailure();
            error = "Enter the PIN.";
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(_record.salt);
            expected = Convert.FromBase64String(_record.hash);
        }
        catch (Exception)
        {
            error = "The stored PIN is damaged. Reset the admin panel to continue.";
            return false;
        }

        byte[] actual = Derive(pin, salt, _record.iterations);
        if (FixedTimeEquals(actual, expected))
        {
            _record.failedAttempts = 0;
            _record.lockoutUntilUtc = "";
            Save();
            return true;
        }

        RegisterFailure();
        error = "Incorrect PIN.";
        return false;
    }

    /// <summary>Changes the PIN. Requires the current one.</summary>
    public static bool ChangePin(string currentPin, string newPin, out string error)
    {
        Load();
        error = "";

        if (!VerifyPin(currentPin, out error)) return false;

        string formatError = ValidateFormat(newPin);
        if (formatError.Length > 0)
        {
            error = formatError;
            return false;
        }

        byte[] salt = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        _record.salt = Convert.ToBase64String(salt);
        _record.iterations = Iterations;
        _record.hash = Convert.ToBase64String(Derive(newPin, salt, Iterations));
        _record.failedAttempts = 0;
        _record.lockoutUntilUtc = "";
        Save();
        return true;
    }

    /// <summary>Clears the lockout and the failure counter.</summary>
    public static void ResetLockout()
    {
        Load();
        _record.failedAttempts = 0;
        _record.lockoutUntilUtc = "";
        Save();
    }

    /// <summary>
    /// Removes the PIN entirely, so the next visit to the admin panel asks
    /// the teacher to create a new one. Used by the "erase everything"
    /// action in the Security tab.
    /// </summary>
    public static void DeletePin()
    {
        Load();
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminSecurity] Failed to delete the PIN file: " + e.Message);
        }
        _record = new PinRecord();
        _loaded = false;
        Load();
    }

    private static void RegisterFailure()
    {
        _record.failedAttempts++;
        if (_record.failedAttempts >= MaxFailedAttempts)
        {
            _record.lockoutUntilUtc = DateTime.UtcNow.AddMinutes(LockoutMinutes).ToString("o");
            _record.failedAttempts = 0;
        }
        Save();
    }

    private static string ValidateFormat(string pin)
    {
        if (string.IsNullOrEmpty(pin)) return "Enter a PIN.";
        if (pin.Length < MinPinLength)
            return "The PIN must be at least " + MinPinLength + " digits.";
        if (pin.Length > MaxPinLength)
            return "The PIN must be at most " + MaxPinLength + " digits.";
        for (int i = 0; i < pin.Length; i++)
        {
            if (!char.IsDigit(pin[i])) return "The PIN must contain digits only.";
        }
        return "";
    }

    // ------------------------------------------------------------------
    // Crypto helpers
    // ------------------------------------------------------------------

    private static byte[] Derive(string pin, byte[] salt, int iterations)
    {
        byte[] password = Encoding.UTF8.GetBytes(pin);

        try
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }
        catch (PlatformNotSupportedException)
        {
            // Older or restricted platforms may not expose the SHA256 flavour
            // of PBKDF2. Fall back to a plain salted SHA-256, which is weaker
            // but keeps the panel usable.
            Debug.LogWarning("[AdminSecurity] PBKDF2-SHA256 unavailable, using salted SHA-256.");
            using (SHA256 sha = SHA256.Create())
            {
                byte[] payload = new byte[password.Length + salt.Length];
                Buffer.BlockCopy(password, 0, payload, 0, password.Length);
                Buffer.BlockCopy(salt, 0, payload, password.Length, salt.Length);
                return sha.ComputeHash(payload);
            }
        }
    }

    /// <summary>Compares two byte arrays without an early exit.</summary>
    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
