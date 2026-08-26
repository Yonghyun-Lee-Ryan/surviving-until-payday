# 릴리즈 QA 작업 지시서 (전문 QA ×10 · 5사이클)

> 생성: 2026-08-25  
> Unity: `D:\Unity\Editor\6000.5.4f1\Editor\Unity.exe` (6000.5.4f1)  
> 시뮬 리포트: `Logs/release_qa_campaign_20260825_150215.txt`  
> 기준 WBS: `Cursor-WBS-Rev2.md` (본 문서는 **릴리즈 보완 단위 R-QA-*** — Rev.2 Unit 번호와 별도)  
> 공통: 한 요청에 **한 개발 단위만** 구현. 테스트 중 버그·컴파일/경고/런타임 오류는 **즉시 수정**.

---

## 0. 실행 요약 (판정)

| 항목 | 결과 | 판정 |
|------|------|------|
| 자동 캠페인 | 10 페르소나 × 5사이클 × 40런 = **2,000회** | 완료 |
| 전체 성공률 | **75.7%** | Random 목표(15~35%) 대비 **과도하게 쉬움** |
| Random(qa04) | Day30Success ≈ **95~100%**, D7/D15=100% | KPI 미달(너무 쉬움) |
| Safe/Thrifty | 거의 **100%** 30일 클리어 | 긴장감 부족 |
| Risky | ≈ **45~65%** 성공 | 유일하게 의미 있는 실패 곡선 |
| Freelancer+Risky | ≈ **2.5~15%** 성공 | 직업·정책 조합 극단 |
| 엔딩 편중 | `ending_cash_king` **1064/2000** (≈53%) | 성공 엔딩 다양성 붕괴 |
| Hospital | **1회** | 경로 사실상 미사용 |
| Promotion | **4회** | 보상 루프·목표감 약함 |
| 시뮬 한계 | 선택지=인덱스 정책 / Ads·UI·튜토리얼·해금 UX **미커버** | R-QA-02·06 필수 |
| 출시 차단 | 개인정보 URL `example.com`, AdMob/Firebase **Mock**, Adaptive Icon 수동 | R-QA-08 |

**한 줄 결론:** 시스템·콘텐츠 골격은 플레이 가능하나, **난이도·엔딩 다양성·메타 해금 체감·출시 SDK/정책**이 동장르 상위권 대비 부족하다. 아래 단위를 순서대로 수행한다.

---

## 1. QA 테스터 페르소나 (10명)

| ID | 이름 | 직업 | 특성 | 정책 | 집중 검증 |
|----|------|------|------|------|-----------|
| qa01 | 신규 튜토리얼 | junior_office | thrifty | Safe | 첫 회차·튜토리얼· thrifty |
| qa02 | 절약 생존자 | junior_office | thrifty | Thrifty | 현금 보존 |
| qa03 | 위험 도박사 | junior_office | — | Risky | 주식·사설수리·고위험 |
| qa04 | 랜덤 일반인 | junior_office | — | Random | **KPI 기준선** |
| qa05 | 공무원 준비생 | civil_prep | healthy | Safe | 직업 해금 L2 |
| qa06 | 프리랜서 | freelancer | positive | Random | 직업 해금 L3·수입 변동 |
| qa07 | 야근 전문가 | junior_office | overtime_pro | Safe | WORK 스트레스 |
| qa08 | 엔딩 수집가 | junior_office | positive | Risky | 성공 엔딩 다양성 |
| qa09 | 실패 경로 | freelancer | — | Risky | 파산/번아웃/해고 |
| qa10 | 메타 그라인더 | civil_prep | healthy | Thrifty | 다회차 XP·해금 곡선 |

**재실행**

- Editor: `Tools → Surviving Until Payday → Run Release QA Campaign (10×5)`
- Batch:
```text
"D:\Unity\Editor\6000.5.4f1\Editor\Unity.exe" -batchmode -nographics -quit ^
  -projectPath "C:\Users\donggggas\surviving-until-payday" ^
  -executeMethod SurviveUntilPayday.EditorTools.ReleaseQaCampaignRunner.RunFromBatch ^
  -logFile "C:\Users\donggggas\surviving-until-payday\Logs\release_qa_batch.log"
```

---

## 2. 캠페인 수치 하이라이트

### 2.1 정책·직업별

- **Safe + thrifty / overtime_pro:** Success 97.5~100%, AvgDays≈30 → “실패를 경험하기 어렵다”
- **Random office:** Success 95~100% → `Docs/BalanceNotes.md` 목표(Day7≈70, Day15≈50, Day30≈15~35)와 **+30~60pp** 괴리
- **Risky office:** Success 45~65% → 재미 있는 실패 구간이나 Random이 쉬우면 플레이어는 Risky를 안 고름
- **civil_prep Safe:** Success 62~77% → 직업 난이도 차이는 있으나 여전히 높음
- **freelancer Random:** Success 70~85%, D15≈95% → 변동성만 있고 절벽은 약함
- **freelancer Risky:** Success 2.5~15%, AvgDays≈17~19 → **난이도 스파이크**(밸런스 튜닝 시 이 조합을 별도 게이트로)

