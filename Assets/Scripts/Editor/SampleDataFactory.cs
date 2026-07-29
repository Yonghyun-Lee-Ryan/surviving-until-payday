using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 2: 샘플 Job/Event ScriptableObject를 생성한다.
    /// </summary>
    public static class SampleDataFactory
    {
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string EventsFolder = "Assets/Data/Events";
        private const string TraitsFolder = "Assets/Data/Traits";

        [MenuItem("Tools/Surviving Until Payday/Create Sample Data (Unit 2/17)")]
        public static void CreateSampleData()
        {
            EnsureFolder(JobsFolder);
            EnsureFolder(EventsFolder);
            EnsureFolder(TraitsFolder);

            var job = CreateJuniorOfficeJob();
            var trait = CreateThriftyTrait();
            CreateHealthyTrait();
            CreatePositiveTrait();
            CreateOvertimeProTrait();
            var overtimeEvent = CreateOvertimeEvent();
            var phoneEvent = CreatePhoneCrackEvent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = overtimeEvent;
            EditorGUIUtility.PingObject(overtimeEvent);

            Debug.Log(
                "[SampleDataFactory] Sample data created.\n" +
                $"- Job: {AssetDatabase.GetAssetPath(job)}\n" +
                $"- Trait: {AssetDatabase.GetAssetPath(trait)}\n" +
                $"- Event: {AssetDatabase.GetAssetPath(overtimeEvent)}\n" +
                $"- Event: {AssetDatabase.GetAssetPath(phoneEvent)}\n" +
                "Inspector에서 OnValidate 경고가 없는지 확인하세요.");
        }

        [MenuItem("Tools/Surviving Until Payday/Validate All Data Assets")]
        public static void ValidateAllDataAssets()
        {
            var warningCount = 0;
            warningCount += ValidateAssets<JobData>("t:JobData");
            warningCount += ValidateAssets<TraitData>("t:TraitData");
            warningCount += ValidateAssets<EventData>("t:EventData");
            warningCount += ValidateAssets<EndingData>("t:EndingData");

            if (warningCount == 0)
            {
                Debug.Log("[SampleDataFactory] All data assets passed validation.");
            }
            else
            {
                Debug.LogWarning($"[SampleDataFactory] Validation finished with {warningCount} warning(s).");
            }
        }

        private static int ValidateAssets<T>(string filter) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets(filter, new[] { "Assets/Data" });
            var warnings = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                List<string> errors = asset switch
                {
                    JobData job => job.Validate(),
                    TraitData trait => trait.Validate(),
                    EventData eventData => eventData.Validate(),
                    EndingData ending => ending.Validate(),
                    _ => new List<string>()
                };

                foreach (var error in errors)
                {
                    warnings++;
                    Debug.LogWarning($"[{typeof(T).Name}:{asset.name}] {error}", asset);
                }
            }

            return warnings;
        }

        private static JobData CreateJuniorOfficeJob()
        {
            const string path = JobsFolder + "/Job_JuniorOffice.asset";
            var job = LoadOrCreate<JobData>(path);

            var so = new SerializedObject(job);
            so.FindProperty("id").stringValue = "job_junior_office";
            so.FindProperty("displayName").stringValue = "중소기업 신입사원";
            so.FindProperty("description").stringValue =
                "표준 난도의 직장 생활. 월급은 안정적이지만 생활비 부담도 만만치 않다.";
            so.FindProperty("salary").longValue = 2_800_000L;
            so.FindProperty("startingCash").longValue = 2_800_000L;
            so.FindProperty("startingHealth").intValue = 80;
            so.FindProperty("startingStress").intValue = 20;
            so.FindProperty("startingHappiness").intValue = 50;
            so.FindProperty("startingCompanyScore").intValue = 50;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(job);
            return job;
        }

        private static TraitData CreateThriftyTrait()
        {
            var trait = CreateTrait(
                "Trait_Thrifty.asset",
                "trait_thrifty",
                "짠돌이",
                "생활비 현금 감소를 5% 완화하고, 행복 획득은 줄어든다. 시작 현금 보너스.",
                0);
            SetStartingModifiers(
                trait,
                new StatEffect(StatType.Cash, 150_000L),
                new StatEffect(StatType.Happiness, -3));
            trait.EditorSetRuntimeMultipliers(0.95f, 0.5f, 1f);
            EditorUtility.SetDirty(trait);
            return trait;
        }

        private static TraitData CreateHealthyTrait()
        {
            var trait = CreateTrait(
                "Trait_Healthy.asset",
                "trait_healthy",
                "체력왕",
                "건강 최대치가 높아 체력이 높게 시작한다.",
                2);
            SetStartingModifiers(trait, new StatEffect(StatType.Health, 20));
            trait.EditorSetRuntimeMultipliers(1f, 1f, 1f);
            EditorUtility.SetDirty(trait);
            return trait;
        }

        private static TraitData CreatePositiveTrait()
        {
            var trait = CreateTrait(
                "Trait_Positive.asset",
                "trait_positive",
                "긍정왕",
                "긍정적인 마음으로 행복도가 높게 시작한다.",
                3);
            SetStartingModifiers(trait, new StatEffect(StatType.Happiness, 10));
            trait.EditorSetRuntimeMultipliers(1f, 1f, 1f);
            EditorUtility.SetDirty(trait);
            return trait;
        }

        private static TraitData CreateOvertimeProTrait()
        {
            var trait = CreateTrait(
                "Trait_OvertimePro.asset",
                "trait_overtime_pro",
                "야근 전문가",
                "야근(WORK)에서 스트레스 증가가 줄고, 시작 스트레스가 낮고 회사 평가가 높다.",
                4);
            SetStartingModifiers(
                trait,
                new StatEffect(StatType.Stress, -5),
                new StatEffect(StatType.CompanyScore, 5));
            trait.EditorSetRuntimeMultipliers(1f, 1f, 0.7f);
            EditorUtility.SetDirty(trait);
            return trait;
        }

        private static void SetStartingModifiers(TraitData trait, params StatEffect[] effects)
        {
            var so = new SerializedObject(trait);
            var prop = so.FindProperty("startingStatModifiers");
            prop.ClearArray();
            for (var i = 0; i < effects.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("statType").enumValueIndex = (int)effects[i].StatType;
                element.FindPropertyRelative("value").longValue = effects[i].Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trait);
        }

        private static TraitData CreateTrait(
            string fileName,
            string id,
            string displayName,
            string description,
            int unlockLevel)
        {
            var path = TraitsFolder + "/" + fileName;
            var trait = LoadOrCreate<TraitData>(path);
            trait.EditorSet(id, displayName, description, unlockLevel);
            EditorUtility.SetDirty(trait);
            return trait;
        }

        private static EventData CreateOvertimeEvent()
        {
            const string path = EventsFolder + "/Event_Overtime_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorConfigure(newMaxStress: 95, newMinCompanyScore: 0);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_overtime_do",
                    "야근하고 끝낸다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, -5),
                        new StatEffect(StatType.Stress, 12),
                        new StatEffect(StatType.Happiness, -5),
                        new StatEffect(StatType.CompanyScore, 10)
                    }),
                new EventChoiceData(
                    "choice_overtime_delay",
                    "내일 하겠다고 말한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 4),
                        new StatEffect(StatType.Happiness, 2),
                        new StatEffect(StatType.CompanyScore, -8)
                    }),
                new EventChoiceData(
                    "choice_overtime_help",
                    "동료에게 도움을 요청한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -15_000L),
                        new StatEffect(StatType.Health, -2),
                        new StatEffect(StatType.Stress, 3),
                        new StatEffect(StatType.Happiness, 1),
                        new StatEffect(StatType.CompanyScore, 3)
                    })
            };

            eventData.EditorSetCore(
                "event_overtime_001",
                "갑작스러운 야근",
                "퇴근 10분 전, 팀장이 오늘 안에 끝내야 하는 업무를 전달했다.",
                EventCategory.Work,
                2,
                27,
                100,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreatePhoneCrackEvent()
        {
            const string path = EventsFolder + "/Event_PhoneCrack_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_phone_official",
                    "공식 서비스센터",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -280_000L),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "choice_phone_private",
                    "사설 수리점",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -110_000L)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "phone_ok",
                            "정상적으로 수리되었다.",
                            70),
                        new RandomOutcome(
                            "phone_fail_again",
                            "며칠 후 다시 고장 났다.",
                            20,
                            new StatEffect(StatType.Stress, 8)),
                        new RandomOutcome(
                            "phone_data_loss",
                            "수리는 됐지만 데이터가 날아갔다.",
                            10,
                            new StatEffect(StatType.Happiness, -10),
                            new StatEffect(StatType.Stress, 5))
                    }),
                new EventChoiceData(
                    "choice_phone_ignore",
                    "그냥 사용한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 10),
                        new StatEffect(StatType.Happiness, -5)
                    })
            };

            eventData.EditorSetCore(
                "event_phone_crack_001",
                "휴대전화 액정 파손",
                "주머니에서 꺼낸 휴대전화 액정이 심하게 금이 가 있다.",
                EventCategory.Accident,
                3,
                28,
                80,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                Debug.LogError($"[SampleDataFactory] Invalid folder path: {assetPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
