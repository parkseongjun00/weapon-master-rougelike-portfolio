# 개발 노트 01: 코어 루프 지원

> 공개 편집본 p1 · 2026-10-23 · 원본 `703affa`. 현재 안내는 [공개본 문서 색인](../README.md)을 따른다. 과거 기록은 당시 상태이며 현재 코드 전체의 검증 결과가 아니다.

> 공통 작성 규칙: [README.md](README.md) 참고.

---

## 1. 2단계: 코어 루프를 지원하는 재미 구현

### 1.1 증강 효과 적용 구조

> 일자: 2026-07-27
> 참고: GDD 7.1 / 11-8

컴포넌트는 다음과 같이 분리한다.

- `AugmentDefinition`(ScriptableObject)
	- 대상 스탯(StatType enum), Max레벨, 레벨별 완결 보너스값 배열을 보유한다.
	- 배열 값은 누적합이 아니라 그 레벨의 최종값이다.
- `AugmentInstance`(순수 C# 클래스, MonoBehaviour 아님)
	- `AugmentDefinition` 참조와 현재 레벨을 보유한다.
	- `LevelUp()`(Max 클램프), `IsMaxed`, `CurrentBonus` 계산을 노출한다.
	- 레벨 규칙은 이 클래스가 소유한다.
- `PlayerAugments`(MonoBehaviour, Player 오브젝트에 WeaponSlot/PlayerMovement/HealthComponent/PlayerXP와 동급으로 부착)
	- `AugmentInstance[]`를 `(int)StatType`으로 인덱싱해 보유한다.
	- Dictionary 대신 array를 쓴 이유: StatType이 소수 고정 enum이라 array가 더 단순하고, Unity/IL2CPP의 enum-키 Dictionary boxing 이슈도 회피한다.
	- `GetMultiplier(StatType)`, `GetEligibleCandidates()`(Max 안 찍은 것만), `LevelUp(StatType)`을 노출한다.
- `PlayerXP`(기존 확장)
	- XP 누적은 기존대로 유지한다.
	- 레벨업 임계값 판정은 `while(누적 XP >= 다음 임계값)` 루프로 처리한다.
		- 한 번의 XP 획득으로 여러 레벨이 오르면 `OnLevelUp`을 레벨 수만큼 순차 발행한다.
	- 증강 자체는 모른다.
- `AugmentSelectionController`(신규)
	- `OnLevelUp` 구독 → 일시정지 → `PlayerAugments.GetEligibleCandidates()`로 3택을 뽑는다(3개 미만 남으면 남은 만큼만, 0개면 팝업 없이 풀피만).
	- 선택 시 `PlayerAugments.LevelUp()` 호출 + 풀피 회복을 지시한다.
	- `OnLevelUp`이 연속으로 여러 번 들어오면 큐로 처리한다.
		- 마지막 하나까지 다 고르기 전엔 재개(unpause)하지 않고 팝업을 연달아 띄운다.
	- 구현 참고: 일시정지 중에도 팝업 자체의 연출(페이드/하이라이트 등)은 동작해야 하므로 `Time.deltaTime`이 아니라 `Time.unscaledDeltaTime`을 쓴다.
- 무기(MeleeWeapon/RangedWeapon)·PlayerMovement·HealthComponent
	- `PlayerAugments` 참조만 갖고 `GetMultiplier(StatType.X)`로 배율을 곱해 쓴다.
		- 레벨/Max 여부는 몰라도 된다.
	- 맨손 공격(`UnarmedFists`)도 별도 클래스가 아니라 스탯만 약한 `MeleeWeapon` 인스턴스다.
		- `MeleeWeapon`이 `PlayerAugments`를 참조하게만 만들면 맨손 공격에도 자동으로 전역 증강이 적용돼, 별도 분기 처리가 불필요하다.

> 확정 v1 증강 목록: 공격력, 공격속도, 이동속도, 최대체력
> (무기 내구도 증가는 아직 제외. 더 고민 필요, GDD 11-8).

---

### 1.2 증강 카탈로그를 `AugmentRoster`로 분리

> 일자: 2026-08-03

위 '증강 효과 적용 구조'에서 `PlayerAugments`가 직렬화 필드로 직접 들고 있던 `AugmentDefinition[]`(게임에 존재하는 모든 증강 카탈로그)을 별도 SO `AugmentRoster`로 분리했다. `WeaponRoster`/`AchievementRoster`(3.4, 2026-08-01)와 동일한 슬림 패턴(`List<AugmentDefinition>` + `EnsureRegistered` + 에디터 재스캔 버튼, `IsDiscovered` 같은 발견 추적은 없다).

- **이유**: 패턴 통일이 아니라 책임 분리 문제였다.
	- 카탈로그(게임 전역, 플레이어 무관)와 `instances`(플레이어가 실제로 찍은 레벨, 진짜 플레이어 상태)가 `PlayerAugments` 한 클래스에 같이 얹혀 있었다.
	- `PlayerAugments`라는 이름은 '플레이어 상태'를 약속하는데 실제로는 게임 전역 데이터까지 들고 있어 책임이 어긋나 있었다.
- `PlayerAugments`
	- `definitions` 필드 삭제
	- `roster`(`AugmentRoster` 참조)로 교체
	- `Awake()`는 `roster.Augments`를 순회
- `Stage1SceneBuilder`
	- `BuildAugmentRoster(AugmentDefinition[])` 신설(`BuildAchievementRoster`와 동일 패턴, `Assets/Data/AugmentRoster.asset`)
	- `BuildPlayer`가 `AugmentDefinition[]` 대신 `AugmentRoster`를 받아 배선
- 신규 파일
	- `Assets/Scripts/Augments/AugmentRoster.cs`
	- `Assets/Scripts/Editor/RosterInspectors/AugmentRosterEditor.cs`(폴더 `Assets/Data/Augments` 재스캔)

---

### 1.3 `PlayerAugments` → `PlayerAugmentSystem` 개명

> 일자: 2026-08-03

- **개명 이유**: 'PlayerAugments'라는 복수형 명사가 단순 데이터 꾸러미처럼 읽힌다는 지적으로 개명했다.
	- §3단계 '네이밍 컨벤션. 4분류로 재정의' 표에 대입하면 static 접근 가능(`GetMultiplierSafe` 같은 static 헬퍼로 인스턴스 없이 호출) + 씬 컴포넌트로 실체화(`Instance` 싱글턴) 두 축 모두 `AchievementSystem`과 같은 칸(`~System`)이라 이름을 맞췄다.
	- `git mv`로 파일명도 함께 변경(`.meta` GUID 보존).
- **참고: 애초에 왜 static 싱글턴이 됐는가**: 처음부터 의도적으로 설계한 게 아니라 이미 있던 프로젝트 관례를 그대로 물려받았다.
	- `RunStats`/`PlayerXP`가 먼저 같은 이유(무기 필드 스폰 프리팹처럼 씬 바깥 애셋에 사는 컴포넌트는 씬 전용 오브젝트에 대한 참조를 직렬화할 수 없다. 아래 '씬 전용 오브젝트는 직렬화 필드 대신 static `Instance` 접근' 참고, §2.9)로 static `Instance` 패턴을 쓰고 있었고, 같은 날(2026-07-27) 증강 시스템을 설계하면서 `MeleeWeapon`/`RangedWeapon`(둘 다 무기 필드 스폰 프리팹에 존재)이 배율을 읽어야 하는 동일한 제약에 부딪혀 같은 해법을 채택했다.
	- `GetMultiplierSafe` static 래퍼는 그보다 나중에, 호출부마다 반복되는 `Instance != null ? ... : 1f` 삼항연산자를 줄이려고 편의상 추가됐다.
	- 결과적으로 이게 '완전한 static API 표면'을 만들어 `~System` 분류로 이어졌다.

---

### 1.4 런 결과 저장 방식

> 일자: 2026-07-27

- **저장 범위**: 사망 시 마지막 런 결과(생존시간/처치수)를 로컬에 저장한다.
	- 결과를 보여주는 화면 UI 자체는 4단계 '재시작/메뉴 화면'에서 구현하고, 2단계에서는 데이터 저장까지만 담당한다.
- **저장 방식**: `PlayerPrefs`의 `SetFloat`/`SetInt`로 원시 값 두 개만 저장한다.
	- 항목이 두 개뿐이라 JSON 직렬화는 불필요하다.
	- 3단계 메타 재화/해금 목록처럼 저장 데이터가 구조적으로 늘어나면, 그때 `PlayerPrefs` 문자열 키에 `JsonUtility`로 직렬화하는 방식으로 확장 검토한다.

---

### 1.5 무기 ScriptableObject 리팩터링 범위

> 일자: 2026-07-27

- **후순위 스탯 필드 동시 정의**: 이동속도 페널티, 등급/희귀도 필드도 지금 함께 정의하기로 확정한다.
	- GDD 5.2가 이 스탯들을 '정식 채택, 개발 순서상 후순위'로 명시했으므로, MVP 단계에선 값 미사용/기본값이어도 필드 자체는 지금 넣어 Stage 4/5에서 SO를 다시 뜯어고치지 않도록 한다.
- **`Stage1SceneBuilder.cs`도 리팩터링 범위에 포함된다**
	- 현재 `BuildMeleeWeaponPrefab`/`BuildRangedWeaponPrefab`이 데미지·쿨다운 등을 프리팹에 직접 `SetField`로 박아넣는 방식이라, SO 에셋을 생성/참조하도록 함께 고쳐야 한다.
	- 빌더가 매 실행마다 기존 산출물을 지우고 재생성하는 구조라 기존 프리팹의 별도 마이그레이션은 불필요하다.
	- `UnarmedFists`(맨손 공격)는 `BuildPlayer()` 안에서 두 정식 무기와 다른 경로로 하드코딩되고 있으므로, 리팩터링 시 빠뜨리지 않고 이것도 `WeaponDefinition` SO를 참조하도록 맞춘다.

---

### 1.6 적 체력바/데미지 숫자 렌더링 방식

> 일자: 2026-07-28

적 체력바·피격 데미지 숫자 팝업 도입 검토 중, 장르 특성(GDD 6.2: 시간 경과에 따라 동시 스폰 적 수가 계속 증가하는 엔드리스 난이도 곡선)을 고려해 Canvas 기반 UI를 배제하기로 결정했다.

- 적 체력바: 개체별 World Space Canvas 대신 `SpriteRenderer` 2장(배경 + 채움, 채움의 `transform.localScale.x`로 비율 표현)으로 구현했다.
	- URP SRP 배칭으로 다수 적 렌더링 비용을 낮춘다.
	- 카메라가 고정 오프셋 추적(`QuarterViewCameraFollow`)이라 회전이 없으므로 별도 빌보드 처리가 불필요하다.
- 데미지 숫자: UGUI Canvas가 아닌 순수 `TextMeshPro`(3D 월드 텍스트, Canvas 불필요)로 구현했다.
- 기각한 대안: 통합 오버레이 Canvas + `WorldToScreenPoint` 좌표 투사 방식이다.
	- Canvas 비용 자체는 피하지만 좌표추적/풀링 매니저를 새로 지어야 해서, Canvas를 아예 안 쓰는 스프라이트/월드텍스트 방식보다 복잡도 대비 이점이 없다고 판단해 배제했다.
- `HealthComponent.TakeDamage`가 기존엔 `OnHealthChanged(current, max)`만 발행해 델타(순수 피해량)를 안 넘긴다.
	- 데미지 숫자 팝업을 위해 `OnDamaged(float amount)` 이벤트 추가가 필요하다.
- TextMeshPro는 `com.unity.ugui` 2.0.0에 포함되어 별도 패키지 설치 불필요(첫 사용 시 TMP Essential Resources 임포트만 필요).

---

### 1.7 오브젝트 풀링 도입 범위

> 일자: 2026-07-28

호드 서바이벌 장르(GDD 6.2, 동시 스폰 적 수 무한 증가)에서 반복적인 `Instantiate`/`Destroy`로 인한 GC 압박이 프레임 안정성에 직접 영향을 준다고 판단, 스폰 빈도가 높은 지점에 오브젝트 풀 적용을 확정했다.

- 적용 대상: 적 스폰(`EnemySpawner`), 투사체 스폰(`Projectile`, 원거리 무기 발사 시), 데미지 숫자 팝업
- 보류 대상: 무기 필드 스폰(`WeaponSpawner`)은 고정 스폰 포인트에서 주기적으로 소수만 리스폰되는 낮은 빈도라 GC 부담이 무시할 만한 수준이다.
	- 3단계 무기 등급/희귀도 리팩터로 `WeaponSpawner`/`WeaponSlot`을 다시 만질 때 함께 처리하기로 보류한다(이중작업 방지).
- `EnemySpawner`(현재 `Instantiate`)와 `EnemyXPReward.HandleDeath()`(현재 `Destroy(gameObject)`)를 풀 체크아웃/반납 방식으로 전환해야 한다.
- `HealthComponent.Awake()`가 `CurrentHealth = MaxHealth`를 1회만 초기화하는 구조라, 풀 재사용 시(재활성화만 되고 `Awake`는 다시 호출되지 않는다) 낡은 체력값이 남는 문제를 발견했다.
	- 풀에서 체크아웃할 때 호출할 명시적 리셋 메서드가 필요하다.
	- 플레이어는 실제 `Instantiate` 경로만 타므로 기존 `Awake` 동작과 호환을 유지해야 한다.

---

### 1.8 적 체력바/데미지 팝업 고정 회전: '빌보드 불필요' 판단 정정

> 기록일: 2026-07-28

- **정정 내용**: 위 '적 체력바/데미지 숫자 렌더링 방식' 항목에서 '카메라가 고정 오프셋 추적이라 회전이 없으므로 별도 빌보드 처리 불필요'라고 판단했으나, 실제 플레이테스트에서 틀린 것으로 확인됐다.
	- 카메라 자체는 회전하지 않는 게 맞지만, 체력바 앵커가 **적 루트의 자식**이고 적 루트는 `EnemyAI`가 이동 방향에 따라 매 프레임 회전시킨다.
	- 자식의 `localRotation`을 고정해도 부모 회전에 실려 함께 돌아가 버려 카메라를 향한 각도가 깨진다.
- **해결**: 카메라를 매 프레임 바라보는 진짜 빌보드(카메라 참조 + `LookRotation` 계산)는 여전히 불필요하다고 판단했다.
	- 카메라 각도 자체가 절대 바뀌지 않는 설계(고정 피치, 회전 없는 추적)이므로 목표 회전값이 상수이기 때문이다.
	- 대신 `Core/FixedWorldRotation.cs`(신규, `LateUpdate`에서 `transform.rotation`을 고정 월드 Euler 값으로 강제하는 최소 컴포넌트)를 적 체력바 앵커와 데미지 숫자 팝업 프리팹 양쪽에 부착해 부모 회전과 무관하게 카메라 방향 각도를 유지한다.
	- 두 곳이 같은 각도 상수(`Stage1SceneBuilder.CameraFacingEuler`)를 공유한다.

---

### 1.9 2단계 UI 라운드 완료 보고에서 승격

> 기록일: 2026-07-30

Claude Code의 2단계 UI 마무리 라운드(체력바/XP게이지/데미지팝업/풀링) 완료 보고에서 나온 설계 판단 중, 위 항목들에 아직 반영 안 된 부분만 승격했다.

- **체력바 채움 표현**: 스프라이트 피벗 트릭 대신 로컬 X 위치 보정 방식을 채택했다.
	- 배경/채움이 같은 중앙-피벗 스프라이트를 공유하고, 채움은 `localScale.x`를 줄이면서 `localPosition.x`를 왼쪽 기준으로 보정한다.
	- 스프라이트 애셋은 `CreateMaterial`과 동일한 캐시 패턴으로 1회 생성(런타임 `Texture2D`+`Sprite.Create` 후 `AssetDatabase`에 저장) 후 재사용한다.
- **풀 소유 구조**: 스포너 내장 방식과 독립 싱글턴 방식을 대상별로 다르게 택한 비대칭 구조다.
	- 적 풀은 `EnemySpawner`가 내부 소유한다(`static Instance` + `ReturnEnemy()` 노출, `RunStats`/`PlayerXP`와 동일한 관례).
	- 발사체는 프리팹별 `Dictionary`를 가진 별도 `ProjectilePool` 싱글턴이다(발사체 프리팹 종류가 늘어날 여지 고려).
	- 데미지 팝업도 별도 `DamageNumberPool` 싱글턴이다.
- **`DamagePopupSpawner` 폴더 위치**: `Core`가 아니라 `UI` 네임스페이스/폴더다.
	- 유일한 의존 대상이 `DamageNumberPool`(UI 레이어)이라, Core가 UI를 참조하지 않는 기존 레이어링을 지키기 위해서다.
	- `HealthComponent`와 마찬가지로 플레이어/적 양쪽에 부착한다.

---

### 1.10 EnemyAI 분리(separation) 로직의 O(n²) 확장성: 알려진 한계

> 기록일: 2026-07-28

- **알려진 한계**: `EnemyAI.ComputeSeparation()`이 정적 리스트(`Active`)를 매 프레임 O(n²)로 순회하는 구조다(구현 시점 주석: 'MVP 단계의 적 수는 적어서 이 정도로 충분히 저렴하다').
	- 오브젝트 풀링 도입 논의 중 재확인됐다.
	- 풀링은 GC 압박만 줄일 뿐 이 알고리즘 자체의 프레임 비용 증가(적 수 제곱에 비례)는 해결하지 못한다.
	- 엔드리스 난이도 곡선(GDD 6.2)상 동시 적 수가 계속 늘어나는 설계이므로, 풀링보다 먼저 이쪽이 성능 병목이 될 가능성이 크다.
- **재검토 트리거**: 스폰 강도 곡선 실측 튜닝 중 동시 적 수가 늘어나며 프레임 드랍이 관측되면, 공간 분할(그리드/쿼드트리 등) 기반 이웃 탐색으로 전환을 검토한다.

---
