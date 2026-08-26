using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-05: 신규→Lv2/3/4 직업·특성 해금 시나리오. 상점은 사용하지 않는다.
    /// batch: -executeMethod SurviveUntilPayday.EditorTools.MetaUnlockScenarioRunner.RunFromBatch
    /// </summary>
    public static class MetaUnlockScenarioRunner
    {
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string TraitsFolder = "Assets/Data/Traits";

        [MenuItem("Tools/Surviving Until Payday/Run Meta Unlock Scenario (R-QA-05)")]
        public static void RunFromMenu()
        {
            var path = RunAndSaveReport();
            Debug.Log($"[R-QA-05] 해금 시나리오 완료. {path}");
        }

        public static void RunFromBatch()
        {
            try
            {
                AchievementPackFactory.CreatePack();
                var path = RunAndSaveReport();
                Debug.Log($"[R-QA-05] batch OK. {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[R-QA-05] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static string RunAndSaveReport()
        {
            AchievementPackFactory.CreatePack();

            var jobs = LoadAll<JobData>(JobsFolder);
            var traits = LoadAll<TraitData>(TraitsFolder);
            if (jobs.Count < 3 || traits.Count < 4)
            {
                throw new InvalidOperationException(
                    "[R-QA-05] 직업 3·특성 4 에셋을 찾지 못했습니다.");
            }

            var civil = FindJob(jobs, "job_civil_prep");
            var freelancer = FindJob(jobs, "job_freelancer");
            var healthy = FindTrait(traits, "trait_healthy");
            var positive = FindTrait(traits, "trait_positive");
            var overtime = FindTrait(traits, "trait_overtime_pro");
            if (civil == null || freelancer == null || healthy == null || positive == null || overtime == null)
            {
                throw new InvalidOperationException("[R-QA-05] 해금 대상 직업/특성 id가 없습니다.");
            }

            var report = new StringBuilder();
            report.AppendLine("R-QA-05 Meta Unlock Scenario");
            report.AppendLine($"Unity {Application.unityVersion}");
            report.AppendLine($"When {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("Shop/IAP: not used");
            report.AppendLine($"Achievement SO: {AchievementCatalog.ResourceCount}");
            report.AppendLine();

            var failures = new List<string>();
            var meta = new MetaProgressionManager();
            meta.RefreshUnlocksFromLevel(traits, jobs);

            AppendSnapshot(report, "신규 (0 XP)", meta, jobs, traits);
            ExpectLocked(failures, "신규 공무원", meta, civil, false);
            ExpectLockedTrait(failures, "신규 체력왕", meta, healthy, false);
            var nextGoal = MetaGrowthHint.BuildNextGoal(meta, jobs, traits);
            report.AppendLine($"  UI 다음 목표: {nextGoal}");
            if (nextGoal.IndexOf("Lv.2", StringComparison.Ordinal) < 0
                || nextGoal.IndexOf("100", StringComparison.Ordinal) < 0)
            {
                failures.Add($"신규 다음 목표가 Lv.2·100 XP가 아님: {nextGoal}");
            }

            GrantToLevel(meta, jobs, traits, targetXp: 100);
            AppendSnapshot(report, "Lv.2 (누적 100 XP) — 공무원 준비생·체력왕", meta, jobs, traits);
            ExpectLocked(failures, "Lv2 공무원", meta, civil, true);
            ExpectLockedTrait(failures, "Lv2 체력왕", meta, healthy, true);
            ExpectLocked(failures, "Lv2 프리랜서 아직", meta, freelancer, false);

            GrantToLevel(meta, jobs, traits, targetXp: 300);
            AppendSnapshot(report, "Lv.3 (누적 300 XP) — 프리랜서·긍정왕", meta, jobs, traits);
            ExpectLocked(failures, "Lv3 프리랜서", meta, freelancer, true);
            ExpectLockedTrait(failures, "Lv3 긍정왕", meta, positive, true);
            ExpectLockedTrait(failures, "Lv3 야근 아직", meta, overtime, false);

            GrantToLevel(meta, jobs, traits, targetXp: 600);
            AppendSnapshot(report, "Lv.4 (누적 600 XP) — 야근전문가", meta, jobs, traits);
            ExpectLockedTrait(failures, "Lv4 야근전문가", meta, overtime, true);

            report.AppendLine();
            report.AppendLine("--- 출석 스트릭 (상점 없음) ---");
            var dailyMeta = new MetaProgressionManager();
            var dummyMission = ScriptableObject.CreateInstance<DailyMissionData>();
            dummyMission.Configure(
                "scenario_dummy",
                "더미",
                "",
                DailyMissionGoalType.SurviveMinDays,
                0,
                1,
                null,
                0,
                0);
            var pool = new[] { dummyMission };
            dailyMeta.Daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 23, 10, 0, 0));
            var day1 = dailyMeta.Daily.TryGrantVisitBonus(dailyMeta, traits, jobs);
            report.AppendLine($"  8/23 출석: streak={dailyMeta.Daily.LoginStreak} +{day1} XP");
            dailyMeta.Daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 24, 10, 0, 0));
            var day2 = dailyMeta.Daily.TryGrantVisitBonus(dailyMeta, traits, jobs);
            report.AppendLine($"  8/24 출석: streak={dailyMeta.Daily.LoginStreak} +{day2} XP");
            dailyMeta.Daily.EnsureForLocalDate(pool, new DateTime(2026, 8, 26, 10, 0, 0));
            var skip = dailyMeta.Daily.TryGrantVisitBonus(dailyMeta, traits, jobs);
            report.AppendLine($"  8/26 결석 후: streak={dailyMeta.Daily.LoginStreak} +{skip} XP (리셋 기대 1일/+5)");
            if (dailyMeta.Daily.LoginStreak != 1 || skip != DailyContentState.VisitBonusXpPerStreakDay)
            {
                failures.Add(
                    $"스트릭 결석 리셋 실패 streak={dailyMeta.Daily.LoginStreak} xp={skip}");
            }

            if (day1 != 5 || day2 != 10)
            {
                failures.Add($"연속 출석 XP 기대 5/10, 실제 {day1}/{day2}");
            }

            report.AppendLine();
            if (failures.Count == 0)
            {
                report.AppendLine("RESULT: PASS");
            }
            else
            {
                report.AppendLine("RESULT: FAIL");
                for (var i = 0; i < failures.Count; i++)
                {
                    report.AppendLine("  - " + failures[i]);
                }
            }

            var logsDir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDir);
            var fileName = $"meta_unlock_scenario_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(logsDir, fileName);
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "[R-QA-05] 해금 시나리오 실패:\n" + string.Join("\n", failures));
            }

            return path;
        }

        private static void GrantToLevel(
            MetaProgressionManager meta,
            IReadOnlyList<JobData> jobs,
            IReadOnlyList<TraitData> traits,
            int targetXp)
        {
            var need = targetXp - meta.TotalExperience;
            if (need > 0)
            {
                meta.AddBonusExperience(need, traits, jobs);
            }
            else
            {
                meta.RefreshUnlocksFromLevel(traits, jobs);
            }
        }

        private static void AppendSnapshot(
            StringBuilder report,
            string title,
            MetaProgressionManager meta,
            IReadOnlyList<JobData> jobs,
            IReadOnlyList<TraitData> traits)
        {
            report.AppendLine($"--- {title} ---");
            report.AppendLine($"  Lv.{meta.Level}  XP={meta.TotalExperience}");
            report.AppendLine(
                $"  직업 {CountUnlockedJobs(meta, jobs)}/{jobs.Count}  특성 {CountUnlockedTraits(meta, traits)}/{traits.Count}  업적 {meta.Achievements.UnlockedCount}/{AchievementIds.CatalogCount}");
            report.AppendLine($"  {MetaGrowthHint.BuildNextGoal(meta, jobs, traits)}");
        }

        private static int CountUnlockedJobs(MetaProgressionManager meta, IReadOnlyList<JobData> jobs)
        {
            var n = 0;
            for (var i = 0; i < jobs.Count; i++)
            {
                if (meta.IsJobUnlocked(jobs[i]))
                {
                    n++;
                }
            }

            return n;
        }

        private static int CountUnlockedTraits(MetaProgressionManager meta, IReadOnlyList<TraitData> traits)
        {
            var n = 0;
            for (var i = 0; i < traits.Count; i++)
            {
                if (meta.IsTraitUnlocked(traits[i]))
                {
                    n++;
                }
            }

            return n;
        }

        private static void ExpectLocked(
            List<string> failures,
            string label,
            MetaProgressionManager meta,
            JobData job,
            bool shouldUnlock)
        {
            var unlocked = meta.IsJobUnlocked(job);
            if (unlocked != shouldUnlock)
            {
                failures.Add($"{label}: unlocked={unlocked} expected={shouldUnlock}");
            }
        }

        private static void ExpectLockedTrait(
            List<string> failures,
            string label,
            MetaProgressionManager meta,
            TraitData trait,
            bool shouldUnlock)
        {
            var unlocked = meta.IsTraitUnlocked(trait);
            if (unlocked != shouldUnlock)
            {
                failures.Add($"{label}: unlocked={unlocked} expected={shouldUnlock}");
            }
        }

        private static JobData FindJob(List<JobData> jobs, string id)
        {
            for (var i = 0; i < jobs.Count; i++)
            {
                if (jobs[i] != null && jobs[i].Id == id)
                {
                    return jobs[i];
                }
            }

            return null;
        }

        private static TraitData FindTrait(List<TraitData> traits, string id)
        {
            for (var i = 0; i < traits.Count; i++)
            {
                if (traits[i] != null && traits[i].Id == id)
                {
                    return traits[i];
                }
            }

            return null;
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
