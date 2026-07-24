# 밸런스 노트 (Unit 27 — 1차 패스)

## 목표 KPI

- **기준:** `Job_JuniorOffice`, **trait=null**(첫 회차에 가깝게), **Random** 정책, 1,000회
- Day 7 생존(도달)률 ≈ **70%**
- Day 15 생존(도달)률 ≈ **50%**
- Day 30 **성공**률: 너무 쉽지도, 불가능하지도 않게 (대략 15~35% 구간 목표)
- Day 1 즉시 파산 비율 과다 방지
- 후반(22~30일) 긴장 유지 + Rest/Health 회복 사건 비율 유지

## 조정 전 가설/관찰

Unity Editor에서 사전 시뮬 전 **에셋 수치 기준 사전 조정**으로 진행했다.

- 시작 현금 280만 + **Day1 고정 월세 60만** → Safe 정책도 1주 내 현금 압박
- **Day15 카드값** + Unit 19 `DifficultyScaler`(현금 손실 1.1x~) → 중반 일괄 지출 부담
- `Event_Sale_001` / `Event_StockIntro_001` **minDay=1~4** → Random/Risky에서 2~5일차 all-in 빈도 과다 우려
- `Event_Rest_Fallback` weight 50 → 회복·완충 사건 상대 빈도 낮음
- 초반 `Event_Overtime_001` Safe(첫 선택) 스트레스·체력 페널티가 누적 탈진에 기여

## 조정 내용

| 사건 | 변경 |
|------|------|
| `Event_Rent_001` | 전액 -600k→**-520k**, 절약 -600k→**-450k**, 대출 -650k→**-580k** |
| `Event_CardBill_001` | 전액 -200k→**-170k**, 일부 -100k→**-85k**(스트레스 5→4), 최소 -40k→**-35k**(스트레스 10→8) |
| `Event_Overtime_001` | Safe(야근) 체력 -5→**-4**, 스트레스 12→**10** |
| `Event_Rest_Fallback` | weight 50→**80** |
| `Event_Sale_001` | minDay 1→**5** |
| `Event_StockIntro_001` | minDay 4→**7** |
| `Event_Sleep_001` | weight 85→**95** |
| `Event_Cold_001` | weight 90→**95** |
| `Event_BackPain_001` | weight 80→**90** |
| `Event_QuitImpulse_001` | 변경 없음 (minDay 20 유지) |

`MvpEventPackFactory` Create* 메서드도 동일 수치로 맞춤 (팩 재생성 시 롤백 방지).

## 적용 지점: EffectResolver cash-loss difficulty (Unit 19)

- `DifficultyScaler.GetMultiplier(day)` — Day 8~14: 1.1x, 15~21: 1.2x, 22~27: 1.35x, 28~30: 1.5x
- **현금 감소(StatType.Cash < 0)** 에만 적용; 회복·가중치·양수 현금은 미적용
- 이번 패스는 **이벤트 base 값**만 조정; 스케일러 로직은 변경하지 않음

## 조정 후 시뮬

Editor에서 재측정:

1. **메뉴:** `Tools → Surviving Until Payday → Run Balance Pass Report (Unit 27)`
2. **또는** `Run Simulator Window` → **Balance Pass (4 policies × 1,000)**

출력:

- `Logs/balance_pass_YYYYMMDD_HHmmss.txt` — Random/Safe/Thrifty/Risky 4정책 리포트
- Console + (실행 시) 본 문서 「조정 후 시뮬」 섹션에 Random KPI 자동 append

리포트 항목: ReachDay7/15/21/30, Day1FailRate, 실패 원인 비율, 성공률.

## 다음 패스(2차) 메모

- Random KPI가 목표에서 ±10%p 이상 벗어나면 **weight만** 미세 조정 (고정비 base는 유지)
- Risky 정책 Day7 < 40%면 소비/투자 사건 minDay 추가 상향 검토
- Safe 정책 Day30 성공 > 50%면 후반 FixedExpense(공과금·카드) 추가 완화 또는 weight 재분배
- Unit 20 연쇄 사건 도입 후 주식·카드 이월 플래그 밸런스 별도 패스 필요

### 2026-07-24 17:42 측정
- 리포트: `balance_pass_20260724_174220.txt`
- Random KPI: Day7=100.0 %, Day15=100.0 %, Day30Success=97.0 %, Day1Fail=0.0 %
