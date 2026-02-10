using System;
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
                ? (File.Exists(SavePath)
                    ? JsonConvert.DeserializeObject<List<StorageItem>>(File.ReadAllText(SavePath))
                    : new List<StorageItem>())
                : new List<StorageItem>();

        private static readonly List<(int original, int current, string id)> ChangeHistoryList = new();

        public static Action SettingsChanged;

        public static int GetValue(string identifier)
        {
            var value = 0;

            foreach (var category in SettingsItems.Settings)
            foreach (var settingsItem in category.Items)
                if (settingsItem.Identifier == identifier)
                    if (settingsItem.ValueSet is SuccessiveIntegerValueSet successiveValueSet)
                        value = successiveValueSet.DefaultValue;
                    else if (settingsItem.ValueSet is SeparatedValueSet separatedValueSet)
                        value = separatedValueSet.DefaultValueIndex;

            foreach (var storageItem in StorageItems)
                if (storageItem.Identifier == identifier)
                    if (storageItem.IsSeparatedValue)
                        value = storageItem.Value;
                    else
                        value = storageItem.Value;

            return value;
        }

        public static void SetValue(string identifier, int value)
        {
            (int original, int current, string id) pair;

            if (!ChangeHistoryList.Exists(x => x.id == identifier))
            {
                pair = new ValueTuple<int, int, string>(GetValue(identifier), value, identifier);
                ChangeHistoryList.Add(pair);
            }
            else
            {
                pair = ChangeHistoryList.Find(x => x.id == identifier);
                pair.current = value;
            }

            ValueSet valueSet = null;

            foreach (var settings in SettingsItems.Settings)
            foreach (var item in settings.Items)
            {
                if (item.Identifier != identifier)
                    continue;

                valueSet = item.ValueSet;
            }

            var found = false;
            foreach (var storageItem in StorageItems)
                if (storageItem.Identifier == identifier)
                {
                    storageItem.Value = value;
                    found = true;
                }

            if (!found)
                StorageItems.Add(new StorageItem(identifier, value, valueSet is SeparatedValueSet));
        }

        public static void Save()
        {
            foreach (var pair in ChangeHistoryList)
                if (pair.original != pair.current)
                {
                    SettingsChanged?.Invoke();

                    break;
                }

            ChangeHistoryList.Clear();

            File.WriteAllText(SavePath, JsonConvert.SerializeObject(StorageItems));
        }

        private class StorageItem
        {
            public readonly string Identifier;
            public readonly bool IsSeparatedValue;
            public int Value;

            public StorageItem(string identifier, int value, bool isSeparatedValue)
            {
                Identifier = identifier;
                Value = value;
                IsSeparatedValue = isSeparatedValue;
            }
        }
    }
}