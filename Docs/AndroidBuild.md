# Android 빌드 (내부 테스트)

## 적용된 Player Settings

Editor 메뉴 **Tools → Surviving Until Payday → Apply Android AAB PlayerSettings (Unit 15)** 로 다음을 맞춘다.

| 항목 | 값 |
| --- | --- |
| Application Id | `com.surviveuntilpayday.game` |
| Product Name | 월급날까지 살아남기 |
| Version | `0.1.0` (비어 있거나 `1.0`일 때) |
| Bundle Version Code | 릴리즈 세팅 시마다 +1 (현재 프로젝트값 기준) |
| Min SDK | API 26 (Android 8.0) |
| Target SDK | Auto |
| Architecture | ARM64 |
| Scripting Backend | IL2CPP |
| Orientation | Portrait 고정 |
| Build App Bundle | 켜짐 |

전체 준비(스플래시·동의·설정 포함): **Setup Release Prep (Unit 15)**

## 빌드 순서

1. Unity에서 Android 모듈·JDK·SDK·NDK가 설치돼 있는지 확인한다.
2. `Apply Android AAB PlayerSettings` 실행 (**versionCode +1**).
3. `Setup Android Release Signing` 실행 (Release Keystore 연결. 서명 세팅 시에도 **versionCode +1**).
4. `PrivacyPolicyConfig` URL을 실제 방침 주소로 바꾼다 (`Assets/Data/Config/PrivacyPolicyConfig.asset`).
5. Project Settings → Player → Android → Icon에 Adaptive Icon을 지정한다.
6. File → Build Settings → **Development Build 끄기** → **Build App Bundle**로 AAB 빌드.
7. Play Console 내부 테스트 트랙에 업로드한다.

> Play Console 오류 「버전 코드는 이미 사용되었습니다」: 위 릴리즈 세팅 메뉴를 한 번 실행한 뒤 AAB를 다시 빌드하면 됩니다. (이미 versionCode를 **2**로 올려 둠)

### Play Console 「디버그 모드로 서명」 오류

Debug Keystore로 만든 AAB는 업로드할 수 없다.

1. **Tools → Surviving Until Payday → Setup Android Release Signing**
2. `Keystore/release.keystore` 생성·백업 (분실 시 업데이트 불가)
3. Build Settings에서 **Development Build 해제** 후 AAB 재빌드

비밀번호는 `Keystore/android-signing.properties` (gitignore). 예시는 `android-signing.properties.example`.

## 실기기 스모크

- [ ] 첫 실행 동의 → 메인 메뉴
- [ ] 새 게임 30일 클리어(또는 Debug로 단축)
- [ ] 강제 종료 후 이어하기
- [ ] 보상형 광고 실패/취소 시 진행 가능 (Mock/실 SDK)
- [ ] Release 빌드에서 DebugPanel 비활성

## 참고

- 상점은 제거됨. 보상형 광고(리롤·재시도·부업·경험치 2배·긴급대출)와 전면 광고 Mock 규칙은 유지.
- 실제 AdMob/Firebase는 Unit 14 범위이며, 현재는 Mock·로컬 Remote Config 폴백.
