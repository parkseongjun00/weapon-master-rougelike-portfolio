# Weapon Master Roguelike

Unity 6·C# 기반 1인 개발 액션 로그라이트 프로젝트의 **핵심 코드와 개발 문서**입니다. 주변 무기를 획득·교체하고 내구도가 소진되면 맨손으로 전환하는 전투를 구현하고 있습니다.

이 저장소는 코드 열람용 발췌본입니다. 씬·프리팹·모델·패키지 설정은 포함하지 않아 독립적인 Unity 실행 프로젝트로 사용할 수 없습니다. 코드 기준은 개발 저장소의 `703affa`(2026-10-19)이며, 공개본 정리일은 2026-10-23입니다.

## 1. 코드 읽기 순서

| 순서 | 확인할 내용 | 시작 코드 | 이어서 읽을 코드 |
|---|---|---|---|
| 1 | 적의 판단과 공격·이동 요청 | [EnemyAI](Scripts/Enemies/EnemyAI.cs) | [SlotFollower](Scripts/Movement/SlotFollower.cs), [WeaponController](Scripts/Weapons/Interaction/WeaponController.cs) |
| 2 | 군집 자리 배정과 예약 수명 | [SurroundSlots](Scripts/Movement/SurroundSlots.cs) | [SlotGeometry](Scripts/Movement/SlotGeometry.cs), [NavigationMovement](Scripts/Movement/NavigationMovement.cs) |
| 3 | 입력과 공통 전투 실행의 경계 | [PlayerCombatInput](Scripts/Player/PlayerCombatInput.cs) | [WeaponController](Scripts/Weapons/Interaction/WeaponController.cs), [CharacterAnimationController](Scripts/Animation/CharacterAnimationController.cs) |
| 4 | 타격 구간·중복 피해·공격 중단 | [MeleeWeapon](Scripts/Weapons/Combat/MeleeWeapon.cs) | [AttackTargetRules](Scripts/Weapons/Combat/AttackTargetRules.cs), [WeaponBase](Scripts/Weapons/Combat/WeaponBase.cs) |
| 5 | 생성·사망·퇴장·재사용 | [EnemySpawner](Scripts/Enemies/EnemySpawner.cs) | [HealthComponent](Scripts/Core/HealthComponent.cs), [SimpleObjectPool](Scripts/Core/SimpleObjectPool.cs) |

## 2. 주요 구현

### 2.1 군집 접근과 이동 책임 분리

`EnemyAI → SlotFollower → NavigationMovement → NavMeshAgent`

- `SurroundSlots`: 몸체 반경으로 다중 링을 계산하고 독점 자리를 배정. `Epoch`는 대상 생애, `Revision`은 예약 변경을 구분합니다.
- `SlotFollower`: 자리 확보·도착·재시도 판단. 이전 예약의 실패 보고가 새 예약에 적용되지 않도록 확인합니다.
- `NavigationMovement`: 경로 요청·도착·정지·회전·재사용 초기화. 경로 계산 대기와 도달 불가 상태를 구분합니다.
- 직접 이동의 장애물 우회 한계를 확인한 뒤 NavMeshAgent로 전환했습니다. 슬롯 배정 책임은 유지했습니다.

설계 변경과 당시 검증: [개발 노트5.30](Docs/DevNotes/05-EnemyAnimation.md#530-navmesh-일반-이동과-역할-이름-전환).

---

### 2.2 플레이어와 적의 공통 전투

`PlayerCombatInput / EnemyAI → WeaponController → 무기 실행·CharacterAnimationController`

- 입력·AI는 공격 의도를 전달하고, 공통 컨트롤러는 장착·공격·중단을 담당합니다.
- 애니메이션의 타격 시작·종료 신호가 근접 판정 구간을 제어합니다.
- `MeleeWeapon`은 공격당 대상 중복 피해를 막고, 명중한 공격의 타격 구간 종료 시 내구도를 소비합니다.
- 공격 중 교체·파괴·비활성화 때 진행 상태와 타격 Collider를 정리합니다.
- 기본 적은 공통 맨손 공격을 사용합니다. 적의 현장 무기 획득 AI는 후속 검토 범위입니다.

구현 범위와 사용자 확인: [개발 노트5.33~5.34](Docs/DevNotes/05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치).

---

### 2.3 계측을 통한 반복 정지 원인 추적

플레이어 이동 중 적이 정지·출발을 반복하는 문제에서, [NavigationTrace](Scripts/Movement/NavigationTrace.cs)로 프레임·배치 갱신·예약 변경·정지 이유를 연결했습니다.

`반경 0.5 → 0.50000006 → 슬롯 재구성 → 예약 변경 → 이동 정지`

- 미세 반경 오차를 실제 크기 변화로 처리한 비교 조건 세 곳을 수정했습니다.
- 실제 크기 변화와 이전 예약 보호 규칙은 유지했습니다.
- 사용자 확인은 반복 정지 증상의 해결 범위입니다. 전체 군집 성능 개선을 의미하지 않습니다.

원인·수정·확인 기록: [개발 노트5.31~5.32](Docs/DevNotes/05-EnemyAnimation.md#531-반경-계산-오차로-인한-슬롯-재구성-반복-수정).

## 3. 문서 읽기

| 관심 내용 | 문서 |
|---|---|
| 문서 체계와 공개 범위 | [문서 색인](Docs/README.md), [DocSystem](Docs/DocSystem.md) |
| 게임 규칙과 설계 범위 | [GDD](Docs/GDD.md), [Systems](Docs/Systems.md) |
| 구현 순서와 보류 과제 | [DevPlan](Docs/DevPlan.md), [Backlog](Docs/Backlog.md) |
| 기술 결정과 변경 이유 | [DevNotes 색인](Docs/DevNotes/README.md) |
| 조사·실패·정정 과정 | [Troubleshooting 색인](Docs/Troubleshooting/README.md) |
| 환경·적 디자인의 확정과 후보 | [FirstMap](Docs/Art/FirstMap.md), [EnemyDesign](Docs/Art/EnemyDesign.md) |

## 4. 개발 방식과 확인 범위

- Claude·Codex와 협업해 설계 검토·구현·코드 검토·문서화를 진행했습니다. 프로젝트 방향·작업 범위·수용 판단과 직접 플레이 확인은 개발자가 담당합니다. 개별 제안과 변경 경위는 개발 노트에 기록했습니다.
- 현재 코드에는 NavMesh 이동과 공통 전투가 반영되어 있습니다. 근접 재공격과 기본 적 타격, 반복 정지 수정은 개발 기록의 사용자 확인 범위입니다.
- 이전 이동 구현의 133개 검사는 당시 결과입니다. 최신 이동·전투 전체의 회귀 통과를 뜻하지 않습니다.
- 빌드·장시간 군집·전체 회귀·성능은 별도 검증 대상입니다. 이번 공개본 정리에서는 Unity 실행 검증을 수행하지 않았습니다.

## 5. 발췌 기준

이동·전투·생애주기와 해당 흐름을 읽는 데 필요한 지원 코드를 포함했습니다. UI·업적·증강·카메라·Editor 도구·자동 생성 입력 클래스는 제외했습니다. 문서는 전체 개발 범위를 다루므로 발췌하지 않은 기능도 등장합니다.

[파일별 원본 대응표](Docs/SourceMap.md)에서 포함 파일과 생략 범위를 확인할 수 있습니다. 실행 코드의 동작은 원본을 유지했고, 입력·저장 코드 세 곳의 주석에서 배포 대상 표현만 제거했습니다.
