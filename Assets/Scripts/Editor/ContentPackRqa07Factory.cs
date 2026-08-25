using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// R-QA-07: 직업 +1 · 특성 +5 · 관계 플래그 연쇄 사건. 상점은 사용하지 않는다.
    /// </summary>
    public static class ContentPackRqa07Factory
    {
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string TraitsFolder = "Assets/Data/Traits";
        private const string EventsFolder = "Assets/Data/Events";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        public const string CorpJobId = "job_corp_associate";

        [MenuItem("Tools/Surviving Until Payday/Create Content Pack (R-QA-07)")]
        public static void CreateContentPack()
        {
            EnsureFolder(JobsFolder);
            EnsureFolder(TraitsFolder);
            EnsureFolder(EventsFolder);

            CreateCorpJob();
            CreateTraits();
            var created = new List<EventData>();
            created.AddRange(CreateRelationshipEvents());
            created.AddRange(CreateCorpEvents());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var warnings = 0;
            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] == null)
                {
                    continue;
                }

                var errors = created[i].Validate();
                for (var e = 0; e < errors.Count; e++)
                {
                    warnings++;
                    Debug.LogWarning($"[ContentPackRqa07:{created[i].name}] {errors[e]}", created[i]);
                }
            }

            Debug.Log(
                $"[ContentPackRqa07] 직업 1 + 특성 5 + 사건 {created.Count}개. 경고={warnings}. " +
                "Wire Content Pack To Scenes (R-QA-07)를 실행하세요. 상점 없음.");
        }

        [MenuItem("Tools/Surviving Until Payday/Wire Content Pack To Scenes (R-QA-07)")]
        public static void WireToScenes()
        {
            WireGameScene();
            WireMainMenuCatalogs();
        }

        public static void RunFromBatch()
        {
            try
            {
                CreateContentPack();
                WireToScenes();
                Debug.Log("[ContentPackRqa07] batch OK.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContentPackRqa07] batch FAIL: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static void CreateCorpJob()
        {
            var path = $"{JobsFolder}/Job_CorpAssociate.asset";
            var job = LoadOrCreate<JobData>(path);
            job.EditorSet(
                CorpJobId,
                "대기업 사원",
                "월급은 두껍지만 야근·정치·고과가 일상을 잠식한다. 해금 Lv.5.",
                5,
                3_500_000L,
                3_200_000L,
                78,
                32,
                48,
                58);
            EditorUtility.SetDirty(job);
        }

        private static void CreateTraits()
        {
            UpsertTrait(
                "Trait_Networker.asset",
                "trait_networker",
                "인맥왕",
                "사람을 잘 챙긴다. 시작 행복이 높고 현금은 조금 빠듯하다.",
                5,
                1f,
                1f,
                1f,
                new StatEffect(StatType.Happiness, 6),
                new StatEffect(StatType.Cash, -80_000L));
            UpsertTrait(
                "Trait_NightOwl.asset",
                "trait_night_owl",
                "올빼미",
                "야근 스트레스가 줄지만 시작 스트레스가 높다.",
                5,
                1f,
                1f,
                0.85f,
                new StatEffect(StatType.Stress, 8));
            UpsertTrait(
                "Trait_PeoplePleaser.asset",
                "trait_people_pleaser",
                "착한 사람",
                "거절을 못 해서 행복 획득은 늘고, 지출 타격이 조금 크다.",
                6,
                1.08f,
                1.2f,
                1f,
                new StatEffect(StatType.Happiness, 4));
            UpsertTrait(
                "Trait_IronStomach.asset",
                "trait_iron_stomach",
                "강철 위장",
                "배달·회식에도 몸이 잘 버틴다. 시작 건강이 높다.",
                6,
                1f,
                1f,
                1f,
                new StatEffect(StatType.Health, 10));
            UpsertTrait(
                "Trait_Boundary.asset",
                "trait_boundary",
                "선 긋기",
                "업무 스트레스를 덜 받지만 시작 행복이 낮다.",
                7,
                1f,
                0.85f,
                0.8f,
                new StatEffect(StatType.Happiness, -6),
                new StatEffect(StatType.CompanyScore, 4));
        }

        private static List<EventData> CreateRelationshipEvents()
        {
            return new List<EventData>
            {
                MakeEvent(
                    "Event_CoworkerLunch_001",
                    "event_coworker_lunch_001",
                    "동료 점심 제안",
                    "같은 팀 후배가 오늘은 편의점 말고 밥을 먹자고 한다.",
                    EventCategory.Relationship,
                    28,
                    null,
                    new[] { RunFlags.CloseWithCoworker },
                    BackgroundId.Restaurant,
                    Choice("event_coworker_lunch_001_a", "같이 간다", -18_000, 0, -3, 6, 1, RunFlags.CloseWithCoworker),
                    Choice("event_coworker_lunch_001_b", "김밥으로 타협한다", -6_000, 0, -1, 2, 0, RunFlags.CloseWithCoworker),
                    Choice("event_coworker_lunch_001_c", "오늘은 혼자 먹는다", 0, 0, 1, -3, -1)),
                MakeEvent(
                    "Event_CoworkerCover_001",
                    "event_coworker_cover_001",
                    "동료의 부탁",
                    "친해진 동료가 오늘 자리를 비워야 하니 업무를 봐 달라고 한다.",
                    EventCategory.Work,
                    92,
                    new[] { RunFlags.CloseWithCoworker },
                    null,
                    BackgroundId.Office,
                    Choice("event_coworker_cover_001_a", "대신 처리한다", 0, -3, 8, 2, 2, null, RunFlags.CloseWithCoworker),
                    Choice("event_coworker_cover_001_b", "반만 도와준다", 0, -1, 3, 0, 1, null, RunFlags.CloseWithCoworker),
                    Choice("event_coworker_cover_001_c", "못 한다고 한다", 0, 0, 1, -6, -2, null, RunFlags.CloseWithCoworker)),
                MakeEvent(
                    "Event_BlindDate_001",
                    "event_blind_date_001",
                    "지인의 소개팅",
                    "단톡방에 ‘이번 주만 시간 되냐’는 소개팅 제안이 올라왔다.",
                    EventCategory.Relationship,
                    26,
                    null,
                    new[] { RunFlags.Dating },
                    BackgroundId.Restaurant,
                    Choice("event_blind_date_001_a", "제대로 나가본다", -35_000, -2, 2, 8, 0, RunFlags.Dating),
                    Choice("event_blind_date_001_b", "카페만 짧게", -12_000, 0, 1, 4, 0, RunFlags.Dating),
                    Choice("event_blind_date_001_c", "정중히 거절한다", 0, 0, -1, -2, 0)),
                MakeEvent(
                    "Event_DateAnniversary_001",
                    "event_date_anniversary_001",
                    "기념일 압박",
                    "사귄 지 얼마 안 됐는데도 기념일을 챙기라는 공기가 무겁다.",
                    EventCategory.Relationship,
                    90,
                    new[] { RunFlags.Dating },
                    null,
                    BackgroundId.Restaurant,
                    Choice("event_date_anniversary_001_a", "제대로 챙긴다", -80_000, 0, -4, 12, 0, null, RunFlags.Dating),
                    Choice("event_date_anniversary_001_b", "현실적으로 한다", -25_000, 0, -1, 4, 0, null, RunFlags.Dating),
                    Choice("event_date_anniversary_001_c", "미룬다", 0, 0, 6, -10, 0, null, RunFlags.Dating)),
                MakeEvent(
                    "Event_MentorCoffee_001",
                    "event_mentor_coffee_001",
                    "사수의 커피",
                    "사수가 ‘잠깐 나와’라며 커피를 사겠다고, 아니 사라고 한다.",
                    EventCategory.Work,
                    38,
                    null,
                    new[] { RunFlags.MentorBond },
                    BackgroundId.Office,
                    Choice("event_mentor_coffee_001_a", "내가 쏜다", -15_000, 0, -2, 1, 4, RunFlags.MentorBond),
                    Choice("event_mentor_coffee_001_b", "더치페이", -7_000, 0, 0, 0, 2, RunFlags.MentorBond),
                    Choice("event_mentor_coffee_001_c", "바쁜 척한다", 0, 0, 2, -1, -3)),
                MakeEvent(
                    "Event_MentorAsk_001",
                    "event_mentor_ask_001",
                    "멘토의 한 수",
                    "라인이 생긴 사수가 이번 주 고과 전에 방향을 정해 주겠다고 한다.",
                    EventCategory.Work,
                    88,
                    new[] { RunFlags.MentorBond },
                    null,
                    BackgroundId.Office,
                    Choice("event_mentor_ask_001_a", "조언을 따른다", 0, 0, -4, 1, 5, null, RunFlags.MentorBond),
                    Choice("event_mentor_ask_001_b", "작은 선물을 한다", -30_000, 0, -2, 2, 3, null, RunFlags.MentorBond),
                    Choice("event_mentor_ask_001_c", "거리를 둔다", 0, 0, 1, -2, -4, null, RunFlags.MentorBond)),
                MakeEvent(
                    "Event_NeighborComplaint_001",
                    "event_neighbor_complaint_001",
                    "층간소음 민원",
                    "위층 발소리가 자정에도 그치지 않는다. 아래층에서도 경고가 왔다.",
                    EventCategory.Accident,
                    40,
                    null,
                    new[] { RunFlags.NeighborFeud },
                    BackgroundId.Home,
                    Choice("event_neighbor_complaint_001_a", "참고 이어폰을 켠다", 0, -2, 8, -4, 0, RunFlags.NeighborFeud),
                    Choice("event_neighbor_complaint_001_b", "인터폰으로 말한다", 0, 0, 5, -3, 0, RunFlags.NeighborFeud),
                    Choice("event_neighbor_complaint_001_c", "관리실에 민원을 넣는다", 0, 0, 6, -2, 0, RunFlags.NeighborFeud)),
                MakeEvent(
                    "Event_NeighborPeace_001",
                    "event_neighbor_peace_001",
                    "이웃 화해 시도",
                    "소음 갈등이 일주일째다. 화해할지, 계속 버틸지.",
                    EventCategory.Relationship,
                    86,
                    new[] { RunFlags.NeighborFeud },
                    null,
                    BackgroundId.Home,
                    Choice("event_neighbor_peace_001_a", "간식을 놓고 온다", -20_000, 0, -8, 3, 0, null, RunFlags.NeighborFeud),
                    Choice("event_neighbor_peace_001_b", "시간을 합의한다", 0, 0, -4, 1, 0, null, RunFlags.NeighborFeud),
                    Choice("event_neighbor_peace_001_c", "계속 참는다", 0, -1, 5, -3, 0, null, RunFlags.NeighborFeud)),
                MakeEvent(
                    "Event_FamilyVisit_001",
                    "event_family_visit_001",
                    "본가 방문",
                    "주말에 얼굴 한번 비추라는 연락이 왔다. 교통비와 체력이 동시에 나간다.",
                    EventCategory.Relationship,
                    28,
                    null,
                    new[] { RunFlags.FamilySupport },
                    BackgroundId.Home,
                    Choice("event_family_visit_001_a", "내려간다", -45_000, -3, 3, 8, 0, RunFlags.FamilySupport),
                    Choice("event_family_visit_001_b", "용돈만 보낸다", -80_000, 0, 1, 2, 0, RunFlags.FamilySupport),
                    Choice("event_family_visit_001_c", "이번엔 못 간다", 0, 0, 4, -6, 0)),
                MakeEvent(
                    "Event_FamilyEmergency_001",
                    "event_family_emergency_001",
                    "가족 경조사",
                    "연락이 닿던 쪽에서 갑자기 부조·병문안 이야기가 온다.",
                    EventCategory.Relationship,
                    84,
                    new[] { RunFlags.FamilySupport },
                    null,
                    BackgroundId.Home,
                    Choice("event_family_emergency_001_a", "제대로 챙긴다", -120_000, -2, 6, 4, 0, null, RunFlags.FamilySupport),
                    Choice("event_family_emergency_001_b", "최소한만 보낸다", -40_000, 0, 3, -2, 0, null, RunFlags.FamilySupport),
                    Choice("event_family_emergency_001_c", "못 보낸다고 한다", 0, 0, 8, -12, 0, null, RunFlags.FamilySupport)),
                MakeEvent(
                    "Event_WeekendGroup_001",
                    "event_weekend_group_001",
                    "주말 단톡 약속",
                    "‘이번엔 진짜 모이자’던 단톡이 토요일 저녁을 찍었다.",
                    EventCategory.Relationship,
                    48,
                    null,
                    null,
                    BackgroundId.Restaurant,
                    DayOfWeekConstraint.WeekendOnly,
                    Choice("event_weekend_group_001_a", "끝까지 따라간다", -55_000, -4, -6, 10, 0),
                    Choice("event_weekend_group_001_b", "2차 전에 집에 간다", -22_000, -1, -3, 5, 0),
                    Choice("event_weekend_group_001_c", "잠수 탄다", 0, 0, 2, -8, 0))
            };
        }

        private static List<EventData> CreateCorpEvents()
        {
            return new List<EventData>
            {
                MakeJobEvent(
                    "Event_CorpWorkshop_001",
                    "event_corp_workshop_001",
                    "강제 팀 워크숍",
                    "주말 워크숍 공지가 떴다. 불참은 평가에 찍힌다는 말이 돈다.",
                    EventCategory.Work,
                    Choice("event_corp_workshop_001_a", "끝까지 참석한다", -40_000, -4, 6, -2, 6),
                    Choice("event_corp_workshop_001_b", "반나절만 얼굴 비춘다", -18_000, -1, 2, 0, 2),
                    Choice("event_corp_workshop_001_c", "핑계를 댄다", 0, 0, -3, 2, -8)),
                MakeJobEvent(
                    "Event_CorpReview_001",
                    "event_corp_review_001",
                    "고과 시즌",
                    "상대평가 시즌이다. 숫자를 맞출지, 담백하게 갈지.",
                    EventCategory.Work,
                    Choice("event_corp_review_001_a", "야근으로 숫자를 맞춘다", 0, -6, 10, -4, 12, RunFlags.PromotionTrack),
                    Choice("event_corp_review_001_b", "있는 그대로 보고한다", 0, 0, 2, 0, 3),
                    Choice("event_corp_review_001_c", "성과를 부풀린다", 0, 0, 4, -6, 7)),
                MakeJobEvent(
                    "Event_CorpPolitics_001",
                    "event_corp_politics_001",
                    "사내 정치",
                    "팀 미팅 전에 ‘어느 줄이냐’는 눈치 게임이 시작됐다.",
                    EventCategory.Work,
                    Choice("event_corp_politics_001_a", "줄에 선다", 0, 0, 4, -4, 6),
                    Choice("event_corp_politics_001_b", "일만 한다", 0, 0, 1, 1, -2),
                    Choice("event_corp_politics_001_c", "양쪽 말을 맞춰 준다", 0, 0, 3, -2, 2)),
                MakeJobEvent(
                    "Event_CorpNight_001",
                    "event_corp_night_001",
                    "대기업 야근 문화",
                    "6시가 돼도 모니터가 꺼지지 않는다. ‘아직 계시네요’가 인사다.",
                    EventCategory.Work,
                    Choice("event_corp_night_001_a", "남아서 끝낸다", 0, -5, 8, -3, 8, RunFlags.PromotionTrack),
                    Choice("event_corp_night_001_b", "정시에 퇴근한다", 0, 1, -2, 4, -6),
                    Choice("event_corp_night_001_c", "재택 핑계를 낸다", 0, 0, -1, 1, -4))
            };
        }

        private static EventData MakeJobEvent(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            EventChoiceData c1,
            EventChoiceData c2,
            EventChoiceData c3)
        {
            return MakeEvent(
                file,
                id,
                title,
                description,
                category,
                72,
                null,
                null,
                BackgroundId.Office,
                DayOfWeekConstraint.Any,
                CorpJobId,
                c1,
                c2,
                c3);
        }

        private static EventData MakeEvent(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            int weight,
            string[] requiredFlags,
            string[] forbiddenFlags,
            BackgroundId background,
            EventChoiceData c1,
            EventChoiceData c2,
            EventChoiceData c3)
        {
            return MakeEvent(
                file, id, title, description, category, weight,
                requiredFlags, forbiddenFlags, background, DayOfWeekConstraint.Any, string.Empty,
                c1, c2, c3);
        }

        private static EventData MakeEvent(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            int weight,
            string[] requiredFlags,
            string[] forbiddenFlags,
            BackgroundId background,
            DayOfWeekConstraint dayConstraint,
            EventChoiceData c1,
            EventChoiceData c2,
            EventChoiceData c3)
        {
            return MakeEvent(
                file, id, title, description, category, weight,
                requiredFlags, forbiddenFlags, background, dayConstraint, string.Empty,
                c1, c2, c3);
        }

        private static EventData MakeEvent(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            int weight,
            string[] requiredFlags,
            string[] forbiddenFlags,
            BackgroundId background,
            DayOfWeekConstraint dayConstraint,
            string requiredJobId,
            EventChoiceData c1,
            EventChoiceData c2,
            EventChoiceData c3)
        {
            var path = $"{EventsFolder}/{file}.asset";
            var eventData = LoadOrCreate<EventData>(path);
            var conditions = new EventCondition();
            conditions.EditorConfigure(
                newRequiredJobId: requiredJobId ?? string.Empty,
                newDayOfWeekConstraint: dayConstraint);
            if (requiredFlags != null || forbiddenFlags != null)
            {
                conditions.EditorSetFlags(requiredFlags, forbiddenFlags);
            }

            eventData.EditorSetCore(
                id,
                title,
                description,
                category,
                1,
                30,
                weight,
                conditions,
                new List<EventChoiceData> { c1, c2, c3 });
            eventData.EditorSetArt(true, background, false, ExpressionId.Default);
            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventChoiceData Choice(
            string choiceId,
            string text,
            long cash,
            int health,
            int stress,
            int happiness,
            int company,
            string setFlag = null,
            string clearFlag = null)
        {
            var effects = new List<StatEffect>();
            if (cash != 0)
            {
                effects.Add(new StatEffect(StatType.Cash, cash));
            }

            if (health != 0)
            {
                effects.Add(new StatEffect(StatType.Health, health));
            }

            if (stress != 0)
            {
                effects.Add(new StatEffect(StatType.Stress, stress));
            }

            if (happiness != 0)
            {
                effects.Add(new StatEffect(StatType.Happiness, happiness));
            }

            if (company != 0)
            {
                effects.Add(new StatEffect(StatType.CompanyScore, company));
            }

            List<string> setFlags = string.IsNullOrEmpty(setFlag) ? null : new List<string> { setFlag };
            List<string> clearFlags = string.IsNullOrEmpty(clearFlag) ? null : new List<string> { clearFlag };
            return new EventChoiceData(choiceId, text, effects, null, setFlags, clearFlags);
        }

        private static void UpsertTrait(
            string fileName,
            string id,
            string displayName,
            string description,
            int unlockLevel,
            float cashLoss,
            float happinessGain,
            float workStress,
            params StatEffect[] starting)
        {
            var trait = LoadOrCreate<TraitData>($"{TraitsFolder}/{fileName}");
            trait.EditorSet(id, displayName, description, unlockLevel);
            trait.EditorSetRuntimeMultipliers(cashLoss, happinessGain, workStress);
            var so = new SerializedObject(trait);
            var prop = so.FindProperty("startingStatModifiers");
            prop.ClearArray();
            for (var i = 0; i < starting.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("statType").enumValueIndex = (int)starting[i].StatType;
                element.FindPropertyRelative("value").longValue = starting[i].Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trait);
        }

        private static void WireGameScene()
        {
            if (!File.Exists(GameScenePath))
            {
                throw new FileNotFoundException(GameScenePath);
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<GamePlayPresenter>();
            if (presenter == null)
            {
                throw new System.InvalidOperationException("[R-QA-07] GamePlayPresenter missing.");
            }

            var so = new SerializedObject(presenter);
            WireObjectList(so.FindProperty("eventCatalog"), LoadAll<EventData>(EventsFolder));
            WireObjectList(so.FindProperty("allJobs"), LoadAll<JobData>(JobsFolder));
            WireObjectList(so.FindProperty("allTraits"), LoadAll<TraitData>(TraitsFolder));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ContentPackRqa07] Game 씬 카탈로그 연결.");
        }

        private static void WireMainMenuCatalogs()
        {
            if (!File.Exists(MainMenuPath))
            {
                throw new FileNotFoundException(MainMenuPath);
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                throw new System.InvalidOperationException("[R-QA-07] MainMenuController missing.");
            }

            var jobs = LoadAll<JobData>(JobsFolder);
            var traits = LoadAll<TraitData>(TraitsFolder);
            var events = LoadAll<EventData>(EventsFolder);
            var playable = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].Id != "event_rest_fallback")
                {
                    playable++;
                }
            }

            var so = new SerializedObject(controller);
            WireObjectList(so.FindProperty("jobCatalog"), jobs);
            WireObjectList(so.FindProperty("traitCatalog"), traits);
            WireObjectList(so.FindProperty("eventCatalog"), events);
            so.FindProperty("totalJobCount").intValue = jobs.Count;
            so.FindProperty("totalTraitCount").intValue = traits.Count;
            so.FindProperty("totalEventCount").intValue = playable;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                $"[ContentPackRqa07] MainMenu 카탈로그 jobs={jobs.Count} traits={traits.Count} events={playable}. 상점 없음.");
        }

        private static void WireObjectList<T>(SerializedProperty property, List<T> values) where T : Object
        {
            property.ClearArray();
            values.Sort((a, b) => string.CompareOrdinal(GetId(a), GetId(b)));
            for (var i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static string GetId(Object asset)
        {
            if (asset is JobData job)
            {
                return job.Id;
            }

            if (asset is TraitData trait)
            {
                return trait.Id;
            }

            if (asset is EventData eventData)
            {
                return eventData.Id;
            }

            return asset != null ? asset.name : string.Empty;
        }

        private static List<T> LoadAll<T>(string folder) where T : ScriptableObject
        {
            var list = new List<T>();
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            for (var i = 0; i <  guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
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
