# 개발 노트 03: 플레이어 캐릭터와 애니메이션

> 공개 편집본 p1 · 2026-10-23 · 원본 `703affa`. 현재 안내는 [공개본 문서 색인](../README.md)을 따른다. 과거 기록은 당시 상태이며 현재 코드 전체의 검증 결과가 아니다.

> 공통 작성 규칙: [README.md](README.md) 참고.

---

## 3. 4단계: 캐릭터/애니메이션 적용

### 3.1 캐릭터 아키텍처 확정

> 일자: 2026-08-20
> 상태: 확정, 구현 착수

`DevPlan.md` §2.5로 4단계에 앞당겨진 '플레이어 캐릭터 모델(치비 스타일)/애니메이션 적용' 작업의 설계다. `Assets/Docs/Scratchpad.md`에서 여러 라운드에 걸쳐 논의된 안이 이번에 전부 확정됐다. 사용자가 향후 신규 플레이어블 캐릭터 추가를 이미 결정된 사실로 명시했으므로("가상의 미래 요구사항을 위해 설계하지 않는다" 원칙에 해당하지 않음), 처음부터 확장 가능한 구조로 잡는다.

- **Rig는 Humanoid로 통일한다.**
	- `Boy_Model.fbx`와 애니메이션 클립(`Alert` 제외 7종) 전부 Humanoid로 전환한다.
	- Humanoid 아바타는 스켈레톤이 달라도 근육 공간(muscle space)으로 리타겟팅되므로, 같은 애니메이션 자산을 캐릭터가 몇 명으로 늘어나든 재사용할 수 있다.
	- 확장성의 핵심 메커니즘이다.
- **캐릭터 데이터는 기존 Definition/Roster 패턴을 따른다.**
	- `WeaponDefinition`/`WeaponRoster`, `AugmentDefinition`/`AugmentRoster`, `AchievementDefinition`/`AchievementRoster`와 동일한 모양이다.
	- `CharacterDefinition`(SO, 신설): `displayName`, `characterPrefab`, `animatorOverrideController`(nullable, 비우면 공용 베이스 컨트롤러 사용).
	- `CharacterRoster`(SO, 신설) + `Editor/CharacterRosterEditor.cs`(`RosterEditor<TRoster, TDefinition>` 제네릭 베이스 상속, §2.22 참고).
- **`characterPrefab`이 겉모습뿐 아니라 `CharacterController`(충돌 캡슐)까지 책임진다.**
	- 필드명을 `visualPrefab`이 아니라 `characterPrefab`으로 정한 이유이기도 하다.
	- 캡슐 height/radius/center를 `CharacterDefinition`에 숫자 필드로 두지 않는다.
	- 모델과 겹쳐 보면서 눈으로 맞추는 게 숫자 입력보다 정확하다는 사용자 판단(2026-08-20)에 따라, 이 값은 프리팹 자체(Prefab 편집 모드에서 Scene 뷰 캡슐 기즈모로 조정)가 유일한 소스다.
	- `Stage1SceneBuilder.BuildPlayer()`는 빈 `GameObject("Player")`를 만들고 `AddCapsuleVisual`/`AddComponent<CharacterController>()`로 값을 박아넣던 기존 방식 대신, `CharacterDefinition.characterPrefab`을 인스턴스화해 그 자체를 Player 루트로 쓴다.
	- `CharacterController`가 이미 프리팹에 있으므로 빌더가 값을 세팅할 필요가 없다.
- **`animatorOverrideController`의 정체는 Unity 내장 `AnimatorOverrideController`다.**
	- 사용자 질문(Humanoid 아닌 특이 리그를 위한 필드인지)을 계기로 명확히 했다.
	- 아니다: Rig는 위에서 이미 Humanoid로 무조건 통일했다.
	- Humanoid로 못 만드는 캐릭터가 생기면 로스터 패턴 자체를 벗어나는 별개 문제다(그 시점 재논의 대상).
	- 진짜 용도 1: 동작(상태 그래프) 자체가 완전히 다른 캐릭터(예: 4족 보행)를 위한 탈출구다.
	- 지금은 쓸 일이 없다.
	- 진짜 용도 2(이번에 실제로 씀): 상태/전환/파라미터를 정의하는 베이스 `Animator Controller`(로직) 하나를 모든 캐릭터가 공유하고, 각 캐릭터는 "이 상태엔 이 클립"이라는 얇은 매핑표(Override Controller)만 얹는다.
	- Boy도 처음부터 자기 전용 Override Controller를 가지며, 클립 하나가 잘못 골라졌다고 판단되면 베이스 컨트롤러(상태 그래프)는 안 건드리고 Override 애셋의 슬롯 하나만 바꾸면 된다.
- **Root Motion은 쓰지 않는다.**
	- 이동은 계속 `CharacterController` 기반 코드 구동(`PlayerMovement`)을 유지하고, 애니메이션은 제자리 동작만 재생한다.

---

### 3.2 애니메이션 클립 매핑 확정

> 일자: 2026-08-20
> 상태: 확정, 구현 착수

제공된 8개 클립 중 7개에 용도를 부여한다.

| 클립 | 매핑 | 근거 |
|---|---|---|
| `Idle` | 기본 대기 | 이름 그대로 |
| `Walking` / `Animation_Running` | 이동속도 기반 Idle/Walk/Run 블렌드 트리 | 5단계 '무기별 이동속도 페널티'(`DevPlan.md` §2.6)가 들어오면 실제 이동속도가 무기마다 달라지므로, 이 2단계 블렌드가 실사용된다 |
| `Left_Short_Hook_from_Guard` / `Right_Upper_Hook_from_Guard` | 맨손(`UnarmedFists`) 근접 공격, 교대로 트리거 | `MeleeWeapon`(맨손도 동일 클래스 인스턴스)의 공격 시 좌/우 번갈아 재생 |
| `Right_Hand_Sword_Slash` | 장착 근접무기 공격 | 이름 그대로 |
| `Run_and_Shoot` | 원거리 무기 장착 + 공격 홀드 중 포즈(이동/정지 공용) | 이동과 사격이 이미 한 클립에 합쳐져 있어, 매 발사마다 풀 클립을 트리거하는 대신 '공격 홀드 중엔 이 포즈'로 블렌드하는 쪽이 자연스럽다(근접 쿨다운 0.5초/원거리 0.4초로 둘 다 빨라서, 원거리에 매 발사마다 풀 클립을 재트리거하면 초당 2~3회 재생이 겹친다) |

- **`Alert` 클립은 사용하지 않는다.**
	- 용도를 추정할 근거가 약해 억지로 자리를 만들지 않기로 했다(2026-08-20 확인).
	- 필요해지면 나중에 재검토한다.
- **알려진 단순화 1**: `Run_and_Shoot`을 정지 상태 공격 홀드에도 재사용한다(전용 정지 사격 포즈 클립 없음).
	- 사용자가 동의했다(2026-08-20).
- **알려진 단순화 2**: `PlayerAim`이 조준 방향으로 캐릭터 전체를 회전시키는 구조(GDD 9.1/9.2 aim vector)라 이동 입력 방향과 캐릭터가 바라보는 방향이 다를 수 있는데, 이동 클립이 전진 방향 하나뿐이라 스트레이프/후퇴 중에도 같은 애니메이션을 속도 크기로만 재생한다.
	- 사용자가 동의했다(2026-08-20).
- 위 두 단순화가 실전에서 어색하면, §3.1의 `AnimatorOverrideController` 구조로 해당 클립만 즉시 교체할 수 있다(베이스 컨트롤러/코드 변경 불필요).

---

### 3.3 게임플레이 ↔ 애니메이션 연결부

