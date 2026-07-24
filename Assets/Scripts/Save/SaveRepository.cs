using System;
using UnityEngine;

namespace SurviveUntilPayday.Save
{
    /// <summary>
    /// JSON 직렬화/역직렬화와 손상 데이터 복구.
    /// </summary>
    public sealed class SaveRepository
    {
        private readonly ISaveService saveService;

        public SaveRepository(ISaveService saveService)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public SaveData LoadOrCreate()
        {
            if (!saveService.Exists())
            {
                return CreateDefault();
            }

            try
            {
                var json = saveService.ReadAllText();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return RecoverWithDefaults("Empty save file.");
                }

                json = StripBom(json).Trim();
                if (string.IsNullOrEmpty(json))
                {
                    return RecoverWithDefaults("Save file contained only whitespace.");
                }

                SaveData data;
                try
                {
                    data = JsonUtility.FromJson<SaveData>(json);
                }
                catch (ArgumentException ex)
                {
                    return RecoverWithDefaults($"JSON parse error: {ex.Message}");
                }

                if (data == null)
                {
                    return RecoverWithDefaults("Failed to parse save JSON.");
                }

                var originalVersion = data.version;
                var normalized = Normalize(data);

                // 버전 마이그레이션·필드 보정 결과를 즉시 기록해 다음 로드에서 경고가 반복되지 않게 한다.
                if (originalVersion != SaveVersion.Current)
                {
                    Save(normalized);
                    Debug.Log(
                        $"[SaveRepository] Migrated save version {originalVersion} -> {SaveVersion.Current} and rewrote file.");
                }

                return normalized;
            }
            catch (Exception ex)
            {
                return RecoverWithDefaults(ex.Message);
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var normalized = Normalize(data);
            normalized.version = SaveVersion.Current;
            var json = JsonUtility.ToJson(normalized, prettyPrint: true);
            saveService.WriteAllText(json);
        }

        public void ClearRunAndSave(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.run = new RunSaveData();
            Save(data);
        }

        public static SaveData CreateDefault()
        {
            return Normalize(new SaveData());
        }

        public static SaveData Normalize(SaveData data)
        {
            if (data == null)
            {
                return CreateDefault();
            }

            if (data.version <= 0)
            {
                data.version = SaveVersion.Current;
            }

            if (data.version != SaveVersion.Current)
            {
                data.version = SaveVersion.Current;
            }

            data.run ??= new RunSaveData();
            data.meta ??= new MetaSaveData();
            data.run.recentEventIds ??= new System.Collections.Generic.List<string>();
            data.run.runFlags ??= new System.Collections.Generic.List<string>();
            data.run.queuedEventIds ??= new System.Collections.Generic.List<string>();
            data.meta.unlockedEndingIds ??= new System.Collections.Generic.List<string>();
            data.meta.unlockedEventIds ??= new System.Collections.Generic.List<string>();
            data.meta.unlockedTraitIds ??= new System.Collections.Generic.List<string>();
            data.meta.unlockedAchievementIds ??= new System.Collections.Generic.List<string>();
            data.run.jobId ??= string.Empty;
            data.run.traitId ??= string.Empty;
            data.run.lastSelectedEventId ??= string.Empty;
            data.run.pendingEventId ??= string.Empty;

            if (data.run.currentDay < 1)
            {
                data.run.currentDay = 1;
            }

            return data;
        }

        private SaveData RecoverWithDefaults(string reason)
        {
            var defaults = CreateDefault();
            try
            {
                Save(defaults);
                Debug.Log($"[SaveRepository] Recovered corrupt/empty save and rewrote defaults. {reason}");
            }
            catch (Exception saveEx)
            {
                Debug.LogWarning(
                    $"[SaveRepository] Recovered corrupt save with in-memory defaults, but rewrite failed. {reason} | {saveEx.Message}");
            }

            return defaults;
        }

        private static string StripBom(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text[0] == '\uFEFF')
            {
                return text.Substring(1);
            }

            return text;
        }
    }
}
