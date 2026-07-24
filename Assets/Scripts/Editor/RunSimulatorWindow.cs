using System.Collections.Generic;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.DebugTools;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 11: 30일 회차 자동 시뮬레이터 Editor 창.
    /// </summary>
    public sealed class RunSimulatorWindow : EditorWindow
    {
        private int iterations = 100;
        private int baseSeed = 1;
        private SimulatorChoicePolicy policy = SimulatorChoicePolicy.Random;
        private JobData job;
        private TraitData trait;
        private EventData fallbackEvent;
        private EndingData fallbackEnding;
        private Vector2 scroll;
        private string lastReport = string.Empty;

        [MenuItem("Tools/Surviving Until Payday/Run Simulator Window")]
        public static void Open()
        {
            var window = GetWindow<RunSimulatorWindow>("Run Simulator");
            window.minSize = new Vector2(420f, 480f);
            window.TryAutoAssign();
            window.Show();
        }

        private void OnEnable()
        {
            TryAutoAssign();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("30일 자동 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "지정 횟수만큼 회차를 자동 실행하고 성공률·평균 생존일·실패 원인·평균 잔액을 출력합니다.",
                MessageType.Info);

            iterations = EditorGUILayout.IntField("Iterations", iterations);
            baseSeed = EditorGUILayout.IntField("Base Seed", baseSeed);
            policy = (SimulatorChoicePolicy)EditorGUILayout.EnumPopup(
                new GUIContent("Choice Policy", "Random / Safe(안전) / Thrifty(절약) / Risky(위험)"),
                policy);

            job = (JobData)EditorGUILayout.ObjectField("Job", job, typeof(JobData), false);
            trait = (TraitData)EditorGUILayout.ObjectField("Trait", trait, typeof(TraitData), false);
            fallbackEvent = (EventData)EditorGUILayout.ObjectField(
                "Fallback Event",
                fallbackEvent,
                typeof(EventData),
                false);
            fallbackEnding = (EndingData)EditorGUILayout.ObjectField(
                "Fallback Ending",
                fallbackEnding,
                typeof(EndingData),
                false);

            using (new EditorGUI.DisabledScope(!CanRun()))
            {
                if (GUILayout.Button("Run Simulation", GUILayout.Height(36f)))
                {
                    Run();
                }

                if (GUILayout.Button("Run 1,000 (파일 저장)", GUILayout.Height(28f)))
                {
                    iterations = 1000;
                    Run();
                }
            }

            if (GUILayout.Button("Load Sample Assets"))
            {
                TryAutoAssign();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastReport, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private bool CanRun()
        {
            return iterations > 0 && job != null && fallbackEvent != null;
        }

        private void Run()
        {
            var events = LoadAll<EventData>("Assets/Data/Events");
            if (events.Count == 0 && fallbackEvent != null)
            {
                events.Add(fallbackEvent);
            }

            var endings = LoadAll<EndingData>("Assets/Data/Endings");
            var simulator = new RunSimulator(
                job,
                trait,
                events,
                fallbackEvent,
                endings,
                fallbackEnding);

            var summary = simulator.Run(iterations, baseSeed, policy);
            lastReport = summary.ToString();

            var logsDir = System.IO.Path.Combine(Application.dataPath, "..", "Logs");
            var savedPath = summary.WriteToFile(logsDir);
            lastReport += "\n\nSaved: " + savedPath;

            Debug.Log("[RunSimulator]\n" + lastReport);
            Repaint();
        }

        private void TryAutoAssign()
        {
            if (job == null)
            {
                job = AssetDatabase.LoadAssetAtPath<JobData>("Assets/Data/Jobs/Job_JuniorOffice.asset");
            }

            if (trait == null)
            {
                trait = AssetDatabase.LoadAssetAtPath<TraitData>("Assets/Data/Traits/Trait_Thrifty.asset");
            }

            if (fallbackEvent == null)
            {
                fallbackEvent = AssetDatabase.LoadAssetAtPath<EventData>(
                    "Assets/Data/Events/Event_Rest_Fallback.asset");
            }

            if (fallbackEnding == null)
            {
                fallbackEnding = AssetDatabase.LoadAssetAtPath<EndingData>(
                    "Assets/Data/Endings/Ending_BarelySurvived.asset");
                if (fallbackEnding == null)
                {
                    var endings = LoadAll<EndingData>("Assets/Data/Endings");
                    for (var i = 0; i < endings.Count; i++)
                    {
                        if (endings[i] != null && !endings[i].IsFailureEnding)
                        {
                            fallbackEnding = endings[i];
                            break;
                        }
                    }
                }
            }
        }

        private static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return list;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }
    }
}