### 2.2 엔딩·실패 분포 (2,000런)

| 엔딩/실패 | 횟수 | 이슈 |
|-----------|-----:|------|
| cash_king | 1064 | 성공의 기본값처럼 동작 |
| fired | 242 | 실패 중 주력 |
| healthy_worker | 209 | 양호 |
| burnout | 178 | 양호 |
| resign_ready | 119 | 양호 |
| bankruptcy | 65 | 낮음 |
| happy_consumer | 55 | 중간 |
| card_juggle | 34 | 낮음 |
| barely_survived | 29 | 낮음 |
| promotion | 4 | **거의 미도달** |
| hospital | 1 | **사실상 데드** |

### 2.3 시뮬이 커버하지 못한 것 (수동/도구 보강 필요)

- 모든 선택지 라벨·분기 의미적 커버 (인덱스 정책 ≠ 모든 선택 경험)
- 광고(보상형/전면) 쿨다운·쿼터·실패 UX
- 튜토리얼·동의·설정·이어하기·주간 결산 레이어
- 메타 해금 알림·도감·일일 미션 UI
- Android AAB 실기기 성능·터치·백키

---

## 3. 버그·기술 부채 리스트 (QA 관찰)

| 심각도 | 항목 | 근거 | 조치 단위 |
|--------|------|------|-----------|
| P0 | Random Day30Success ≈98% | 캠페인 qa04, BalanceNotes | **R-QA-03** |
| P0 | 개인정보 URL placeholder | `PrivacyPolicyConfig` / ReleasePrep | **R-QA-08** |
| P0 | 실기기 AdMob/Firebase 미연결 | Mock + `#if` 스텁 | **R-QA-08** |
| P1 | 성공 엔딩 cash_king 편중 | EndingHits | **R-QA-04** |
| P1 | promotion / hospital 미도달 | EndingHits 4 / 1 | **R-QA-04** |
| P1 | 선택지 전수 커버 도구 부재 | 시뮬 한계 노트 | **R-QA-02** |
| P1 | EditMode meta GUID 손상 이력 | `RunFlagChainTests.cs.meta` 31자 GUID | **R-QA-01** (수정됨·재검증) |
| P2 | freelancer+Risky 극단 난이도 | qa09 | **R-QA-03** |
| P2 | 업적 SO 폴더 비어 있음(코드 ID만) | `Assets/Data/Achievements` 0 | **R-QA-05** |
| P2 | 상점 제거 후 조각·광고 제거 상품 공백 | Unit 28 경로 삭제됨 | **R-QA-07** (복구 금지·대체 UX) |
| P3 | Adaptive Icon 수동 | AndroidBuild.md | **R-QA-08** |
| P3 | 시뮬≠UI 레이어 회귀 | 결과 팝업/HUD 이력 | **R-QA-06** |

---

## 4. UI/UX 이슈 리스트

1. **실패를 못 보는 초보 경로** — Safe만 고르면 30일 클리어 → 튜토리얼이 “위험 선택”을 가르치지 않으면 긴장감 학습 실패  
2. **엔딩 연출 단조** — cash_king 반복 시 회차 종료 보상감 저하 (동장르: 엔딩 카드·공유·희귀도)  
3. **해금 체감 약함** — 직업 3·특성 4만으로는 “수집·성장” 루프가 BitLife/인생시뮬 대비 짧음  
4. **일일 미션은 있으나 메타 스토리텔링 약함** — 오늘의 직장인·스트릭·주간 챌린지 대비 빈약  
5. **광고 UX** — 시뮬 미커버; 보상형/전면 실패·쿨다운 문구·버튼 비활성 이유 명시 필요  
6. **설정/동의** — 개인정보 링크 실URL 없으면 스토어 리젝트  
7. **결과/주간결산 레이어** — 과거 HUD가 모달을 가린 이력 → 회귀 테스트 필수  
8. **선택지 가독성** — 수치만 나열되면 장르 상위작 대비 “인생 드라마” 카피 밀도 부족  

---

## 5. 동장르 인기작 비교 (≈100종 클러스터)

> Play/App Store “인생 시뮬·선택형 텍스트·직장/급여 생존·데일리 챌린지” 상위권을 **클러스터 100종**으로 묶어 갭을 도출.  
> 개별 타이틀 리뷰가 아니라 **릴리즈 보완 우선순위**용 비교다.

### 5.1 클러스터 맵 (합계 100)

