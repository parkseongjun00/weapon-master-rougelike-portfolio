# Weapon Master Roguelike (가제)

| 항목 | 내용 |
|---|---|
| 장르 | 로그라이트, 엔드리스 웨이브 서바이벌 |
| 시점 | 3D 쿼터뷰 |
| 엔진 | Unity 6 (URP) |
| 타겟 플랫폼 | 웹(WebGL) 우선 → 모바일 이식 |
| 상태 | 개발 중 — 4단계(캐릭터/애니메이션 적용) 진행 중 |

## 이 저장소에 대하여

이 저장소는 실제로 진행 중인 Unity 프로젝트에서 **코드와 설계 문서만 발췌**한 포트폴리오용 저장소입니다. 씬·프리팹·리소스를 포함한 전체 Unity 프로젝트는 별도로 관리되며 계속 개발 중입니다.

전체 프로젝트를 그대로 올리지 않고 코드와 문서만 정리해서 공개하는 이유는 두 가지입니다.

- Unity 프로젝트 특유의 부수 파일(`.meta`, `ProjectSettings`, 자동 생성 캐시 등)을 걷어내고, 읽을 가치가 있는 부분만 남기기 위해서.
- 아직 비주얼/콘텐츠가 최소 상태라(아래 "진행 상황과 비주얼에 대하여" 참고), 시스템 설계와 코드로 먼저 판단받는 편이 정확하다고 판단했기 때문.

## 프로젝트 개요

고정된 단일 아레나에서 시간이 지날수록 강해지는 적 웨이브를 상대하는 엔드리스 서바이벌입니다. 승리 조건 없이 최대한 오래 생존하거나 많은 적을 처치하는 것이 목표입니다. 맨손으로 시작해 필드에 스폰되는 무기를 주워 싸우고, 무기는 내구도를 소모하며 파괴되면 다시 맨손이 됩니다. 근접 무기는 사거리 내 모든 적을 동시에 타격하고 원거리 무기는 최초 명중 대상에게만 데미지를 주는 등, 장르 관행을 그대로 따르지 않고 의도적으로 다르게 설계한 지점들이 있습니다.

## 진행 상황

| 단계 | 내용 | 상태 |
|---|---|---|
| 1단계 | 코어 루프 (이동/조준, 무기 획득·교체·내구도, 적 조우, XP) | 완료, 플레이테스트 검증 |
| 2단계 | XP/레벨업/증강, Boids 기반 적 이동, 난이도 곡선, 런 기록 | 완료, 플레이테스트 검증 |
| 3단계 | 메타 시스템 (무기 등급/컨디션, 칭호, 무기 도감, 저장) | 완료, 플레이테스트 검증 |
| 4단계 | 캐릭터 모델/애니메이션 적용 | 진행 중 |
| 5단계 | 콘텐츠 확장 (무기 특화 증강, 적 다양화, 메뉴 UI) | 예정 |
| 6단계 | 세계관/아트 스타일/모바일 이식 | 예정 |

### 진행 상황과 비주얼에 대하여

지금 시점의 무기·적·환경은 대부분 Unity 기본 도형(캡슐/큐브 등)에 최소한의 색상 구분만 되어 있는 상태입니다. 방치가 아니라 의도한 순서입니다. 세계관/아트 스타일/리소스 제작은 코어 루프가 실제로 재미있는지 검증된 뒤(6단계)로 명시적으로 미뤄뒀고, 그 전까지는 시스템(무기 내구도, 등급, 메타 진행, 증강 등)을 먼저 확실히 다지는 쪽을 택했습니다. 플레이어 캐릭터만 4단계에서 예외적으로 먼저 AI 생성 모델을 적용했습니다.

## 기술 포인트

### 1. 데이터 기반 무기 시스템 (ScriptableObject)

무기 스탯은 하드코딩 대신 `ScriptableObject`로 정의됩니다. 근접/원거리를 서브클래스로 나누지 않고 `category` enum 하나로 커버해, 세 번째 카테고리가 추가되어도 새 enum 값과 필드만 있으면 되도록 설계했습니다.

```csharp
[CreateAssetMenu(menuName = "Weapon Master/Weapon Definition", fileName = "WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [SerializeField] private WeaponCategory category;
    [SerializeField] private float damage;
    [SerializeField] private int maxDurability;

    [Header("Grade (GDD 5.5)")]
    [SerializeField] private WeaponRarity rarity = WeaponRarity.Common;
    // ...
}
```

