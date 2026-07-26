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
