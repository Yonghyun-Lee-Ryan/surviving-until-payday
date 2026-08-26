# 메타 해금 시나리오 (R-QA-05)

상점·IAP는 사용하지 않는다. 성장은 **인생 경험치 → 레벨 → 직업/특성 해금**, **도감 해금률·다음 목표**, **업적 SO**, **일일 미션 보상**, **출석 스트릭 XP**로만 체감한다.

## 레벨 곡선

`PlayerLevel`: 레벨 n→n+1에 **n×100 XP**. 최대 50.

| 누적 XP | 레벨 | 새로 열리는 것 |
|--------:|-----:|----------------|
| 0 | 1 | 직업 `중소기업 신입사원`, 특성 `알뜰살뜰` (UnlockLevel 0) |
| **100** | **2** | 직업 **공무원 준비생**, 특성 **체력왕** |
| **300** | **3** | 직업 **프리랜서**, 특성 **긍정왕** |
| **600** | **4** | 특성 **야근전문가** |

같은 레벨의 직업·특성은 동시에 열린다. 도감/회차 시작의 「다음 목표」는 아직 잠긴 것 중 **UnlockLevel이 가장 낮은 직업**을 먼저 보여 주고, 직업이 없으면 특성, 둘 다 없으면 미해금 업적 제목이다.

신규(0 XP) 예시:

`다음 목표: Lv.2 직업 「공무원 준비생」 · 경험치 100 남음`

## 플레이어가 보는 곳

- **도감:** 엔딩/사건/특성/직업/업적 해금률 + **전체 해금률** + 다음 목표
- **회차 시작:** 특성 안내 위에 같은 다음 목표
- **결과:** 일일 미션 완료 제목, 미션 XP, 레벨 변화
- **오늘의 직장인:** `[진행]/[완료] 제목 · +XP · 조각`, **연속 접속 N일 · 오늘 출석 +XP**
- **메인 토스트:** 새 해금 목록, 없으면 출석 XP

## 출석 스트릭 (기존 일일 상태 확장)

- 달력 날짜가 바뀌면 스트릭 +1, 하루를 건너뛰면 1로 리셋
- 그날 첫 메뉴 진입 한 번만 `5 × min(스트릭, 7)` XP
- 미션 보상·출석 XP는 상점이 아니라 메타 경험치/조각으로만 지급

## 에디터 검증

메뉴: `Tools → Surviving Until Payday → Run Meta Unlock Scenario (R-QA-05)`

배치:

```text
"D:\Unity\Editor\6000.5.4f1\Editor\Unity.exe" -batchmode -nographics ^
  -projectPath "C:\Users\donggggas\surviving-until-payday" ^
  -executeMethod SurviveUntilPayday.EditorTools.MetaUnlockScenarioRunner.RunFromBatch ^
  -logFile "C:\Users\donggggas\surviving-until-payday\Logs\rqa05_meta_unlock.log"
```

리포트: `Logs/meta_unlock_scenario_*.txt`  
업적 SO: `Tools → Create Achievement Pack (R-QA-05)` → `Assets/Resources/Achievements/`
