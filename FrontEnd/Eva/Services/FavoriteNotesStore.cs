namespace Eva.Services
{
    public static class FavoriteNotesStore
    {
        static string Key(string stableId) => "eva_fav_note_" + stableId;

        public static string? Get(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return null;
            }

            return Preferences.Get(Key(stableId), (string?)null);
        }

        public static void Set(string stableId, string? note)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                Preferences.Remove(Key(stableId));
            }
            else
            {
                Preferences.Set(Key(stableId), note.Trim());
            }
        }
    }
}
