using Eva.Entidades;
using Newtonsoft.Json;

namespace Eva.Services
{
    public static class SavedPlacesStore
    {
        const string Key = "eva_saved_places_v1";
        const int MaxPlaces = 24;

        public static List<SavedPlace> Load()
        {
            try
            {
                string? json = Preferences.Get(Key, null);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<SavedPlace>();
                }

                List<SavedPlace>? list = JsonConvert.DeserializeObject<List<SavedPlace>>(json);
                return list ?? new List<SavedPlace>();
            }
            catch
            {
                return new List<SavedPlace>();
            }
        }

        public static void Save(IReadOnlyList<SavedPlace> places)
        {
            List<SavedPlace> trimmed = places.Take(MaxPlaces).ToList();
            string json = JsonConvert.SerializeObject(trimmed);
            Preferences.Set(Key, json);
        }

        public static void Upsert(SavedPlace place)
        {
            List<SavedPlace> list = Load();
            int i = list.FindIndex(p => p.Id == place.Id);
            if (i >= 0)
            {
                list[i] = place;
            }
            else
            {
                list.Add(place);
            }

            Save(list);
        }

        public static void Remove(string id)
        {
            List<SavedPlace> list = Load();
            list.RemoveAll(p => p.Id == id);
            Save(list);
        }
    }
}
