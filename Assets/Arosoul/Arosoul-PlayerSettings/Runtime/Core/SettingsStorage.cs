using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>Where setting values are persisted. Swap the implementation to use cloud saves etc.</summary>
    public interface ISettingsStorage {
        /// <returns>False if nothing has been saved yet.</returns>
        bool TryLoad(Dictionary<string, string> values);
        void Save(IReadOnlyDictionary<string, string> values);
    }

    /// <summary>Stores every value as a string in a small JSON file in the persistent data path.</summary>
    public sealed class JsonFileSettingsStorage : ISettingsStorage {
        [Serializable] sealed class Entry { public string id; public string value; }
        [Serializable] sealed class FileData { public int version = 1; public List<Entry> entries = new List<Entry>(); }

        readonly string path;

        public JsonFileSettingsStorage(string fileName = "player-settings.json") {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public bool TryLoad(Dictionary<string, string> values) {
            if (!File.Exists(path)) return false;

            try {
                var data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
                if (data?.entries == null) return false;
                foreach (var e in data.entries) {
                    if (!string.IsNullOrEmpty(e.id)) values[e.id] = e.value ?? string.Empty;
                }
                return true;
            }
            catch (Exception e) {
                Debug.LogWarning($"[Settings] Could not read '{path}', using defaults. {e.Message}");
                return false;
            }
        }

        public void Save(IReadOnlyDictionary<string, string> values) {
            var data = new FileData();
            foreach (var kv in values) data.entries.Add(new Entry { id = kv.Key, value = kv.Value });

            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }

}
