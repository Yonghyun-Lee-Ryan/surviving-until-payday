# 밸런스 노트 (Unit 27)

## 목표 KPI

- **기준:** `Job_JuniorOffice`, **trait=null**(첫 회차에 가깝게), **Random** 정책, 1,000회
- Day 7 생존(도달)률 ≈ **70%**
- Day 15 생존(도달)률 ≈ **50%**
- Day 30 **성공**률: 너무 쉽지도, 불가능하지도 않게 (대략 15~35% 구간 목표)
- Day 1 즉시 파산 비율 과다 방지
- 후반(22~30일) 긴장 유지 + Rest/Health 회복 사건 비율 유지

## 1차 패스 (완화)

### 조정 전 가설

- 시작 현금 280만 + **Day1 고정 월세 60만** → Safe 정책도 1주 내 현금 압박
- **Day15 카드값** + Unit 19 `DifficultyScaler`(현금 손실 1.1x~) → 중반 일괄 지출 부담
- `Event_Sale_001` / `Event_StockIntro_001` **minDay=1~4** → Random/Risky에서 2~5일차 all-in 빈도 과다 우려
- `Event_Rest_Fallback` weight 50 → 회복·완충 사건 상대 빈도 낮음

### 1차 조정

| 사건 | 변경 |
|------|------|
| `Event_Rent_001` | 전액 -600k→**-520k**, 절약 -600k→**-450k**, 대출 -650k→**-580k** |
| `Event_CardBill_001` | 전액 -200k→**-170k**, 일부 -100k→**-85k**, 최소 -40k→**-35k** |
| `Event_Overtime_001` | Safe 체력 -5→**-4**, 스트레스 12→**10** |
| `Event_Rest_Fallback` | weight 50→**80** |
| `Event_Sale_001` | minDay 1→**5** |
| `Event_StockIntro_001` | minDay 4→**7** |
| `Event_Sleep_001` / `Cold` / `BackPain` | weight 상향 |

### 1차 측정 (2026-07-24 17:42)

- 리포트: `balance_pass_20260724_174220.txt`
- Random KPI: Day7=**100.0%**, Day15=**100.0%**, Day30Success=**97.0%**, Day1Fail=**0.0%**
- 판정: **과도하게 쉬움** → 2차 패스(강화) 필요

## 2차 패스 (강화, 현재)

### 근거

1차 완화 후 Random이 거의 전 구간 생존. Day1은 이미 안전(0%)이므로 고정비를 원점에 가깝게 되돌리고, Rest/회복 weight를 낮추며 소비·후반 Special을 올려 중·후반 실패를 만든다. 시스템/`DifficultyScaler`는 변경하지 않음.

### 2차 조정

| 사건 | 변경 |
|------|------|
| `Event_Rent_001` | 전액 **-580k**, 절약 **-500k**, 대출 **-620k** |
| `Event_CardBill_001` | 전액 **-200k**, 일부 **-100k**(스트레스 5), 최소 **-40k**(스트레스 10) |
| `Event_Utility_001` | 납부 **-95k**, 절약 **-75k**, 연체 **-110k** |
| `Event_Overtime_001` | Safe 체력 **-5**, 스트레스 **12** (1차 완화 되돌림) |
| `Event_Rest_Fallback` | weight 80→**50** |
| `Event_Sleep_001` | weight 95→**75** |
| `Event_Cold_001` | weight 95→**75** |
| `Event_BackPain_001` | weight 90→**70** |
| `Event_QuitImpulse_001` | weight 60→**95** |
| `Event_Lunch_001` | weight 100→**120** |
| `Event_FriendHangout_001` | weight 90→**115** |
| `Event_Wedding_001` | weight 80→**105** |
| `Event_Sale_001` | weight 90→**110** (minDay 5 유지) |
| `Event_DinnerBoss_001` | weight 90→**105** |

`MvpEventPackFactory` Create* 메서드도 동일 수치로 맞춤.

### 리포트 보강 (Unit 27 도구)

- `SimulationSummary`: `SurvivalCurve`(D1/3/5/7/10/15/21/28/30) + `Bucket:D1-7|8-14|15-21|22-30` (ends / failRate / avgEndCash)
- `BalancePassRunner`: Random KPI vs 목표(±pp) 요약 출력, `Docs/BalanceNotes.md` append

## 적용 지점: EffectResolver cash-loss difficulty (Unit 19)

- `DifficultyScaler.GetMultiplier(day)` — Day 8~14: 1.1x, 15~21: 1.2x, 22~27: 1.35x, 28~30: 1.5x
- **현금 감소(StatType.Cash < 0)** 에만 적용; 이번 패스는 **이벤트 base/weight만** 조정

## 조정 후 시뮬

Editor에서 재측정:

1. **메뉴:** `Tools → Surviving Until Payday → Run Balance Pass Report (Unit 27)`
2. **또는** `Run Simulator Window` → **Balance Pass (4 policies × 1,000)**

출력:

- `Logs/balance_pass_YYYYMMDD_HHmmss.txt` — Random/Safe/Thrifty/Risky 4정책 리포트
- Console + 본 문서 「조정 후 시뮬」 섹션에 Random KPI 자동 append

### 다음 패스 메모

- Random Day7이 여전히 >85%면 Rest weight를 더 낮추거나 소비 사건 효과를 소폭 강화
- Day1Fail >5%면 Rent 전액만 소폭 완화 (−560k 근처)
- Day30Success <10%면 QuitImpulse weight 또는 Utility만 되돌림

## 조정 후 시뮬

### 2026-07-24 17:42 측정 (1차 후)
- 리포트: `balance_pass_20260724_174220.txt`
- Random KPI: Day7=100.0 %, Day15=100.0 %, Day30Success=97.0 %, Day1Fail=0.0 %

### 2026-07-26 21:18 측정
- 리포트: `balance_pass_20260726_211831.txt`
- Random KPI: Day7=100.0 %, Day15=100.0 %, Day30Success=98.7 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=100.0 % (목표 50 %, +50.0pp)

### 2026-08-25 17:17 측정
- 리포트: `balance_pass_20260825_171739.txt`
- Random KPI: Day7=100.0 %, Day15=100.0 %, Day30Success=98.1 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=100.0 % (목표 50 %, +50.0pp)

## 3차 패스 (R-QA-03, 릴리즈 KPI)

### 근거

2차·3a(3b) 후에도 Random Day7/D15=100%, Day30Success≈98%. 실패가 D22~30에만 집중 → 고정비·소비 weight·스트레스 누적을 대폭 강화하고 Rest/부업·주식 income을 축소. `freelancer+Risky`(qa09)는 프리랜서 수입·세금 defer 완화로 하한(10~25%) 확보.

### 3차 조정 (MvpEventPackFactory + Unit23)

| 사건 | 변경 |
|------|------|
| `Event_Rent_001` | 전액 **-950k**, 절약 **-830k**, 대출 **-990k** |
| `Event_Utility_001` | **-195k/-162k/-225k**, fixedDay **4** |
| `Event_CardBill_001` | **-420k/-210k/-90k**, fixedDay **8** |
| `Event_Lunch_001` | weight **240**, skip stress **+15** health **-12** |
| `Event_Overtime_001` | stress **30**, weight **180** |
| `Event_Rest_Fallback` | weight **3**, 회복 효과 축소 |
| `Event_Sale_001` | splurge **-420k**, weight **175** |
| `Event_SideJob_001` | income↓, weight **18** |
| `Event_QuitImpulse_001` | weight **150**, venting stress **+5** |
| `Event_FriendHangout_001` | weight **160**, full **-52k** |
| `Event_Wedding_001` | generous **-185k**, weight **155** |
| `Event_Cold_001` | weight **45** |
| `Event_ParentsMoney_001` | generous **-120k**, weight **75** |
| `Event_StockIntro_001` | weight **55** |
| Freelance (Unit23) | weight **130**, Pitch/Invoice defer **130k**, Cowork 집 **100k**, Rate/Scope Risky 수입↑ |

**재측정:** `Tools → Apply Balance Pass 3 + Measure (R-QA-03)` 또는 batch `Rqa03BalanceRunner.RunFromBatch`

### 2026-08-25 17:45 측정
- 리포트: `balance_pass_20260825_174510.txt`
- Random KPI: Day7=100.0 %, Day15=99.7 %, Day30Success=86.7 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=99.7 % (목표 50 %, +49.7pp)

### 2026-08-25 18:09 측정
- 리포트: `balance_pass_20260825_180909.txt`
- Random KPI: Day7=100.0 %, Day15=98.8 %, Day30Success=73.0 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=98.8 % (목표 50 %, +48.8pp)

### 2026-08-25 18:23 측정
- 리포트: `balance_pass_20260825_182329.txt`
- Random KPI: Day7=100.0 %, Day15=97.8 %, Day30Success=64.1 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=97.8 % (목표 50 %, +47.8pp)

### 2026-08-25 18:38 측정 (최종 채택)
- 리포트: `balance_pass_20260825_183853.txt`
- 캠페인: `release_qa_campaign_20260825_183853.txt`
- Random KPI: Day7=100.0 %, Day15=98.8 %, Day30Success=**73.2 %**, Day1Fail=0.0 %
- Safe Success=**51.8 %** (≤90% OK), Risky=13.8 %, Thrifty=99.9 %
- qa04 Random 캠페인: 70~90 % / qa09 freelancer+Risky: **0~5 %** (목표 10~25% 미달)
- vs Target: Day30Success 여전히 HIGH(+38pp). Day7/D15 생존 곡선은 개선 전과 동일(초반 100%)
- 판정: **98%→73%로 대폭 하향**했으나 KPI 완전 충족은 `startingCash=월급`·Random 균등 선택 구조상 이벤트만으로 한계. R-QA-04 전 추가 미세조정 가능.

## R-QA-04 엔딩 다양성 (2026-08-25)

### 조정

