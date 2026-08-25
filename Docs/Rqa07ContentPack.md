# R-QA-07 콘텐츠 팩 (상점 없음)

동장르 G3/G4/G8를 **소수 고품질**로만 채운다. 80개 사건 한 방은 하지 않는다.

에디터: `Tools → Surviving Until Payday → Create Content Pack (R-QA-07)`  
이어서: `Wire Content Pack To Scenes (R-QA-07)`  
배치: `-executeMethod SurviveUntilPayday.EditorTools.ContentPackRqa07Factory.RunFromBatch`

## 추가

| 종류 | 수량 | 내용 |
|------|-----:|------|
| 직업 | +1 | `job_corp_associate` 대기업 사원 (Lv.5, 전용 사건 4) |
| 특성 | +5 | 인맥왕·올빼미(Lv.5), 착한 사람·강철 위장(Lv.6), 선 긋기(Lv.7) |
| 관계 플래그 | +5 | `closeWithCoworker` / `dating` / `mentorBond` / `neighborFeud` / `familySupport` |
| 사건 | +15 | 관계 연쇄 11 + 대기업 전용 4 |

관계 입문 사건은 **금지 플래그**로 중복 소개를 막고, 후속은 **필수 플래그**로만 등장한다. 후속 선택지는 플래그를 **지워서 1회만** 탄다 (주식 스윙과 같은 패턴). 플래그를 남기면 weight 80대 후속이 매일 반복되어 공무원+Safe가 초반에 무너진다.

## 밸런스

- Random Day30Success: 팩 직후 71.8% → 후속 1회화 후 **73.9%** (R-QA-03 최종 73.0%와 동대, 재조정 없음)
- qa05 civil+Safe 0%는 R-QA-04(사건 56)부터 동일. 본 팩 회귀가 아니다
- 스윕: 71사건×3 = 213, 예외 0 (`Logs/choice_sweep_20260825_202029.txt`)
- 캠페인: `Logs/release_qa_campaign_20260825_202121.txt`

## 아트

개별 `Resources/Art/Events/{id}` 스프라이트가 없으면 `EventArtResolver`가 카테고리 배경으로 폴백한다. 새 사건은 `EditorSetArt`로 식당/집/회사 슬롯을 지정했다.

## 상점

IAP·조각 상점 경로를 복구하지 않는다.
