using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// Unit 23: 직업 2개 갱신 + 일반 사건 +18 + 직업 전용 8+8.
    /// </summary>
    public static class ContentPackUnit23Factory
    {
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string EventsFolder = "Assets/Data/Events";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Surviving Until Payday/Create Content Pack (Unit 23)")]
        public static void CreateContentPack()
        {
            EnsureFolder(JobsFolder);
            EnsureFolder(EventsFolder);

            CreateOrUpdateJobs();
            var created = new List<EventData>();
            created.AddRange(CreateGeneralEvents());
            created.AddRange(CreateCivilPrepEvents());
            created.AddRange(CreateFreelancerEvents());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var warnings = 0;
            foreach (var e in created)
            {
                if (e == null)
                {
                    continue;
                }

                foreach (var err in e.Validate())
                {
                    warnings++;
                    Debug.LogWarning($"[ContentPackUnit23:{e.name}] {err}", e);
                }
            }

            Debug.Log(
                $"[ContentPackUnit23] 사건 {created.Count}개 + 직업 갱신 완료. 경고={warnings}.\n" +
                "이어서 Wire All Events / Wire Jobs To Game Scene / Setup MainMenu Run Start를 실행하세요.");
        }

        [MenuItem("Tools/Surviving Until Payday/Wire Jobs To Game Scene (Unit 23)")]
        public static void WireJobsToGameScene()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError("[ContentPackUnit23] Game.unity missing.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<SurviveUntilPayday.UI.GamePlayPresenter>();
            if (presenter == null)
            {
                Debug.LogError("[ContentPackUnit23] GamePlayPresenter missing.");
                return;
            }

            var jobs = LoadAllJobs();
            var so = new SerializedObject(presenter);
            var allJobs = so.FindProperty("allJobs");
            allJobs.ClearArray();
            for (var i = 0; i < jobs.Count; i++)
            {
                allJobs.InsertArrayElementAtIndex(i);
                allJobs.GetArrayElementAtIndex(i).objectReferenceValue = jobs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ContentPackUnit23] GamePlayPresenter.allJobs = {jobs.Count}");
        }

        private static void CreateOrUpdateJobs()
        {
            UpsertJob(
                "Job_JuniorOffice.asset",
                "job_junior_office",
                "\uC911\uC18C\uAE30\uC5C5 \uC2E0\uC785\uC0AC\uC6D0",
                "\uD45C\uC900 \uB09C\uB3C4\uC758 \uC9C1\uC7A5 \uC0DD\uD65C. \uC6D4\uAE09\uC740 \uC548\uC815\uC801\uC774\uC9C0\uB9CC \uC0DD\uD65C\uBE44 \uBD80\uB2F4\uB3C4 \uB9CC\uB9CC\uCE58 \uC54A\uB2E4.",
                0,
                2_800_000L,
                2_800_000L,
                80,
                20,
                50,
                50);

            UpsertJob(
                "Job_CivilPrep.asset",
                "job_civil_prep",
                "\uACF5\uBB34\uC6D0 \uC900\uBE44\uC0DD",
                "\uB0AE\uC740 \uC218\uC785\uC774\uC9C0\uB9CC \uC9C1\uC7A5 \uC0AC\uAC74\uC740 \uC801\uB2E4. \uACF5\uBD80\uC640 \uC2DC\uD5D8 \uC2A4\uD2B8\uB808\uC2A4\uAC00 \uC9C3\uB2E4.",
                2,
                1_200_000L,
                1_800_000L,
                75,
                35,
                45,
                20);

            UpsertJob(
                "Job_Freelancer.asset",
                "job_freelancer",
                "\uD504\uB9AC\uB79C\uC11C",
                "\uC218\uC785 \uBCC0\uB3D9\uC774 \uD070 \uC790\uC720\uB85C\uC6B4 \uC77C. \uD504\uB85C\uC81D\uD2B8\uC640 \uACE0\uAC1D \uAD00\uB9AC\uAC00 \uC0B6\uC744 \uC88B\uC74C.",
                3,
                2_200_000L,
                2_400_000L,
                70,
                30,
                55,
                15);
        }

        private static void UpsertJob(
            string fileName,
            string id,
            string displayName,
            string description,
            int unlockLevel,
            long salary,
            long startingCash,
            int health,
            int stress,
            int happiness,
            int company)
        {
            var path = $"{JobsFolder}/{fileName}";
            var job = LoadOrCreate<JobData>(path);
            job.EditorSet(
                id,
                displayName,
                description,
                unlockLevel,
                salary,
                startingCash,
                health,
                stress,
                happiness,
                company);
            EditorUtility.SetDirty(job);
        }

        private static List<EventData> CreateGeneralEvents()
        {
            return new List<EventData>
            {
                Make(
                    "Event_CoffeeMachine_001",
                    "event_coffee_machine_001",
                    "\ucee4\ud53c\uba38\uc2e0 \uace0\uc7a5",
                    "\uc0ac\ubb34\uc2e4 \ucee4\ud53c\uba38\uc2e0\uc774 \ub610 \uace0\uc7a5 \ub0ac\ub2e4.",
                    EventCategory.Work,
                    "\uce74\ud398\uc5d0\uc11c \uc0ac \ub9c8\uc2e0\ub2e4", -6000, 0, -1, 2, 0, "\ubb3c\uc744 \ub9c8\uc2e0\ub2e4", 0, 1, 2, -2, 0, "\ub3d9\ub8cc\uc640 \ud22c\ub35c\uac70\ub9ac\uba70 \ubc84\ud2f4\ub2e4", 0, 0, -3, 1, -1),
                Make(
                    "Event_CommuteRain_001",
                    "event_commute_rain_001",
                    "\ube57\uc18d \ucd9c\uadfc",
                    "\uac11\uc790\uae30 \uc3df\uc544\uc9c4 \ube44\uc5d0 \uc6b0\uc0b0\ub3c4 \uc5c6\uc774 \ucd9c\uadfc\ud574\uc57c \ud55c\ub2e4.",
                    EventCategory.Consumption,
                    "\ud0dd\uc2dc\ub85c \uac04\ub2e4", -25000, 0, -2, 2, 1, "\ub6f0\uc5b4\uc11c \uac04\ub2e4", 0, -5, 4, -3, 0, "\uc9c0\uac01\uc744 \uac10\uc218\ud55c\ub2e4", 0, 0, 6, -5, -8),
                Make(
                    "Event_DeliveryTip_001",
                    "event_delivery_tip_001",
                    "\ubc30\ub2ec\ube44 \ub51c\ub808\ub9c8",
                    "\ubc30\ub2ec\ube44\uac00 \uc62c\ub790\ub2e4.",
                    EventCategory.Consumption,
                    "\uadf8\ub0e5 \uc2dc\ud0a8\ub2e4", -22000, 0, -1, 3, 0, "\uc9c1\uc811 \uc0ac\ub7ec \uac04\ub2e4", -12000, 1, 2, 0, 0, "\uad75\ub294\ub2e4", 0, -3, 4, -4, 0),
                Make(
                    "Event_FamilyCall_001",
                    "event_family_call_001",
                    "\uac00\uc871 \uc804\ud654",
                    "\ubd80\ubaa8\ub2d8\uc5d0\uac8c\uc11c \uc548\ubd80 \uc804\ud654\uac00 \uc654\ub2e4.",
                    EventCategory.Relationship,
                    "\uae38\uac8c \ud1b5\ud654\ud55c\ub2e4", 0, 0, -4, 6, 0, "\uc9e7\uac8c \uc548\ubd80\ub97c \uc804\ud55c\ub2e4", 0, 0, -1, 2, 0, "\ub2e4\uc74c\uc5d0 \ud558\uaca0\ub2e4\uace0 \ubbf8\ub8e8\ub2e4", 0, 0, 2, -4, 0),
                Make(
                    "Event_GroupChat_001",
                    "event_group_chat_001",
                    "\ub2e8\ud1a1\ubc29 \uba58\uc158",
                    "\uc5c5\ubb34 \ub2e8\ud1a1\ubc29\uc5d0 \uba58\uc158\uc774 \uc794\ub729 \uc300\uc600\ub2e4.",
                    EventCategory.Work,
                    "\ubc14\ub85c \ud655\uc778\ud55c\ub2e4", 0, 0, 3, -2, 2, "\uc911\uc694\ud55c \uac83\ub9cc \ubcf8\ub2e4", 0, 0, 1, 0, 1, "\ub0b4\uc77c \ubcf8\ub2e4", 0, 0, -2, 2, -3),
                Make(
                    "Event_GymTrial_001",
                    "event_gym_trial_001",
                    "\ud5ec\uc2a4\uc7a5 \uccb4\ud5d8",
                    "\ud5ec\uc2a4\uc7a5 \ubb34\ub8cc \uccb4\ud5d8\uad8c\uc774 \ub3c4\ucc29\ud588\ub2e4.",
                    EventCategory.Health,
                    "\uccb4\ud5d8\ud558\uace0 \ub4f1\ub85d\ud55c\ub2e4", -89000, 8, -4, 3, 0, "\uccb4\ud5d8\ub9cc \ud55c\ub2e4", 0, 4, -2, 2, 0, "\ubb34\uc2dc\ud55c\ub2e4", 0, -2, 0, -1, 0),
                Make(
                    "Event_InsuranceCall_001",
                    "event_insurance_call_001",
                    "\ubcf4\ud5d8 \uad8c\uc720 \uc804\ud654",
                    "\ubaa8\ub974\ub294 \ubc88\ud638\ub85c \ubcf4\ud5d8 \uad8c\uc720 \uc804\ud654\uac00 \uc654\ub2e4.",
                    EventCategory.Consumption,
                    "\uc815\uc911\ud788 \uac70\uc808\ud55c\ub2e4", 0, 0, 1, 0, 0, "\ub04a\ub294\ub2e4", 0, 0, -1, 0, 0, "\uad00\uc2ec \uc788\ub294 \ucc99 \ub4e3\ub294\ub2e4", 0, 0, 2, -2, 0),
                Make(
                    "Event_LateNightSnack_001",
                    "event_late_night_snack_001",
                    "\uc57c\uc2dd \uc720\ud639",
                    "\ubc24\ub2a6\uac8c \ubc30\ub2ec \uc571 \uc54c\ub9bc\uc774 \uc6b8\ub9b0\ub2e4.",
                    EventCategory.Consumption,
                    "\uc2dc\ud0a8\ub2e4", -18000, -3, -2, 4, 0, "\uacfc\uc77c\ub9cc \uba39\ub294\ub2e4", -3000, 1, 0, 1, 0, "\ucc38\ub294\ub2e4", 0, 2, 3, -2, 0),
                Make(
                    "Event_LostCard_001",
                    "event_lost_card_001",
                    "\uce74\ub4dc \ubd84\uc2e4 \uc758\uc2ec",
                    "\uc9c0\uac11\uc5d0 \uce74\ub4dc\uac00 \uc548 \ubcf4\uc778\ub2e4.",
                    EventCategory.Consumption,
                    "\uc989\uc2dc \uc815\uc9c0\ud55c\ub2e4", 0, 0, 4, -3, 0, "\uc9d1\uc744 \ub4a4\uc838\ubcf8\ub2e4", 0, 0, 3, -1, 0, "\ud558\ub8e8\ub9cc \ub354 \uae30\ub2e4\ub824\ubcf8\ub2e4", 0, 0, 2, -2, 0),
                Make(
                    "Event_MeetingPrep_001",
                    "event_meeting_prep_001",
                    "\ud68c\uc758 \uc790\ub8cc",
                    "\ub0b4\uc77c \ud68c\uc758 \uc790\ub8cc\uac00 \uc544\uc9c1 \ub35c \ub410\ub2e4.",
                    EventCategory.Work,
                    "\ubc24\uc0c8 \ub9c8\ubb34\ub9ac\ud55c\ub2e4", 0, -6, 8, -3, 5, "\ud575\uc2ec\ub9cc \uc815\ub9ac\ud55c\ub2e4", 0, -2, 3, 0, 2, "\ub0b4\uc77c \uc544\uce68\uc5d0 \ud55c\ub2e4", 0, 1, 2, 1, -4),
                Make(
                    "Event_NeighborNoise_001",
                    "event_neighbor_noise_001",
                    "\uce35\uac04\uc18c\uc74c",
                    "\uc704\uce35 \uc18c\uc74c\uc774 \uc7a0\uc744 \ubc29\ud574\ud55c\ub2e4.",
                    EventCategory.Consumption,
                    "\uc815\uc911\ud788 \uc5f0\ub77d\ud55c\ub2e4", 0, 0, 3, -2, 0, "\uc774\uc5b4\ud3f0\uc744 \ub07c\uace0 \uacac\ub518\ub2e4", 0, -2, 5, -3, 0, "\uad00\ub9ac\uc2e4\uc5d0 \ubbfc\uc6d0\uc744 \ub123\ub294\ub2e4", 0, 0, -2, 1, 0),
                Make(
                    "Event_OfficeAC_001",
                    "event_office_ac_001",
                    "\uc5d0\uc5b4\ucee8 \uc804\uc7c1",
                    "\uc0ac\ubb34\uc2e4 \uc628\ub3c4\ub97c \ub450\uace0 \uc758\uacac\uc774 \uac08\ub9b0\ub2e4.",
                    EventCategory.Work,
                    "\ucc38\uace0 \uc77c\ud55c\ub2e4", 0, -2, 3, -2, 1, "\uc790\ub9ac\ub97c \uc62e\uaca8 \ubcf8\ub2e4", 0, 1, 1, 0, 0, "\uc194\uc9c1\ud788 \ub9d0\ud55c\ub2e4", 0, 0, 2, 1, -2),
                Make(
                    "Event_OnlineCourse_001",
                    "event_online_course_001",
                    "\uc628\ub77c\uc778 \uac15\uc758 \uc138\uc77c",
                    "\uad00\uc2ec \uc788\ub358 \uac15\uc758\uac00 \ubc18\uac12 \uc138\uc77c \uc911\uc774\ub2e4.",
                    EventCategory.Opportunity,
                    "\uacb0\uc81c\ud55c\ub2e4", -69000, 0, 2, 4, 2, "\uc704\uc2dc\ub9ac\uc2a4\ud2b8\uc5d0\ub9cc \ub2f4\ub294\ub2e4", 0, 0, 1, -1, 0, "\ubb34\uc2dc\ud55c\ub2e4", 0, 0, -1, -2, 0),
                Make(
                    "Event_PackageDelay_001",
                    "event_package_delay_001",
                    "\ud0dd\ubc30 \uc9c0\uc5f0",
                    "\uae30\ub2e4\ub9ac\ub358 \ud0dd\ubc30\uac00 \ub610 \uc9c0\uc5f0\ub410\ub2e4.",
                    EventCategory.Consumption,
                    "\uace0\uac1d\uc13c\ud130\uc5d0 \ubb38\uc758\ud55c\ub2e4", 0, 0, 3, -2, 0, "\uadf8\ub0e5 \uae30\ub2e4\ub9b0\ub2e4", 0, 0, 2, -1, 0, "\ucde8\uc18c\ud558\uace0 \ub2e4\ub978 \uac78 \uc0b0\ub2e4", -15000, 0, -1, 1, 0),
                Make(
                    "Event_ParkWalk_001",
                    "event_park_walk_001",
                    "\uacf5\uc6d0 \uc0b0\ucc45",
                    "\ub0a0\uc528\uac00 \uc88b\uc544 \uc0b0\ucc45\uc774 \ub2f9\uae34\ub2e4.",
                    EventCategory.Health,
                    "\ud55c \uc2dc\uac04 \uac77\ub294\ub2e4", 0, 5, -5, 4, 0, "\uc9e7\uac8c \uc0b0\ucc45\ud55c\ub2e4", 0, 2, -2, 2, 0, "\uc9d1\uc5d0 \uc788\ub294\ub2e4", 0, -1, 0, -1, 0),
                Make(
                    "Event_Subscription_001",
                    "event_subscription_001",
                    "\uad6c\ub3c5 \uc815\ub9ac",
                    "\uc548 \uc4f0\ub294 \uad6c\ub3c5\uc774 \uce74\ub4dc \uba85\uc138\uc5d0 \ucc0d\ud600 \uc788\ub2e4.",
                    EventCategory.Consumption,
                    "\uc804\ubd80 \ud574\uc9c0\ud55c\ub2e4", 0, 0, -1, 2, 0, "\ud558\ub098\ub9cc \ub0a8\uae34\ub2e4", -9000, 0, 0, 1, 0, "\ub098\uc911\uc5d0 \ubcf8\ub2e4", 0, 0, 1, -2, 0),
                Make(
                    "Event_TeamDinner_001",
                    "event_team_dinner_001",
                    "\ud68c\uc2dd \uc81c\uc548",
                    "\ud300 \ud68c\uc2dd \uc81c\uc548\uc774 \ub4e4\uc5b4\uc654\ub2e4.",
                    EventCategory.Relationship,
                    "\ucc38\uc11d\ud55c\ub2e4", -45000, 0, -3, 6, 4, "\ud55c\uc794\ub9cc \ud558\uace0 \ub098\uc628\ub2e4", -20000, 0, 2, 2, 1, "\uc815\uc911\ud788 \uac70\uc808\ud55c\ub2e4", 0, 0, -2, -4, -3),
                Make(
                    "Event_WeekendPlan_001",
                    "event_weekend_plan_001",
                    "\uc8fc\ub9d0 \uacc4\ud68d",
                    "\uc8fc\ub9d0\uc5d0 \ubb34\uc5c7\uc744 \ud560\uc9c0 \uace0\ubbfc\ub41c\ub2e4.",
                    EventCategory.Consumption,
                    "\uce5c\uad6c\ub97c \ub9cc\ub09c\ub2e4", -40000, 0, -3, 8, 0, "\uc9d1\uc5d0\uc11c \uc26e\ub2e4", 0, 4, -6, 3, 0, "\ubc00\ub9b0 \uc77c\uc744 \ud55c\ub2e4", 0, -2, 4, -2, 3)
            };
        }

        private static List<EventData> CreateCivilPrepEvents()
        {
            return new List<EventData>
            {
                MakeJob(
                    "Event_CivilAcademy_001",
                    "job_civil_prep",
                    "event_civil_academy_001",
                    "\ud559\uc6d0 \ud2b9\uac15",
                    "\uc720\uba85 \uac15\uc0ac \ud2b9\uac15 \uc2e0\uccad\uc774 \uc5f4\ub838\ub2e4.",
                    EventCategory.Opportunity,
                    "\ub4f1\ub85d\ud55c\ub2e4", -120000, 0, 3, 2, 0, "\ubb34\ub8cc \uc790\ub8cc\ub9cc \ubcf8\ub2e4", 0, 0, 1, 0, 0, "\ud328\uc2a4\ud55c\ub2e4", 0, 0, -1, -1, 0),
                MakeJob(
                    "Event_CivilAnxiety_001",
                    "job_civil_prep",
                    "event_civil_anxiety_001",
                    "\ud569\uaca9 \ubd88\uc548",
                    "\ud569\uaca9 \uc18c\uc2dd\uc774 \ub04a\uc774\uc9c0 \uc54a\uc544 \ube44\uad50 \ubd88\uc548\uc774 \uc62c\ub77c\uc628\ub2e4.",
                    EventCategory.Special,
                    "\uc0b0\ucc45\uc73c\ub85c \ud658\uae30\ud55c\ub2e4", 0, 2, -5, 3, 0, "\uacf5\ubd80\ub85c \ubb3b\ub294\ub2e4", 0, -2, 3, -1, 0, "SNS\ub97c \ub04a\ub294\ub2e4", 0, 0, -3, 2, 0),
                MakeJob(
                    "Event_CivilFriend_001",
                    "job_civil_prep",
                    "event_civil_friend_001",
                    "\ucde8\uc900 \ub3d9\uae30 \ub9cc\ub0a8",
                    "\uac19\uc740 \uc2dc\ud5d8\uc744 \uc900\ube44\ud558\ub294 \uce5c\uad6c\ub97c \ub9cc\ub0ac\ub2e4.",
                    EventCategory.Relationship,
                    "\uc815\ubcf4 \uad50\ud658\ud55c\ub2e4", -20000, 0, -2, 4, 0, "\uc9e7\uac8c \uc778\uc0ac\ub9cc", 0, 0, 0, 1, 0, "\uac70\uc808\ud55c\ub2e4", 0, 0, 1, -2, 0),
                MakeJob(
                    "Event_CivilLibrary_001",
                    "job_civil_prep",
                    "event_civil_library_001",
                    "\ub3c4\uc11c\uad00 \uc790\ub9ac",
                    "\ub3c4\uc11c\uad00 \uc778\uae30 \uc790\ub9ac\uac00 \ube44\uc5c8\ub2e4.",
                    EventCategory.Special,
                    "\ubc14\ub85c \uc608\uc57d\ud55c\ub2e4", 0, 0, 2, 2, 0, "\uce74\ud398\ub85c \uac04\ub2e4", -8000, 0, 1, 1, 0, "\uc9d1\uc5d0\uc11c \uacf5\ubd80\ud55c\ub2e4", 0, 0, 0, -1, 0),
                MakeJob(
                    "Event_CivilMockTest_001",
                    "job_civil_prep",
                    "event_civil_mock_test_001",
                    "\ubaa8\uc758\uace0\uc0ac",
                    "\uc8fc\ub9d0 \ubaa8\uc758\uace0\uc0ac \uc77c\uc815\uc774 \uc7a1\ud614\ub2e4.",
                    EventCategory.Special,
                    "\uc751\uc2dc\ud55c\ub2e4", -15000, -2, 6, 2, 0, "\uc9d1\uc5d0\uc11c \ud63c\uc790 \ud478\ub2e4", 0, -1, 3, 1, 0, "\uac74\ub108\ub6f0\ub2e4", 0, 1, -2, -3, 0),
                MakeJob(
                    "Event_CivilPartTime_001",
                    "job_civil_prep",
                    "event_civil_part_time_001",
                    "\ub2e8\uae30 \uc54c\ubc14 \uc81c\uc548",
                    "\ud558\ub8e8 \uc54c\ubc14 \uc81c\uc548\uc774 \uc654\ub2e4.",
                    EventCategory.Consumption,
                    "\ud55c\ub2e4", 80000, -2, 4, -2, 0, "\uac70\uc808\ud55c\ub2e4", 0, 0, -1, 1, 0, "\ubc18\ub098\uc808\ub9cc \ud55c\ub2e4", 40000, -1, 2, 0, 0),
                MakeJob(
                    "Event_CivilSleep_001",
                    "job_civil_prep",
                    "event_civil_sleep_001",
                    "\uc218\uba74 \ubd80\ucc44",
                    "\uba87 \uce60\uc9f8 \uc218\uba74\uc774 \ubd80\uc871\ud558\ub2e4.",
                    EventCategory.Health,
                    "\uc624\ub298\uc740 \uc77c\ucc0d \uc794\ub2e4", 0, 6, -4, 2, 0, "\ucee4\ud53c\ub85c \ubc84\ud2f4\ub2e4", -4000, -3, 2, -1, 0, "\uc218\uba74\uc81c\ub85c \uc870\uc808\ud574\ubcf8\ub2e4", -8000, 3, -2, 0, 0),
                MakeJob(
                    "Event_CivilStudyPlan_001",
                    "job_civil_prep",
                    "event_civil_study_plan_001",
                    "\uacf5\ubd80 \uacc4\ud68d \uc810\uac80",
                    "\uc774\ubc88 \uc8fc \uacf5\ubd80 \uacc4\ud68d\uc774 \ubc00\ub9ac\uace0 \uc788\ub2e4.",
                    EventCategory.Special,
                    "\uacc4\ud68d\ub300\ub85c \ubc00\uace0 \uac04\ub2e4", 0, -3, 5, -2, 0, "\ubc94\uc704\ub97c \uc904\uc778\ub2e4", 0, 0, 2, 1, 0, "\ud558\ub8e8 \uc26e\ub2e4", 0, 3, -4, 3, 0)
            };
        }

        private static List<EventData> CreateFreelancerEvents()
        {
            return new List<EventData>
            {
                MakeJob(
                    "Event_FreelanceBurnout_001",
                    "job_freelancer",
                    "event_freelance_burnout_001",
                    "\ubc88\uc544\uc6c3 \uc9d5\ud6c4",
                    "\uc5f0\uc18d \ub9c8\uac10\uc5d0 \ubab8\uc774 \uba3c\uc800 \ubc18\uc751\ud55c\ub2e4.",
                    EventCategory.Health,
                    "\ud558\ub8e8 \uc26e\ub2e4", 0, 5, -6, 3, -1, "\uc2a4\ucf00\uc904\uc744 \uc904\uc778\ub2e4", 0, 2, -3, 1, 0, "\uce74\ud398\uc778\uc73c\ub85c \ubc84\ud2f4\ub2e4", -5000, -4, 3, -2, 1),
                MakeJob(
                    "Event_FreelanceCowork_001",
                    "job_freelancer",
                    "event_freelance_cowork_001",
                    "\uacf5\uc720\uc624\ud53c\uc2a4",
                    "\uc9d1\uc911\uc774 \uc548 \ub3fc \uacf5\uc720\uc624\ud53c\uc2a4 \ub370\uc774\ud328\uc2a4\uac00 \ub208\uc5d0 \ub744\ub2e4.",
                    EventCategory.Consumption,
                    "\ud558\ub8e8 \uc774\uc6a9\ud55c\ub2e4", -25000, 0, -2, 3, 1, "\uce74\ud398\ub85c \uac04\ub2e4", -12000, 0, 1, 2, 0, "\uc9d1\uc5d0\uc11c \ubc84\ud2f4\ub2e4", 0, 0, 3, -2, 0),
                MakeJob(
                    "Event_FreelanceInvoice_001",
                    "job_freelancer",
                    "event_freelance_invoice_001",
                    "\ub300\uae08 \uc785\uae08 \uc9c0\uc5f0",
                    "\uc9c0\ub09c \ud504\ub85c\uc81d\ud2b8 \ub300\uae08\uc774 \ub2a6\uc5b4\uc9c4\ub2e4.",
                    EventCategory.Consumption,
                    "\ub3c5\ucd09 \uba54\uc77c\uc744 \ubcf4\ub0b8\ub2e4", 0, 0, 4, -2, 1, "\ud558\ub8e8 \ub354 \uae30\ub2e4\ub9b0\ub2e4", 0, 0, 2, -1, 0, "\ud560\uc778\ud574\uc11c\ub77c\ub3c4 \ubc1b\uaca0\ub2e4\uace0 \ud55c\ub2e4", -50000, 0, -2, 1, -1),
                MakeJob(
                    "Event_FreelancePitch_001",
                    "job_freelancer",
                    "event_freelance_pitch_001",
                    "\uc2e0\uaddc \uc81c\uc548\uc11c",
                    "\uc7a0\uc7ac \uace0\uac1d\uc5d0\uac8c \uc81c\uc548\uc11c\ub97c \ubcf4\ub0bc \ud0c0\uc774\ubc0d\uc774\ub2e4.",
                    EventCategory.Work,
                    "\uacf5\ub4e4\uc5ec \ubcf4\ub0b8\ub2e4", 0, -2, 4, 1, 3, "\ud15c\ud50c\ub9bf\uc73c\ub85c \ube60\ub974\uac8c", 0, 0, 2, 0, 1, "\ub2e4\uc74c\uc73c\ub85c \ubbf8\ub8e8\ub2e4", 0, 1, -1, -1, -2),
                MakeJob(
                    "Event_FreelancePortfolio_001",
                    "job_freelancer",
                    "event_freelance_portfolio_001",
                    "\ud3ec\ud2b8\ud3f4\ub9ac\uc624 \uc5c5\ub370\uc774\ud2b8",
                    "\ud3ec\ud2b8\ud3f4\ub9ac\uc624\uac00 \uc624\ub798\ub418\uc5b4 \ubcf4\uc778\ub2e4.",
                    EventCategory.Opportunity,
                    "\uc8fc\ub9d0\uc744 \ud22c\uc790\ud55c\ub2e4", 0, -3, 4, 2, 2, "\ud575\uc2ec\ub9cc \uace0\uce5c\ub2e4", 0, -1, 2, 1, 1, "\ub2e4\uc74c\uc5d0 \ud55c\ub2e4", 0, 1, -1, -1, -1),
                MakeJob(
                    "Event_FreelanceRate_001",
                    "job_freelancer",
                    "event_freelance_rate_001",
                    "\ub2e8\uac00 \ud611\uc0c1",
                    "\uc0c8 \ud504\ub85c\uc81d\ud2b8 \ub2e8\uac00 \ud611\uc0c1 \uc790\ub9ac\ub2e4.",
                    EventCategory.Consumption,
                    "\ub2e8\uac00\ub97c \uc62c\ub9b0\ub2e4", 0, 0, 4, 1, 2, "\ud604\ud589\uc744 \uc720\uc9c0\ud55c\ub2e4", 0, 0, 1, 0, 1, "\uc2f8\uac8c \uc218\uc8fc\ud55c\ub2e4", 0, 0, -2, 2, -1),
                MakeJob(
                    "Event_FreelanceScope_001",
                    "job_freelancer",
                    "event_freelance_scope_001",
                    "\ubc94\uc704 \ucd94\uac00 \uc694\uccad",
                    "\uace0\uac1d\uc774 \uacc4\uc57d \ubc94\uc704\ub97c \ub118\ub294 \uc218\uc815\uc744 \uc694\uccad\ud55c\ub2e4.",
                    EventCategory.Work,
                    "\ucd94\uac00 \uacac\uc801\uc744 \ub0b8\ub2e4", 0, 0, 3, 0, 2, "\uc774\ubc88\ub9cc \ud574\uc900\ub2e4", 0, -3, 2, -1, 1, "\uac70\uc808\ud55c\ub2e4", 0, 0, 1, -2, -2),
                MakeJob(
                    "Event_FreelanceTax_001",
                    "job_freelancer",
                    "event_freelance_tax_001",
                    "\uc138\uae08 \uc2e0\uace0 \uc54c\ub9bc",
                    "\uc138\uae08 \uc2e0\uace0 \ub9c8\uac10\uc774 \ub2e4\uac00\uc628\ub2e4.",
                    EventCategory.Consumption,
                    "\uc138\ubb34\uc0ac\uc5d0\uac8c \ub9e1\uae34\ub2e4", -150000, 0, -3, 2, 0, "\uc9c1\uc811 \ucc98\ub9ac\ud55c\ub2e4", 0, -2, 6, -2, 0, "\ubbf8\ub8e8\ub2e4", 0, 0, 3, -3, 0)
            };
        }

        private static EventData Make(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            string c1, long cash1, int hp1, int st1, int hap1, int co1,
            string c2, long cash2, int hp2, int st2, int hap2, int co2,
            string c3, long cash3, int hp3, int st3, int hap3, int co3)
        {
            return MakeInternal(
                file, id, title, description, category, string.Empty,
                c1, cash1, hp1, st1, hap1, co1,
                c2, cash2, hp2, st2, hap2, co2,
                c3, cash3, hp3, st3, hap3, co3);
        }

        private static EventData MakeJob(
            string file,
            string requiredJobId,
            string id,
            string title,
            string description,
            EventCategory category,
            string c1, long cash1, int hp1, int st1, int hap1, int co1,
            string c2, long cash2, int hp2, int st2, int hap2, int co2,
            string c3, long cash3, int hp3, int st3, int hap3, int co3)
        {
            return MakeInternal(
                file, id, title, description, category, requiredJobId,
                c1, cash1, hp1, st1, hap1, co1,
                c2, cash2, hp2, st2, hap2, co2,
                c3, cash3, hp3, st3, hap3, co3);
        }

        private static EventData MakeInternal(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            string requiredJobId,
            string c1, long cash1, int hp1, int st1, int hap1, int co1,
            string c2, long cash2, int hp2, int st2, int hap2, int co2,
            string c3, long cash3, int hp3, int st3, int hap3, int co3)
        {
            var path = $"{EventsFolder}/{file}.asset";
            var eventData = LoadOrCreate<EventData>(path);
            var conditions = new EventCondition();
            if (!string.IsNullOrEmpty(requiredJobId))
            {
                conditions.EditorConfigure(newRequiredJobId: requiredJobId);
            }

            var choices = new List<EventChoiceData>
            {
                Choice(id + "_a", c1, cash1, hp1, st1, hap1, co1),
                Choice(id + "_b", c2, cash2, hp2, st2, hap2, co2),
                Choice(id + "_c", c3, cash3, hp3, st3, hap3, co3)
            };

            eventData.EditorSetCore(id, title, description, category, 1, 30, 80, conditions, choices);
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
            int company)
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

            return new EventChoiceData(choiceId, text, effects);
        }

        private static List<JobData> LoadAllJobs()
        {
            var list = new List<JobData>();
            var guids = AssetDatabase.FindAssets("t:JobData", new[] { JobsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var job = AssetDatabase.LoadAssetAtPath<JobData>(path);
                if (job != null)
                {
                    list.Add(job);
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