등급별 최종 스탯은 런타임 공식으로 계산하지 않고, 사람이 직접 배분하거나 외부 밸런싱 도구가 주입하는 authored 값으로 결정합니다. ([`WeaponDefinition.cs`](Scripts/Weapons/WeaponDefinition.cs))

### 2. 저장 시스템 단일 창구

`PlayerPrefs` 직접 호출을 `SaveHandler` 하나로 몰아뒀습니다. WebGL의 `PlayerPrefs`/IndexedDB 저장 신뢰성 문제를 실제 빌드 테스트 대신 설계로 흡수하기 위한 선택으로, 나중에 저장 유실이 실측되어도 이 창구 하나만 고치면 되게 만들었습니다.

```csharp
public static class SaveHandler
{
    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save(); // 저장 빈도가 낮아 매번 flush해도 비용 부담 없음
    }
    // ...
}
```

([`SaveHandler.cs`](Scripts/Core/SaveHandler.cs))

### 3. 필요한 만큼만 채택한 Boids

다수의 적이 동시에 스폰될 때 서로 겹치는 문제를 막기 위해 Boids 알고리즘 중 separation(분리) 규칙만 적용했습니다. cohesion/alignment는 추격 방향이 이미 모두를 플레이어 한 점으로 끌어당기므로 굳이 필요하지 않다고 판단해 제외했습니다.

```csharp
private Vector3 ComputeSeparation()
{
    Vector3 separation = Vector3.zero;
    foreach (EnemyAI other in Active)
    {
        if (other == this) continue;
        Vector3 offset = transform.position - other.transform.position;
        float distance = offset.magnitude;
        if (distance > 0.0001f && distance < _separationRadius)
            separation += offset.normalized * (1f - distance / _separationRadius);
    }
    return separation;
}
```

([`EnemyAI.cs`](Scripts/Enemies/EnemyAI.cs))

### 4. 다중 스택 증강의 곱연산 합산

증강은 레벨링 방식으로 여러 번 선택할 수 있고, 같은 스탯을 겨냥하는 증강이 여러 개 선택되어 있을 수 있습니다. `PlayerAugmentManager`는 매번 전부 순회해 곱해 합산한 뒤 실제 소비처(무기 컨트롤러/이동/체력)에 밀어넣는 구조로, 증강 종류가 늘어나도 로직을 바꿀 필요가 없습니다. ([`PlayerAugmentManager.cs`](Scripts/Augments/PlayerAugmentManager.cs))

### 5. 이름이 곧 설계 문서

`RunStats`→`RunRecordManager`, `SaveSystem`→`SaveHandler`, `WeaponSlot`→`PlayerWeaponController` 등, 개발 중 여러 클래스가 실제 역할에 더 정확히 맞는 이름으로 리네이밍됐습니다. 각 이름이 왜 바뀌었는지는 코드 주석과 `Docs/DevNotes.md`에 남아있습니다.

## 폴더 구조

```
Scripts/
├── Achievements/   칭호(도전과제) 시스템
├── Arena/          아레나 경계
├── Augments/       증강(레벨업 강화) 시스템
├── CameraControl/  쿼터뷰 카메라
├── Characters/     캐릭터 정의/로스터
├── Core/           체력, 저장, 오브젝트 풀링 등 공용 인프라
├── DebugTools/     디버그 오버레이
├── Editor/         에디터 확장 툴(로스터 스캔, 인풋 액션/애니메이션 레이어 세팅)
├── Enemies/        적 AI/스폰/보상
├── Player/         플레이어 이동/조준/애니메이션 연동
├── UI/             HUD, 팝업
└── Weapons/        무기 정의/내구도/등급/컨디션/도감

Docs/
├── GDD.md        게임 디자인 문서 (버전별 변경 이력 포함)
├── DevPlan.md    단계별 개발 계획
├── Systems.md    시스템 체크리스트
└── DevNotes.md   구현 결정 로그
```

## 기술 스택

- Unity 6 (URP), 신규 Input System
- C#, 데이터 기반(ScriptableObject) 설계
- 저장: PlayerPrefs (단일 창구 SaveHandler 경유)
