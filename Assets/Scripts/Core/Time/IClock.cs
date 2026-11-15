using System;
using System.Globalization;

namespace DearPompom.Core
{
    /// <summary>
    /// The only source of "now" for game logic. Production code uses <see cref="SystemClock"/>;
    /// tests inject a fake so rules that depend on dates are deterministic.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }

    /// <summary>
    /// Timestamps are stored as ISO 8601 strings with offset ("o" format), because Unity's
    /// JsonUtility cannot serialize DateTime. All parsing and formatting goes through here.
    /// </summary>
    public static class Timestamp
    {
        public static string Format(DateTimeOffset value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        /// <summary>Returns false (and default) for null, empty or malformed input instead of throwing.</summary>
        public static bool TryParse(string text, out DateTimeOffset value)
        {
            if (string.IsNullOrEmpty(text))
            {
                value = default(DateTimeOffset);
                return false;
            }
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }
    }
}
