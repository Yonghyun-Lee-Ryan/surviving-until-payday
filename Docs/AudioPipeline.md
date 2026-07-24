# 사운드·BGM 파이프라인 (Unit 22)

클립이 없어도 no-op이며, Placeholder WAV로 파이프라인을 검증할 수 있다.

## 슬롯

| 종류 | Id | Resources 이름 |
|------|-----|----------------|
| BGM | Main / Play / Crisis / Result | `bgm_main` … `bgm_result` |
| SFX | Click, Cash±, Stress↑, Success, Fail, Payday | `sfx_click` … `sfx_payday` |

런타임 로드: `Resources.Load<AudioClip>("Audio/…")`  
(`UnityAudioService.TryLoadPlaceholdersFromResources`)

## 설정 연동

- `AppSettingsService.SoundEnabled` / `SoundVolume`
- 변경 시 `AudioSettingsChanged` → `AppRoot.Audio.ApplySettings`
- 사운드 OFF면 BGM 정지, SFX 재생 안 함

## 훅

| 장면 | 동작 |
|------|------|
| MainMenu | Main BGM, 버튼 Click SFX |
| Game | Play/Crisis BGM (스트레스≥80 또는 28–29일), 선택 Click, 결과 Cash/Stress SFX, 월급 Payday |
| Result | Result BGM + Success/Fail SFX |

## 셋업

Unity 메뉴: **Tools → Surviving Until Payday → Setup Audio Pipeline (Unit 22)**

실에셋은 `Assets/Resources/Audio/`의 동명 파일을 교체하거나, 씬의 `UnityAudioService` 인스펙터 슬롯에 직접 할당한다.

현재 미디어 팩(CC0)이 이미 배치되어 있다. 출처는 `Docs/AssetCredits.md`.
Unity 메뉴 **Import Media Pack**으로 ArtCatalog·폰트 부트스트랩을 한 번에 연결할 수 있다.
