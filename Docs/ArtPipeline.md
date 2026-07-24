# 아트 파이프라인 (Unit 21)

Placeholder로도 동작하며, 실에셋은 슬롯만 채우면 된다.

## 슬롯

| 종류 | Enum | 개수 |
|------|------|------|
| 배경 | `BackgroundId` | 집/회사/지하철/식당/병원 + 예비3 |
| 표정 | `ExpressionId` | 기본/행복/당황/분노/피곤/절망 |

카탈로그: `Assets/Data/Art/ArtCatalog.asset`  
런타임 로드: `Resources/Art/ArtCatalog` (`Resources.Load`)

## 폴더·네이밍 (권장)

```
Assets/Art/Backgrounds/bg_home.png
Assets/Art/Backgrounds/bg_office.png
Assets/Art/Backgrounds/bg_subway.png
Assets/Art/Backgrounds/bg_restaurant.png
Assets/Art/Backgrounds/bg_hospital.png
Assets/Art/Expressions/face_default.png
Assets/Art/Expressions/face_happy.png
Assets/Art/Expressions/face_surprised.png
Assets/Art/Expressions/face_angry.png
Assets/Art/Expressions/face_tired.png
Assets/Art/Expressions/face_despair.png
```

1. 이미지를 Import한다.
2. `ArtCatalog` 인스펙터에서 해당 슬롯에 드래그한다.
3. `Assets/Resources/Art/ArtCatalog.asset`도 동일하게 맞추거나, Setup 메뉴로 복사한다.

## 매핑

- 사건 **카테고리 → 기본 배경** (`ArtCategoryDefaults`)
- `EventData`에서 `overrideBackground` / `overrideExpression`으로 개별 지정 가능
- 선택 결과 후 표정은 `ExpressionResolver`가 능력치 변화로 결정

## Editor

`Tools → Surviving Until Payday → Setup Art Pipeline (Unit 21)`
