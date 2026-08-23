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

지금 시점의 무기·적·환경은 대부분 Unity 기본 도형(캡슐/큐브 등)에 최소한의 색상 구분만 되어 있는 상태입니다. 세계관/아트 스타일/리소스 제작은 코어 루프가 실제로 재미있는지 검증된 뒤(6단계)로 명시적으로 미뤄뒀고, 그 전까지는 시스템(무기 내구도, 등급, 메타 진행, 증강 등)을 먼저 확실히 다지는 쪽을 택했습니다. 플레이어 캐릭터만 4단계에서 예외적으로 먼저 AI 생성 모델을 적용했습니다.

## 개발 방식

이 프로젝트는 설계와 구현 전 과정에서 Claude와 협업하는 구조로 진행됩니다.

먼저 `GDD.md`(게임 디자인 문서)로 무엇을 만들지 정의하고, 이를 바탕으로 필요한 시스템을 나열한 `Systems.md`, 어떤 순서로 구현할지 정하는 `DevPlan.md`를 차례로 작성합니다. 각 단계에 들어갈 때마다 무엇을 구현할지 Claude와 논의하고 그 결과를 `HANDOFF.md`로 정리해 넘기면, 구현을 맡는 Claude Code는 이를 바탕으로 `Scratchpad.md`에 구현 계획과 수정 범위를 명시하고, 여기 적힌 범위를 지키며 작업합니다. 구현이 끝나면 코드 리뷰와 플레이테스트를 거칩니다.

씬을 통째로 세팅해주는 일회성 편의 스크립트(`Stage1SceneBuilder.cs` 등) 몇 개는 결과만 확인하고 넘어가 이 저장소에서 제외했지만, 그 외 게임플레이 코드는 어떤 부분이 왜 바뀌었는지를 `DevNotes.md`에 기록하며 계속 추적하고 있습니다.

## 기술 포인트

### 1. Definition - Roster - Editor 패턴

무기·칭호·증강·캐릭터, 네 시스템 모두 같은 3단 구조를 씁니다. `Definition`(ScriptableObject 하나)은 개별 요소 하나의 데이터를, `Roster`는 그중 실제로 게임에 포함시킬 것들의 목록을 사람이 확정한 채로 보관합니다. 이 목록을 채우는 작업은 `RosterEditor<TRoster, TDefinition>` 공용 베이스가 담당하는데, 인스펙터에 버튼 하나를 추가해 누르면 지정된 폴더를 스캔해 `Roster`를 채워줍니다.

```csharp
public abstract class RosterEditor<TRoster, TDefinition> : Editor
    where TRoster : UnityEngine.Object
    where TDefinition : UnityEngine.Object
{
    protected abstract string FolderPath { get; }
    protected abstract string FieldName { get; }
    // 폴더 스캔 → Roster 필드 채우기는 여기 한 곳에만 있고,
    // 각 시스템은 FolderPath/FieldName만 지정해 상속받는다.
}
```

새 시스템을 추가할 때 이 패턴을 반복 구현하지 않고 그대로 상속만 받으면 되도록 만들었습니다. ([`RosterEditor.cs`](Scripts/Editor/RosterEditor.cs))

### 2. 이벤트 기반 디커플링

칭호 시스템처럼 여러 시스템의 상태 변화(무기 장착/파괴, 적 처치, 사망 등)에 반응해야 하는 기능을 안정적으로 붙이려면, 반응하는 쪽이 원본 시스템을 구독하되 원본 시스템은 누가 듣는지 몰라야 한다고 판단했습니다. `PlayerWeaponController`는 무기 장착/해제/파괴 이벤트를 발행할 뿐 `Achievements`나 `Animation` 쪽을 전혀 참조하지 않고, 반대로 그쪽에서 구독합니다.

```csharp
// PlayerWeaponController.cs
public event Action<WeaponBase> WeaponEquipped;
public event Action WeaponBecameUnarmed;
public event Action WeaponDestroyed;

// AchievementTracker.cs — 구독하는 쪽만 상대를 안다
playerWeaponController.WeaponEquipped += HandleWeaponEquipped;
playerWeaponController.WeaponBecameUnarmed += HandleWeaponBecameUnarmed;
```

같은 원칙이 체력(`HealthComponent`가 배율이 증강에서 온다는 걸 모름), 애니메이션(`PlayerAnimationController`가 게임플레이 쪽을 구독하는 순수 브릿지) 등 여러 곳에 반복 적용되어 있습니다. ([`PlayerWeaponController.cs`](Scripts/Weapons/PlayerWeaponController.cs), [`AchievementTracker.cs`](Scripts/Achievements/AchievementTracker.cs))

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
