using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 22: Placeholder WAV + Resources 로드 경로.
    /// </summary>
    public static class AudioPipelineSetup
    {
        private const string ResourcesAudioFolder = "Assets/Resources/Audio";

        private static readonly string[] ClipNames =
        {
            "bgm_main",
            "bgm_play",
            "bgm_crisis",
            "bgm_result",
            "sfx_click",
            "sfx_cash_gain",
            "sfx_cash_loss",
            "sfx_stress_up",
            "sfx_success",
            "sfx_fail",
            "sfx_payday"
        };

        [MenuItem("Tools/Surviving Until Payday/Setup Audio Pipeline (Unit 22)")]
        public static void Setup()
        {
            EnsureFolder("Assets/Audio");
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResourcesAudioFolder);

            foreach (var name in ClipNames)
            {
                var isBgm = name.StartsWith("bgm_");
                var path = $"{ResourcesAudioFolder}/{name}.wav";
                if (!File.Exists(path))
                {
                    WritePlaceholderWav(path, isBgm ? 440f : 880f, isBgm ? 0.35f : 0.08f);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log(
                "[AudioPipelineSetup] Resources/Audio placeholder WAV 준비 완료.\n" +
                "런타임은 UnityAudioService.TryLoadPlaceholdersFromResources()로 로드합니다.\n" +
                "실에셋은 Docs/AudioPipeline.md 경로에 교체하세요.");
        }

        private static void WritePlaceholderWav(string assetPath, float frequencyHz, float durationSeconds)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
            var data = new short[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - (t / durationSeconds);
                var sample = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * envelope * 0.25f;
                data[i] = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            }

            using (var stream = new FileStream(assetPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                var byteCount = data.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + byteCount);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(byteCount);
                for (var i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
