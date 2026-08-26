# R-QA-09 잔여 폴리시 · 카피 · 접근성

출시 직전 잡음 제거. 상점·IAP 복구 없음. 전체 회귀(R-QA-10)는 다음 단위.

## P0 / P1

잔여 **0**. R-QA-01~08에서 조치됨.

| 원래 항목 | 조치 |
|-----------|------|
| Random Day30Success ≈98% | R-QA-03 |
| 개인정보 URL placeholder | R-QA-08 |
| AdMob/Firebase Mock만 | R-QA-08 (`#if` + versionDefines) |
| cash_king 편중 / promotion·hospital | R-QA-04 |
| 선택지 전수 스윕 | R-QA-02 |
| GUID 손상 | R-QA-01 |

## P2 티켓 (이번 단위에서 재밸런스·상점 복구 안 함)

| ID | 항목 | 상태 | 메모 |
|----|------|------|------|
| T-P2-01 | freelancer + Risky 극단 난이도 (qa09) | **티켓** | R-QA-03 관측. 실패 경로 학습용으로 유지. R-QA-10에서 관측만, 게이트 실패 조건 아님 |
| T-P2-02 | 업적 SO 폴더 비어 있음 | **닫음** | `Assets/Resources/Achievements/` 에 AchievementData SO |
| T-P2-03 | 상점 제거 후 조각·광고 제거 상품 공백 | **수용** | 복구 금지. 회차 내 보상형 광고·경험치로 대체 |

## P3

| 항목 | 상태 |
|------|------|
| Adaptive Icon 수동 | **닫음** (R-QA-08 `Assets/Art/Icons`) |
| 시뮬≠UI 레이어 | **닫음** (R-QA-06 모달 레이어) |

## 이번 단위에서 한 카피·접근성

- 빈 상태 문구 한곳 관리: `EmptyStateCopy` (이어하기·미션·도감·결과)
- 설정: 배경음/효과음 한글 라벨, **크레딧·라이선스**, 오프라인 본편 안내
- 광고 미준비 + 오프라인 → `AdBlockReasonCopy.Offline` (본편은 계속)
- 도감 설명 최소 글자 20, 설정 버튼 최소 높이 56
- `Docs/AssetCredits.md` Adaptive Icon 표기. 인게임 크레딧은 `CreditsCopy`

## 스모크

- Editor: `Tools → Surviving Until Payday → Run Smoke Campaign (R-QA-09)`
- 규모: 10 페르소나 × 1사이클 × 10런 = **100런** (전체 2,000런은 R-QA-10)
- 체크리스트: `Run Copy Checklist (R-QA-09)` (R-QA-06 UX 체크리스트 재사용)
