using System;
using System.IO;
using UnityEngine;

namespace SurviveUntilPayday.Save
{
    /// <summary>
    /// persistentDataPath JSON 파일 저장.
    /// </summary>
    public sealed class JsonFileSaveService : ISaveService
    {
        public const string DefaultFileName = "survive_until_payday_save.json";

        private readonly string filePath;

        public JsonFileSaveService(string fileName = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName is required.", nameof(fileName));
            }

            filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string FilePath => filePath;

        public bool Exists()
        {
            return File.Exists(filePath);
        }

        public string ReadAllText()
        {
            if (!Exists())
            {
                return null;
            }

            return File.ReadAllText(filePath);
        }

        public void WriteAllText(string contents)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, contents);
        }

        public void Delete()
        {
            if (Exists())
            {
                File.Delete(filePath);
            }
        }
    }
}
