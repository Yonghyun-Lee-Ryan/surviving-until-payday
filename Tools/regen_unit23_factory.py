# -*- coding: utf-8 -*-
"""Regenerate ContentPackUnit23Factory.cs from Unit 23 event assets."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
EVENTS = ROOT / "Assets" / "Data" / "Events"
OUT = ROOT / "Assets" / "Scripts" / "Editor" / "ContentPackUnit23Factory.cs"

CAT_NAME = {
    0: "FixedExpense",
    1: "Work",
    2: "Health",
    3: "Consumption",
    4: "Relationship",
    5: "Opportunity",
    6: "Accident",
    7: "Rest",
    8: "Special",
}

GENERAL_FILES = {
    "Event_CommuteRain_001",
    "Event_TeamDinner_001",
    "Event_GymTrial_001",
    "Event_DeliveryTip_001",
    "Event_NeighborNoise_001",
    "Event_OnlineCourse_001",
    "Event_FamilyCall_001",
    "Event_CoffeeMachine_001",
    "Event_WeekendPlan_001",
    "Event_PackageDelay_001",
    "Event_OfficeAC_001",
    "Event_Subscription_001",
    "Event_LateNightSnack_001",
    "Event_MeetingPrep_001",
    "Event_LostCard_001",
    "Event_ParkWalk_001",
    "Event_GroupChat_001",
    "Event_InsuranceCall_001",
}


def esc(s: str) -> str:
    return "".join(f"\\u{ord(ch):04x}" if ord(ch) > 127 else ch for ch in s)


def parse_event(path: pathlib.Path):
    text = path.read_text(encoding="utf-8")
    eid = re.search(r"^  id: (.+)$", text, re.M).group(1).strip()
    title = re.search(r'^  title: "(.*)"$', text, re.M).group(1)
    desc = re.search(r'^  description: "(.*)"$', text, re.M).group(1)
    cat = int(re.search(r"^  category: (\d+)$", text, re.M).group(1))
    job_m = re.search(r"^    requiredJobId: (.*)$", text, re.M)
    job = job_m.group(1).strip() if job_m else ""
    texts = re.findall(r'^    text: "(.*)"$', text, re.M)
    parts = re.split(r"^  - choiceId:", text, flags=re.M)[1:]
    names = ["Cash", "Health", "Stress", "Happiness", "CompanyScore"]
    choices = []
    for part, choice_text in zip(parts, texts):
        effects = {n: 0 for n in names}
        for st, val in re.findall(r"statType: (\d+)\n      value: (-?\d+)", part):
            effects[names[int(st)]] = int(val)
        choices.append(
            (
                choice_text,
                effects["Cash"],
                effects["Health"],
                effects["Stress"],
                effects["Happiness"],
                effects["CompanyScore"],
            )
        )
    return path.stem, eid, title, desc, CAT_NAME[cat], job, choices


def make_call(event, job: bool = False) -> str:
    file, eid, title, desc, cat, jobid, choices = event
    assert len(choices) == 3
    args = []
    for text, cash, hp, st, hap, co in choices:
        args.extend([f'"{esc(text)}"', str(cash), str(hp), str(st), str(hap), str(co)])
    joined = ", ".join(args)
    if job:
        return f"""                MakeJob(
                    "{file}",
                    "{jobid}",
                    "{eid}",
                    "{esc(title)}",
                    "{esc(desc)}",
                    EventCategory.{cat},
                    {joined})"""
    return f"""                Make(
                    "{file}",
                    "{eid}",
                    "{esc(title)}",
                    "{esc(desc)}",
                    EventCategory.{cat},
                    {joined})"""


def main() -> None:
    general, civil, free = [], [], []
    for path in sorted(EVENTS.glob("Event_*.asset")):
        name = path.stem
        if name in GENERAL_FILES or name.startswith("Event_Civil") or name.startswith(
            "Event_Freelance"
        ):
            event = parse_event(path)
            if event[5] == "job_civil_prep":
                civil.append(event)
            elif event[5] == "job_freelancer":
                free.append(event)
            else:
                general.append(event)

    gen_calls = ",\n".join(make_call(e) for e in general)
    civil_calls = ",\n".join(make_call(e, True) for e in civil)
    free_calls = ",\n".join(make_call(e, True) for e in free)

    cs = f"""using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{{
    /// <summary>
    /// Unit 23: 직업 2개 갱신 + 일반 사건 +18 + 직업 전용 8+8.
    /// </summary>
    public static class ContentPackUnit23Factory
    {{
        private const string JobsFolder = "Assets/Data/Jobs";
        private const string EventsFolder = "Assets/Data/Events";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Surviving Until Payday/Create Content Pack (Unit 23)")]
        public static void CreateContentPack()
        {{
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
            {{
                if (e == null)
                {{
                    continue;
                }}

                foreach (var err in e.Validate())
                {{
                    warnings++;
                    Debug.LogWarning($"[ContentPackUnit23:{{e.name}}] {{err}}", e);
                }}
            }}

            Debug.Log(
                $"[ContentPackUnit23] 사건 {{created.Count}}개 + 직업 갱신 완료. 경고={{warnings}}.\\n" +
                "이어서 Wire All Events / Wire Jobs To Game Scene / Setup MainMenu Run Start를 실행하세요.");
        }}

        [MenuItem("Tools/Surviving Until Payday/Wire Jobs To Game Scene (Unit 23)")]
        public static void WireJobsToGameScene()
        {{
            if (!File.Exists(GameScenePath))
            {{
                Debug.LogError("[ContentPackUnit23] Game.unity missing.");
                return;
            }}

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<SurviveUntilPayday.UI.GamePlayPresenter>();
            if (presenter == null)
            {{
                Debug.LogError("[ContentPackUnit23] GamePlayPresenter missing.");
                return;
            }}

            var jobs = LoadAllJobs();
            var so = new SerializedObject(presenter);
            var allJobs = so.FindProperty("allJobs");
            allJobs.ClearArray();
            for (var i = 0; i < jobs.Count; i++)
            {{
                allJobs.InsertArrayElementAtIndex(i);
                allJobs.GetArrayElementAtIndex(i).objectReferenceValue = jobs[i];
            }}

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ContentPackUnit23] GamePlayPresenter.allJobs = {{jobs.Count}}");
        }}

        private static void CreateOrUpdateJobs()
        {{
            UpsertJob(
                "Job_JuniorOffice.asset",
                "job_junior_office",
                "\\uC911\\uC18C\\uAE30\\uC5C5 \\uC2E0\\uC785\\uC0AC\\uC6D0",
                "\\uD45C\\uC900 \\uB09C\\uB3C4\\uC758 \\uC9C1\\uC7A5 \\uC0DD\\uD65C. \\uC6D4\\uAE09\\uC740 \\uC548\\uC815\\uC801\\uC774\\uC9C0\\uB9CC \\uC0DD\\uD65C\\uBE44 \\uBD80\\uB2F4\\uB3C4 \\uB9CC\\uB9CC\\uCE58 \\uC54A\\uB2E4.",
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
                "\\uACF5\\uBB34\\uC6D0 \\uC900\\uBE44\\uC0DD",
                "\\uB0AE\\uC740 \\uC218\\uC785\\uC774\\uC9C0\\uB9CC \\uC9C1\\uC7A5 \\uC0AC\\uAC74\\uC740 \\uC801\\uB2E4. \\uACF5\\uBD80\\uC640 \\uC2DC\\uD5D8 \\uC2A4\\uD2B8\\uB808\\uC2A4\\uAC00 \\uC9C3\\uB2E4.",
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
                "\\uD504\\uB9AC\\uB79C\\uC11C",
                "\\uC218\\uC785 \\uBCC0\\uB3D9\\uC774 \\uD070 \\uC790\\uC720\\uB85C\\uC6B4 \\uC77C. \\uD504\\uB85C\\uC81D\\uD2B8\\uC640 \\uACE0\\uAC1D \\uAD00\\uB9AC\\uAC00 \\uC0B6\\uC744 \\uC88B\\uC74C.",
                3,
                2_200_000L,
                2_400_000L,
                70,
                30,
                55,
                15);
        }}

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
        {{
            var path = $"{{JobsFolder}}/{{fileName}}";
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
        }}

        private static List<EventData> CreateGeneralEvents()
        {{
            return new List<EventData>
            {{
{gen_calls}
            }};
        }}

        private static List<EventData> CreateCivilPrepEvents()
        {{
            return new List<EventData>
            {{
{civil_calls}
            }};
        }}

        private static List<EventData> CreateFreelancerEvents()
        {{
            return new List<EventData>
            {{
{free_calls}
            }};
        }}

        private static EventData Make(
            string file,
            string id,
            string title,
            string description,
            EventCategory category,
            string c1, long cash1, int hp1, int st1, int hap1, int co1,
            string c2, long cash2, int hp2, int st2, int hap2, int co2,
            string c3, long cash3, int hp3, int st3, int hap3, int co3)
        {{
            return MakeInternal(
                file, id, title, description, category, string.Empty,
                c1, cash1, hp1, st1, hap1, co1,
                c2, cash2, hp2, st2, hap2, co2,
                c3, cash3, hp3, st3, hap3, co3);
        }}

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
        {{
            return MakeInternal(
                file, id, title, description, category, requiredJobId,
                c1, cash1, hp1, st1, hap1, co1,
                c2, cash2, hp2, st2, hap2, co2,
                c3, cash3, hp3, st3, hap3, co3);
        }}

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
        {{
            var path = $"{{EventsFolder}}/{{file}}.asset";
            var eventData = LoadOrCreate<EventData>(path);
            var conditions = new EventCondition();
            if (!string.IsNullOrEmpty(requiredJobId))
            {{
                conditions.EditorConfigure(newRequiredJobId: requiredJobId);
            }}

            var choices = new List<EventChoiceData>
            {{
                Choice(id + "_a", c1, cash1, hp1, st1, hap1, co1),
                Choice(id + "_b", c2, cash2, hp2, st2, hap2, co2),
                Choice(id + "_c", c3, cash3, hp3, st3, hap3, co3)
            }};

            eventData.EditorSetCore(id, title, description, category, 1, 30, 80, conditions, choices);
            EditorUtility.SetDirty(eventData);
            return eventData;
        }}

        private static EventChoiceData Choice(
            string choiceId,
            string text,
            long cash,
            int health,
            int stress,
            int happiness,
            int company)
        {{
            var effects = new List<StatEffect>();
            if (cash != 0)
            {{
                effects.Add(new StatEffect(StatType.Cash, cash));
            }}

            if (health != 0)
            {{
                effects.Add(new StatEffect(StatType.Health, health));
            }}

            if (stress != 0)
            {{
                effects.Add(new StatEffect(StatType.Stress, stress));
            }}

            if (happiness != 0)
            {{
                effects.Add(new StatEffect(StatType.Happiness, happiness));
            }}

            if (company != 0)
            {{
                effects.Add(new StatEffect(StatType.CompanyScore, company));
            }}

            return new EventChoiceData(choiceId, text, effects);
        }}

        private static List<JobData> LoadAllJobs()
        {{
            var list = new List<JobData>();
            var guids = AssetDatabase.FindAssets("t:JobData", new[] {{ JobsFolder }});
            foreach (var guid in guids)
            {{
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var job = AssetDatabase.LoadAssetAtPath<JobData>(path);
                if (job != null)
                {{
                    list.Add(job);
                }}
            }}

            return list;
        }}

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {{
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {{
                return existing;
            }}

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }}

        private static void EnsureFolder(string assetPath)
        {{
            if (AssetDatabase.IsValidFolder(assetPath))
            {{
                return;
            }}

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {{
                return;
            }}

            if (!AssetDatabase.IsValidFolder(parent))
            {{
                EnsureFolder(parent);
            }}

            AssetDatabase.CreateFolder(parent, folderName);
        }}
    }}
}}
"""
    OUT.write_text(cs, encoding="utf-8")
    print(f"wrote {OUT} general={len(general)} civil={len(civil)} free={len(free)}")


if __name__ == "__main__":
    main()
