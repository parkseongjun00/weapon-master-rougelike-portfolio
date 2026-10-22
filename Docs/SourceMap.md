# 공개 코드 원본 대응표

- 공개 편집본: p1 / 2026-10-23
- 원본 커밋: `703affae9ea3a2796f5d657dca5f368c07846b6c`
- 포함 코드: 53개 파일

## 1. 포함 기준

군집 접근·공통 전투·생성 및 재사용과 그 지원 코드를 발췌했다. 원본 폴더 관계는 `Assets/Scripts/`를 `Scripts/`로 옮겨 유지한다.

## 2. 파일 대응

| 공개 파일 | 원본 파일 | 변경 |
|---|---|---|
| [Scripts/Animation/CharacterAnimationController.cs](../Scripts/Animation/CharacterAnimationController.cs) | `Assets/Scripts/Animation/CharacterAnimationController.cs` | 원본 유지 |
| [Scripts/Arena/ArenaBounds.cs](../Scripts/Arena/ArenaBounds.cs) | `Assets/Scripts/Arena/ArenaBounds.cs` | 원본 유지 |
| [Scripts/Arena/EnvironmentQuery.cs](../Scripts/Arena/EnvironmentQuery.cs) | `Assets/Scripts/Arena/EnvironmentQuery.cs` | 원본 유지 |
| [Scripts/Characters/CharacterDefinition.cs](../Scripts/Characters/CharacterDefinition.cs) | `Assets/Scripts/Characters/CharacterDefinition.cs` | 원본 유지 |
| [Scripts/Core/HealthComponent.cs](../Scripts/Core/HealthComponent.cs) | `Assets/Scripts/Core/HealthComponent.cs` | 원본 유지 |
| [Scripts/Core/IDamageable.cs](../Scripts/Core/IDamageable.cs) | `Assets/Scripts/Core/IDamageable.cs` | 원본 유지 |
| [Scripts/Core/SaveHandler.cs](../Scripts/Core/SaveHandler.cs) | `Assets/Scripts/Core/SaveHandler.cs` | 주석의 배포 대상 표현 제거 |
| [Scripts/Core/SimpleObjectPool.cs](../Scripts/Core/SimpleObjectPool.cs) | `Assets/Scripts/Core/SimpleObjectPool.cs` | 원본 유지 |
| [Scripts/Core/StatType.cs](../Scripts/Core/StatType.cs) | `Assets/Scripts/Core/StatType.cs` | 원본 유지 |
| [Scripts/Effects/HitEffectAnchorSet.cs](../Scripts/Effects/HitEffectAnchorSet.cs) | `Assets/Scripts/Effects/HitEffectAnchorSet.cs` | 원본 유지 |
| [Scripts/Effects/HitElasticReaction.cs](../Scripts/Effects/HitElasticReaction.cs) | `Assets/Scripts/Effects/HitElasticReaction.cs` | 원본 유지 |
| [Scripts/Effects/HitFlash.cs](../Scripts/Effects/HitFlash.cs) | `Assets/Scripts/Effects/HitFlash.cs` | 원본 유지 |
| [Scripts/Effects/HitSparkEffect.cs](../Scripts/Effects/HitSparkEffect.cs) | `Assets/Scripts/Effects/HitSparkEffect.cs` | 원본 유지 |
| [Scripts/Effects/HitSparkPool.cs](../Scripts/Effects/HitSparkPool.cs) | `Assets/Scripts/Effects/HitSparkPool.cs` | 원본 유지 |
| [Scripts/Effects/WeaponTrailEffect.cs](../Scripts/Effects/WeaponTrailEffect.cs) | `Assets/Scripts/Effects/WeaponTrailEffect.cs` | 원본 유지 |
| [Scripts/Enemies/EnemyAI.cs](../Scripts/Enemies/EnemyAI.cs) | `Assets/Scripts/Enemies/EnemyAI.cs` | 원본 유지 |
| [Scripts/Enemies/EnemySpawner.cs](../Scripts/Enemies/EnemySpawner.cs) | `Assets/Scripts/Enemies/EnemySpawner.cs` | 원본 유지 |
| [Scripts/Enemies/EnemyXPReward.cs](../Scripts/Enemies/EnemyXPReward.cs) | `Assets/Scripts/Enemies/EnemyXPReward.cs` | 원본 유지 |
| [Scripts/Movement/IMovementSpeed.cs](../Scripts/Movement/IMovementSpeed.cs) | `Assets/Scripts/Movement/IMovementSpeed.cs` | 원본 유지 |
| [Scripts/Movement/NavigationMovement.cs](../Scripts/Movement/NavigationMovement.cs) | `Assets/Scripts/Movement/NavigationMovement.cs` | 원본 유지 |
| [Scripts/Movement/NavigationTrace.cs](../Scripts/Movement/NavigationTrace.cs) | `Assets/Scripts/Movement/NavigationTrace.cs` | 원본 유지 |
| [Scripts/Movement/SlotFollower.cs](../Scripts/Movement/SlotFollower.cs) | `Assets/Scripts/Movement/SlotFollower.cs` | 원본 유지 |
| [Scripts/Movement/SlotGeometry.cs](../Scripts/Movement/SlotGeometry.cs) | `Assets/Scripts/Movement/SlotGeometry.cs` | 원본 유지 |
| [Scripts/Movement/SurroundSlots.cs](../Scripts/Movement/SurroundSlots.cs) | `Assets/Scripts/Movement/SurroundSlots.cs` | 원본 유지 |
| [Scripts/Player/AimVectorProvider.cs](../Scripts/Player/AimVectorProvider.cs) | `Assets/Scripts/Player/AimVectorProvider.cs` | 주석의 배포 대상 표현 제거 |
| [Scripts/Player/MeleeAttackStateExitNotifier.cs](../Scripts/Player/MeleeAttackStateExitNotifier.cs) | `Assets/Scripts/Player/MeleeAttackStateExitNotifier.cs` | 원본 유지 |
| [Scripts/Player/PlayerAim.cs](../Scripts/Player/PlayerAim.cs) | `Assets/Scripts/Player/PlayerAim.cs` | 원본 유지 |
| [Scripts/Player/PlayerCombatInput.cs](../Scripts/Player/PlayerCombatInput.cs) | `Assets/Scripts/Player/PlayerCombatInput.cs` | 원본 유지 |
| [Scripts/Player/PlayerInputReader.cs](../Scripts/Player/PlayerInputReader.cs) | `Assets/Scripts/Player/PlayerInputReader.cs` | 주석의 배포 대상 표현 제거 |
| [Scripts/Player/PlayerMovement.cs](../Scripts/Player/PlayerMovement.cs) | `Assets/Scripts/Player/PlayerMovement.cs` | 원본 유지 |
| [Scripts/Player/PlayerXP.cs](../Scripts/Player/PlayerXP.cs) | `Assets/Scripts/Player/PlayerXP.cs` | 원본 유지 |
| [Scripts/Weapons/Collection/WeaponCollectionHandler.cs](../Scripts/Weapons/Collection/WeaponCollectionHandler.cs) | `Assets/Scripts/Weapons/Collection/WeaponCollectionHandler.cs` | 원본 유지 |
| [Scripts/Weapons/Collection/WeaponRoster.cs](../Scripts/Weapons/Collection/WeaponRoster.cs) | `Assets/Scripts/Weapons/Collection/WeaponRoster.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/AttackTargetRules.cs](../Scripts/Weapons/Combat/AttackTargetRules.cs) | `Assets/Scripts/Weapons/Combat/AttackTargetRules.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/FistHitbox.cs](../Scripts/Weapons/Combat/FistHitbox.cs) | `Assets/Scripts/Weapons/Combat/FistHitbox.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/IWeapon.cs](../Scripts/Weapons/Combat/IWeapon.cs) | `Assets/Scripts/Weapons/Combat/IWeapon.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/MeleeHitboxSource.cs](../Scripts/Weapons/Combat/MeleeHitboxSource.cs) | `Assets/Scripts/Weapons/Combat/MeleeHitboxSource.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/MeleeWeapon.cs](../Scripts/Weapons/Combat/MeleeWeapon.cs) | `Assets/Scripts/Weapons/Combat/MeleeWeapon.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/RangedWeapon.cs](../Scripts/Weapons/Combat/RangedWeapon.cs) | `Assets/Scripts/Weapons/Combat/RangedWeapon.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/WeaponAttackHitbox.cs](../Scripts/Weapons/Combat/WeaponAttackHitbox.cs) | `Assets/Scripts/Weapons/Combat/WeaponAttackHitbox.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/WeaponBase.cs](../Scripts/Weapons/Combat/WeaponBase.cs) | `Assets/Scripts/Weapons/Combat/WeaponBase.cs` | 원본 유지 |
| [Scripts/Weapons/Combat/WeaponDurability.cs](../Scripts/Weapons/Combat/WeaponDurability.cs) | `Assets/Scripts/Weapons/Combat/WeaponDurability.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponCategory.cs](../Scripts/Weapons/Data/WeaponCategory.cs) | `Assets/Scripts/Weapons/Data/WeaponCategory.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponCondition.cs](../Scripts/Weapons/Data/WeaponCondition.cs) | `Assets/Scripts/Weapons/Data/WeaponCondition.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponConditionData.cs](../Scripts/Weapons/Data/WeaponConditionData.cs) | `Assets/Scripts/Weapons/Data/WeaponConditionData.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponData.cs](../Scripts/Weapons/Data/WeaponData.cs) | `Assets/Scripts/Weapons/Data/WeaponData.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponGradeData.cs](../Scripts/Weapons/Data/WeaponGradeData.cs) | `Assets/Scripts/Weapons/Data/WeaponGradeData.cs` | 원본 유지 |
| [Scripts/Weapons/Data/WeaponRarity.cs](../Scripts/Weapons/Data/WeaponRarity.cs) | `Assets/Scripts/Weapons/Data/WeaponRarity.cs` | 원본 유지 |
| [Scripts/Weapons/Interaction/WeaponController.cs](../Scripts/Weapons/Interaction/WeaponController.cs) | `Assets/Scripts/Weapons/Interaction/WeaponController.cs` | 원본 유지 |
| [Scripts/Weapons/Interaction/WeaponPickup.cs](../Scripts/Weapons/Interaction/WeaponPickup.cs) | `Assets/Scripts/Weapons/Interaction/WeaponPickup.cs` | 원본 유지 |
| [Scripts/Weapons/Interaction/WeaponSpawner.cs](../Scripts/Weapons/Interaction/WeaponSpawner.cs) | `Assets/Scripts/Weapons/Interaction/WeaponSpawner.cs` | 원본 유지 |
| [Scripts/Weapons/Projectiles/Projectile.cs](../Scripts/Weapons/Projectiles/Projectile.cs) | `Assets/Scripts/Weapons/Projectiles/Projectile.cs` | 원본 유지 |
| [Scripts/Weapons/Projectiles/ProjectilePool.cs](../Scripts/Weapons/Projectiles/ProjectilePool.cs) | `Assets/Scripts/Weapons/Projectiles/ProjectilePool.cs` | 원본 유지 |

## 3. 생략 범위

- UI·업적·증강·카메라·Editor 도구와 자동 생성 `GameInputActions` 클래스.
- 씬·프리팹·Animator Controller·모델·재질·ScriptableObject 에셋·패키지 설정.
- Unity 연결과 생성 입력 클래스 등이 필요하므로 발췌본만의 컴파일·실행은 제공 범위가 아니다.
- 과거 검증 도구는 이전 이동·접촉 피해 계약을 포함한다. 최신 코드의 실행 가능한 테스트 묶음으로 오인되지 않도록 제외했다.
- 개발 노트에 등장하는 과거 클래스명은 변경 경위이며, 공개 코드의 누락 오류를 뜻하지 않는다.