> 후속 변경(2026-10-19): [일부 변경] 플레이어 전용 실행/모션·상체 고정 구성과 이벤트 수신은 [3.31](#331-범용-무기와-사람형-모션-통합)에서 공통 무기/모션·정지 전신/이동 하체 구성으로 이관했다. 당시 규칙·검증과 이관 범위 밖의 동작은 보존한다.

> 일자: 2026-08-20
> 상태: 확정, 구현 착수

- `PlayerMovement`/`PlayerWeaponController`는 애니메이션의 존재를 몰라야 한다(증강 배율 개편, §2.16에서 확립한 '핵심 시스템이 부가 시스템을 모르게 한다' 원칙과 동일선상).
- 신규 `PlayerAnimationController`: `PlayerMovement`의 정규화 속도, `PlayerWeaponController`의 이벤트를 구독해 `Animator` 파라미터로 변환만 하는 순수 브릿지다.
- `PlayerWeaponController`에 신규 이벤트 `AttackPerformed(WeaponCategory)`를 추가했다.
	- 기존 `WeaponEquipped`/`WeaponBecameUnarmed`/`WeaponDestroyed`뿐이라 '공격이 실제로 발생했다'는 신호가 없었다.
	- `Update()`에서 `TryAttack(...)`이 `true`를 반환할 때 발행한다.
- `PlayerMovement`에 정규화 속도 조회용 getter를 추가했다.
- 무기 소켓(`WeaponSocket`)을 `Animator.GetBoneTransform(HumanBodyBones.RightHand)`로 얻은 오른손 본에 재부착한다.
	- 스윙 애니메이션이 들어가면 고정 오프셋 소켓은 손과 따로 논다.
	- Humanoid 기반이라 향후 캐릭터도 같은 본 조회 로직이 그대로 통한다.

---

### 3.4 Meshy 루트 노드 이름 불일치 해결 + 애니메이션 파일 교체

> 일자: 2026-08-22
> 상태: 확정, 적용 완료

- **문제**: §3.2에서 매핑을 확정한 8개 클립을 실제로 Humanoid 아바타에 연결하려 하자 Copy From Other Avatar가 "Rig Error: ... Parent for 'Hips' differs ..."로 전부 실패.
	- 원인은 Meshy AI가 export 시 최상위 루트 오브젝트(Hips의 부모) 이름을 export 세션 문맥(씬 이름/클립 이름)에 따라 제각각 붙이는 습관이다.
	- 모델은 `Armature`, 애니메이션 클립들은 각자 클립명(`Idle`, `Running` 등)으로 나와 이름이 서로 안 맞았다.
- **기각된 해결책**: Unity 안에서 재임포트 시마다 루트 이름을 고정 문자열 `Root`로 강제로 맞추는 Editor 도구(`AssetPostprocessor` + 파일별 `.meta`의 `ModelImporter.userData` 마커로 게이팅하는 방식).
	- `Assets/Docs/Scratchpad.md`에서 여러 라운드에 걸쳐 설계까지 마쳤으나, 아래 대안이 나오면서 채택되지 않고 폐기됐다.
- **실제 채택한 해결책**: Meshy에서 애니메이션을 다시 받을 때 **"With Skin" export 옵션**을 켜서 받으니, 애니메이션 FBX가 모델과 동일한 스켈레톤/루트 계층(`Boy_Model(Clone)` → `Armature` → `Hips` ...)을 통째로 포함해서 나온다.
	- 루트 이름 문제 자체가 발생하지 않는다.
	- Editor 도구는 불필요해져 폐기했다.
	- 기존 8개 애니메이션 FBX(구 이름: `Alert`, `Animation_Running`, `Idle`, `Left_Short_Hook_from_Guard`, `Right_Hand_Sword_Slash`, `Right_Upper_Hook_from_Guard`, `Run_and_Shoot`, `Walking`)를 전부 삭제하고 "With Skin"으로 재다운로드했다.
	- 재다운로드한 파일명이 `Meshy_AI_Casual_T_Pose_Boy_biped_Animation_<이름>_withSkin.fbx` 형태로 길게 나와서, 접두어(`Meshy_AI_Casual_T_Pose_Boy_biped_Animation_`)와 접미어(`_withSkin`)를 잘라내 기존 짧은 이름으로 되돌렸다(`Idle`은 `Idle_02`로 소폭 변경, 나머지는 동일하다).
		- §3.2 표의 클립명은 이 새 파일명 기준으로 다시 읽으면 된다.
	- **FBX 내부 애니메이션 클립(Take) 이름은 그대로 둔다.**
		- 파일명만 정리하고 내부 클립 이름 정리는 하지 않기로 했다.
		- 지금 당장 문제 되지 않는다고 판단했다.
- **부수 효과(`PlayerBaseController.controller` 참조 깨짐과 재연결)**: 파일을 통째로 삭제 후 재다운로드했기 때문에 각 FBX의 Unity GUID가 전부 바뀌었고, `PlayerBaseController`의 6개 `AnimatorState`(`Locomotion` 블렌드 트리 3칸 포함)가 구 GUID를 참조하고 있어 전부 끊어졌다.
	- 다행히 각 참조의 `fileID`(`-623603649343465320`)는 파일이 바뀌어도 동일하게 유지됐다.
		- 이 값은 "모델 안에 클립이 하나뿐일 때 Unity가 그 클립에 붙이는 고정 식별자"라 클립의 실제 이름과 무관하기 때문으로 보인다.
		- 그래서 `guid`만 새 파일 것으로 바꾸는 텍스트 치환만으로 복구했다(그래프 구조/코드 변경 없음).
	- `Alert.fbx`는 애초에(§3.2 결정대로) 컨트롤러 어디에도 연결돼 있지 않아 이번 파일 교체로 깨진 것도 없다.
	- **검증 결과**: 자동 텍스트 치환은 실패했다.
		- 새 FBX들의 내부 `fileID`가 예전 규칙을 따르지 않았기 때문이다.
		- 최종적으로 Unity 에디터에서 6개 상태의 Motion 필드를 각각 수동으로 재드래그해서 해결했다.
		- Play 테스트로 애니메이션 재생 자체는 정상 확인했다.

---

### 3.5 이동 방향과 조준(애니메이션) 방향 불일치: 2D Blend Tree 검토 후 기각

> 일자: 2026-08-22
> 상태: 확정, 기각 (§3.2 단순화 유지)

- **문제**: 몸 전체가 항상 조준 방향으로 회전하는데(§3.1, GDD 9.1/9.2 aim vector 설계) 로코모션 클립이 전진 방향 하나뿐이라, 스트레이프/후퇴 중에도 같은 "앞으로 뛰는" 애니메이션만 재생된다.
	- §3.2 "알려진 단순화 2"에서 이미 합의했던 부분이 Play 테스트에서 다시 도마 위에 오른 것이지 버그가 아니다.
- **기각된 대안 (몸 방향 자체를 바꾸는 안)**: 몸 전체를 조준 방향 대신 이동 방향으로 향하게 바꾸기.
	- 원거리 무기가 360도 자유 조준이라, 조준 대상이 이동 방향 기준 뒤쪽에 있으면 상체가 비정상적인 각도로 꺾여야 해서 기각했다.
- **검토한 해결안 (채택 안 함)**: 몸 방향(=조준 방향)은 유지한 채, "몸 기준 상대 이동 방향"(전진/후진/좌우 스트레이프)을 2번째 축으로 하는 2D Blend Tree를 도입하는 안이다.
	- 신규 클립 필요(후진 1개 + 스트레이프 1개, Unity Humanoid `Mirror` 옵션으로 좌우 하나 재사용 가능해 보이나 미검증 추정), Meshy에서 추가로 받아야 한다.
- **기각 사유**: 사용자 결정이다.
	- 신규 클립 확보 비용 대비 실익이 낮다고 판단, 현재 단일 전진 클립 단순화를 그대로 유지한다.
- **재검토 트리거**: 없다.
	- 폐기가 확정됐으며 `Backlog.md`에도 올리지 않는다(재검토 예정 작업이 아니라 완전히 접은 안이므로, `Backlog.md` §0 작성 규칙대로 이 문서에만 판단과 사유를 남긴다).

---

### 3.6 상하체 애니메이션 분리 (Upper Body 레이어 + Avatar Mask) 구현

> 후속 변경(2026-10-19): [일부 변경] 플레이어 전용 실행/모션·상체 고정 구성과 이벤트 수신은 [3.31](#331-범용-무기와-사람형-모션-통합)에서 공통 무기/모션·정지 전신/이동 하체 구성으로 이관했다. 당시 규칙·검증과 이관 범위 밖의 동작은 보존한다.

> 일자: 2026-08-22
> 상태: 스크립트 작성 완료, Unity 실행 + Play 테스트 검증 대기

- **목표**: HANDOFF §3-2. 이동 중 공격/사격 시에도 로코모션(다리)이 유지된 채 공격 애니메이션(팔/상체)만 그 위에 덧씌워지게 한다.
- 설계 자체는 이미 이견 없이 확정돼 있던 부분(§3.2, `Scratchpad.md` 논의)을 그대로 따름: Avatar Mask(상체만) + `Upper Body` 레이어(Override 블렌딩) 신설, 공격 4상태(`Attack_MeleeWeapon`/`Attack_UnarmedLeft`/`Attack_UnarmedRight`/`RangedFire`)를 이 레이어로 이관, `Base Layer`엔 `Locomotion`만 남김.
- **구현 방식**: `Stage1SceneBuilder.BuildPlayerAnimatorController`를 고치지 않고 별도 스크립트로 분리했다.
	- 그 함수는 컨트롤러 파일이 이미 있으면 그냥 반환하고 끝나는 가드가 걸려있어(기존 자산을 실수로 덮어쓰지 않기 위함) 이후 변경 사항을 전혀 모르고, `§3.4` 파일명 정리 이후로도 갱신 안 된 옛 fbx 경로(`Idle.fbx`, `Animation_Running.fbx` 등)를 그대로 참조하고 있어 이미 그 자체로 stale하다.
	- 이번 참에 같이 고치는 건 범위 밖으로 보고 넘김(`Backlog.md` #8 참고).
	- 대신 `Assets/Scripts/Editor/Animation/PlayerUpperBodyLayerSetup.cs`(메뉴: `Weapon Master/Stage 4/Setup Upper Body Animation Layer`)가 기존 `PlayerBaseController.controller` 자산을 그 자리에서 수정한다.
- Upper Body 레이어의 기본 상태(공격 중이 아닐 때)는 Base Layer의 `Locomotion`과 **같은 `LocomotionBlend` 블렌드 트리 에셋을 그대로 재참조**한다.
	- 팔도 이동에 맞춰 자연스럽게 움직이다가, 공격 트리거 시에만 Override로 팔/상체가 공격 클립으로 바뀐다.
	- Exit Time 이후 다시 이 상태로 복귀한다.
- **멱등성**: `Base Layer`에 공격 상태 4개가 이미 하나도 없으면(=이미 적용 완료) 아무 것도 하지 않고 종료.
	- `Upper Body` 레이어가 이미 있으면(직전 실행이 상태 이관 도중 실패한 경우 등) 지우고 새로 만들어 항상 깨끗한 상태에서 재구성한다.
	- 여러 번 실행해도 안전하게 설계.
- **검증 완료**: 사용자가 메뉴 실행 후 Play 테스트로 이동 중 공격 시 다리(Base)/팔(Upper) 동시 재생을 확인했다.

**동작 원리** (Animator 레이어/마스크가 처음이라면 이 부분부터 읽을 것)

- Animator Controller는 레이어를 여러 개 가질 수 있고, 각 레이어는 완전히 독립된 State Machine(상태 그래프)을 가진다.
	- 레이어는 포토샵 레이어처럼 위에서 아래로 합성된다: `Base Layer`가 맨 아래(항상 전신 기본값), `Upper Body`가 그 위.
	- 같은 이름의 상태(`Locomotion`, `Attack_MeleeWeapon` 등)가 두 레이어에 각각 별도 객체로 존재하는 이유가 이것이다.
	- `AnimatorState`는 하나의 State Machine에만 속할 수 있다.
- **Avatar Mask**: 레이어별로 "이 레이어가 어떤 뼈에 대해 발언권을 가지는지" 정하는 필터다.
	- `UpperBodyMask.mask`는 `Body`(척추)/`Head`/`LeftArm`·`RightArm`/`LeftFingers`·`RightFingers`만 켜고, 다리(`LeftLeg`/`RightLeg`)·`Root`·각종 IK는 끈다.
	- 그래서 `Upper Body` 레이어는 다리에 대해 아예 관여하지 않고, 다리는 항상 `Base Layer`가 결정한다.
- **Blending Mode = Override**: 마스크로 켜진 부위(팔/상체)에 한해, 이 레이어가 지금 재생 중인 애니메이션이 아래 레이어 값과 섞이지 않고 통째로 덮어쓴다(반대인 Additive는 "더하는" 방식이라 이번엔 안 씀).
- **파라미터는 컨트롤러 전체 공유, 전환 규칙은 레이어별로 별도**: `AttackTrigger` 등 파라미터는 특정 레이어 소속이 아니라 컨트롤러 전체의 공용 값이다.
	- 하지만 그 값에 "어떻게 반응할지"(Any State 전환)는 레이어마다 따로 걸어야 한다.
	- 그래서 스크립트가 `Base Layer`에 있던 공격 진입용 Any State 전환(조건/타이밍)을 그대로 복사해 `Upper Body`에도 심고, `Base Layer` 쪽 전환은 제거했다.
	- 같은 트리거 하나에 두 레이어가 각자 독립적으로 반응한다.
- **Upper Body 레이어의 기본 상태가 왜 `Locomotion`인가**: Override 레이어는 "지금 재생 중인 것"이 곧 최종 결과다.
	- 기본 상태가 비어있으면 평소(공격 안 할 때) 팔이 다리 움직임과 안 맞게 고정돼 보인다.
	- 그래서 `Base Layer`와 **같은 `LocomotionBlend` 에셋을 그대로 재참조**해서, 평소엔 두 레이어가 사실상 같은 걸 재생하고(육안으론 안 보이는 상태), `AttackTrigger` 발동 시에만 `Upper Body` 레이어만 공격 클립으로 갈아탄다.
- **실행 흐름 예시(근접 공격)**: `AttackTrigger` 발동 → `Base Layer`는 이제 이 트리거에 반응하는 전환이 없어 `Locomotion` 유지(다리 계속 뜀) → `Upper Body`는 조건에 맞는 Any State 전환으로 `Attack_MeleeWeapon`로 전환 → 합성 결과: 팔/상체는 Upper 레이어의 공격 포즈, 다리는 Base 레이어의 달리는 포즈 → Exit Time(0.9) 이후 Upper 레이어가 다시 자기 `Locomotion`(팔 흔들기)으로 복귀.

---

### 3.7 근접 히트 판정 타이밍: Animation Event 채택

> 후속 변경(2026-10-19): [일부 변경] 플레이어 전용 실행/모션·상체 고정 구성과 이벤트 수신은 [3.31](#331-범용-무기와-사람형-모션-통합)에서 공통 무기/모션·정지 전신/이동 하체 구성으로 이관했다. 당시 규칙·검증과 이관 범위 밖의 동작은 보존한다.

> 일자: 2026-08-22
> 상태: 확정, 구현 착수

- **HANDOFF §3-1 결정**: Animation Event 방식을 채택한다.
	- 스윙 클립(`Right_Hand_Sword_Slash`, 펀치 클립들)의 실제 접촉 프레임에 이벤트를 심어 그 시점에 `MeleeWeapon`의 판정 함수를 호출한다.
	- 코드 딜레이 타이머 대안은 기각했다.
- 맨주먹 접촉 판정(§2.5, HANDOFF 3-4)은 `UnarmedFists`가 이미 `MeleeWeapon`을 그대로 쓰고 있어 이 결정이 자동 적용된다.
	- 별도 작업 불필요.
- **착수 순서**: 장착 근접무기 스윙 클립(`Right_Hand_Sword_Slash`)에 먼저 이벤트를 건다.
	- 펀치 클립은 §3.8 방향에 따라 재다운로드될 예정이라, 재다운로드 이후에 이벤트를 심는다.

**추가 결정(2026-08-30): 판정 방식은 순간 판정(펄스), 상시 콜라이더는 보류**

- 위 결정은 "판정 시점"만 다뤘고, "판정을 어떻게 할지"(한 순간 범위 검사 vs 손/무기에 상시 트리거 콜라이더를 붙여 구간 동안 겹침을 감지)는 별도로 열려 있었다.
- **잠정 채택**: 한 순간 범위 검사(`OverlapSphereNonAlloc`, 기존 방식 그대로) 쪽으로 간다.
	- 스윙이 크거나 빠른 공격은 이벤트를 클립당 1개로 제한하지 않고 여러 개(예: 스윙 60%/90% 지점) 심어 펄스를 여러 번 쏘는 방식으로 대응한다. 이벤트 개수를 클립마다 자유롭게 두면 이 대응이 별도 코드 없이 자연히 따라온다.
	- 상시 트리거 콜라이더(손/무기 본에 물리 콜라이더를 붙이고 구간 동안 `OnTriggerEnter`로 잡는 방식)는 채택하지 않는다.
- **판단 근거**: 이 프로젝트 스케일(적 이동속도, 스윙 길이, 동시 타격 대상 수)에서 두 방식의 런타임 비용 차이는 무시할 수준이라 성능이 결정 기준은 아니었다.
	- 실제 기준은 "플레이 테스트로 실제 느낌을 확인하는 데 드는 구현 비용"이다. 순간 판정은 기존 판정 함수를 이벤트로 옮기기만 하면 되고, 상시 콜라이더는 무기별 콜라이더 형상/부착 위치 설계, 스윙 1회당 중복 히트 방지, `HitRadius`의 의미 자체(구 반경 → 콜라이더 형상) 재정의까지 필요해 플레이 가능한 상태에 도달하는 비용이 훨씬 크다.
	- 싸게 먼저 검증 가능한 가설부터 시도한다는 원칙(이 문서 다른 절에서도 반복되는 "단순하게 먼저, 필요해지면 올린다" 기조)에 따라 순간 판정을 먼저 채택했다.
- **재검토 트리거**: 순간 판정으로 구현 후 플레이 테스트에서 다음 중 하나가 실제로 체감되면 상시 콜라이더 방식으로 전환을 검토한다.
	- 빠르게 스치는 적을 판정 프레임에 놓치는 문제가 눈에 띄게 발생.
	- §3.8에서 이미 제기된 시각(주먹 위치)-판정(hitRadius) 괴리가 다중 펄스로도 해소되지 않음.
- 경위 원본은 `Scratchpad.md`(휘발성, 이번 라운드 종료 후 삭제될 수 있음)에 있었으며 이 항목으로 이관 완료.

---

### 3.8 펀치 사거리: IK 스트레치 기각, hitRadius/lunge 조합으로 방향 확정

> 일자: 2026-08-22
> 상태: 방향 확정, 구체 수치는 신규 펀치 클립 확보 후 튜닝

- **HANDOFF §3-2 결정**: 팔 스트레치(IK)는 쓰지 않는다.
	- 사용자 확정이다.
- 신규 펀치 클립은 사용자가 직접 Meshy에서 찾아 재다운로드할 예정이다.
- 캐릭터 팔이 짧아 펀치 애니메이션의 시각적 리치가 매우 짧다는 문제가 논의 중 제기됐다.
	- hitRadius만 크게 잡으면 눈에 보이는 주먹 위치보다 훨씬 먼 적이 맞는 게 노골적으로 드러나 이질감이 커질 수 있다.
	- lunge(공격 시 짧은 전진)를 병행해 실제 캐릭터-적 거리를 좁히는 쪽이 hitRadius 단독 확대보다 시각-판정 괴리를 줄이는 데 유리하다고 판단한다.
- 구체적인 hitRadius 값/lunge 거리는 신규 펀치 클립 확보 후, 실제 팔 길이를 보면서 튜닝하기로 한다.

---

### 3.9 Meshy 루트 노드 이름 불일치 재발: Blender 헤드리스 정규화 도구 도입

> 일자: 2026-08-25
> 상태: 확정, 구현 완료(Blender 실제 실행 검증 대기)

- **배경**: §3.4에서 "With Skin" export로 루트 노드 이름 불일치 문제를 해결했다고 판단했으나, 이후 일부 케이스는 With Skin으로 받아도 같은 문제가 재발했다.
- **결정**: §3.4에서 기각했던 "Editor 도구로 루트 이름 강제 정규화" 방향을 다시 채택하되, Unity 내부(`AssetPostprocessor` + `.meta` 마커)가 아니라 Blender를 헤드리스로 실행해 FBX 자체를 재export하는 외부 파이프라인으로 구현했다.
	- §3.4의 기각 사유는 "AssetPostprocessor + meta 마커" 방식 특유의 복잡함이었고, 이번 Blender 외부 파이프라인은 별도로 새로 검토된 방식이라 같은 기각 사유가 적용되지 않는다.
- **출력 방식**: 별도 출력 폴더 없이 원본 FBX를 그 자리에서 덮어쓴다.
	- 정규화와 재import를 한 단계로 줄이기 위한 선택이다.
	- 실행 전 확인 다이얼로그를 최소한의 안전장치로 둔다.
	- 잘못 실행해도 git으로 복구할 수 있다는 전제다.
- **미리보기(dry-run) 미도입**: 탐지 로직이 충분히 단순하고 견고하다고 보고, 바로 실행한 뒤 로그로 결과를 확인하는 방식을 채택했다.
- **입력**: 대상은 폴더가 아니라 개별 파일의 명시적 목록으로 관리한다.
	- 파일 다이얼로그로 FBX를 한 개씩 추가하거나(`Add File`), Unity Project 창에서 선택된 FBX들로 목록 전체를 채운다(`Fill From Project Selection`).
	- `Fill From Project Selection`은 선택 개수와 무관하게 선택된 파일 전부를 그대로 대상으로 삼는다.
	- 폴더 단위 입력이나 폴더 스캔은 두지 않는다. 여러 파일을 한꺼번에 다룰 때도 공통 폴더로 묶지 않고 개별 파일 목록 그대로 처리한다.
- **탐지 로직**: 원안에 있던 `KNOWN_ROOT_ALIASES`(알려진 이름 목록) 방식은 폐기했다.
	- 새 별명이 나올 때마다 스크립트를 수동으로 고쳐야 하는 유지보수 부담이 있었기 때문이다.
	- 대신 파일 안에서 타입이 Armature인 오브젝트를 구조적으로 찾아 그 이름을 정규화한다.
	- Unity 에러(`Parent for 'Hips' differs`)가 실제로 가리키는 대상은 'Hips 본의 부모 트랜스폼'이다.
	- 이 부모 트랜스폼은 사실상 항상 Armature 오브젝트이므로, 이름이 뭐든 타입만으로 정확히 찾을 수 있다.
	- 파일에 Armature 오브젝트가 0개거나 2개 이상이면(비정상 케이스) 그 파일만 스킵하고 로그에 경고를 남긴 뒤 나머지 파일은 계속 처리한다.
- **파일 위치**: Blender 헤드리스 스크립트는 `Assets/Scripts/Editor/BlenderScripts/fbx_root_normalizer.py`, Unity `EditorWindow`는 `Assets/Scripts/Editor/Animation/FbxRootNormalizerWindow.cs`다.
	- 메뉴 경로는 `Tools/FBX Root Normalizer`다(2026-08-30, Editor 도구 메뉴를 `Weapon Master/Stage N/...`에서 `Tools/...`로 평탄화하며 변경됨).
	- 다른 Stage 4 Editor 도구(`PlayerUpperBodyLayerSetup.cs`)와 동일하게 `WeaponMaster.EditorTools` 네임스페이스를 쓴다.
- **대상 파일 리스트**: 현재 관리 중인 대상 목록을 창에 `Target Files (N)`으로 바로 표시한다.
	- 실행 전에 무엇이 처리될지 눈으로 바로 확인할 수 있다.
	- 실행 확인 다이얼로그에도 파일 개수를 같이 표시한다.
- **기본값 Armature**: `canonicalName` 기본값을 `"Root"`에서 `"Armature"`로 바꿨다.
	- 이미 정상 동작하는 `Boy_Model.fbx`의 루트 이름이 원래부터 "Armature"라서, 이 값을 기준으로 맞추면 그 파일은 건드릴 필요가 없어진다.
	- 폴더 스캔을 걷어내고 대상을 개별 파일 목록으로 바꾸면서(위 "입력" 참고) `fbx_root_normalizer.py`의 CLI 인자 순서도 `<rename to> <file1> [file2] ...`로 바뀌었고, `canonicalName`은 더 이상 스크립트 자체의 폴백 기본값이 아니라 Unity 쪽 UI 기본값(`"Armature"`)에서 항상 전달받는 필수 인자다.
- **재검토 트리거**: With Skin으로도 안 잡히는 케이스의 근본 원인이 나중에 밝혀지면(예: 특정 Meshy export 설정 문제), 그쪽을 고치는 게 더 나을 수 있다.
	- 이 도구는 그 전까지의 보완책이다.

---

### 3.10 `PlayerInputReader` NRE 해결 + 입력 액션 조회를 Generate C# Class로 전환

> 일자: 2026-08-30
> 상태: 확정, 구현 완료(Unity 에디터 재임포트로 `GameInputActions.cs` 생성 확인 필요)

- **배경**: `PlayerInputReader.Update()`에서 `AimPoint` 액션 조회 실패로 인한 NRE가 에디터 재시작마다 반복 발생했다. 원인은 `AimPoint`/`DropWeapon` 액션을 코드로 주입하던 `PlayerInputActionsSetup.cs`의 저장 방식(`EditorUtility.SetDirty`+`AssetDatabase.SaveAssets()`)이 ScriptedImporter 기반 애셋(`.inputactions`)에는 반영되지 않아, 액션이 디스크에 한 번도 저장되지 않았기 때문이었다. 조사 경위 전체는 `Assets/Docs/Troubleshooting/02-PlayerInput-AimPoint-NRE.md` 참고.
- **버그 해결**: `AimPoint`(Vector2, `<Mouse>/position`)와 `DropWeapon`(Button, `<Keyboard>/q`)을 Input Actions 에디터 창에서 직접 추가하고 저장. `PlayerInputActionsSetup.cs`는 삭제하고, `Stage1SceneBuilder.BuildScene()`의 호출부도 제거했다.
- **개선(같은 라운드에 함께 적용, 사용자 결정)**: `Assets/InputSystem_Actions.inputactions`의 임포터 설정에서 `Generate C# Class`를 켜서, `PlayerInputReader`가 문자열 기반 `FindAction("이름")` 대신 자동 생성된 타입 세이프 클래스를 쓰도록 전환했다.
	- 생성 위치: `Assets/Scripts/Input/GameInputActions.cs`, 네임스페이스 `WeaponMaster.Input`, 클래스명 `GameInputActions`.
	- `Player`뿐 아니라 `UI` 액션맵도 이 클래스 하나에 같이 노출되므로, 특정 도메인(`Player`)이 아닌 전용 `Input` 폴더/네임스페이스로 분리했다. 입력 관련 클래스는 이 폴더에서 관리한다.
	- `PlayerInputReader.Awake()`가 `new GameInputActions()`로 인스턴스를 만들고 `_actions.Player.Move` 등 프로퍼티로 액션을 캐싱하는 방식으로 바뀌었다. `[SerializeField] private InputActionAsset inputActions` 필드는 제거됐고, `OnDestroy()`에서 `_actions?.Dispose()`를 호출한다.
	- `Stage1SceneBuilder.BuildPlayer()`의 `SetField(inputReader, "inputActions", inputActions)` 호출과 `inputActions` 매개변수도 함께 제거했다(더 이상 그 필드가 없으므로). `BuildEventSystem`/`BuildAugmentSelectionUI`가 쓰는 `InputActionAsset inputActions` 지역 변수 자체는 UI 내비게이션(`InputSystemUIInputModule`)용으로 별개 용도라 그대로 남아 있다.
	- 이 전환으로 액션 이름 오타/누락이 런타임 null이 아니라 컴파일 에러로 잡히게 됐다 — 이번 NRE와 같은 부류의 버그가 구조적으로 재발할 수 없다.

---

### 3.11 무기 획득 방식: 자동 픽업 폐기, 상호작용 버튼으로 통합

> 일자: 2026-08-30
> 상태: 확정, 구현 완료(Play 테스트 검증 대기)

- **배경**: 무기 위를 밟으면 자동 장착되는 기존 동작(`GDD.md` §5.4 구버전)이 사용자가 원래 의도한 "장착도 수동으로" 방향과 어긋난다는 게 뒤늦게 확인됐다. 근접 히트 판정 애니메이션 이관 작업(§3.7) 설계 도중, "스윙 도중 실수로 무기 위를 지나가 자동 장착되면서 이미 커밋한 판정이 무산되는" 문제를 분석하다 드러났다.
- **결정**: 드랍 전용이던 버튼(`DropWeapon` 액션, Q)을 장착/교체/드랍 겸용 상호작용 버튼으로 확장한다. 새 입력 액션이나 새 키 바인딩은 추가하지 않는다.
	- 손 무기 있음 + 바닥 무기 있음 → 교체.
	- 손 무기 없음 + 바닥 무기 있음 → 장착.
	- 손 무기 있음 + 바닥 무기 없음 → 드랍.
	- 손 무기 없음 + 바닥 무기 없음 → 무시.
- **구현**: 앞의 세 경우 중 "교체"와 "장착"은 이미 있던 `PlayerWeaponController.Equip()` 하나로 처리된다. `Equip()` 내부의 `DropCurrentWeapon()`이 "현재 무기 없음"을 이미 안전하게 처리하고 있어 분기를 나눌 필요가 없었다. "드랍"은 기존 `DropCurrentWeapon()` 그대로, "무시"는 두 조건이 모두 거짓이라 별도 코드 없이 자연히 처리된다.
	- `WeaponPickup.OnTriggerEnter`가 더 이상 `Equip()`을 직접 호출하지 않고, `PlayerWeaponController.NotifyPickupInRange(this)`로 "범위 진입"만 알린다. 신규 `OnTriggerExit`가 대칭으로 `NotifyPickupOutOfRange(this)`를 알린다.
	- `PlayerWeaponController`가 범위 안의 바닥 무기를 `_nearbyPickup` 하나로 추적하고, 상호작용 버튼 입력(`HandleInteract()`)에서 위 표를 그대로 코드로 옮겼다.
	- 기존 `repickupGuardTime` 가드(방금 버린 무기를 그 자리에서 바로 다시 줍는 것 방지)는 그대로 유지되며 새 구조에서도 동일하게 동작한다 - 가드 시간 동안은 `NotifyPickupInRange` 자체가 안 불린다.
	- `PlayerInputReader`: `DropPerformed`/`OnDrop` 이벤트·메서드명을 `InteractPerformed`/`OnInteract`로 변경(순수 C# 이름 변경). 언더라잉 Input System 액션(`DropWeapon`, Q)은 그대로 재사용해 `.inputactions` 에셋이나 `GameInputActions.cs` 재생성이 필요 없었다.
- **문서 동기화**: `GDD.md`(§5.4/§9.2, v0.30), `Systems.md`(v0.15), `CLAUDE.md`의 "Core design constraints" 목록도 이번 라운드에 함께 갱신했다. `DevPlan.md`의 1단계 범위 설명(당시 기준 "픽업 자동")은 특정 시점의 스테이지 범위 기록이라 그대로 두었다(`DevPlan.md`는 "언제 만들었는지"를 다루고, 현재 동작의 기준은 `GDD.md`라는 기존 문서 역할 분담을 따름).

---

### 3.12 근접 히트 판정 Animation Event 이관 + 공격속도-재생속도 동기화: 구현 완료

> 후속 변경(2026-10-19): [일부 변경] 플레이어 전용 실행/모션·상체 고정 구성과 이벤트 수신은 [3.31](#331-범용-무기와-사람형-모션-통합)에서 공통 무기/모션·정지 전신/이동 하체 구성으로 이관했다. 당시 규칙·검증과 이관 범위 밖의 동작은 보존한다.

> 일자: 2026-08-30
> 상태: 구현 완료, Unity 에디터 수작업(Animation Event 삽입, Editor 스크립트 재실행, 클립 필드 연결) + Play 테스트 검증 대기

- **배경**: §3.7에서 "Animation Event 채택"을 결정만 해두고 실제 구현은 안 된 상태였다. 이번 라운드에 실제 구현까지 끝냈고, 그 과정에서 파생된 결정 3건(캔슬 방식, 판정 파라미터 재샘플링, 공격속도-재생속도 동기화)도 함께 확정/구현했다. 경위 전체는 `Scratchpad.md`(휘발성, 조만간 다음 라운드 내용으로 교체될 수 있음) §1~§5에 있었으며 이 항목으로 이관 완료.

**① 판정 시점 이관 (§1)**

- `IWeapon`에 `ResolveAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)` 추가. `WeaponBase.TryAttack`은 이제 쿨다운 게이트만 통과시키고 `protected virtual void OnAttackArmed(...)`를 호출한다 - 기본 구현(원거리)은 그 자리에서 바로 `PerformAttack`, `MeleeWeapon`은 재정의해서 실제 판정을 `ResolveAttack` 호출 시점(스윙 클립의 접촉 프레임 Animation Event)까지 미룬다.
- 원점/조준 방향은 스윙 시작 시점에 캐시하지 않고 `ResolveAttack` 호출 시점(그 순간의 `weaponSocket.position`/`aim.AimDirection`)을 그대로 받는다 - 이동/회전이 공격 중에도 자유로워서(§3.6), 캐시하면 판정 시점에 시각-판정 괴리가 생기기 때문(§3.8과 같은 종류의 문제).
- `PlayerWeaponController.ResolveMeleeHit()`이 Animation Event의 호출 대상 - `Animator`와 같은 GameObject에 있어 별도 중계 컴포넌트 없이 직접 받을 수 있었다.
- **스윙 도중 무기 교체 시**: 판정을 조용히 무산시키는 대신, `PlayerAnimationController`가 `WeaponEquipped`/`WeaponBecameUnarmed` 발생 시 재생 중이던 공격 애니메이션 자체를 `animator.Play("Locomotion", upperBodyLayer, 0f)`로 강제 캔슬한다(단, `GetCurrentAnimatorStateInfo(layer).IsTag("Attack")`일 때만 - 평소 무기 교체까지 매번 캔슬하면 `Locomotion`이 불필요하게 리셋됨). 클립이 끊기므로 판정 이벤트도 자연히 발동하지 않는다. 자동 픽업이 이미 폐기된 상태(§3.11)라 이 케이스는 이제 "유저가 스윙 도중 의도적으로 상호작용 버튼을 누른" 경우로 한정된다.

**② 공격속도-재생속도 동기화 (§2)**

- 원래 §3.7에서 "쿨다운 ≥ 애니메이션 길이"를 저작 시점 불변 조건으로 지키자고 했으나, 공격속도 증강이 런타임에 배율을 올리면 이 전제가 다시 깨질 수 있어 근본 해결이 아니었다. 대신: `targetCooldown`(=`Cooldown / attackSpeedMultiplier`)이 클립 길이보다 짧아지면, `AnimatorState.speedParameterActive`/`speedParameter`("AttackSpeedMultiplier" float 파라미터)로 그 상태의 재생 속도만 올려 스윙이 항상 목표 쿨다운 안에 맞춰 끝나게 한다. 판정 Animation Event는 노멀라이즈드 타임 기준이라 재생 속도가 올라가도 비례해서 앞당겨진다.
- **`Animator.speed`(전역) 대신 `AnimatorState.speedParameter`를 쓴 이유**: 전역 배율은 `Base Layer`의 `Locomotion`(다리)까지 같이 빨라져 §3.6의 상하체 분리 취지를 깬다. `speedParameter`는 상태 하나만 골라 적용 가능하다.
- 적용 대상은 근접/맨손/원거리 4개 공격 상태 전부다. 근접/맨손은 `PlayerAnimationController.HandleAttackPerformed`에서 1회성으로 계산(트리거 걸기 직전), 원거리는 발사 1회당 트리거가 따로 없어(`IsFiring` 연속 블렌드) `Update()`에서 발사 홀드 중 매 프레임 재계산한다.
- **상한 클램프는 도입하지 않음**: 지금 공격속도를 올리는 채널이 증강 하나뿐이라 한 런에서 도달 가능한 배율 자체가 이미 제한적이다. 실제 플레이에서 시각적으로 읽기 힘든 속도가 관측되거나 공격속도 상승 채널이 늘어나면 재검토.
- `PlayerUpperBodyLayerSetup.cs` 구조 변경: 기존엔 "Base Layer에 공격 상태가 남아있으면 최초 이관, 없으면(이미 이관 완료) 즉시 종료"였는데, 이러면 이미 이관된 컨트롤러에 새로 필요한 태그(`Attack`)/`speedParameter` 설정을 추가할 방법이 없었다. "최초 이관"(`MigrateAttackStatesToUpperLayer`)과 "태그+파라미터 적용"(`ApplyAttackAnimationSettings`)을 분리해, 이관 여부와 무관하게 후자는 재실행 때마다 항상 적용되게 재구성했다.

**③ `WeaponBase`/`MeleeWeapon` 리팩토링 (구현 도중 사용자 리뷰로 파생)**

- `ResolveAttack`을 처음엔 `WeaponBase`가 `_hasPendingAttack` 필드 + `protected virtual bool ResolveImmediately` 스위치로 직접 구현했으나, 사용자가 코드 리뷰 중 "이 필드가 원거리한텐 왜 필요한지 모르겠다"고 지적 - 실제로 원거리는 `ResolveImmediately`가 항상 참이라 플래그가 같은 줄에서 true→false로 바로 소비돼 아무 의미가 없었다.
- 템플릿 메서드 패턴으로 교체: `WeaponBase.OnAttackArmed`(기본 구현 = 즉시 판정)를 `MeleeWeapon`이 재정의해 `_hasPendingAttack`을 자기 전용 상태로 갖는다. `RangedWeapon`은 코드 변경 없이 상속만으로 기존과 동일하게 동작(회귀 없음).
- 같은 리뷰에서 `_conditionDamageMultiplier`(스폰 시 굴린 `Condition`에서 100% 유도 가능한 캐시값, 캐싱 실익 없음)와 `WeaponConditionValueCount`(리플렉션으로 enum 값 개수를 구하던 필드)도 제거 대상으로 지목됨.
	- `WeaponConditionData`에 `RollRandom()` 신규 - 이미 있던 `DamageMultiplierByCondition.Length`로 리플렉션 없이 개수를 구한다. "컨디션 종류/배율/굴리는 방법"이 이 클래스 한 곳에 모임.
	- `WeaponBase.Awake()`는 `Condition = WeaponConditionData.RollRandom();`만 호출, `EffectiveDamage`는 `WeaponConditionData.GetDamageMultiplier(Condition)`을 그때그때 직접 호출.
	- `ResolvePendingAttack` → `ResolveAttack`으로도 개명(§3.7 결정에서 파생) - "Pending"이 근접(진짜 대기)엔 맞지만 원거리(즉시 소비)엔 안 맞는 이름이었다.
- 이 리팩토링 3건 전부 순수 정리(동작 변화 없음, `RangedWeapon`/`PlayerWeaponController` 등 호출부 영향 없음)로, 근본 설계 변경은 아니다.

**남은 것 (Unity 에디터 수작업, 코드로 대신 못 함)**

1. `Tools/Setup Upper Body Animation Layer` 메뉴 재실행 - `Attack` 태그/`AttackSpeedMultiplier` 파라미터를 실제 컨트롤러 에셋에 반영.
2. 씬의 Player `PlayerAnimationController`에 신규 클립 필드 4개(근접/좌펀치/우펀치/원거리) 인스펙터 연결.
3. `Right_Hand_Sword_Slash`/펀치 클립 2개에 접촉 프레임 Animation Event 삽입(`ResolveMeleeHit` 호출 대상).
4. Play 테스트 - 아직 한 번도 안 함. 이 항목의 "구현 완료" 상태는 코드 작성 완료를 뜻하며, 실제 동작 검증은 아직이다.

---

### 3.13 스크립트 디렉터리 역할 정리

> 일자: 2026-09-17
> 상태: 확정, 파일 이동 완료(Unity Editor 재임포트 확인 대기)

- **결정**: 스크립트는 함께 변경되는 책임 단위로 하위 폴더를 구성한다.
	- Weapons: Combat, Projectiles, Interaction, Data, Collection으로 분리했다.
	- UI: HUD, Augments, DamageNumbers로 분리했다.
	- Editor: 로스터 인스펙터를 RosterInspectors, 애니메이션 저작 도구를 Animation으로 분리했다.
	- Stage1SceneBuilder는 씬 조립의 단일 진입점이므로 Editor 루트에 유지했다.
- **네임스페이스**: 폴더 이동에 맞춰 세분화하지 않고 기존 네임스페이스를 유지했다.
	- 폴더는 탐색 경계이고, 현재는 하위 네임스페이스나 assembly definition 경계가 필요할 만큼 타입 충돌이나 컴파일 경계가 크지 않다.
- **이동 방식**: 각 .cs와 대응 .meta를 함께 이동했다.
	- 직렬화된 Unity 참조의 GUID를 보존하기 위한 조치다.
	- 문서에 적힌 이전 파일 경로도 새 위치로 갱신했다.

---

### 3.14 근접 타격 생명주기와 무기 그립 포인트 후속 구현

> 후속 변경(2026-10-19): [일부 변경] 플레이어 전용 실행/모션·상체 고정 구성과 이벤트 수신은 [3.31](#331-범용-무기와-사람형-모션-통합)에서 공통 무기/모션·정지 전신/이동 하체 구성으로 이관했다. 당시 규칙·검증과 이관 범위 밖의 동작은 보존한다.

> 일자: 2026-09-21
> 상태: 구현 완료, 사용자 Play 테스트 확인

- **근접 타격 생명주기**: `MeleeWeapon`은 `Idle`에서만 새 공격을 시작하고, 활성 타격 구간이 끝난 뒤에는 애니메이션 상태가 끝날 때까지 회복 상태를 유지한다.
	- 입력을 반복해도 진행 중인 근접 공격 애니메이션이 처음부터 다시 시작하지 않는다.
	- `MeleeAttackStateExitNotifier`가 근접 공격 상태의 종료를 `PlayerWeaponController.EndMeleeAttack()`으로 전달한다.
	- notifier는 최초 호출에서 `PlayerWeaponController` 참조를 얻어 이후 호출에 재사용한다.
- **맨손 판정**: 좌우 손 아래의 `FistHitbox` Trigger Collider를 사용한다.
	- 공격이 승인되면 좌우 주먹을 교대로 선택한다.
	- Animation Event `EnableMeleeHitbox`와 `DisableMeleeHitbox`는 현재 선택된 주먹 Collider만 켜고 끈다.
	- 각 Collider는 `MeleeWeapon`에 접촉을 전달하며, 공격 하나에서 같은 대상은 한 번만 피해를 받는다.
	- 주먹 Collider 크기와 위치로 판정 여유를 조정한다. 별도 lunge는 도입하지 않는다.
- **무기 그립 포인트**: 장착 가능한 무기 프리팹 루트의 직접 자식 `GripPoint`를 장착 기준으로 사용한다.
	- `PlayerWeaponController`는 장착 시 `GripPoint`의 월드 위치와 회전이 `WeaponSocket`에 일치하도록 무기 루트를 보정한다.
	- `GripPoint`가 없는 기존 프리팹은 기존 원점 정렬을 유지하고, 에디터 또는 개발 빌드에서만 경고를 남긴다.
	- `WeaponBase` Custom Inspector의 `그립 포인트 선택 / 생성` 버튼은 `GripPoint`를 선택하거나 새로 만든다.
- **보류**: 실제 무기 모델을 도입한 뒤 그립 회전을 손에 장착한 결과로 조정할 수 있는 미리보기 도구를 검토한다.

---

### 3.15 무기 리소스 프리팹 생성 메뉴

> 일자: 2026-09-25
> 상태: 구현 완료, Unity 에디터 검증 완료

실제 무기 모델을 적용할 때 공통 구조와 필수 참조를 매번 수동으로 구성하지 않도록, Project 창의 생성 메뉴로 무기 프리팹과 정의 에셋을 함께 만드는 흐름을 추가했다.

- **생성 메뉴**: `Assets/Create/Weapon Master/Melee Weapon Prefab`과 `Ranged Weapon Prefab`을 추가했다.
	- 프리팹 경로와 이름은 저장 대화상자에서 정한다. 기본 경로는 `Assets/Prefabs/Weapons`다.
	- `WeaponDefinition`은 기존 데이터 폴더 관례에 따라 `Assets/Data/Weapons/<프리팹 이름>.asset`으로 생성한다.
	- 프리팹 또는 정의 에셋의 같은 경로가 이미 존재하면 생성하지 않는다.
	- 프리팹 저장에 실패하면 방금 만든 정의 에셋을 제거한다.
- **프리팹 루트**: 루트에 `MeleeWeapon` 또는 `RangedWeapon`, `WeaponDurability`, `WeaponPickup`, Trigger `BoxCollider`, kinematic `Rigidbody`를 붙인다.
	- 근접무기의 `hitbox`에는 루트 `BoxCollider`를 연결한다.
	- 현재 필드 픽업 감지와 근접 타격 구간이 같은 루트 Collider를 쓰는 구조를 유지한다.
- **자식 구조**: 루트의 직접 자식으로 빈 `Model`과 `GripPoint`를 만든다.
	- 모델은 `Model` 아래에 배치하고 위치·회전·크기를 그곳에서 조정한다.
	- `GripPoint`는 `WeaponSocket` 정렬 기준이므로 손잡이의 위치와 축에 맞춘다.
- **기본 정의**: 식별 가능한 표시명과 최소 테스트 수치를 넣는다.
	- 공격력 1, 쿨다운 1초, 최대 내구도 1이다.
	- 근접무기는 사거리 1, 타격 반경 0.5다.
	- 원거리 무기는 투사체 속도 10, 총구 오프셋 0.5와 기존 공용 `Projectile` 프리팹 참조를 넣는다.
	- 이 값은 실제 밸런스가 아니며 저작자가 정의 Inspector에서 확정한다.
- **Inspector 안내**: `WeaponGripPointEditor`가 정의 누락, 원거리 투사체 누락, `Model` 누락을 경고한다.
	- 기존 `그립 포인트 선택 / 생성` 버튼은 유지한다.
- **제외 범위**: `WeaponRoster` 등록, 씬 `WeaponSpawner` 배치, 기존 플레이스홀더 교체, 손 장착 미리보기 도구는 자동화하지 않는다.
	- 미리보기는 실제 모델 저작에서 반복 검증 비용이 확인될 때만 `Backlog.md` §9 기준으로 재검토한다.
- **검증**: Unity 에디터 재컴파일은 오류·경고 없이 통과했다.
	- 메뉴 목록에 두 생성 항목이 등록된 것을 확인했다.
	- 에디터 내 임시 객체 검증으로 근접 루트의 필수 컴포넌트, `Model`, `GripPoint`, 정의 연결을 확인했다.
	- 에디터 내 임시 정의 에셋 검증으로 원거리 기본 수치와 공용 `Projectile` 참조를 확인한 뒤 에셋을 삭제했다.

---

### 3.16 무기 픽업·공격 콜라이더 분리와 손 장착 저작 씬

> 일자: 2026-09-25
> 상태: 구현 완료, Unity 에디터 구조 검증 완료, Play 테스트 대기

무기 하나가 바닥에서 손으로 이동하는 기존 생명주기를 유지하면서, 픽업 범위와 근접 공격 판정을 각각 조정할 수 있도록 분리했다.

- **픽업 판정**: 모든 무기 루트에 반경 0.75의 Trigger `SphereCollider`를 두고 `WeaponPickup.pickupCollider`에 명시적으로 연결했다.
	- 루트 스케일을 1로 통일해 모델 크기 조정이 픽업 범위를 바꾸지 않도록 했다.
	- 장착 시 픽업 Collider만 끄고, 드롭 시 다시 켠다.
- **근접 공격 판정**: 기존 `Model`에 Trigger `BoxCollider`와 `WeaponAttackHitbox`를 붙였다.
	- 공격 Collider는 기본적으로 꺼져 있으며 Animation Event가 지정한 타격 구간에만 활성화된다.
	- `WeaponAttackHitbox`가 접촉을 루트 `MeleeWeapon.ReportHit`에 전달한다. 루트 `MeleeWeapon`은 픽업 Collider의 Trigger 이벤트를 공격으로 처리하지 않는다.
	- 원거리 무기의 `Model`에는 공격 Collider를 두지 않는다.
- **프리팹 전환**: 블레이드, 블래스터, 신규 근접무기 프리팹을 같은 구조로 전환했다.
	- 블레이드와 블래스터는 기존 루트 배율을 `Model`로 옮겨 화면상 크기와 무기 정의 참조를 유지했다.
	- 기존 두 프리팹에 현재 없는 `GripPoint`는 임의로 추가하지 않았다. 장착 시 기존 원점 정렬 폴백이 적용된다.
	- 새 프리팹 생성 메뉴와 `Stage1SceneBuilder`의 무기 생성 경로도 같은 구조를 만든다. 전체 씬 빌더는 실행하지 않았다.
- **저작 도구**: `WeaponAuthoringScene`에 플레이어, 오른손 `WeaponSocket`, 바닥, 카메라와 조명을 배치했다.
	- Project 창에서 무기 프리팹을 선택하고 `Assets/Weapon Master/View in Authoring Scene`을 실행하면 선택 무기의 미리보기 인스턴스를 소켓에 장착한다.
	- 미리보기는 실제 장착과 같은 `GripPoint` 정렬 함수를 사용하며 플레이어·카메라는 교체하지 않는다.
	- 프리팹 편집 모드에서 저장되지 않은 사용자 수정을 건드리지 않도록 메뉴 실행을 거부한다.
- **검증**: Unity 에디터 재컴파일에서 오류와 경고가 없었고, 세 프리팹의 Collider 위치·참조·기본 활성 상태를 다시 열어 확인했다.
	- 기존 플레이스홀더 두 개의 렌더러 바운드는 전환 전후 동일했다.
	- 선택 무기의 미리보기 장착에서 `GripPoint`와 소켓의 위치·회전이 일치했다.
	- `GameScene` Play Mode에서 스폰된 무기 4개의 픽업 Collider가 활성화되고 근접 공격 Collider가 대기 상태에서 비활성화된 것을 확인했다. 콘솔 오류·경고는 없었다.
	- 실제 입력으로 픽업·공격·드롭·내구도 파괴까지 잇는 흐름은 아직 검증하지 않았다.

---

### 3.17 프리팹 중심 무기 데이터와 스폰 등록 통합

> 일자: 2026-09-26
> 상태: 구현 완료, Unity 에디터 재컴파일 완료, Play 테스트 대기

무기 하나를 저작할 때 프리팹과 `WeaponDefinition` SO를 따로 관리하고, `WeaponRoster`와 `WeaponSpawner`에도 각각 등록해야 했던 경로를 프리팹 중심으로 통합했다.

- **프리팹 내장 데이터**: `WeaponDefinition` SO를 제거하고 `[Serializable] WeaponData`를 `WeaponBase`에 내장했다.
	- `WeaponData`는 공통 데이터와 `MeleeWeaponData`, `RangedWeaponData`를 함께 소유한다.
	- 데이터는 별도 애셋이 아니라 각 무기 프리팹에 직렬화된다.
	- `WeaponBase` Custom Inspector는 실제 컴포넌트 타입에 맞는 전용 데이터 그룹만 표시한다.
	- 무기 카테고리는 직렬화하지 않고 `MeleeWeapon`과 `RangedWeapon`의 실제 타입에서 반환한다.
- **도감 식별자**: `WeaponData.id`를 저장 키로 사용한다.
	- 기존 SO 애셋 이름을 ID로 이전해 기존 `weapon_collection_<이름>` 저장 키와의 호환을 유지했다.
	- 정적 데이터는 읽기 전용 프로퍼티로 노출하고 런타임 컨디션과 현재 내구도는 기존 컴포넌트가 계속 관리한다.
- **필드 무기 로스터**: `WeaponRoster`는 `WeaponBase` 프리팹 목록을 보관한다.
	- 맨손은 플레이어 기본 공격이며 필드 무기가 아니므로 로스터에서 제외했다.
	- 블레이드, 신규 근접무기, 블래스터 프리팹을 등록했다.
	- `WeaponSpawner`는 개별 프리팹 배열 대신 `WeaponRoster` 하나를 참조한다.
- **저작 도구**: 새 무기 생성 메뉴는 프리팹과 내장 데이터를 한 번에 생성한다.
	- 로스터 인스펙터의 폴더 스캔은 `Assets/Prefabs/Weapons`의 무기 프리팹을 수집한다.
	- `Stage1SceneBuilder`도 정의 SO를 만들지 않고 프리팹 내장 데이터를 구성한다.
- **기존 데이터 이전**: 기존 네 개의 `WeaponDefinition` 값을 대응 프리팹과 플레이어 맨손 컴포넌트로 옮긴 뒤 SO 애셋과 클래스 파일을 제거했다.
	- 맨손은 GDD 5.6에 맞춰 컨디션 적용 대상에서 제외했다.
	- `GameScene`의 `WeaponSpawner`는 기존 `WeaponRoster` 애셋을 참조하도록 전환했다.
- **검증**: Unity 에디터 재컴파일에서 오류와 경고가 없었다.
	- 실제 Play Mode의 스폰, 도감 기록, 장착, 공격 흐름은 아직 검증하지 않았다.

---

### 3.18 근접 피해 소유자 탐색과 적 접촉 대상 제한

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-09-26
> 상태: 구현 완료, Unity 에디터 검증 완료, Play 테스트 대기

근접 공격이 개별 적을 찾지 못하고 적끼리 접촉 피해를 주던 문제를 함께 수정했다. 두 현상은 `EnemySpawner` 아래에 풀링된 적의 계층과 접촉 피해의 대상 제한 부재에서 각각 발생했다.

- **피해 소유자 탐색**: `DamageableLookup`이 충돌 Collider 또는 Transform에서 부모 방향으로 가장 가까운 `IDamageable`을 찾는다.
	- `Transform.root`는 관리용 부모인 `EnemySpawner`를 반환하므로 게임플레이 개체 식별자로 사용하지 않는다.
	- 부모 탐색은 충돌 시점의 짧은 계층에서만 실행된다.
	- 현재 규모에서는 Collider별 캐시나 별도 `Hurtbox` 컴포넌트보다 구조가 단순하고 비용이 충분히 작다.
- **근접 타격 대상**: `MeleeWeapon`의 Trigger 판정과 `OverlapSphere` 폴백이 찾은 피해 소유자를 기준으로 동작한다.
	- 공격자 자신의 피해 소유자는 제외한다.
	- 한 공격의 중복 타격 기록도 피해 소유자 기준으로 관리한다.
	- 같은 적의 여러 Collider는 한 번만 맞고, 같은 스포너 아래의 서로 다른 적은 각각 맞는다.
- **접촉 피해 대상**: `EnemyContactDamage`는 찾은 피해 소유자가 `Player` 태그일 때만 피해를 준다.
	- 플레이어 프리팹 루트에 기존 `Player` 태그가 있고 적은 `Untagged`이므로 추가 진영 시스템은 도입하지 않았다.
	- 적끼리의 접촉은 피해를 발생시키지 않는다.
- **진단 로그**: 근접 및 접촉 피해 로그가 피해 소유자 이름과 Unity 인스턴스 ID를 표시한다.
	- 같은 이름과 계층을 가진 풀 인스턴스를 로그에서 구분할 수 있다.
	- 로그는 Unity Editor와 Development Build에서만 출력한다.
- **검증**: Unity 에디터 재컴파일에서 오류와 경고가 없었다.
	- Editor 내 임시 `관리 부모 → 피해 대상 → 자식 Collider` 계층에서 개별 대상의 `HealthComponent`가 선택되는 것을 확인했다.
	- Editor 내 임시 접촉 피해 호출에서 `Player` 대상은 피해 8을 받고 `Untagged` 대상은 체력이 유지되는 것을 확인했다.
	- 실제 전투 입력으로 맨손·방망이 다중 타격, 적 간 무피해, 플레이어 접촉 피해, 내구도 소모를 잇는 Play 테스트는 남아 있다.

---

### 3.19 전투 역할 레이어와 맨손 자기 타격 차단

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-09-26
> 상태: 구현 완료, Unity 에디터 검증 완료, Play 테스트 대기

§3.18의 `Player` 태그 기반 접촉 피해 제한을 전투 역할 레이어 기반으로 교체했다. 같은 조사에서 확인된 맨손 자기 타격도 함께 수정했다.

- **전투 역할 레이어**: `PlayerBody`, `PlayerAttack`, `Enemy`를 각각 플레이어 피격부, 플레이어 공격 판정, 적 개체에 사용한다.
	- `PlayerBody ↔ PlayerAttack`과 `PlayerAttack ↔ PlayerAttack` 충돌 이벤트는 런타임 초기화에서 차단한다.
	- `Enemy ↔ Enemy` 물리 충돌은 유지한다.
	- `EnemyContactDamage`는 `PlayerBody` Collider에만 반응하므로 적끼리 접촉해도 피해를 주지 않는다.
- **태그의 범위**: 전투 피해 판정의 `Player` 태그 검사를 제거했다.
	- 기존 `Player` 태그는 `Stage1SceneBuilder` 등 비전투 용도가 남아 있어 프로젝트에서 제거하지 않았다.
	- 피해 컴포넌트 위치는 레이어가 아니라 §3.18의 부모 방향 `IDamageable` 탐색으로 계속 찾는다.
- **자동 지정**: 관련 컴포넌트가 자신의 GameObject에 전투 역할 레이어를 적용한다.
	- `PlayerWeaponController`, `FistHitbox`, `WeaponAttackHitbox`, `EnemyContactDamage`가 컴포넌트 추가 시와 실행 시 레이어를 지정한다.
	- 새 무기는 기존 제작 규칙대로 공격 Collider에 `WeaponAttackHitbox`를 붙이면 별도 수동 설정 없이 `PlayerAttack` 레이어가 적용된다.
	- 필수 레이어가 없으면 Editor와 Development Build에서 오류를 출력한다.
	- 프로젝트 전체 검사기는 현재 규모와 문제 범위에 비해 복잡하므로 도입하지 않았다.
- **맨손 자기 타격**: 공격자 피해 소유자 초기화를 공통 Hitbox 활성화 지점으로 옮겼다.
	- 장착 무기는 기존 경로에서 초기화됐지만 맨손의 `EnableExternalHitbox` 경로는 이를 건너뛰어 자기 자신 제외가 실패했다.
	- 주먹 Collider의 크기는 접촉 가능성을 높였을 뿐 직접 원인은 아니었다.
	- Trigger 판정과 `OverlapSphere` 폴백 모두 같은 공격자 소유자를 사용한다.
- **검증**: Unity 에디터 재컴파일은 오류와 경고 없이 통과했다.
	- `PlayerBody=8`, `PlayerAttack=9`, `Enemy=10`을 확인했다.
	- `PlayerBody ↔ PlayerAttack`과 `PlayerAttack ↔ PlayerAttack`은 차단되고 `Enemy ↔ Enemy`는 유지되는 것을 확인했다.
	- 실제 입력을 통한 맨손 자기 타격 방지와 접촉 피해 회귀 검증은 남아 있다.

---

### 3.20 원거리 무기 자세 미리보기와 GripPoint 역산 저작

> 일자: 2026-09-27
> 상태: 구현 완료, Unity 에디터 검증 완료, 사용자 저작 확인 대기

`Run_and_Shoot` 자세에서 무기를 먼저 배치한 뒤 그 결과로 `GripPoint`를 계산하는 저작 흐름을 추가했다.

- **기능 위치**: 기존 `WeaponGripPointEditor`의 저작 씬 전용 영역에 기능을 배치했다.
	- 특정 무기를 편집할 때만 필요한 기능이므로 새 상위 메뉴와 EditorWindow를 추가하지 않았다.
	- 저작 씬 밖에서는 기존 `저작 씬에서 WeaponSocket에 장착` 버튼만 표시한다.
- **애니메이션 미리보기**: 원거리 무기 프리뷰 Inspector에서 `Run_and_Shoot`을 재생하거나 원하는 시점을 직접 확인한다.
	- 재생, 일시 정지, 프레임 슬라이더, 정규화 시간 슬라이더, `기준 자세(T-Pose)` 복귀를 제공한다.
	- Play Mode에 진입하지 않고 `AnimationMode`로 클립을 샘플링한다.
	- 미리보기 시작 전 플레이어 계층의 자세를 보관하고 종료 시 복원한다.
	- 무기 프리뷰 계층은 복원 대상에서 제외해 미리보기 중 조정한 무기와 `GripPoint` 값을 유지한다.
- **GripPoint 역산**: 현재 무기 루트의 월드 배치와 `WeaponSocket`에서 `GripPoint`의 로컬 위치·회전을 계산한다.
	- 계산 직후 실제 장착 함수로 다시 정렬해 같은 배치가 재현되는지 확인한다.
	- 무기 루트 Scale은 `(1, 1, 1)`일 때만 계산할 수 있다.
	- 시각적 크기는 기존 규칙대로 `Model` 자식에서 조정한다.
- **프리팹 적용**: 별도 버튼이 계산된 `GripPoint`의 로컬 위치·회전만 원본 프리팹에 적용한다.
	- 저작 씬 프리뷰의 이름, 부모, 무기 루트 배치 등 다른 Override는 적용하지 않는다.
	- 계산과 프리팹 저장을 분리해 결과를 확인한 뒤 확정할 수 있다.
- **검증**: Unity 에디터 재컴파일은 오류와 경고 없이 통과했다.
	- `Run_and_Shoot` 클립의 길이 0.667초, 30 FPS, 0~20 프레임 탐색 범위를 확인했다.
	- 중간 프레임 샘플링으로 오른손 자세가 변경되고 미리보기 종료 후 위치 오차 약 `1.2e-7`, 회전 오차 `0도`로 기준 자세가 복원되는 것을 확인했다.
	- 미리보기 중 변경한 무기 로컬 위치·회전은 복귀 후 오차 없이 유지되는 것을 확인했다.
	- 임의의 무기·소켓 배치에서 역산한 GripPoint의 위치 오차 약 `6.3e-7`, 회전 오차 `0도`를 확인했다.
	- 임시 프리팹에서 GripPoint 위치·회전만 적용되고 저장 오차가 없는 것을 확인한 뒤 임시 에셋을 삭제했다.
	- 실제 K2 배치와 Inspector 조작의 시각적 확인은 사용자의 저작 작업으로 남아 있다.

---

### 3.21 범용 애니메이션 미리보기와 무기 저작 책임 분리

> 일자: 2026-09-27
> 상태: 구현 완료, Unity 에디터 검증 완료, 사용자 시각 확인 대기

§3.20의 `Run_and_Shoot` 전용 미리보기를 범용 EditorWindow로 옮기고, 무기 Inspector에는 GripPoint 저작 책임만 남겼다.

- **범용 미리보기 창**: `Tools/Weapon Master/Animation Preview`에서 씬의 Animator와 임의의 AnimationClip을 연결한다.
	- 대상 Animator 직접 지정, 현재 선택 캐릭터 사용, 씬에 Animator가 하나일 때의 자동 지정을 제공한다.
	- Project 창에서 AnimationClip을 선택하면 `Auto Preview` 설정에 따라 한 번의 선택으로 재생을 시작한다.
	- FBX에 재생 가능한 클립이 여러 개면 창의 팝업에서 대상 클립을 선택한다.
	- 재생, 일시 정지, 정지, 자세 복원, 프레임 슬라이더, 정규화 시간 슬라이더를 제공한다.
- **미리보기 모드**: Full Body와 Layered를 분리했다.
	- Full Body는 단일 클립을 전신에 적용한다.
	- Layered는 `AnimationLayerMixerPlayable`로 Base Clip과 Upper Body Clip을 합성하고 기존 `UpperBodyMask`를 적용한다.
	- 두 클립은 하나의 미리보기 시간으로 이동하며 각 클립 길이에 맞춰 반복한다.
	- Animator Controller의 전이와 파라미터 재현은 포함하지 않았다.
- **상태 보존**: 별도 미리보기 세션이 Playables 구성과 원래 자세 복원을 담당한다.
	- 미리보기 동안 Root Motion을 끄고 캐릭터 루트 위치·회전·크기를 고정한다.
	- 정지, 창 종료, 씬 전환, Domain Reload, Play Mode 진입 전에 캡처한 자세를 복원한다.
	- `WeaponBase` 계층은 복원 대상에서 제외해 미리보기 중 수정한 무기 Transform을 유지한다.
- **무기 도구 책임**: `WeaponAuthoringSceneTool`에서 클립 경로와 애니메이션 상태를 제거했다.
	- 도구는 저작 씬 생성·열기, WeaponSocket 탐색, 프리뷰 장착, GripPoint 역산과 프리팹 적용만 담당한다.
	- `WeaponGripPointEditor`에서 애니메이션 UI를 제거하고 저작 버튼과 경고의 사용자 노출 텍스트를 영어로 전환했다.
- **Editor UI 언어**: 루트 `AGENTS.md`에 Unity Editor 사용자 노출 텍스트를 영어로 작성하는 장기 규칙을 추가했다.
	- 코드 주석은 기존 규칙대로 한국어를 유지한다.
	- 기존 프로젝트 전체 Editor UI의 일괄 변환은 이번 범위에 포함하지 않았다.
- **검증**: Unity 에디터 재컴파일은 오류와 경고 없이 통과했다.
	- Project 창의 단일 AnimationClip 선택이 창의 클립 필드에 지정되고 `Auto Preview`를 시작하는 것을 확인했다.
	- `Run_and_Shoot` 단일 미리보기에서 오른손 회전이 약 `58.096도` 변경되는 것을 확인했다.
	- `Running`과 `Run_and_Shoot`의 Layered 미리보기에서 오른손 회전이 Base 자세 대비 약 `32.857도` 변경되고 왼발 위치 차이가 `0`인 것을 확인했다.
	- 두 모드에서 캐릭터 루트가 이동하지 않고 정지 후 원래 자세가 복원되는 것을 확인했다.
	- 미리보기 중 변경한 무기 로컬 위치·회전이 정지 후 유지되는 것을 확인했다.
	- 실제 EditorWindow 조작감과 K2 배치의 시각적 결과 확인은 남아 있다.

---

### 3.22 전투 레이어 설정의 에디터 타임 이전

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-10-14
> 상태: 구현 완료, Unity Editor 컴파일·구조 검증 완료, Play 테스트 대기

§3.19에서 도입한 런타임 레이어 자동 지정은 프리팹과 Project Settings에 저장된 설정을 중복했다. 오브젝트가 실행 중 자신의 레이어를 변경하지 않도록 책임을 에디터 타임으로 이전했다.

- **레이어 설정 소유권**: 기존 프리팹의 `PlayerBody`, `PlayerAttack`, `Enemy` 설정과 Physics 충돌 매트릭스를 기준으로 사용한다.
	- `PlayerBody ↔ PlayerAttack`과 `PlayerAttack ↔ PlayerAttack` 차단은 `ProjectSettings/DynamicsManager.asset`에 저장한다.
	- `PlayerWeaponController`, `FistHitbox`, `WeaponAttackHitbox`, `EnemyContactDamage`는 실행 중 레이어를 변경하지 않는다.
- **신규 프리팹 생성**: `WeaponPrefabCreator`와 `Stage1SceneBuilder`가 프리팹 저장 전에 역할 레이어를 지정한다.
	- 런타임 보정 없이 생성 결과 자체가 올바른 레이어를 갖도록 한다.
- **접촉 피해 대상**: `EnemyContactDamage.damageTargetLayers`를 Inspector에 노출하고 적 프리팹에서 `PlayerBody`만 선택한다.
	- Collider에서 피해 소유자를 찾는 §3.18의 `DamageableLookup`은 유지한다.
	- 근접 공격의 자기 소유자 제외와 공격당 중복 타격 방지도 유지한다.
- **제거 범위**: `CombatLayers`와 관련 `Awake`·`Reset` 자동 지정 코드를 제거했다.
	- 피해 경로를 확인하는 `[ContactDamage]`와 `[MeleeHit]` 진단 로그는 이번 범위에서 유지했다.
- **검증**: Unity 재컴파일에서 오류와 경고가 없었다.
	- `PlayerBody=8`, `PlayerAttack=9`, `Enemy=10`을 확인했다.
	- 플레이어 루트, 양쪽 주먹, 두 적 프리팹, 근접 무기 공격 오브젝트의 저장 레이어를 확인했다.
	- 두 적 프리팹의 피해 대상 마스크가 `PlayerBody` 비트만 포함하고, 두 충돌 차단 조합이 유지되는 것을 확인했다.

---

### 3.23 접촉 피해 대상 설정 고정

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-10-14
> 상태: 구현 완료, Unity Editor 컴파일·구조 검증 완료, Play 테스트 대기

§3.22의 `damageTargetLayers`는 현재 게임 규칙에 존재하지 않는 프리팹별 선택지를 Inspector에 노출했다. 적의 접촉 피해 대상이 항상 플레이어라는 규칙에 맞춰 설정 항목을 제거했다.

- **대상 판정**: `EnemyContactDamage`가 충돌한 오브젝트의 레이어를 고정된 `PlayerBody` 레이어와 직접 비교한다.
	- Collider에서 피해 소유자를 찾는 `DamageableLookup`과 피해 적용 절차는 변경하지 않았다.
- **직렬화 제거**: 두 적 프리팹에서 삭제된 마스크 필드의 저장 데이터를 제거했다.
	- `Stage1SceneBuilder`에서도 해당 마스크를 지정하던 코드를 제거했다.
- **유지 범위**: 에디터 타임 레이어 지정, Physics 충돌 매트릭스, `[ContactDamage]`와 `[MeleeHit]` 진단 로그는 유지했다.
- **검증**: Unity 재컴파일에서 오류와 경고가 없었다.
	- 소스와 프리팹에서 `damageTargetLayers` 참조가 제거된 것을 확인했다.
	- 실제 접촉 피해의 Play Mode 회귀 테스트는 남아 있다.

---

### 3.24 전투 판정 진단 로그 제거

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-10-14
> 상태: 구현 완료, Unity Editor 컴파일 검증 완료, Play 테스트 대기

전투 판정 버그의 원인 확인과 수정이 끝나 진단 전용 로그를 제거했다.

- **제거 범위**: `MeleeWeapon`의 `[MeleeHit]`와 `EnemyContactDamage`의 `[ContactDamage]` 로그를 제거했다.
	- 로그만을 위한 피해 전후 체력 비교와 Collider 계층 경로 생성 코드도 함께 제거했다.
- **유지 범위**: `PauseToggle`, `DamageableLookup`, 공격자 자기 제외, 공격당 중복 타격 방지와 전투 역할 레이어 판정은 유지했다.
- **검증**: Unity 재컴파일에서 오류와 경고가 없었다.
	- 실제 전투 입력을 통한 회귀 테스트는 남아 있다.

---

### 3.25 피해 소유자 탐색 유틸리티 제거

> 후속 변경(2026-10-19): [일부 변경] 공통 접촉 피해 정책은 [적 개발 노트5.29](05-EnemyAnimation.md#529-유형별-공격-피해-정책으로-전환)의 유형별 공격 정책으로 변경했다. 아래 코드·레이어·대상 제한과 검증은 당시 기록이다. 기본 적의 전환은 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), 공통 대상 규칙은 [3.31](#331-범용-무기와-사람형-모션-통합)을 따른다.

> 일자: 2026-10-14
> 상태: 구현 완료, Unity Editor 컴파일 검증 완료, Play 테스트 대기

`DamageableLookup`은 Unity의 부모 컴포넌트 탐색과 같은 작업을 별도 클래스에서 반복했다. 호출부에서 Unity 기본 API를 직접 사용하도록 단순화했다.

- **제거 범위**: `DamageableLookup` 클래스와 메타 파일을 제거했다.
- **탐색 방식**: `MeleeWeapon`과 `EnemyContactDamage`가 `GetComponentInParent<IDamageable>(true)`로 가장 가까운 피해 소유자를 찾는다.
	- 탐색 결과를 `Component`로 사용해 공격자 자기 제외와 공격당 중복 타격 방지를 유지한다.
- **검증**: 삭제된 클래스의 코드 참조가 남아 있지 않으며 Unity 재컴파일에서 오류와 경고가 없었다.
	- 실제 근접 공격과 접촉 피해의 Play Mode 회귀 테스트는 남아 있다.

---

### 3.26 Cinemachine 기반 추적과 건물 충돌 대응

> 후속 변경: 충돌 확장은3.27에서 Deoccluder로 교체했고, 주시 방식은3.29·사용자 수용은3.30을 따른다. 당시 전환·검증 기록은 보존한다.

> 일자: 2026-10-16
> 상태: 구현·컴파일·직접 API 검사 43개·Play Mode 검사 11개 완료, 사용자 수용·실행 빌드 검증 대기
> 참고: FirstMap 6장, Backlog 14·15

사용자가 공식 Unity 기능을 활용한 카메라 충돌 대응 구현을 승인해 Cinemachine 3.1.7을 적용했다.

- **재현**: SE04 북쪽의 플레이어 위치 `(67, 0, -57)`에서 기존 오프셋의 카메라 `(67, 12, -66)`가 건물 몸체 내부에 놓이는 것을 확인했다.
	- 현재 작업 씬은 `Assets/Settings/Scenes/GameScene.unity`였다.
	- 씬 폴더 이동은 카메라 작업과 별개의 기존 변경으로 취급했다.
- **책임**: Main Camera의 CinemachineBrain이 실제 카메라 출력을 소유한다.
	- 별도 Gameplay Camera의 CinemachineFollow가 월드 기준 고정 오프셋 추적을 수행한다.
	- CinemachineDecollider가 카메라와 겹치는 WorldObstacle에서 추적 대상 방향으로 빠져나온다.
	- 시야 확보용 Deoccluder와 건물 투명화는 추가하지 않았다.
- **추적 기준점**: 플레이어의 씬 인스턴스 아래 Camera Target을 로컬 높이 1.5m에 추가했다.
	- 발밑을 기준으로 당길 때 발만 크게 보이는 것을 실제 Play 화면에서 확인했다.
	- FollowOffset을 `(0, 10.5, -9)`로 보정해 평상시 플레이어 기준 `(0, 12, -9)`와 약53.13도 피치를 유지했다.
	- 별도 회전 제어 컴포넌트와 일반 추적 지연은 추가하지 않았다.
- **충돌 설정**: Camera Radius 0.6m, Obstacle Layers WorldObstacle, Use Follow Target 활성, Y Offset 0, Damping 0.5, Smoothing Time 0.1을 적용했다.
	- Terrain Resolution은 비활성이다.
	- 충돌 시 즉시 당기고, 해제 후 유지 시간과 감쇠를 거쳐 원래 거리로 복원한다.
	- 건물 밖에서 캐릭터만 가려지는 상태는 허용한다.
- **줌과 조준**: CameraFovZoom은 Cinemachine 렌즈에 FOV를 기록하고 같은 값을 실제 Camera에도 즉시 반영한다.
	- Brain의 후속 갱신으로 줌이 덮어써지는 것을 방지한다.
	- 기본45도·범위30~60도·감도3과 기존 일시정지·UI·직교 카메라 입력 차단을 유지했다.
	- AimVectorProvider의 실제 카메라 참조는 유지했다.
- **제작 경로**: Stage1SceneBuilder의 카메라 생성·정리도 같은 구성으로 전환했다.
	- 이전 QuarterViewCameraFollow와 메타 파일은 Unity API로 제거했다.
	- 런타임 충돌 알고리즘이나 별도 관리 컴포넌트는 작성하지 않았다.
- **생애주기와 의존**: Brain은 LateUpdate에서 최종 위치와 렌즈를 적용한다.
	- CameraFovZoom은 Awake에서 초기 FOV를 설정하고 Update에서 입력을 처리한다.
	- 추적 대상은 씬에 직렬화된 자식 Transform이며, 씬 재시작 때 해당 씬의 새 인스턴스에 연결된다.
	- 플레이어 이동·전투 코드는 Cinemachine을 참조하지 않는다.
- **검증**: Unity Editor 재컴파일이 완료됐고 최종 코드 자체 검토를 수행했다.
	- 직접 API 검사 43개는 기본 오프셋·시점, 건물 몸체24개의 북쪽 접근, 내부 시작, 단순 가림 허용, 트리거·플레이어 레이어 제외, 복원과 81개 연속 접근 표본을 포함했다.
	- 위 43개 중 9개는 FOV 30·45·60과 화면 비율 4:3·16:9·21:9에서 Near Clip 모서리가 설정 반경에 포함되는지 계산한 검사다.
	- 안전 판정은 카메라 중심과 Near Clip 면의 물리적 침범으로 확인했다.
	- Play Mode의 9개 검사는 휠 입력·FOV 상하한·Brain의 줌 유지·일시정지·직교 차단·실제 충돌 보정·피치·보정 후 조준 연결을 포함했다.
	- 추가 Play 검사2개에서 장애물 이탈 후 `(0, 12, -9)` 복원과 씬 재시작 후 대상·Brain·FOV45 초기화를 확인했다.
	- 씬 직렬화4593개 블록 중4588개가 동일했다.
	- 변경4개·제거1개·추가8개는 Main Camera 구성·줌 참조·플레이어 자식 등록·Cinemachine 객체·SceneRoots에 한정됐다.
- **검사 자료**: 로컬 보조 소스는 AgentScripts/CinemachineMigration.cs, CinemachineValidation.cs, CinemachinePlayValidation.cs에 남겼다.
	- 결과는 Temp/CameraMigration의 reproduction.json, validation.json, play-validation.json, reload-validation.json과 비교 PNG에 저장했다.
	- AgentScripts와 Temp는 Git 제외 대상이므로 영구 검증 근거는 이 기록의 범위를 따른다.
- **연결 문제**: 서버 포트는 열려 있으나 연결 정보가 사라지는 상태 때문에 임시 Editor 복구 스크립트를 사용했다.
	- 이 파일과 메타는 적용 후 Unity API로 제거했다.
	- 일시적인 연결 성공을 영구 해결로 판단하지 않았다.
- **남은 확인**: 벽에 밀착할 때의 확대 정도·주변 적 가독성·직접 조작의 조준 감각은 사용자 수용 대기다.
	- 보정 후 조준 연결 검사는 이동·충돌 전환 중 모든 프레임의 조준 일치를 증명하지 않는다.
	- 모든 건물 모서리·차양·돌출 외관과 극단적 화면 비율의 화면 검증은 수행하지 않았다.
	- 실제 메시가 기존 Collider 밖으로 돌출된 부분은 충돌체 범위 밖일 수 있다.
	- 실행 빌드·성능과 장시간 전투 회귀는 별도 검증 대상이다.
	- 최소 추적 거리나 추가 우회·투명화는 이번에 도입하지 않았다.

---

### 3.27 Deoccluder로 낮은 건물의 시야 가림 대응

> 후속 변경: 고정 피치·주시 구성은3.29의 높이 보간으로 변경했다. Deoccluder 충돌·가림 설정과 당시 검증 기록은 유지한다.

> 일자: 2026-10-16
> 상태: 사용자 승인 구현·컴파일·직접 API68개·Play12개 검사 완료, 직접 플레이 수용·실행 빌드 대기
> 참고: 이전 카메라 구성은3.26을 따른다.

사용자가 낮은 건물의 가림에 대한 Deoccluder 적용을 승인해 기존 Decollider를 공식 시야 보정 기능으로 교체했다.

- **재현**: NW03 북쪽의 플레이어 위치 `(-28, 0, 42)`에서 카메라 `(-28, 12, 33)`는 충돌하지 않았지만 상체 기준점까지의 시선이 가려졌다.
- **구성 비교**: Decollider 단독·Deoccluder 단독·병용을 열린 공간·NW03·SE04에서 각각 비교했다.
	- Deoccluder 단독과 병용은 세 위치에서 같은 결과를 보였다.
	- NW03의 보정 위치는 약 `(-28, 2.184, 41.414)`였으며 기준점까지 시선과 카메라 중심·Near Clip 안전 검사를 통과했다.
	- 중복 보정을 피하기 위해 Deoccluder 단독을 채택하고 씬의 Decollider를 제거했다.
- **책임**: Follow가 기본 위치를 계산하고 Deoccluder가 WorldObstacle에 의한 상체 기준점 가림을 해소한다.
	- Brain·상체 기준점·기본 오프셋·고정 피치·CameraFovZoom과 조준 코드의 연결은 유지했다.
	- 낮은 건물에만 한정하지 않고 해당 레이어의 시야 장애물을 처리한다.
- **설정**: Pull Camera Forward, Camera Radius0.6, Use Follow Target 활성·YOffset0, Distance Limit0, Minimum Occlusion Time0, Maximum Effort4를 적용했다.
	- Damping When Occluded0으로 즉시 보정하고 Damping0.5·Smoothing Time0.1로 복원한다.
	- Minimum Distance From Target0.3과 나머지 기본 설정을 유지했다.
	- 이 최소 거리 값은 대상 가까이의 장애물을 무시하는 거리이며 카메라 최소 추적 거리를 보장하지 않는다.
- **변경 범위**: Stage1SceneBuilder.BuildCamera와 현재 `Assets/Settings/Scenes/GameScene.unity`의 확장 컴포넌트만 변경했다.
	- 새 런타임 코드·패키지·레이어·Collider·재질은 추가하지 않았다.
	- 제작 코드와 저장 씬에 같은 설정을 반영했다.
- **정책 변화**: 이전의 단순 가림 허용에서 상체 기준점까지의 시야 확보를 시도하는 동작으로 전환했다.
	- 전신과 주변 적·드롭 무기가 항상 보인다는 보장은 아니다.
	- 고정 시점 기준과 추가 투명화의 승인 경계는 유지했다.
- **검증**: 컴파일·최종 코드 자체 검토와 직접 API68개·Play12개 검사를 완료했다.
	- 직접 API는24개 건물 위치의 카메라 안전·기준점 시선, NW03 재현, 기존 SE04 접근·81개 연속 표본·복원·트리거와 플레이어 레이어 제외 등을 포함한다.
	- NW06·NW07의 북쪽 표본은 맵 경계 Collider 안에 있어 제외하고 남쪽의 통행 가능한 표본으로 교체했다.
	- 따라서 건물 검사 위치는 북쪽22곳·남쪽2곳이며 모든 방향·모든 모서리의 합격을 뜻하지 않는다.
	- 68개 중9개는 기존 FOV·화면 비율 조합의 Near Clip 반경 포함 계산이다.
	- Play10개는 실제 낮은 건물 보정·상체 시선·피치·줌·조준 연결을 확인했다.
	- 추가2개는 장애물 이탈 후 기본 위치 복원과 씬 재시작 후 Deoccluder 단독·기준점·FOV45 초기화를 확인했다.
	- 직렬화 비교는 Gameplay Camera의 컴포넌트 교체만 확인했으며 기존 건물·조명·충돌체는 보존했다.
- **근거 파일**: 로컬 AgentScripts/DeoccluderMigration.cs·DeoccluderValidation.cs·DeoccluderPlayValidation.cs를 사용했다.
	- Temp/DeoccluderMigration에 작업 전 씬·comparison.json·validation.json·play-validation.json·reload-validation.json과 Play 화면을 보관했다.
	- 이 로컬 자료는 Git 제외 대상이다.
- **완료 경계**: 저장된 게임 씬으로 복귀하고 Play Mode를 종료했다.
	- 직접 조작 중 조준·전투 가독성·확대 정도, 전체 돌출 메시와 모서리, 실행 빌드·성능은 미검증이다.
	- 갑작스러운 자동 근접의 안내·연출과 실루엣·투명화는 적용하지 않았다.
	- 커밋·푸시는 수행하지 않았다.

---

### 3.28 접근·복귀 중 상체 기준점의 화면 중앙 유지

> 후속 변경: Hard Look At과 고정 상체 주시는3.29의 CameraProximityLookAt으로 교체했다. 이 절의47개 검사는 당시 구성의 결과다.

> 일자: 2026-10-17
> 상태: 사용자 승인 구현·컴파일·Play47개 검사 완료, 직접 조준 감각·실행 빌드 대기
> 참고: 충돌·시야 보정은3.27을 기반으로 한다.

- **문제와 원인**: 사용자가 건물 접근·복귀 시 캐릭터의 화면 중앙 이탈을 보고했다. 기존 구성은 발밑을 바라보는 초기 각도와 상체를 향한 이동 기준이 달랐고 지속적인 중앙 주시 컴포넌트가 없었다.
- **결정**: 공식 CinemachineHardLookAt을 추가하고 Follow·LookAt을 기존 높이1.5m Camera Target으로 통일했다. 중심 유지의 기준은 캐릭터 외형 전체의 중심이 아닌 이 Transform이다.
- **범위와 영향**: Stage1SceneBuilder.BuildCamera와 저장된 GameScene에 같은 구성을 반영했다. 새 런타임 코드·패키지·재질·Collider는 추가하지 않았다.
	- 기본 위치 오프셋과 Deoccluder 설정은 유지한다. 회전은 보정된 위치에서 상체 기준점을 바라본다.
	- 평상시 피치는 약53.13도에서 약49.40도로 바뀐다. 근접·복귀 중에는 기준점을 향하는 방향에 맞춰 회전한다.
- **검증**: Unity 컴파일 완료와 Play43개·재시작4개 검사를 통과했다. NUnit 검사 수가 아닌 로컬 실행 검증 항목 수다.
	- NW03·SE04에서 FOV30·45·60별 밀착·왕복61표본의 중앙 유지·카메라 중심과 Near Clip 안전·열린 공간 복귀·FOV 출력을 확인했다.
	- 최초 NW03 경로는 옆 NW07 건물 내부까지 진입해 유효하지 않았다. 실패 근거를 보존하고 통행 가능한4.5m 왕복 경로로 재검사했다. SE04는9m 경로다.
	- 재시작 후 공식 주시 컴포넌트·공유 기준점·중앙 좌표·FOV45·기존 조준 카메라 참조를 확인했다.
- **근거**: 로컬 AgentScripts/CameraCentering.cs와 Temp/CameraCentering의 before.unity·invalid-path.json·play.json·reload.json. Git 제외 자료다.
- **검증 대기**: 직접 조작의 조준 감각·전투 가독성, 모든 모서리·돌출 메시와 화면 비율, 실행 빌드는 별도 확인한다. 이번 FOV 검사는 휠 입력의 재검사를 뜻하지 않는다.
- **보류**: 자동 근접의 인식 문제·안내·지연 연출·다른 게임 사례 조사는 적용하지 않았다. Play Mode 종료 후 편집 상태로 복귀했으며 커밋·푸시는 수행하지 않았다.

---

### 3.29 카메라 근접 정도에 따른 주시 높이 선형 보간

> 후속 확인: 사용자 직접 확인 통과 판정은3.30을 따른다. 아래 구현 당시 검증 대기는 해당 시점의 기록이다.

> 일자: 2026-10-17
> 상태: 사용자 승인 구현·코드 검토 완료, Unity 동작 검증은 사용자 진행 대기
> 참고: 충돌·시야 보정은3.27, 이전 고정 상체 주시는3.28을 따른다.

사용자가 평상시 발밑과 최대 근접 시 상체를 바라보는 선형 변화를 요청해 주시 계산을 분리했다.

- **구성**: Hard Look At을 CameraProximityLookAt 확장 컴포넌트로 교체했다.
	- 기존 Camera Target은 Y1.5로 고정해 Follow·LookAt·Deoccluder 기준으로 유지했다.
	- 주시 원점은 Player Transform을 직접 참조한다.
	- 추적 기준점을 움직여 카메라 위치 계산에 다시 영향을 주는 피드백을 피했다.
- **계산**: Aim 단계에서 장애물 보정 전후 카메라와 Follow 기준점의 거리를 구한다.
	- 보정 전 거리를 근접0%, 보정 후 거리1m 이하를 근접100%로 삼아 주시 높이를0~1.5m로 선형 보간한다.
	- 1m는 모든 건물에서 관찰된 절대 최소 거리가 아닌 조정 가능한 보간 종료 기준이다.
	- 주시 높이는 Look At Origin 기준 월드 Up 방향이며 Camera Target의 실제 Transform 높이는 바꾸지 않는다.
	- FOV 휠 줌은 근접 정도에 포함하지 않는다.
- **설정**: Inspector의 Normal Height0·Close Height1.5·Close Distance1을 기본값으로 둔다.
	- Close Distance는 기본 추적 거리보다 작은 값으로 조정한다.
	- 기본 위치 오프셋·충돌·복귀 감쇠·렌즈와 조준 코드 연결은 유지했다.
- **비용**: 프레임마다 거리2회·선형 보간·회전 계산을 수행한다.
	- 추가 물리 검사·장면 검색·컬렉션 생성·이전 프레임 상태 저장은 하지 않는다.
	- 성능 측정은 수행하지 않았다.
- **변경 범위**: CameraProximityLookAt.cs·Stage1SceneBuilder.BuildCamera·저장된 GameScene과 관련 문서를 갱신했다.
	- 스크립트 메타데이터는 Unity 임포트로 생성하고 씬 구성은 Unity API로 저장했다.
	- 기존 건물·재질·Collider·카메라 충돌 설정은 변경하지 않았다.
- **검증 경계**: 코드와 패키지의 Body→Aim 호출 순서를 검토했다.
	- 씬 연결을 위한 스크립트 임포트만 진행했으며 Play Mode·자동 동작 검사·별도 테스트는 실행하지 않았다.
	- 평상시0·밀착1.5·왕복 시 연속 변화와 가림·조준 감각은 사용자가 직접 확인한다.
	- 기존3.28의47개 통과 결과는 이번 변경의 검증 근거가 아니다.
- **보류**: 자동 접근 속도·설명 연출·다른 게임 사례 조사·커밋·푸시는 수행하지 않았다.
---

### 3.30 주시 높이 보간의 사용자 수용

> 일자: 2026-10-17
> 상태: 사용자 직접 확인 통과
> 참고: 구현은3.29를 따른다.

사용자가 주시 높이 보간 결과를 확인하고 통과 판정을 요청했다.

- **판정**: 현재 카메라의 근접 정도에 따른 주시 높이 변화는 사용자 직접 확인 기준으로 통과했다.
- **경계**: Codex는 추가 Unity 테스트를 실행하지 않았다.
	- 전체 건물·화면 비율·장시간 전투·실행 빌드 성능의 검증 완료를 뜻하지 않는다.
- **후속**: 자동 근접의 인식 문제와 전환 속도 대안을 다시 논의한다.
	- 새 카메라 동작·충돌 설정·안내 UI는 이번 기록에서 변경하지 않았다.
---

### 3.31 범용 무기와 사람형 모션 통합

> 후속 수정(2026-10-19): 공격 레이어와 내부 상태 머신 이름 불일치로 첫 근접 공격 이후 진행 상태가 해제되지 않는 문제를 [3.32](#332-공격-상태-경로-이관-누락-수정)에서 수정했다. 통합 구조와 당시 검증 범위는 보존한다.

> 일자: 2026-10-19
> 상태: 사용자 승인 후 코드·프리팹 이관과 컴파일·저장 참조 확인 완료. 직접 플레이·자동 동작 검증은 미수행.

장착·공격·타격 전달과 사람형 모션을 플레이어·적이 공유하도록 이관했다. 같은 규칙의 수정 위치를 통일하고 입력·AI 판단·플레이어 기록을 공통 실행에서 분리했다.

| 구성 | 소유 책임 |
|---|---|
| WeaponController | 단일 무기 슬롯·장착/교체/드롭·내구도 구독·파괴 후 맨손·공격 배율·손 선택·진행 중 무기. TryEquip·Drop·TryAttack·CancelAttack·ResetForReuse |
| PlayerCombatInput | 공격/상호작용 입력·조준·획득 후보, 획득 성공 시 도감 기록 |
| CharacterAnimationController | Animator·클립·속도·하체 혼합·타격 창/종료 신호·선택 퇴장 표현 |
| WeaponBase / MeleeWeapon / RangedWeapon | 쿨다운 / 근접 단계·명중 집합·판정 / 발사·투사체 |
| AttackTargetRules | 허용 레이어·가장 가까운 IDamageable 소유자·자기 자신/죽은 대상 제외 |

- **의존**: 입력/AI → WeaponController → IWeapon·공통 모션 순서다.
	- 공통 모션은 입력·AI·무기를 역참조하지 않는다.
	- WeaponController가 타격 창·종료 신호를 활성화 시 구독하고 비활성화 시 해제한다.
	- 기존3.3의 순수 애니메이션 관찰자에서 공통 실행부의 명시적 재생 명령으로 일부 변경했다.
	- 공격 재생과 타격 신호의 경로를 한 곳에서 관리하기 위한 선택이다.
- **장비**: WeaponPickup.TryClaim이 장착 여부·획득 지연·소유자를 확인하고 중복 획득을 거절한다.
	- 기존 소켓/GripPoint·스폰 포인트 반환·드롭 높이·내구도 규칙을 유지한다.
	- 도감은 입력부, 증강/업적은 플레이어의 WeaponController 연결에서만 적용한다.
	- 적이 같은 기능을 호출할 기반만 마련했으며 무기 탐색·획득 AI는 제외했다.
- **표현**: HumanoidCombat은 Base Layer → 전신 Action → Lower Body의3레이어다.
	- 실제 이동 속도에 따라 하체 가중치만 전환하며 공격은 Action 한 곳에서 재생한다.
	- IMovementSpeed로 두 이동 방식의 속도만 조회한다.
	- PlayerMovement는 실제 수평 변위를 보고하며 이동 계산은 유지한다.
	- 타격 이벤트는 공통 모션만 수신하고 상태 해시·재생 버전으로 종료를 대조한다.
	- 취소 프레임에는 새 공격을 막아 이전 상태가 정리된 뒤 요청한다.
	- 선택 퇴장·재사용은 기존 즉시 자세 평가를 유지한다.
- **공격 맥락**: 공격자·대상 마스크·공격 레이어를 무기에 전달한다.
	- 투사체는 발사 때 값을 복사하고 풀 반환 때 공격자·대상을 지운다.
	- 같은 무기도 장착자에 따라 PlayerAttack/EnemyAttack 판정을 사용한다.
- **이관**: PlayerWeaponController와 PlayerAnimationController는 Unity API로 이름·위치를 옮기고 GUID를 보존했다.
	- 두 기존 애니메이션 컨트롤러를 공통 구현으로 대체했다.
	- 증강·업적·무기 저작·씬 제작 도구의 직접 참조를 갱신했다.
	- 기존 상체 마스크는 애니메이션 미리보기에서 사용하므로 유지했다.
- **확인**: Unity 컴파일 오류0·플레이어/적 Missing Script0·저장 후 씬 재열기의 연결을 확인했다.
	- 런타임 표현·교체/파괴·풀 동작의 통과를 의미하지 않는다.
	- 첫 적용 설정·남은 비교는 [적 개발 노트5.33](05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치)을 따른다.
---

### 3.32 공격 상태 경로 이관 누락 수정

> 후속 확인(2026-10-19): 사용자가 재공격 정상 동작을 확인했다. 확인 범위와 커밋은 [적 개발 노트5.34](05-EnemyAnimation.md#534-공통-공격의-사용자-확인과-커밋)을 따른다.

> 일자: 2026-10-19
> 상태: 사용자 보고 후 코드·Animator 수정과 컴파일·상태 경로 확인 완료. 직접 연속 공격·타격 검증은 대기다.

첫 근접 공격 이후 입력을 반복해도 다음 공격이 시작되지 않는다는 사용자 보고를 조사했다. 공격 레이어 이름만 Action으로 바뀌고 내부 상태 머신은 Upper Body로 남은 것이 원인이었다.

- **확인**: 수정 전 씬 Animator.HasState는 Action.Attack_MeleeWeapon=false·Upper Body.Attack_MeleeWeapon=true였다.
	- 공통 모션은 Action 경로의 해시를 기대하므로 진입·타격·종료 신호를 거절했다.
	- 무기의 타격 대기 상태를 Idle로 되돌릴 종료 신호가 전달되지 않는 코드 경로를 확인했다.
	- 조사 당시 편집 상태였으므로 첫 공격 직후의 무기 상태를 실시간 관측한 것은 아니다.
- **수정**: Unity API로 HumanoidCombat.controller의 내부 상태 머신 이름을 Action으로 저장했다.
	- PlayerUpperBodyLayerSetup도 레이어와 상태 머신 이름을 함께 지정하고 상태 머신을 저장 대상으로 표시한다.
	- 기존 타격 구간·공격 간격·공통 실행·종료 방어 규칙은 유지했다.
- **확인 결과**: Unity 컴파일 failed=false·errors=[]였다.
	- 현재 씬의 Action 근접·좌우 펀치 상태 조회는 모두 true이고 예전 Upper Body 근접 경로는 false였다.
	- Play Mode·자동 동작 검증은 실행하지 않았다.
	- 사용자가 새 Play에서 맨손·장착 근접의 반복 공격과 실제 타격을 확인한다.