| 대상 | 변경 |
|------|------|
| `ending_cash_king` | minCash **1,000,000→1,400,000**, priority **100→73** (healthy/promotion/happy보다 낮음) |
| `ending_promotion` | 회사 **80→60** + 필수 플래그 `promotionTrack` (야근 완수·회식 참석), priority **85→95** |
| `ending_barely_survived` | maxCash **900,000**, priority 8. cash_king이 아슬아슬 구간이면 fallback로 강등 |
| `ending_hospital` | 체력 0 외에 `neglectedHealth` + 체력≤35 입원 (점심 거르기·감기/허리 무시) |

### 캠페인 재측정

- 리포트: `Logs/release_qa_campaign_20260825_190415.txt`
- Unity: 6000.5.4f1, 10×5×40 = 2,000런
- 상위 1엔딩: `ending_burnout` **390/2000 (19.5%)** ≤40%
- `ending_cash_king` **105/2000 (5.3%)** (기존 1064/2000 ≈53%)
- `ending_promotion` **297/2000 (14.9%)** ≥2%
- `ending_hospital` **60/2000 (3.0%)** 경로 smoke
- `ending_barely_survived` **106/2000 (5.3%)** (기존 29)
- EditMode: `EndingEvaluatorTests` + `GameStateStatTests` 28 passed

## R-QA-05 메타 성장 체감 (2026-08-25)

상점·IAP는 복구하지 않음. 성장 피드백은 XP/레벨 해금, 도감 해금률·다음 목표, 업적 SO, 일일 미션 보상 카피, 출석 스트릭 XP만 사용.

- 해금 곡선: 100 XP → Lv.2 공무원 준비생·체력왕 / 300 → Lv.3 프리랜서·긍정왕 / 600 → Lv.4 야근전문가
- 출석: `5 × min(연속일, 7)` XP, 하루 1회, 결석 시 스트릭 1 리셋
- 문서: `Docs/MetaUnlockScenario.md`
- 시나리오: `Logs/meta_unlock_scenario_20260825_192322.txt` **PASS** (Unity 6000.5.4f1)
- EditMode: 44 passed / 0 failed (`Rqa05MetaGrowthTests` 등)

## R-QA-06 UX·튜토리얼·레이어 (2026-08-25)

시뮬이 못 보던 튜토리얼·모달 레이어·광고 실패 문구·공유 훅. 상점 없음.

- G9: 튜토리얼 「실패해도 됩니다」+ 안전만 고르면 엔딩이 비슷 / 결과·주간 팁 동일
- G6: 결과 화면 「엔딩 기록 복사」(클립보드)
- G10: 설정 「선택 미리보기」 기본 OFF, 설정에서 켜야 표시 (settings schema 4)
- HUD가 결과/주간결산을 가리지 않도록 `UiModalLayer.RestackModalsAboveHud`
- 문서: `Docs/Rqa06UxChecklist.md`
- Unity 6000.5.4f1: 체크리스트 **PASS** `Logs/rqa06_ux_checklist_20260825_194239.txt`
- EditMode 30 passed / PlayMode 1 passed (`Logs/rqa06_editmode.xml`, `Logs/rqa06_playmode.xml`)

## R-QA-07 콘텐츠 밀도 (2026-08-25)

상점·IAP는 복구하지 않음. 기존 팩토리 패턴만 사용 (`ContentPackRqa07Factory`).

- 직업 +1 `job_corp_associate` (Lv.5, 전용 사건 4)
- 특성 +5 (인맥왕·올빼미 Lv.5, 착한 사람·강철 위장 Lv.6, 선 긋기 Lv.7)
- 관계 플래그 5 + 입문/후속 사건 11. 후속은 선택 시 플래그를 지워 **1회만** 등장 (주식 스윙과 동일)
- Random KPI: 팩 추가 직후 71.8% → 후속 1회화 후 **73.9%** (R-QA-03 최종 73.0%와 동대). 재조정 없음
- qa05 civil+Safe 0% / AvgDays≈11은 **R-QA-04 캠페인(사건 56)부터 동일**. 본 팩 회귀 아님
- 문서: `Docs/Rqa07ContentPack.md`

### 2026-08-25 19:58 측정
- 리포트: `balance_pass_20260825_195827.txt`
- Random KPI: Day7=100.0 %, Day15=98.8 %, Day30Success=71.8 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=98.8 % (목표 50 %, +48.8pp)

### 2026-08-25 20:20 측정
- 리포트: `balance_pass_20260825_202055.txt`
- Random KPI: Day7=100.0 %, Day15=99.1 %, Day30Success=73.9 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=99.1 % (목표 50 %, +49.1pp)

### 2026-08-25 20:57 측정
- 리포트: `balance_pass_20260825_205710.txt`
- Random KPI: Day7=100.0 %, Day15=99.1 %, Day30Success=73.9 %, Day1Fail=0.0 %
- vs Target: Day7=100.0 % (목표 70 %, +30.0pp), Day15=99.1 % (목표 50 %, +49.1pp)
