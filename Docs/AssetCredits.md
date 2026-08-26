# 에셋 출처·라이선스 (미디어 팩)

상업 이용 가능한 **자유 라이선스**만 사용했습니다. (CC0 / SIL OFL)

## 이미지·애니메이션

| 경로 | 내용 | 라이선스 |
|------|------|----------|
| `Assets/Art/Backgrounds/` | 사건 배경 5+예비3 | 프로젝트용 생성 에셋 |
| `Assets/Art/Expressions/` | 표정 6종 | 프로젝트용 생성 에셋 |
| `Assets/Art/UI/` | 패널·메뉴 배경 | 프로젝트용 생성 에셋 |
| `Assets/Art/Icons/` | Android Adaptive Icon (foreground/background) 및 레거시 아이콘 | 프로젝트용 생성 에셋 |

인게임 설정 → **크레딧·라이선스**에서 짧은 요약을 볼 수 있습니다.

애니메이션: `EventPanelView` 사건 카드 **페이드인**, 표정 변경 시 **쉐이크+펀치**.

## 사운드 (CC0)

| 슬롯 | 파일 | 출처 |
|------|------|------|
| BGM Main | `Resources/Audio/bgm_main.ogg` | OpenGameArt — *Chill lofi inspired [loop edit]* (qubodup / omfgdude) |
| BGM Play | `bgm_play.ogg` | OpenGameArt — *Slow Stride* (isaiah658) |
| BGM Crisis | `bgm_crisis.ogg` | OpenGameArt — *Dark Place* |
| BGM Result | `bgm_result.ogg` | OpenGameArt — *Chills* (Holizna, CC0) |
| SFX Click/Cash/Stress | `sfx_*.wav` | [Kenney UI Audio](https://kenney.nl/assets/ui-audio) CC0 |
| SFX Success/Fail/Payday | `sfx_*.ogg` | [Kenney Music Jingles](https://kenney.nl/assets/music-jingles) CC0 |

원본 보관: `Assets/Audio/Sources/`

## 폰트 (SIL OFL)

| 파일 | 출처 |
|------|------|
| `Assets/Fonts/NotoSansKR/*.otf` | [Noto Sans CJK / KR](https://github.com/notofonts/noto-cjk) — SIL Open Font License |
| `Assets/Resources/Fonts/` | 런타임 로드용 복사본 |

`UiFont` / `UiFontBootstrap`이 UI Text에 적용합니다.

## Unity에서 연결

1. 프로젝트 포커스 후 임포트 대기  
2. **Tools → Surviving Until Payday → Import Media Pack (Art·Audio·Font)**  
3. (선택) 각 씬 Canvas에 `UiFontBootstrap` 추가  
4. Play 후 사운드·배경·한글 확인  

Attribution은 CC0라 **의무 아님**. 원작자 응원을 권장합니다.
