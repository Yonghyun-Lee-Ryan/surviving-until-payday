using System.Collections.Generic;
using System.IO;
using SurviveUntilPayday.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurviveUntilPayday.EditorTools
{
    /// <summary>
    /// 개발 단위 16: MVP 사건 팩 20개를 생성·갱신한다.
    /// 기존 3개(야근/휴대전화/휴식 fallback)는 값을 재확인하여 갱신하고,
    /// 나머지 17개를 새로 생성한다.
    /// </summary>
    public static class MvpEventPackFactory
    {
        private const string EventsFolder = "Assets/Data/Events";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Surviving Until Payday/Create MVP Event Pack (Unit 16)")]
        public static void CreateMvpEventPack()
        {
            EnsureFolder(EventsFolder);

            var events = new List<EventData>
            {
                CreateOvertime(),
                CreateWedding(),
                CreatePhoneCrack(),
                CreatePhoneRebreak(),
                CreateLunch(),
                CreateDinnerBoss(),
                CreateCold(),
                CreateSale(),
                CreateSideJob(),
                CreateParentsMoney(),
                CreateStockIntro(),
                CreateStockSwing(),
                CreateRent(),
                CreateUtility(),
                CreateCardBill(),
                CreateSleep(),
                CreateBackPain(),
                CreateFriendHangout(),
                CreateSecondHand(),
                CreateSubwayFine(),
                CreateRestFallback(),
                CreateQuitImpulse()
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var warningCount = 0;
            foreach (var eventData in events)
            {
                if (eventData == null)
                {
                    continue;
                }

                foreach (var error in eventData.Validate())
                {
                    warningCount++;
                    Debug.LogWarning($"[MvpEventPackFactory:{eventData.name}] {error}", eventData);
                }
            }

            if (events.Count > 0)
            {
                Selection.activeObject = events[0];
                EditorGUIUtility.PingObject(events[0]);
            }

            if (warningCount == 0)
            {
                Debug.Log(
                    $"[MvpEventPackFactory] MVP 사건 팩 {events.Count}개 생성/갱신 완료. 경고 없음.\n" +
                    "Tools > Surviving Until Payday > Wire All Events To Game Scene (Unit 16)을 실행해 Game Scene에 연결하세요.");
            }
            else
            {
                Debug.LogWarning(
                    $"[MvpEventPackFactory] MVP 사건 팩 {events.Count}개 생성/갱신 완료. 경고 {warningCount}건 발생.");
            }
        }

        [MenuItem("Tools/Surviving Until Payday/Wire All Events To Game Scene (Unit 16)")]
        public static void WireAllEventsToGameScene()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError("[MvpEventPackFactory] Game.unity not found. Run Setup Project Foundation first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var presenter = Object.FindAnyObjectByType<SurviveUntilPayday.UI.GamePlayPresenter>();
            if (presenter == null)
            {
                Debug.LogError(
                    "[MvpEventPackFactory] GamePlayPresenter not found in Game.unity. Run Setup Game Scene UI (Unit 7) first.");
                return;
            }

            var fallback = AssetDatabase.LoadAssetAtPath<EventData>(EventsFolder + "/Event_Rest_Fallback.asset");
            if (fallback == null)
            {
                Debug.LogError(
                    "[MvpEventPackFactory] Event_Rest_Fallback.asset not found. Run Create MVP Event Pack first.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsFolder });
            var allEvents = new List<EventData>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var eventData = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (eventData != null)
                {
                    allEvents.Add(eventData);
                }
            }

            allEvents.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var so = new SerializedObject(presenter);
            var catalogProp = so.FindProperty("eventCatalog");
            catalogProp.ClearArray();
            for (var i = 0; i < allEvents.Count; i++)
            {
                catalogProp.InsertArrayElementAtIndex(i);
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = allEvents[i];
            }

            so.FindProperty("fallbackEvent").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[MvpEventPackFactory] Game Scene eventCatalog에 사건 {allEvents.Count}개를 연결했습니다. " +
                $"fallback = {fallback.name}.");
        }

        private static EventData CreateOvertime()
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

        private static EventData CreateWedding()
        {
            const string path = EventsFolder + "/Event_Wedding_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekendOnly);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_wedding_generous",
                    "넉넉하게 축의금을 낸다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -150_000L),
                        new StatEffect(StatType.Happiness, 8),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "choice_wedding_moderate",
                    "적당한 금액만 낸다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -70_000L),
                        new StatEffect(StatType.Happiness, 3)
                    }),
                new EventChoiceData(
                    "choice_wedding_skip",
                    "바빠서 참석하지 못하고 축의금만 보낸다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -50_000L),
                        new StatEffect(StatType.Happiness, -5),
                        new StatEffect(StatType.Stress, 4)
                    })
            };

            eventData.EditorSetCore(
                "event_wedding_001",
                "친구의 결혼식",
                "오랜 친구가 결혼한다는 소식을 전해왔다. 축의금을 얼마나 준비해야 할까?",
                EventCategory.Relationship,
                5,
                28,
                105,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreatePhoneCrack()
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
                            new StatEffect[] { new StatEffect(StatType.Stress, 8) },
                            new[] { RunFlags.PhoneStillCracked },
                            null,
                            "event_phone_rebreak_001"),
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
                    },
                    null,
                    new List<string> { RunFlags.PhoneStillCracked })
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

        private static EventData CreatePhoneRebreak()
        {
            const string path = EventsFolder + "/Event_PhoneRebreak_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorSetFlags(new[] { RunFlags.PhoneStillCracked }, null);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_phone_rebreak_repair",
                    "또 사설 수리점에 고친다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -110_000L),
                        new StatEffect(StatType.Stress, -2)
                    },
                    null,
                    null,
                    new List<string> { RunFlags.PhoneStillCracked }),
                new EventChoiceData(
                    "choice_phone_rebreak_ignore",
                    "그냥 사용한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 10),
                        new StatEffect(StatType.Happiness, -5)
                    }),
                new EventChoiceData(
                    "choice_phone_rebreak_replace",
                    "공식 서비스센터에서 교체한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -280_000L),
                        new StatEffect(StatType.Stress, -3)
                    },
                    null,
                    null,
                    new List<string> { RunFlags.PhoneStillCracked })
            };

            eventData.EditorSetCore(
                "event_phone_rebreak_001",
                "액정 재고장",
                "싸게 고친 액정에 다시 금이 갔다.",
                EventCategory.Accident,
                2,
                28,
                100,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateLunch()
        {
            const string path = EventsFolder + "/Event_Lunch_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_lunch_restaurant",
                    "동료와 함께 근사한 식당에서 먹는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -18_000L),
                        new StatEffect(StatType.Happiness, 5),
                        new StatEffect(StatType.Stress, -2)
                    }),
                new EventChoiceData(
                    "choice_lunch_convenience",
                    "편의점 도시락으로 해결한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -4_000L),
                        new StatEffect(StatType.Happiness, -1)
                    }),
                new EventChoiceData(
                    "choice_lunch_skip",
                    "다이어트 겸 점심을 거른다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, -4),
                        new StatEffect(StatType.Stress, 3),
                        new StatEffect(StatType.Happiness, -2)
                    })
            };

            eventData.EditorSetCore(
                "event_lunch_001",
                "점심 메뉴 고민",
                "오늘 점심은 무엇을 먹을까? 지갑 사정도 함께 고려해야 한다.",
                EventCategory.Consumption,
                1,
                30,
                120,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateDinnerBoss()
        {
            const string path = EventsFolder + "/Event_DinnerBoss_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekdayOnly);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_dinner_join",
                    "참석해서 분위기를 맞춘다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -10_000L),
                        new StatEffect(StatType.Stress, 6),
                        new StatEffect(StatType.Health, -2),
                        new StatEffect(StatType.CompanyScore, 8)
                    }),
                new EventChoiceData(
                    "choice_dinner_short",
                    "짧게 참석하고 먼저 일어난다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -5_000L),
                        new StatEffect(StatType.Stress, 2),
                        new StatEffect(StatType.CompanyScore, 3)
                    }),
                new EventChoiceData(
                    "choice_dinner_decline",
                    "선약을 핑계로 거절한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.CompanyScore, -10),
                        new StatEffect(StatType.Happiness, 4),
                        new StatEffect(StatType.Stress, -3)
                    })
            };

            eventData.EditorSetCore(
                "event_dinner_boss_001",
                "상사의 회식 제안",
                "퇴근 무렵, 팀장이 오늘 저녁 회식을 제안했다.",
                EventCategory.Work,
                2,
                29,
                105,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateCold()
        {
            const string path = EventsFolder + "/Event_Cold_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_cold_hospital",
                    "병원에 가서 진료를 받는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -30_000L),
                        new StatEffect(StatType.Health, 10),
                        new StatEffect(StatType.Stress, -2)
                    }),
                new EventChoiceData(
                    "choice_cold_pharmacy",
                    "약국에서 감기약만 사 먹는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -8_000L),
                        new StatEffect(StatType.Health, 4)
                    }),
                new EventChoiceData(
                    "choice_cold_ignore",
                    "그냥 참고 출근한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 5),
                        new StatEffect(StatType.CompanyScore, 2)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "cold_recover",
                            "다행히 감기가 금방 나았다.",
                            60,
                            new StatEffect(StatType.Health, -3)),
                        new RandomOutcome(
                            "cold_worsen",
                            "증상이 심해져 몸살이 났다.",
                            40,
                            new StatEffect(StatType.Health, -12),
                            new StatEffect(StatType.Stress, 5))
                    })
            };

            eventData.EditorSetCore(
                "event_cold_001",
                "갑작스러운 감기",
                "아침부터 몸이 으슬으슬하고 목이 아프다.",
                EventCategory.Health,
                1,
                30,
                75,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateSale()
        {
            const string path = EventsFolder + "/Event_Sale_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_sale_moderate",
                    "필요한 만큼만 구매한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -40_000L),
                        new StatEffect(StatType.Happiness, 6)
                    }),
                new EventChoiceData(
                    "choice_sale_skip",
                    "구매하지 않고 넘어간다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, -2)
                    }),
                new EventChoiceData(
                    "choice_sale_splurge",
                    "한도까지 왕창 구매한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -150_000L),
                        new StatEffect(StatType.Happiness, 12),
                        new StatEffect(StatType.Stress, -3)
                    })
            };

            eventData.EditorSetCore(
                "event_sale_001",
                "한정 할인 상품",
                "온라인 쇼핑몰에서 평소 갖고 싶던 물건을 반값에 판매한다는 알림이 떴다.",
                EventCategory.Consumption,
                5,
                30,
                110,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateSideJob()
        {
            const string path = EventsFolder + "/Event_SideJob_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_sidejob_steady",
                    "꾸준히 할 수 있는 만큼만 수락한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 60_000L),
                        new StatEffect(StatType.Stress, 4),
                        new StatEffect(StatType.Health, -2)
                    }),
                new EventChoiceData(
                    "choice_sidejob_light",
                    "짧게 한 번만 도와준다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 25_000L),
                        new StatEffect(StatType.Stress, 1)
                    }),
                new EventChoiceData(
                    "choice_sidejob_overwork",
                    "무리해서 최대한 많이 맡는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 120_000L),
                        new StatEffect(StatType.Stress, 10),
                        new StatEffect(StatType.Health, -8)
                    })
            };

            eventData.EditorSetCore(
                "event_sidejob_001",
                "부업 제안",
                "지인이 주말에 할 수 있는 부업 자리를 소개해줬다.",
                EventCategory.Opportunity,
                3,
                27,
                70,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateParentsMoney()
        {
            const string path = EventsFolder + "/Event_ParentsMoney_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_parents_generous",
                    "넉넉하게 용돈을 보내드린다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -100_000L),
                        new StatEffect(StatType.Happiness, 10),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "choice_parents_moderate",
                    "형편에 맞게 조금만 보내드린다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -30_000L),
                        new StatEffect(StatType.Happiness, 4)
                    }),
                new EventChoiceData(
                    "choice_parents_decline",
                    "이번엔 사정을 말씀드리고 넘어간다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, -6),
                        new StatEffect(StatType.Stress, 3)
                    })
            };

            eventData.EditorSetCore(
                "event_parents_money_001",
                "부모님 용돈",
                "오랜만에 부모님께 안부 전화를 드렸다. 용돈을 챙겨드릴지 고민된다.",
                EventCategory.Relationship,
                1,
                30,
                80,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateStockIntro()
        {
            const string path = EventsFolder + "/Event_StockIntro_001.asset";
            var eventData = LoadOrCreate<EventData>(path);
            var buyFlags = new List<string> { RunFlags.HasBoughtStock };

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_stock_small",
                    "소액만 안전하게 투자해본다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -50_000L)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "stock_small_up",
                            "약간 올라 소소하게 벌었다.",
                            60,
                            new StatEffect(StatType.Cash, 20_000L)),
                        new RandomOutcome(
                            "stock_small_down",
                            "약간 내려 아쉽게 잃었다.",
                            40,
                            new StatEffect[]
                            {
                                new StatEffect(StatType.Cash, -10_000L),
                                new StatEffect(StatType.Stress, 2)
                            },
                            null,
                            null,
                            "event_stock_swing_001")
                    },
                    buyFlags),
                new EventChoiceData(
                    "choice_stock_watch",
                    "투자하지 않고 지켜본다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, -2),
                        new StatEffect(StatType.Stress, -1)
                    }),
                new EventChoiceData(
                    "choice_stock_allin",
                    "가진 돈을 크게 넣는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -200_000L)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "stock_allin_up",
                            "급등해서 크게 벌었다.",
                            50,
                            new StatEffect[] { new StatEffect(StatType.Cash, 400_000L) },
                            new[] { RunFlags.StockBigWin },
                            null,
                            null),
                        new RandomOutcome(
                            "stock_allin_down",
                            "급락해서 크게 잃었다.",
                            50,
                            new StatEffect[]
                            {
                                new StatEffect(StatType.Cash, -100_000L),
                                new StatEffect(StatType.Stress, 15)
                            },
                            null,
                            null,
                            "event_stock_swing_001")
                    },
                    buyFlags)
            };

            eventData.EditorSetCore(
                "event_stock_intro_001",
                "주식 입문",
                "동료가 요즘 뜨는 주식을 추천했다.",
                EventCategory.Opportunity,
                7,
                26,
                70,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateStockSwing()
        {
            const string path = EventsFolder + "/Event_StockSwing_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorSetFlags(new[] { RunFlags.HasBoughtStock }, null);

            var clearStock = new List<string> { RunFlags.HasBoughtStock };
            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_stock_swing_hold",
                    "그냥 들고 간다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 6),
                        new StatEffect(StatType.Happiness, -4)
                    },
                    null,
                    null,
                    clearStock),
                new EventChoiceData(
                    "choice_stock_swing_sell",
                    "수익 실현하고 판다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 90_000L),
                        new StatEffect(StatType.Stress, -4),
                        new StatEffect(StatType.Happiness, 6)
                    },
                    null,
                    new List<string> { RunFlags.StockBigWin },
                    clearStock),
                new EventChoiceData(
                    "choice_stock_swing_cut",
                    "손절하고 판다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -35_000L),
                        new StatEffect(StatType.Stress, 10)
                    },
                    null,
                    null,
                    clearStock)
            };

            eventData.EditorSetCore(
                "event_stock_swing_001",
                "주식 급등락",
                "예전에 산 종목이 요동친다.",
                EventCategory.Opportunity,
                8,
                28,
                120,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateRent()
        {
            const string path = EventsFolder + "/Event_Rent_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_rent_full",
                    "제때 전액 납부한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -580_000L),
                        new StatEffect(StatType.Stress, -2),
                        new StatEffect(StatType.Happiness, 1)
                    }),
                new EventChoiceData(
                    "choice_rent_tight",
                    "다른 지출을 줄여 마련한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -500_000L),
                        new StatEffect(StatType.Happiness, -4)
                    }),
                new EventChoiceData(
                    "choice_rent_loan",
                    "카드 단기 대출로 메꾼다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -620_000L),
                        new StatEffect(StatType.Stress, 8),
                        new StatEffect(StatType.Happiness, -2)
                    },
                    null,
                    new List<string> { RunFlags.OwesDebt })
            };

            eventData.EditorSetCore(
                "event_rent_001",
                "월세 납부일",
                "이번 달 월세를 납부해야 하는 날이다.",
                EventCategory.FixedExpense,
                1,
                30,
                100,
                new EventCondition(),
                choices);
            eventData.EditorSetFixed(true, 1);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateUtility()
        {
            const string path = EventsFolder + "/Event_Utility_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_utility_pay",
                    "바로 납부한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -95_000L),
                        new StatEffect(StatType.Stress, -1)
                    }),
                new EventChoiceData(
                    "choice_utility_save",
                    "아껴 쓴 만큼 절약해 납부한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -75_000L),
                        new StatEffect(StatType.Happiness, -2)
                    }),
                new EventChoiceData(
                    "choice_utility_delay",
                    "납부를 미루고 연체료를 감수한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -110_000L),
                        new StatEffect(StatType.Stress, 6)
                    })
            };

            eventData.EditorSetCore(
                "event_utility_001",
                "공과금 고지서",
                "전기·수도·가스 공과금 고지서가 도착했다.",
                EventCategory.FixedExpense,
                1,
                30,
                100,
                new EventCondition(),
                choices);
            eventData.EditorSetFixed(true, 10);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateCardBill()
        {
            const string path = EventsFolder + "/Event_CardBill_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_card_full",
                    "전액 결제한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -200_000L),
                        new StatEffect(StatType.Stress, -3),
                        new StatEffect(StatType.Happiness, 1)
                    }),
                new EventChoiceData(
                    "choice_card_partial",
                    "일부만 결제하고 나머지는 이월한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -100_000L),
                        new StatEffect(StatType.Stress, 5)
                    },
                    null,
                    new List<string> { RunFlags.OwesDebt }),
                new EventChoiceData(
                    "choice_card_minimum",
                    "리볼빙으로 최소 금액만 결제한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -40_000L),
                        new StatEffect(StatType.Stress, 10)
                    },
                    null,
                    new List<string> { RunFlags.OwesDebt })
            };

            eventData.EditorSetCore(
                "event_card_bill_001",
                "카드값 결제일",
                "이번 달 카드 대금 결제일이 돌아왔다.",
                EventCategory.FixedExpense,
                1,
                30,
                100,
                new EventCondition(),
                choices);
            eventData.EditorSetFixed(true, 15);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateSleep()
        {
            const string path = EventsFolder + "/Event_Sleep_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_sleep_early",
                    "일찍 잠자리에 든다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, 6),
                        new StatEffect(StatType.Stress, -4),
                        new StatEffect(StatType.Happiness, 1)
                    }),
                new EventChoiceData(
                    "choice_sleep_coffee",
                    "커피로 버틴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -3_000L),
                        new StatEffect(StatType.Stress, 3),
                        new StatEffect(StatType.Health, -2)
                    }),
                new EventChoiceData(
                    "choice_sleep_allnight",
                    "밤새 하고 싶은 걸 하며 버틴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, -8),
                        new StatEffect(StatType.Happiness, 5),
                        new StatEffect(StatType.Stress, -2)
                    })
            };

            eventData.EditorSetCore(
                "event_sleep_001",
                "수면 부족",
                "요 며칠 잠을 제대로 못 자서 눈이 뻑뻑하다.",
                EventCategory.Health,
                1,
                30,
                75,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateBackPain()
        {
            const string path = EventsFolder + "/Event_BackPain_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_backpain_therapy",
                    "병원에서 물리치료를 받는다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -50_000L),
                        new StatEffect(StatType.Health, 10),
                        new StatEffect(StatType.Stress, -2)
                    }),
                new EventChoiceData(
                    "choice_backpain_selfcare",
                    "안마의자·파스로 버틴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -12_000L),
                        new StatEffect(StatType.Health, 3)
                    }),
                new EventChoiceData(
                    "choice_backpain_ignore",
                    "무시하고 계속 일한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, -10),
                        new StatEffect(StatType.Stress, 4),
                        new StatEffect(StatType.CompanyScore, 2)
                    })
            };

            eventData.EditorSetCore(
                "event_backpain_001",
                "허리 통증",
                "오래 앉아 있었더니 허리가 뻐근하고 아프다.",
                EventCategory.Health,
                1,
                30,
                70,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateFriendHangout()
        {
            const string path = EventsFolder + "/Event_FriendHangout_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekendOnly);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_hangout_full",
                    "약속 장소에서 즐겁게 논다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -35_000L),
                        new StatEffect(StatType.Happiness, 10),
                        new StatEffect(StatType.Stress, -6)
                    }),
                new EventChoiceData(
                    "choice_hangout_nearby",
                    "집 근처에서 가볍게 만난다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -12_000L),
                        new StatEffect(StatType.Happiness, 6),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "choice_hangout_cancel",
                    "피곤해서 약속을 취소한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, -8),
                        new StatEffect(StatType.Stress, -2),
                        new StatEffect(StatType.Health, 2)
                    })
            };

            eventData.EditorSetCore(
                "event_friend_hangout_001",
                "친구와의 약속",
                "오랜만에 친구들이 주말에 만나자고 연락이 왔다.",
                EventCategory.Relationship,
                1,
                30,
                115,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateSecondHand()
        {
            const string path = EventsFolder + "/Event_SecondHand_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_secondhand_fair",
                    "적당한 가격에 안전하게 거래한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 30_000L),
                        new StatEffect(StatType.Happiness, 2)
                    }),
                new EventChoiceData(
                    "choice_secondhand_quick",
                    "가격을 낮춰 빠르게 처분한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, 15_000L),
                        new StatEffect(StatType.Stress, -2)
                    }),
                new EventChoiceData(
                    "choice_secondhand_risky",
                    "직거래 약속을 잡고 비싸게 팔아본다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, 2)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "secondhand_success",
                            "무사히 비싸게 팔았다.",
                            65,
                            new StatEffect(StatType.Cash, 60_000L),
                            new StatEffect(StatType.Happiness, 4)),
                        new RandomOutcome(
                            "secondhand_noshow",
                            "상대방이 노쇼를 해서 시간만 버렸다.",
                            35,
                            new StatEffect(StatType.Happiness, -5),
                            new StatEffect(StatType.Stress, 5))
                    })
            };

            eventData.EditorSetCore(
                "event_secondhand_001",
                "중고거래",
                "안 쓰는 물건을 중고 거래 앱에 올렸더니 연락이 왔다.",
                EventCategory.Opportunity,
                1,
                30,
                75,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateSubwayFine()
        {
            const string path = EventsFolder + "/Event_SubwayFine_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var conditions = new EventCondition();
            conditions.EditorConfigure(newDayOfWeekConstraint: DayOfWeekConstraint.WeekdayOnly);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_subway_taxi",
                    "택시로 갈아타 제시간에 도착한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -15_000L),
                        new StatEffect(StatType.Stress, 2),
                        new StatEffect(StatType.CompanyScore, 2)
                    }),
                new EventChoiceData(
                    "choice_subway_run",
                    "역에서부터 뛰어가 시간에 맞춰본다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, -3),
                        new StatEffect(StatType.Stress, 4)
                    },
                    new List<RandomOutcome>
                    {
                        new RandomOutcome(
                            "subway_ontime",
                            "가까스로 도착했다.",
                            60,
                            new StatEffect(StatType.CompanyScore, 1)),
                        new RandomOutcome(
                            "subway_late",
                            "결국 지각했다.",
                            40,
                            new StatEffect(StatType.CompanyScore, -6),
                            new StatEffect(StatType.Stress, 3))
                    }),
                new EventChoiceData(
                    "choice_subway_giveup",
                    "그냥 포기하고 늦게 도착한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.CompanyScore, -10),
                        new StatEffect(StatType.Stress, 5),
                        new StatEffect(StatType.Happiness, -2)
                    })
            };

            eventData.EditorSetCore(
                "event_subway_fine_001",
                "지하철 지각 소동",
                "출근길 지하철이 갑자기 지연되어 지각할 위기에 처했다.",
                EventCategory.Accident,
                1,
                30,
                85,
                conditions,
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateRestFallback()
        {
            const string path = EventsFolder + "/Event_Rest_Fallback.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "rest_home",
                    "집에서 쉰다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, -5),
                        new StatEffect(StatType.Happiness, 3)
                    }),
                new EventChoiceData(
                    "rest_walk",
                    "산책한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Health, 3),
                        new StatEffect(StatType.Stress, -3)
                    }),
                new EventChoiceData(
                    "rest_hobby",
                    "취미를 즐긴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, 6),
                        new StatEffect(StatType.Cash, -8_000L)
                    })
            };

            eventData.EditorSetCore(
                "event_rest_fallback",
                "여유로운 하루",
                "특별히 급한 일은 없다. 어떻게 보내볼까?",
                EventCategory.Rest,
                1,
                30,
                50,
                new EventCondition(),
                choices);

            EditorUtility.SetDirty(eventData);
            return eventData;
        }

        private static EventData CreateQuitImpulse()
        {
            const string path = EventsFolder + "/Event_QuitImpulse_001.asset";
            var eventData = LoadOrCreate<EventData>(path);

            var choices = new List<EventChoiceData>
            {
                new EventChoiceData(
                    "choice_quit_endure",
                    "마음을 다잡고 하루 더 버틴다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Stress, -8),
                        new StatEffect(StatType.Happiness, 3),
                        new StatEffect(StatType.CompanyScore, 2)
                    }),
                new EventChoiceData(
                    "choice_quit_vacation",
                    "휴가를 내고 재충전한다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Cash, -20_000L),
                        new StatEffect(StatType.Stress, -12),
                        new StatEffect(StatType.Happiness, 8),
                        new StatEffect(StatType.Health, 3)
                    }),
                new EventChoiceData(
                    "choice_quit_venting",
                    "홧김에 사직서를 만지작거리며 화를 푼다",
                    new List<StatEffect>
                    {
                        new StatEffect(StatType.Happiness, 10),
                        new StatEffect(StatType.Stress, -15),
                        new StatEffect(StatType.CompanyScore, -20)
                    })
            };

            eventData.EditorSetCore(
                "event_quit_impulse_001",
                "퇴사 충동",
                "반복되는 하루에 지쳐 문득 퇴사하고 싶은 충동이 든다.",
                EventCategory.Special,
                20,
                30,
                95,
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
                Debug.LogError($"[MvpEventPackFactory] Invalid folder path: {assetPath}");
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