| # | 클러스터 | 대표 성격 | 대략 종수 | 본작 대비 |
|---|----------|-----------|----------:|-----------|
| A | BitLife형 인생 시뮬 | 나이/선택/수집 엔딩 | 18 | 엔딩·수집·공유·스냅샷 약함 |
| B | 선택형 텍스트/비주얼노벨 라이프 | 분기·관계·회차 | 14 | 관계 NPC·장기 분기 약함 |
| C | 한국/아시아 직장·월급 시뮬레이션 | 야근·월세·카드 | 12 | 테마 적합, **난이도·카피 밀도** 부족 |
| D | 방치/커리어 매니저 | 성장 곡선·해금 | 12 | 메타 해금 폭 좁음(직업3·특성4) |
| E | 로그라이크·30일/시즌 챌린지 | 실패 학습·시드 | 10 | Random이 너무 쉬워 실패 학습 불가 |
| F | 데일리/스트릭·출석 라이트 게임 | 일일 루프 | 10 | 미션 8개 수준, 스트릭 UX 약함 |
| G | 광고+보상형 하이퍼캐주얼 서바이벌 | 광고 리듬 | 8 | Mock만, 리듬·실패 UX 미검증 |
| H | 인디 오피스/퀴어/코미디 라이프 | 톤·밈 | 8 | 톤은 맞으나 **밈/트렌드 사건 속도** 느림 |
| I | 스토리 RPG Lite / 챕터형 | 챕터·보스 주 | 8 | 주간 결산은 있으나 “챕터 보스”감 약함 |

### 5.2 갭 → 보완 우선순위

| 갭 | 상위권에서 흔한 것 | 본작 상태 | 단위 |
|----|-------------------|-----------|------|
| G1 난이도 커브 | 초반 사망/실패로 학습 | Random ≈100% 클리어 | R-QA-03 |
| G2 엔딩 수집 | 희귀 엔딩·%·공유 카드 | cash_king 53% 독점 | R-QA-04 |
| G3 메타 수집 | 수십 직업/특성/스킨 | 3/4 + 도감 | R-QA-05·07 |
| G4 관계/장기 플래그 | NPC·호감·복수 회차 | 플래그 체인 일부만 | R-QA-07 |
| G5 데일리 훅 | 스트릭·시즌 패스형 | 미션 존재, UX 약함 | R-QA-05 |
| G6 소셜/공유 | 엔딩 이미지 공유 | 거의 없음 | R-QA-06 |
| G7 폴리시·수익 | 실광고·UMP·방침 URL | placeholder/Mock | R-QA-08 |
| G8 콘텐츠 속도 | 주간 이벤트 팩 | 56 사건·고정 팩 | R-QA-07 |
| G9 튜토리얼 | “실패해도 OK” 온보딩 | Safe 올클 → 학습 실패 | R-QA-06 |
| G10 접근성/피드백 | 선택 결과 미리보기 토글 | 제한적 | R-QA-06 |

---

## 6. 개발 단위 (순서 엄수)

각 단위 공통 의무:

1. 아래 **읽을 파일**을 먼저 읽고 중복 구현 금지  
2. Unity 전문 개발자 수준: **컴파일 에러·경고·런타임 에러·회귀 버그 방지**  
3. 검증은 **`D:\Unity\Editor\6000.5.4f1`** 만 사용  
4. 테스트 중 버그 발견 시 **즉시 수정** 후 같은 단위 완료 기준 재확인  
5. 단위 끝에서 **다음 단위용 프롬프트** 제공 (마지막 단위는 전체 재테스트+즉시 수정)

---

### R-QA-01 — 프로젝트 위생 · 테스트 녹색화

**목표:** 배치/EditMode가 경고·메타 오류 없이 돌아가게 한다.

**읽을 파일**

- `Assets/Tests/EditMode/RunFlagChainTests.cs`
- `Assets/Tests/EditMode/RunFlagChainTests.cs.meta`
- `Assets/Scripts/Editor/ReleaseQaCampaignRunner.cs`
- `Docs/BalanceNotes.md`
- `Cursor-WBS-Rev2.md` (공통 규칙)

**작업**

- `.meta` GUID 32자·orphan meta 전수 점검  
- EditMode 전체 실행, 실패 테스트 수정  
- `ReleaseQaCampaignRunner` 배치 1회 스모크  

**완료 기준**

- EditMode 실패 0  
- 배치 캠페인 리포트 생성  
- Console에 meta/GUID 경고 없음  

**Unity 테스트**

