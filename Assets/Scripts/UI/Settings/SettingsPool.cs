using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace UI.Settings
{
    public static class SettingsPool
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "settings.json");

        private static readonly List<StorageItem> StorageItems =
            Directory.Exists(Application.persistentDataPath)
                ? File.Exists(SavePath)
                    ? JsonConvert.DeserializeObject<List<StorageItem>>(File.ReadAllText(SavePath))
                    : new List<StorageItem>()
                : new List<StorageItem>();

        public static int GetValue(string identifier)
        {
            var value = 0;

            foreach (var category in SettingsItems.Settings)
            foreach (var settingsItem in category.Items)
                if (identifier == settingsItem.Identifier)
                    value = settingsItem.ValueSet switch
                    {
                        SuccessiveIntegerValueSet successiveValueSet
                            => successiveValueSet.DefaultValue,

                        SeparatedValueSet separatedValueSet => separatedValueSet.DefaultValueIndex,
                        BoolValueSet boolValueSet => boolValueSet.DefaultValue ? 1 : 0,
                        _ => 0
                    };

            foreach (var storageItem in StorageItems)
                if (storageItem.Identifier == identifier)
                    value = storageItem.Value;

            return value;
        }

        public static void SetValue(string identifier, int value)
        {
            var found = false;

            StorageItems.ForEach(storageItem =>
            {
                if (storageItem.Identifier != identifier)
                    return;

                storageItem.Value = value;
                found = true;
            });

            if (!found)
                StorageItems.Add(new StorageItem(identifier, value));
        }

        public static void Save()
        {
            File.WriteAllText(SavePath, JsonConvert.SerializeObject(StorageItems));
        }

        private class StorageItem
        {
            [JsonProperty] public readonly string Identifier;

            [JsonProperty] public int Value;

            public StorageItem(string identifier, int value)
            {
                Identifier = identifier;
                Value = value;
            }
        }
    }
}