using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 샘플 데이터 ID 중복, Scene UI/Presenter 중복 배치를 정리한다.
    /// </summary>
    public static class DuplicateCleanupTool
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Result.unity",
            "Assets/Scenes/Bootstrap.unity"
        };

        private static readonly string[] DuplicateUiNames =
        {
            "HUD",
            "EventPanel",
            "ChoicePanel",
            "ResultPopup",
            "CodexPanel",
            "ContinueButton",
            "GamePlayPresenter",
            "ResultPresenter",
            "DebugPanel",
            "DebugHint",
            "Title",
            "SceneLabel",
            "ActionButton"
        };

        [MenuItem("Tools/Surviving Until Payday/Cleanup Duplicates (Data + UI)")]
        public static void CleanupAll()
        {
            var report = new StringBuilder();
            report.AppendLine("[DuplicateCleanupTool] Start");

            var dataRemoved = CleanupDuplicateDataAssets(report);
            var uiRemoved = CleanupDuplicateSceneUi(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine($"Done. Removed data={dataRemoved}, ui/objects={uiRemoved}");
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog(
                "Duplicate Cleanup",
                $"데이터 중복 {dataRemoved}개, UI/오브젝트 중복 {uiRemoved}개 제거.\n자세한 내용은 Console 로그를 확인하세요.",
                "OK");
        }

        [MenuItem("Tools/Surviving Until Payday/Cleanup Duplicates/Data Assets Only")]
        public static void CleanupDataOnly()
        {
            var report = new StringBuilder();
            var count = CleanupDuplicateDataAssets(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.AppendLine($"Removed {count} duplicate data assets.");
            Debug.Log(report.ToString());
        }

        [MenuItem("Tools/Surviving Until Payday/Cleanup Duplicates/Scene UI Only")]
        public static void CleanupUiOnly()
        {
            var report = new StringBuilder();
            var count = CleanupDuplicateSceneUi(report);
            report.AppendLine($"Removed {count} duplicate scene objects.");
            Debug.Log(report.ToString());
        }

        private static int CleanupDuplicateDataAssets(StringBuilder report)
        {
            var removed = 0;
            removed += CleanupById<JobData>("t:JobData", asset => asset.Id, report);
            removed += CleanupById<TraitData>("t:TraitData", asset => asset.Id, report);
            removed += CleanupById<EventData>("t:EventData", asset => asset.Id, report);
            removed += CleanupById<EndingData>("t:EndingData", asset => asset.Id, report);
            return removed;
        }

        private static int CleanupById<T>(
            string filter,
            Func<T, string> idSelector,
            StringBuilder report) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets(filter, new[] { "Assets/Data" });
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                var id = idSelector(asset);
                if (string.IsNullOrWhiteSpace(id))
                {
                    report.AppendLine($"- Skip empty id: {path}");
                    continue;
                }

                if (!groups.TryGetValue(id, out var list))
                {
                    list = new List<string>();
                    groups[id] = list;
                }

                list.Add(path);
            }

            var removed = 0;
            foreach (var pair in groups)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                var ordered = pair.Value
                    .OrderBy(p => ScorePath(p))
                    .ThenBy(p => p, StringComparer.Ordinal)
                    .ToList();

                var keep = ordered[0];
                report.AppendLine($"- Keep {typeof(T).Name} id='{pair.Key}' => {keep}");

                for (var i = 1; i < ordered.Count; i++)
                {
                    var path = ordered[i];
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        removed++;
                        report.AppendLine($"  Delete duplicate: {path}");
                    }
                    else
                    {
                        report.AppendLine($"  Failed delete: {path}");
                    }
                }
            }

            return removed;
        }

        private static int ScorePath(string path)
        {
            // 정식 경로/이름일수록 우선 보존
            var score = 0;
            if (path.Contains("/Jobs/")
                || path.Contains("/Traits/")
                || path.Contains("/Events/")
                || path.Contains("/Endings/"))
            {
                score -= 10;
            }

            if (path.Contains(" 1.") || path.Contains("_1.") || path.Contains(" copy", StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            score += path.Length / 100;
            return score;
        }

        private static int CleanupDuplicateSceneUi(StringBuilder report)
        {
            var removed = 0;
            var activeScene = SceneManager.GetActiveScene().path;

            foreach (var scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var sceneRemoved = 0;

                sceneRemoved += RemoveDuplicateRootsByName(scene, DuplicateUiNames, report);
                sceneRemoved += RemoveDuplicateComponentsKeepOne<GamePlayPresenter>(scene, report);
                sceneRemoved += RemoveDuplicateComponentsKeepOne<ResultPresenter>(scene, report);
                sceneRemoved += RemoveDuplicateComponentsKeepOne<MainMenuController>(scene, report);
                sceneRemoved += RemoveDuplicateComponentsKeepOne<CodexPanelView>(scene, report);
                sceneRemoved += RemoveDuplicateComponentsKeepOne<SurviveUntilPayday.DebugTools.DebugPanel>(
                    scene,
                    report);
                sceneRemoved += RemoveDuplicateNamedUnderSafeArea(scene, report);

                if (sceneRemoved > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                removed += sceneRemoved;
                report.AppendLine($"Scene {scenePath}: removed {sceneRemoved}");
            }

            if (!string.IsNullOrEmpty(activeScene) && System.IO.File.Exists(activeScene))
            {
                EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
            }

            return removed;
        }

        private static int RemoveDuplicateRootsByName(Scene scene, string[] names, StringBuilder report)
        {
            var removed = 0;
            var roots = scene.GetRootGameObjects();
            foreach (var name in names)
            {
                var matches = new List<GameObject>();
                foreach (var root in roots)
                {
                    CollectByName(root.transform, name, matches);
                }

                if (matches.Count <= 1)
                {
                    continue;
                }

                // 계층이 깊은 쪽(정상 UI 트리)을 우선 보존
                matches = matches
                    .OrderByDescending(Depth)
                    .ThenBy(go => go.transform.GetSiblingIndex())
                    .ToList();

                report.AppendLine($"- Keep '{name}' => {GetPath(matches[0].transform)}");
                for (var i = 1; i < matches.Count; i++)
                {
                    report.AppendLine($"  Delete '{name}' => {GetPath(matches[i].transform)}");
                    UnityEngine.Object.DestroyImmediate(matches[i]);
                    removed++;
                }
            }

            return removed;
        }

        private static int RemoveDuplicateNamedUnderSafeArea(Scene scene, StringBuilder report)
        {
            var removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                var safe = root.transform.Find("SafeArea")
                           ?? FindDeep(root.transform, "SafeArea");
                if (safe == null)
                {
                    continue;
                }

                var groups = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
                for (var i = 0; i < safe.childCount; i++)
                {
                    var child = safe.GetChild(i);
                    if (!groups.TryGetValue(child.name, out var list))
                    {
                        list = new List<Transform>();
                        groups[child.name] = list;
                    }

                    list.Add(child);
                }

                foreach (var pair in groups)
                {
                    if (pair.Value.Count <= 1)
                    {
                        continue;
                    }

                    report.AppendLine($"- SafeArea keep '{pair.Key}'");
                    for (var i = 1; i < pair.Value.Count; i++)
                    {
                        report.AppendLine($"  Delete SafeArea child '{pair.Key}'");
                        UnityEngine.Object.DestroyImmediate(pair.Value[i].gameObject);
                        removed++;
                    }
                }
            }

            return removed;
        }

        private static int RemoveDuplicateComponentsKeepOne<T>(Scene scene, StringBuilder report)
            where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            if (found == null || found.Length <= 1)
            {
                return 0;
            }

            var ordered = found
                .OrderByDescending(c => Depth(c.gameObject))
                .ThenBy(c => c.transform.GetSiblingIndex())
                .ToList();

            report.AppendLine($"- Keep {typeof(T).Name} => {GetPath(ordered[0].transform)}");
            var removed = 0;
            for (var i = 1; i < ordered.Count; i++)
            {
                var go = ordered[i].gameObject;
                // 전용 오브젝트면 오브젝트 삭제, 아니면 컴포넌트만 제거
                var onlyThis = go.GetComponents<Component>().Length <= 2; // Transform + T
                report.AppendLine(
                    onlyThis
                        ? $"  Delete {typeof(T).Name} object => {GetPath(go.transform)}"
                        : $"  Remove {typeof(T).Name} component => {GetPath(go.transform)}");

                if (onlyThis)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(ordered[i]);
                }

                removed++;
            }

            return removed;
        }

        private static void CollectByName(Transform root, string name, List<GameObject> results)
        {
            if (root.name == name)
            {
                results.Add(root.gameObject);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                CollectByName(root.GetChild(i), name, results);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static int Depth(GameObject go)
        {
            var depth = 0;
            var t = go.transform;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }

            return depth;
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }

            return string.Join("/", stack);
        }
    }
}
