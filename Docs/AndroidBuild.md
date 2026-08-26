# Android 빌드 (내부 테스트)

## 적용된 Player Settings

Editor 메뉴 **Tools → Surviving Until Payday → Apply Android AAB PlayerSettings (Unit 15)** 로 다음을 맞춘다.  
출시 업로드 시에만 실행한다. **versionCode가 +1** 된다.

검증만 할 때(versionCode 유지): `Validate Release Gate (R-QA-08)` 또는

```text
-executeMethod SurviveUntilPayday.EditorTools.Rqa08ReleaseGateRunner.RunFromBatch
```

| 항목 | 값 |
| --- | --- |
| Application Id | `com.surviveuntilpayday.game` |
| Product Name | 월급날까지 살아남기 |
| Version | `0.1.0` (비어 있거나 `1.0`일 때) |
| Bundle Version Code | 릴리즈 세팅·서명 메뉴 실행 시 +1 (검증 게이트는 올리지 않음) |
| Min SDK | API 26 (Android 8.0) |
| Target SDK | Auto |
| Architecture | ARM64 |
| Scripting Backend | IL2CPP |
| Orientation | Portrait 고정 |
| Build App Bundle | 켜짐 |
| Adaptive Icon | `Tools → Assign Android Adaptive Icons (R-QA-08)` (`Assets/Art/Icons`) |

전체 준비(스플래시·동의·설정 포함): **Setup Release Prep (Unit 15)**

## 개인정보처리방침

- 원문: `Docs/privacy.html`
- 앱 설정 URL: `https://yonghyun-lee-ryan.github.io/surviving-until-payday/privacy.html`
- GitHub → Settings → Pages → Deploy from branch, folder `/Docs` 를 켠다.
- 저장소 HTML: `https://github.com/Yonghyun-Lee-Ryan/surviving-until-payday/blob/develop/Docs/privacy.html`
- `example.com` 등 placeholder는 거부한다 (`PrivacyPolicyUrls`).

## AdMob / UMP / Firebase (Mock ↔ Real)

패키지를 **넣지 않으면** Editor·기기는 TestDevice/Mock으로 동작하고, 광고 실패/취소 시에도 본편은 진행된다.

| 심볼 | 켜지는 방법 | 동작 |
| --- | --- | --- |
| `GOOGLE_MOBILE_ADS` | UPM `com.google.ads.mobile` (asmdef versionDefines) 또는 Player Settings Scripting Define | `AdMobAdService` + UMP (`GoogleUmpConsentService`) |
| `FIREBASE_ANALYTICS` | UPM `com.google.firebase.analytics` 또는 Define | `FirebaseAnalyticsService` |
| `FIREBASE_CRASHLYTICS` | UPM `com.google.firebase.crashlytics` 또는 Define | `FirebaseCrashReporter` |

전환 규칙 (`SdkComposition`)

1. **Editor** + `useTestAdsInEditor` (기본 ON) + `allowRealAdsInEditor` OFF → `TestDeviceAdService` (실 SDK 호출 없음)
2. **기기** + 패키지/심볼 ON → AdMob 실연동, 실패 시 `AdShowResult.Failed`만 반환
3. `SdkIntegrationConfig.preferRealAds` / Remote Config `use_real_ads` 킬스위치
4. 테스트 광고 유닛(기본 ON): Google 샘플 App ID `ca-app-pub-3940256099942544~3347511713`, Rewarded `.../5224354917`, Interstitial `.../1033173712`
5. 테스트 기기: `SdkIntegrationConfig.testDeviceHashedIds` (Logcat hashed ID). UMP 디버그 지리: `umpDebugForceEea`
6. Firebase: `Assets/google-services.json` (gitignore). 템플릿 `Assets/google-services.json.example`

실광고 체크리스트

1. Google AdMob 앱/광고 단위 생성 후 Config의 테스트 유닛 플래그를 끄고 실 ID를 넣는다.
2. Google Mobile Ads Unity 플러그인 설치 → 심볼 자동 또는 Define 추가.
3. `GoogleMobileAdsSettings`에 Android App ID.
4. Firebase 콘솔 앱 등록 → `google-services.json`을 `Assets/`에 복사.
5. 실기기에서 첫 실행: 동의 패널 → (EEA) UMP → 메인. 보상형 취소/실패 후 선택 진행 확인.

상점은 제거됨. IAP를 복구하지 않는다.

## 빌드 순서

1. Unity에서 Android 모듈·JDK·SDK·NDK가 설치돼 있는지 확인한다.
2. `Assign Android Adaptive Icons (R-QA-08)` (최초 1회).
3. `Apply Android AAB PlayerSettings` 실행 (**versionCode +1**).
4. `Setup Android Release Signing` 실행 (Release Keystore 연결. 서명 세팅 시에도 **versionCode +1**).
5. `PrivacyPolicyConfig` URL이 Canonical인지 확인 (`Validate Release Gate`).
6. File → Build Settings → **Development Build 끄기** → **Build App Bundle**로 AAB 빌드.
7. Play Console 내부 테스트 트랙에 업로드한다.

> Play Console 오류 「버전 코드는 이미 사용되었습니다」: 위 릴리즈 세팅 메뉴를 한 번 실행한 뒤 AAB를 다시 빌드하면 됩니다.

### Play Console 「디버그 모드로 서명」 오류

Debug Keystore로 만든 AAB는 업로드할 수 없다.

1. **Tools → Surviving Until Payday → Setup Android Release Signing**
2. `Keystore/release.keystore` 생성·백업 (분실 시 업데이트 불가)
3. Build Settings에서 **Development Build 해제** 후 AAB 재빌드

비밀번호는 `Keystore/android-signing.properties` (gitignore). 예시는 `android-signing.properties.example`.

## 실기기 스모크

- [ ] 첫 실행 동의 → (해당 시) UMP → 메인 메뉴
- [ ] 설정에서 개인정보처리방침 HTTPS 페이지가 열린다
- [ ] 새 게임 30일 클리어(또는 Debug로 단축)
- [ ] 강제 종료 후 이어하기
- [ ] 보상형 광고 실패/취소 시 진행 가능 (Mock/실 SDK)
- [ ] Release 빌드에서 DebugPanel 비활성

## 참고

- 상점은 제거됨. 보상형 광고(리롤·재시도·부업·경험치 2배·긴급대출)와 전면 광고 규칙은 유지.
- 게이트 러너: `Tools → Validate Release Gate (R-QA-08)`