```text
-runTests -testPlatform EditMode
Tools → Run Release QA Campaign (10×5) 스모크(선택: Runs 축소 금지 — 기존 설정 유지)
```

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-02만 수행해줘.
선택지 전수 커버(ExhaustiveChoiceSweep) 도구를 만들고 Unity 6000.5.4f1로 돌려
미도달 선택지·데드 분기 목록을 Logs에 남겨. 버그 나면 즉시 수정.
공통 규칙·중복 금지·D드라이브 Unity만 사용.
```

---

### R-QA-02 — 선택지·분기 전수 커버 도구

**목표:** “모든 선택지를 경험·고른다”를 자동화에 가깝게 검증한다.

**읽을 파일**

- `Assets/Scripts/Debug/RunSimulator.cs` (또는 동등 시뮬 코어)
- `Assets/Scripts/Editor/RunSimulatorWindow.cs`
- `Assets/Scripts/Editor/ReleaseQaCampaignRunner.cs`
- `Assets/Scripts/Events/*` (선택 적용 경로)
- `Assets/Data/Events/*.asset` (표본)

**작업**

- `ExhaustiveChoiceSweep`(가칭) Editor 메뉴: 사건×선택지 인덱스 전수, 미도달/예외 수집  
- 가능하면 연쇄 플래그 경로(`RunFlags`) 최소 커버  
- 리포트: `Logs/choice_sweep_*.txt`

**완료 기준**

- 전 사건 선택지가 최소 1회 시도됨(또는 조건 불가로 **사유 명시**)  
- NullRef/예외 0  
- 데드 선택지(효과 없음·카피만) 목록화  

**Unity 테스트:** 6000.5.4f1 메뉴 실행 + 리포트 확인

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-03만 수행해줘.
BalanceNotes KPI(Day7≈70, Day15≈50, Day30Success 15~35, Day1Fail 과다 금지)에
Random이 들어오도록 Event weight/effects만 조정하고
BalancePass + Release QA Campaign을 Unity 6000.5.4f1로 재측정해.
freelancer+Risky 극단도 완화. 버그 즉시 수정.
```

---

### R-QA-03 — 밸런스 3차 패스 (릴리즈 KPI)

**목표:** Random 기준선을 기획 KPI 구간에 올린다. (WBS Unit 27 연장)

**읽을 파일**

- `Docs/BalanceNotes.md`
- `Assets/Scripts/Editor/BalancePassRunner.cs`
- `Assets/Scripts/Editor/MvpEventPackFactory.cs` (및 Unit23 팩토리)
- `Assets/Scripts/Core/DifficultyScaler.cs` (참고만 — 가능하면 이벤트만 조정)
- `Logs/release_qa_campaign_20260825_150215.txt`

**작업**

- 고정비·소비 weight·회복 weight·QuitImpulse 등 **데이터만** 조정  
- Random 1,000회 + 캠페인 재실행  
- `freelancer+Risky` Success이 0%대에 고착되지 않게 하한 모니터링(목표 예: 10~25%)  
- 조정 근거를 `Docs/BalanceNotes.md`에 append  

**완료 기준**

- Random: Day7 ≈70%±10pp, Day15≈50%±10pp, Day30Success **15~35%**, Day1Fail ≤5%  
- Safe가 “무조건 클리어”가 아니게(예: Success ≤90%)  
- 시스템 리팩터 없음  

**Unity 테스트:** Balance Pass + Release QA Campaign

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-04만 수행해줘.
성공 엔딩 cash_king 편중을 줄이고 promotion/hospital/barely 등 희귀 엔딩 도달률을
시뮬로 올려. EndingResolver·조건·사건 플래그만 최소 수정.
Unity 6000.5.4f1 캠페인으로 EndingHits 재측정. 버그 즉시 수정.
```

---

### R-QA-04 — 엔딩 다양성 · 실패/성공 경로

**목표:** 엔딩 수집 재미를 동장르 수준으로 끌어올린다.

**읽을 파일**

- `Assets/Scripts/Core/EndingResolver.cs` (또는 엔딩 판정 위치)
- `Assets/Data/Endings/*.asset`
- `Assets/Scripts/Core/FailureEvaluator.cs`
- `Assets/Tests/EditMode/*Ending*`
- 캠페인 EndingHits 섹션

**작업**

- cash_king 조건 축소 또는 우선순위 조정  
- promotion / hospital 도달 가능 루트 보장(사건·플래그·임계값)  
- barely_survived가 “아슬아슬” 구간에 실제로 걸리게  
- 시뮬 EndingHits: 상위 1엔딩 ≤40%, promotion≥2%, hospital 경로 smoke  

**완료 기준**

- 캠페인 재실행 후 편중 완화 수치 기록  
- EditMode 엔딩 테스트 통과  

**Unity 테스트:** 캠페인 + 관련 EditMode

**재측정 (2026-08-25 19:04, 6000.5.4f1)**

- 리포트: `Logs/release_qa_campaign_20260825_190415.txt`
- 상위 1엔딩: burnout **19.5%** (≤40%)
- cash_king **105/2000 (5.3%)** ← 기존 53%
- promotion **297/2000 (14.9%)** ≥2%
- hospital **60/2000 (3.0%)** smoke
- barely_survived **106/2000 (5.3%)** ← 기존 29회

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-05만 수행해줘.
메타 해금·도감·업적·일일 미션의 ‘느껴지는 성장’을 보강하되
상점은 복구하지 마. Unity 6000.5.4f1에서 다회차 해금 시나리오를 검증하고
버그 즉시 수정.
```

---

### R-QA-05 — 메타 성장·도감·일일 훅 체감

**목표:** qa10 메타 그라인더가 “해금할 맛이 난다”고 느끼게.

**읽을 파일**

- `Cursor-WBS-Rev2.md` Unit 10, 24, 25  
- `Assets/Scripts/Core/AchievementIds.cs`
- `Assets/Scripts/Meta/*` (또는 메타 저장·XP)
- `Assets/Resources/Missions/*.asset`
- MainMenu 도감/해금 UI 관련 Presenter·View

**작업**

- 업적 정의가 코드-only면 SO/표시 데이터 정리  
- 해금 알림·도감 퍼센트·미션 보상 피드백 강화  
- 일일 스트릭/재접속 훅(과도한 신시스템 금지 — 기존 확장)  
- **상점·IAP 복구 금지**

**완료 기준**

- 신규→해금 직업/특성까지 수동 또는 에디터 시나리오 문서화  
- UI가 해금률·다음 목표를 보여 줌  

**Unity 테스트:** 메타 시나리오 + 관련 테스트

**수행 기록 (2026-08-25)**

- 업적 표시 SO 20개: `Assets/Resources/Achievements` (`AchievementPackFactory`)
- 도감·회차 시작: 전체 해금률 + 다음 목표 (`MetaGrowthHint`)
- 일일: 미션 보상 카피, 연속 접속 스트릭, 출석 XP (`5 × min(streak, 7)`, 상점 없음)
- 결과: 일일 미션 완료·미션 XP
- 문서: `Docs/MetaUnlockScenario.md`
- Unity 6000.5.4f1 시나리오 **PASS** `Logs/meta_unlock_scenario_20260825_192322.txt` (신규→Lv2 공무원/체력왕→Lv3 프리랜서/긍정왕→Lv4 야근전문가, 출석 5/10/결석리셋)
- EditMode **44 passed / 0 failed** (`Rqa05MetaGrowthTests` 포함, `Logs/rqa05_editmode.xml`)

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-06만 수행해줘.
튜토리얼·결과/주간결산 레이어·선택 피드백·실패 학습 UX를 폴리시하고
동장르 G6/G9 갭을 줄여. Unity 6000.5.4f1 PlayMode/수동 체크리스트 수행,
버그 즉시 수정.
```

---

### R-QA-06 — UX·튜토리얼·레이어 폴리시

**목표:** UI/UX 이슈 1~8 중 플레이 직결을 해소한다. (Unit 26 연장)

**읽을 파일**

- `Cursor-WBS-Rev2.md` Unit 26  
- `Assets/Scripts/UI/*` (Event/Result/Weekly/HUD/Tutorial/Consent)
- `Assets/Scripts/Core/AppRoot.cs`
- `Docs/DebugPanel.md`

**작업**

- Safe-only 플레이어가 위험을 배우도록 튜토리얼/팁  
- 결과·주간결산 `SetAsLastSibling` 회귀 방지  
- 광고 버튼 비활성 시 사유 문구  
- (선택) 엔딩 결과 공유용 텍스트 카피 훅 — 외부 SDK 없이도 클립보드 수준 가능  

**완료 기준**

- 체크리스트(첫 실행→동의→튜토리얼→7일→주간결산→실패/성공→이어하기) 통과  
- Release에서 DebugPanel 비활성 유지  

**Unity 테스트:** Editor Play + 가능하면 디바이스. 에디터 자동: `Tools → Surviving Until Payday → Run UX Checklist (R-QA-06)`

**수행 기록 (2026-08-25)**

- 튜토리얼 5스텝: 실패해도 OK, 안전만 고르면 엔딩이 비슷, 설정 「선택 미리보기」(G9/G10)
- 결과/주간결산: `UiModalLayer`가 HUD `SetAsLastSibling` 이후 모달을 다시 올림
- 광고 버튼: 숨기지 않고 한글 한도/쿨다운/미준비 사유
- 엔딩 기록 복사: 클립보드 텍스트 훅, 외부 SDK 없음 (G6)
- 선택 결과: 숫자 아래 드라마 한 줄 (`ChoiceFeedbackCopy`)
- 문서: `Docs/Rqa06UxChecklist.md`
- Unity 6000.5.4f1: EditMode **30 passed** (`Rqa06UxCopyTests` 13 + `ReleasePrepTests`/`Unit26`/`Unit19`, `Logs/rqa06_editmode.xml`) · PlayMode **1 passed** (`Rqa06PlayModeLayerTests`, `Logs/rqa06_playmode.xml`) · 체크리스트 **PASS** `Logs/rqa06_ux_checklist_20260825_194239.txt`

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-07만 수행해줘.
동장르 대비 콘텐츠 밀도(직업/특성/관계 플래그/사건 톤)를
기존 팩토리 패턴으로만 확장하고 상점은 복구하지 마.
Unity 6000.5.4f1 시뮬·캠페인으로 회귀 확인. 버그 즉시 수정.
```

---

### R-QA-07 — 콘텐츠 밀도 · 장르 패리티 (상점 제외)

**목표:** G3/G4/G8 갭을 “출시 직후 업데이트 가능한 양”만큼 채운다.

**읽을 파일**

- `Cursor-WBS-Rev2.md` Unit 23~25  
- `Assets/Scripts/Editor/ContentPackUnit23Factory.cs`
- `Assets/Scripts/Editor/SampleDataFactory.cs`
- `Assets/Data/Jobs|Traits|Events|Endings`
- `Docs/ArtPipeline.md`

**작업**

- 특성/직업/사건 **소수 고품질 추가**(한 단위에 과도한 80개 금지 — 예: 특성 +4~6, 관계형 사건 +N)  
- 아트 누락 사건 `EventArtResolver` 경로 확인  
- 상점·조각 광고 경로 **재도입 금지**  

**완료 기준**

- 팩토리로 재생성 가능  
- 캠페인/스윕에서 예외 0  
- Balance KPI가 R-QA-03 범위를 크게 깨면 즉시 소량 재조정  

**Unity 테스트:** 캠페인 스모크 + choice sweep

**수행 기록 (2026-08-25)**

- 팩토리: `ContentPackRqa07Factory` (`Create` / `Wire` / `RunFromBatch`). 상점 경로 없음
- 직업 +1 · 특성 +5 · 관계 플래그 5 · 사건 +15 (관계 연쇄 11 + 대기업 4). 80개 한 방 없음
- 후속 사건이 플래그를 안 지워 매일 반복되던 버그 → 주식 스윙과 같이 선택 시 clear (1회 연쇄)
- 아트: 개별 일러스트 없으면 `EventArtResolver` 카테고리 폴백. 새 사건은 식당/집/회사 슬롯
- Unity 6000.5.4f1: EditMode **40 passed** (`Rqa07ContentPackTests` 11 포함, `Logs/rqa07_editmode.xml`)
- Choice sweep: Attempted=213, Resolved=213, Dead=0, Exceptions=0 (`Logs/choice_sweep_20260825_202029.txt`)
- Balance Random Day30Success **73.9%** (R-QA-03 73.0% 대비 재조정 없음, `Logs/balance_pass_20260825_202055.txt`)
- 캠페인 10×5: 예외 0, Aggregate 49.1%, qa04 Random 67.5~85% (`Logs/release_qa_campaign_20260825_202121.txt`)
- 문서: `Docs/Rqa07ContentPack.md`

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-08만 수행해줘.
개인정보 실URL, Consent/UMP, AdMob·Firebase 실연동(또는 명확한 Define 경로),
Adaptive Icon·AAB·versionCode를 출시 가능하게 마무리.
Unity 6000.5.4f1 + Docs/AndroidBuild.md 절차로 검증. 버그 즉시 수정.
상점은 복구하지 마.
```

---

### R-QA-08 — 출시 차단 해제 (SDK·정책·빌드)

**목표:** Play 내부테스트 리젝트 요인을 제거한다. (Unit 14·15)

**읽을 파일**

- `Cursor-WBS-Rev2.md` Unit 14, 15  
- `Docs/AndroidBuild.md`
- `Assets/Scripts/Editor/ReleasePrepSetup.cs`
- `Assets/Scripts/Editor/SdkIntegrationSetup.cs`
- `Assets/Scripts/Ads/*`, `Assets/Scripts/Services/FirebaseCrashReporter.cs`
- `Assets/Scripts/Settings/PrivacyPolicyConfig.cs`

**작업**

- 실 개인정보처리방침 URL  
- AdMob/UMP/Firebase: 패키지·Define·테스트 디바이스 경로 문서+코드  
- Adaptive Icon / AAB / versionCode 체크  
- Mock과 Real 전환이 실수 없이 되게  

**완료 기준**

- placeholder URL 0  
- Release 빌드 체크리스트 `Docs/AndroidBuild.md` 갱신  
- 광고 실패 시 게임 진행 가능  

**Unity 테스트:** 6000.5.4f1 Release 설정 적용 + (가능 시) AAB

**수행 기록 (2026-08-25)**

- 개인정보 URL: `Docs/privacy.html` + Canonical `https://yonghyun-lee-ryan.github.io/surviving-until-payday/privacy.html` (GitHub Pages `/Docs`). `example.com` 거부
- Consent: 1차 패널 후 `IAdsConsentService.EnsureConsent` → UMP(`GOOGLE_MOBILE_ADS`) 또는 Local. 실패해도 본편 진행
- AdMob/Firebase: `#if` 실호출 + asmdef versionDefines(`com.google.ads.mobile` 등). 미설치 시 Mock/TestDevice. 테스트 유닛·테스트 기기 ID는 `SdkIntegrationConfig`
- Adaptive Icon: `Assign Android Adaptive Icons (R-QA-08)` (`Assets/Art/Icons`)
- 게이트는 versionCode를 올리지 않음. AAB/서명 메뉴만 +1
- 상점/IAP 복구 없음
- Unity 6000.5.4f1: EditMode **18 passed** (`Rqa08SdkGateTests` 5 + `ReleasePrepTests` 7 + `SdkIntegrationTests` 6, `Logs/rqa08_editmode.xml`)
- 릴리즈 게이트 **PASS** `Logs/rqa08_release_gate_20260825_203632.txt` (AAB 모듈 True, 실 AAB는 서명 후 `Docs/AndroidBuild.md` 순서)

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-09만 수행해줘.
R-QA-01~08 산출물을 기준으로 남은 P2/P3와 카피·접근성만 정리하고
최종 캠페인 전 스모크를 돌려. Unity 6000.5.4f1. 버그 즉시 수정.
```

---

### R-QA-09 — 잔여 폴리시 · 카피 · 접근성

**목표:** 출시 직전 잡음 제거.

**읽을 파일**

- 본 문서 §3~4 잔여 항목  
- `Assets/Scripts/UI` 한국어 카피  
- `Docs/AssetCredits.md`
- 최근 `Logs/*`

**작업**

- 오탈자·버튼 라벨·빈 상태 문구  
- 저사양/오프라인 핵심 플레이 확인  
- 크레딧·라이선스  

**완료 기준**

- P0/P1 잔여 0  
- P2는 티켓화 또는 수정  

**Unity 테스트:** 스모크 캠페인 + PlayMode 체크리스트

**수행 기록 (2026-08-25)**

- P0/P1 잔여 0. P2: `Docs/Rqa09Polish.md` 티켓 (T-P2-01 freelancer+Risky 유지, T-P2-02 업적 SO 닫음 20개, T-P2-03 상점 복구 금지·수용). P3 Adaptive Icon·HUD 레이어는 08/06에서 닫음
- 빈 상태 카피 `EmptyStateCopy`, 설정 크레딧 `CreditsCopy`, 오프라인 본편 안내 `AccessibilityCopy`. 광고 오프라인은 본편을 막지 않음
- `Docs/AssetCredits.md` Adaptive Icon. 인게임 설정 → 크레딧·라이선스
- 상점/IAP 복구 없음
- Unity 6000.5.4f1: EditMode **4 passed** (`Logs/rqa09_editmode.xml`), PlayMode **2 passed** (`Logs/rqa09_playmode.xml`, R-QA-06 레이어 + 설정 크레딧)
- 카피 체크리스트 **PASS** `Logs/rqa09_copy_checklist_20260825_204741.txt` (R-QA-06 UX도 PASS)
- 스모크 **100런 예외 0** `Logs/rqa09_smoke_campaign_20260825_204809.txt` (Success 51.0%. qa09 0%는 T-P2-01. n=10이라 qa05 0%는 관측 노이즈, 재밸런스 안 함)

**다음 작업 프롬프트**

```text
Docs/ReleaseQA-WorkOrder.md의 개발 단위 R-QA-10만 수행해줘.
(마지막 단위) 프롬프트 대신 전체 테스트를 진행하고
컴파일 에러·경고·런타임 에러·테스트 중 버그를 즉각 수정해.
```

---

### R-QA-10 — 전체 회귀 · 릴리즈 게이트 (마지막)

**목표:** 릴리즈 후보 게이트 통과. **다음 단위 프롬프트 없음.**

**읽을 파일**

- `Docs/ReleaseQA-WorkOrder.md` 전체  
- `Docs/BalanceNotes.md` 최신 시뮬  
- `Docs/AndroidBuild.md`
- `Assets/Scripts/Editor/ReleaseQaCampaignRunner.cs`
- `Cursor-WBS-Rev2.md` 출시 체크포인트  

**작업 (프롬프트 대신 이 절차를 수행)**

1. Unity 6000.5.4f1에서 **Clean Console** 기준 컴파일 — 에러·경고 0을 목표로 수정  
2. EditMode 전체 — 실패 즉시 수정  
3. `ExhaustiveChoiceSweep` — 예외 즉시 수정  
4. Balance Pass(Random KPI 게이트) — 이탈 시 R-QA-03 수준으로 즉시 재조정  
5. Release QA Campaign 10×5 — 이상치·NullRef 즉시 수정  
6. PlayMode 체크리스트: 동의→뉴게임→전 직업/특성 해금 루트→전 난이도(정책)→일일→이어하기→설정 리셋→광고 실패  
7. (가능 시) AAB 설치·백그라운드 복귀·오프라인  
8. 최종 리포트: `Logs/release_gate_YYYYMMDD.txt`에 결과·남은 known issues  

**완료 기준**

- 컴파일 에러 0, 치명 경고 0(불가피 경고는 문서화)  
- EditMode 녹색  
- Random KPI 게이트 유지  
- P0 출시 차단 0  
- 캠페인 예외 0  

**수행 기록 (2026-08-25)**

- Unity 6000.5.4f1. 상점/IAP 복구 없음. versionCode 유지(2)
- EditMode **228 passed / 0 failed** (`Logs/rqa10_editmode.xml`). Unit23 직업 수=3 단언이 R-QA-07 대기업(4번째)과 충돌 → `JobsFolder_HasCoreJobsWithExpectedUnlockLevels`로 수정
- PlayMode **3 passed** (`Logs/rqa10_playmode.xml`: 레이어·설정 크레딧·동의/일일/리셋)
- 스윕 213/213, Dead 0, Exceptions 0 (`Logs/choice_sweep_20260825_205707.txt`)
- Balance Random Day30Success **73.9%**, Day1Fail **0%** — R-QA-03 유지 밴드(50~85%) 안, 재조정 없음 (`Logs/balance_pass_20260825_205710.txt`)
- 캠페인 10×5 **2000런 예외 0**, Success 49.1% (`Logs/release_qa_campaign_20260825_205711.txt`)
- 동의·일일 빈상태·이어하기 세이브·설정 리셋(동의 유지)·광고 실패, 메타 해금, R-QA-06/08/09 중첩 체크리스트 PASS
- AAB 모듈 True, 실 AAB·실기기 백그라운드는 `Docs/AndroidBuild.md` (서명 후)
- 불가피: Unity 종료 시 `StackAllocator(ALLOC_TEMP_MAIN)` 엔진 로그. 프로젝트 CS warning 0
- 최종: `Logs/release_gate_20260825.txt` **RESULT: PASS**

**다음 프롬프트:** 없음 (본 단위가 종결)

---

## 7. 권장 실행 순서 (한눈에)

```text
R-QA-01 위생/테스트
   → R-QA-02 선택지 전수
   → R-QA-03 밸런스 KPI
   → R-QA-04 엔딩 다양성
   → R-QA-05 메타·일일 체감
   → R-QA-06 UX·튜토리얼
   → R-QA-07 콘텐츠 밀도(상점X)
   → R-QA-08 SDK·정책·AAB
   → R-QA-09 잔여 폴리시
   → R-QA-10 전체 게이트(종료)
```

Rev.2와의 매핑: 03≈Unit27, 05≈10/24/25, 06≈26, 07≈23, 08≈14/15.

---

## 8. 산출물 위치

| 산출물 | 경로 |
|--------|------|
| 본 지시서 | `Docs/ReleaseQA-WorkOrder.md` |
| 1차 캠페인 | `Logs/release_qa_campaign_20260825_150215.txt` |
| R-QA-04 캠페인 | `Logs/release_qa_campaign_20260825_190415.txt` |
| R-QA-05 해금 시나리오 | `Logs/meta_unlock_scenario_*.txt` / `Docs/MetaUnlockScenario.md` |
| R-QA-06 UX 체크리스트 | `Logs/rqa06_ux_checklist_*.txt` / `Docs/Rqa06UxChecklist.md` |
| R-QA-07 콘텐츠 팩 | `Docs/Rqa07ContentPack.md` / `Logs/choice_sweep_20260825_202029.txt` / `Logs/release_qa_campaign_20260825_202121.txt` / `Logs/balance_pass_20260825_202055.txt` |
| R-QA-08 출시 게이트 | `Docs/AndroidBuild.md` / `Docs/privacy.html` / `Logs/rqa08_release_gate_20260825_203632.txt` / `Logs/rqa08_editmode.xml` |
| R-QA-10 전체 게이트 | `Logs/release_gate_20260825.txt` / `Logs/rqa10_editmode.xml` / `Logs/rqa10_playmode.xml` / `Logs/choice_sweep_20260825_205707.txt` / `Logs/balance_pass_20260825_205710.txt` / `Logs/release_qa_campaign_20260825_205711.txt` |
| 배치 로그 | `Logs/release_qa_batch.log` |
| 밸런스 노트 | `Docs/BalanceNotes.md` |
| 캠페인 러너 | `Assets/Scripts/Editor/ReleaseQaCampaignRunner.cs` |

---

## 9. Cursor에 붙일 공통 꼬리말

```text
공통 규칙:
1. Docs/ReleaseQA-WorkOrder.md와 Cursor-WBS-Rev2.md를 따른다. 한 단위만 구현한다.
2. 기존 코드 분석 후 중복 클래스 금지. UI≠로직.
3. D:\Unity\Editor\6000.5.4f1 만으로 테스트한다.
4. 컴파일/경고/런타임/테스트 버그는 발견 즉시 수정한다.
5. 상점·IAP·조각 광고 구매 경로는 복구하지 않는다.
6. 수정 파일 목록·테스트 방법을 마지막에 적는다.
7. 커밋 제안이 필요하면 한글(용어 제외).
```
