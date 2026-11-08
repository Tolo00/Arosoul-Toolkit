using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Arosoul.Essentials {

    public interface ISaveableData { }

    public enum SaveFormat {
        Compact,
        Pretty,
        Encrypted
    }

    public class DataSaver<T> where T : ISaveableData {
        public readonly string FilePath;
        public readonly SaveFormat Format;

        private const string KEY = "5210376";

        public DataSaver(string fileName, SaveFormat format = SaveFormat.Compact) {
            FilePath = Path.Combine(Application.persistentDataPath, fileName);
            this.Format = format;
        }

        public bool Exists => File.Exists(FilePath);

        /// <summary>
        /// Save the data to file synchronously.
        /// </summary>
        public virtual void Save(T data) {
            string json = Serialize(data);
            File.WriteAllText(FilePath, json);
        }

        /// <summary>
        /// Save the data to file asynchronously.
        /// </summary>
        public virtual async Task SaveAsync(T data) {
            string json = Serialize(data);
            byte[] encoded = Encoding.UTF8.GetBytes(json);

            using FileStream stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await stream.WriteAsync(encoded, 0, encoded.Length);
        }

        /// <summary>
        /// Load the data synchronously.
        /// </summary>
        public virtual bool Load(out T data) {
            data = default;
            if (!Exists) {
                return false;
            }

            string file = File.ReadAllText(FilePath);
            data = Deserialize(file);
            return true;
        }

        /// <summary>
        /// Load the data asynchronously.
        /// </summary>
        public virtual async Task<T> LoadAsync() {
            if (!Exists) return default;

            using FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            string data = await reader.ReadToEndAsync();
            return Deserialize(data);
        }

        /// <summary>
        /// Delete the save file.
        /// </summary>
        public void Delete() {
            if (Exists) {
                File.Delete(FilePath);
            }
        }

        private string Serialize(T data) {
            string json = JsonUtility.ToJson(data, Format == SaveFormat.Pretty);
            if (Format == SaveFormat.Encrypted) {
                json = EncryptDecrypt(json, KEY);
            }
            return json;
        }

        private T Deserialize(string json) {
            if (Format == SaveFormat.Encrypted) {
                json = EncryptDecrypt(json, KEY);
            }
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// Basic XOR encryption. Not safe for sensitive data.
        /// </summary>
        public static string EncryptDecrypt(string text, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var data = Encoding.UTF8.GetBytes(text);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= keyBytes[i % keyBytes.Length];
            }

            return Encoding.UTF8.GetString(data);
        }
    }

}
