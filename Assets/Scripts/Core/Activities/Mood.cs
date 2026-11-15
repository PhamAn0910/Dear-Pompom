namespace DearPompom.Core
{
    /// <summary>
    /// Journal mood. These are CATEGORIES, not a low-to-high scale: the value is the index of
    /// the mood button in the Home scene (JournalUIManager.moodButtons). Persisted: append only,
    /// and keep in sync if the buttons are ever reordered.
    /// </summary>
    public enum Mood
    {
        None = -1,
        Productive = 0,
        Tired = 1,
        Happy = 2,
        Sad = 3,
        Surprised = 4,
    }

    public static class Moods
    {
        public static bool IsLow(Mood mood)
        {
            return mood == Mood.Tired || mood == Mood.Sad;
        }

        public static bool IsValid(int value)
        {
            return value >= (int)Mood.Productive && value <= (int)Mood.Surprised;
        }

        /// <summary>Maps a raw button index to a Mood; anything out of range becomes None.</summary>
        public static Mood FromIndex(int value)
        {
            return IsValid(value) ? (Mood)value : Mood.None;
        }
    }
}
