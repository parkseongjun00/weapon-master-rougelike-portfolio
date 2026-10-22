# 개발 노트 02: 메타 시스템

> 공개 편집본 p1 · 2026-10-23 · 원본 `703affa`. 현재 안내는 [공개본 문서 색인](../README.md)을 따른다. 과거 기록은 당시 상태이며 현재 코드 전체의 검증 결과가 아니다.

> 공통 작성 규칙: [README.md](README.md) 참고.

---

## 2. 3단계: 메타 시스템 구현

### 2.1 착수 전 메타 진행 재설계 논의 요약

> 일자: 2026-07-30

3단계 착수 전 상당한 논의를 거쳐 원래 계획(`DevPlan.md` v0.12, `GDD.md` v0.9 7.2)을 전면 재설계했다. 흐름과 근거를 남긴다.

1. **파워 영향 여부(O/X)**: X로 재확인(원안 유지).
	- 캐릭터만 강해지고 적/환경은 그대로면 재미 기둥 1(즉흥 판단, GDD 2장)이 옅어진다는 게 핵심 근거다.
	- 롤(LoL) 사례를 검토한 결과도 이를 뒷받침한다.
		- 계정 레벨/LP/챔피언 마스터리 전부 매치 내 성능에 영향 없이 매치메이킹·과시용으로만 쓰인다.
	- 다만 '성장감이 없어 장기 동기가 떨어지지 않는가'라는 우려에는, Dead Cells의 Boss Cell 사례(파워 대신 난이도를 스스로 올리는 권리를 해금)처럼 '캐릭터가 아니라 도전 난이도가 성장하는' 대안도 있다는 걸 확인했다.
	- 이번 라운드엔 채택하지 않고 향후 재검토 후보로만 남긴다(아래 '이후 단계 재검토 후보' 참고).
2. **무기 종류/등급 해금(gating) 폐기**: 화폐로 해금해도 필드 스폰이 확률이라 '해금했는데 이번 런엔 안 나온다'는 좌절감이 생긴다.
	- 이를 근본적으로 없애기 위해 무기 종류·등급 자체를 해금 대상에서 제외한다.
	- 처음부터 전부 순수 확률로 조우 가능하다.
	- '현질하면 좋은 옵션이 나온다'는 가챠형 게이트를 피하는 게 목적이다.
3. **코스메틱 폐기 → 칭호로 대체**: 비주얼 아트가 5단계까지 보류라 리소스 투자 대비 효과가 낮다고 판단했다.
4. **메타 재화(화폐) 개념 자체 폐기**: 위 2, 3이 폐기되며 화폐의 용처가 사라졌다.
	- 억지로 용처를 만드는 것은 DevPlan 1장 '막히면 범위를 줄인다' 원칙에 반한다고 판단해, 화폐 매개 없이 조건 달성 시 즉시 해금되는 방식(칭호)과 발견 기반 기록 방식(도감)으로 대체했다.
5. **콘텐츠 볼륨 의도적 최소화**: 칭호를 의미 있게 만들려면 최소 30개 안팎, 도감도 20개 이상이 필요해 보이나, 1인 개발 스코프를 고려해 과잉 투자로 판단했다.
	- 3단계는 시스템(조건 트리거·저장·표시)만 다지고 콘텐츠는 최소 세트만 채운 뒤, 콘텐츠가 실제로 늘어나는 4단계 이후 볼륨을 확장하기로 했다.

---

### 2.2 SaveSystem 단일 창구 원칙

> 현재 이름: `SaveHandler`. 아래 `SaveSystem`·`RunStats`는 당시 이름이며 현재 `SaveHandler`·`RunRecordManager`에 대응한다.

> 일자: 2026-07-30
> 공개 편집: 특정 배포 환경의 전제는 생략하고 저장 책임과 호출 규칙을 보존한다.

- **문제**: 칭호·도감 저장 호출을 단일 창구로 모아 저장 동작과 호출부의 책임을 분리한다.
- **원칙**: 3단계 이후 세이브 관련 코드는 `PlayerPrefs`를 여기저기서 직접 호출하지 않고, 단일 `SaveSystem`(가칭) 창구를 통해서만 읽고 쓴다.
	- 나중에 저장 유실이 실측되면 이 창구 내부 구현만 고치면 되고, 칭호/도감 등 호출부는 손댈 필요가 없다.

---

### 2.3 SaveSystem 구현 세부사항

> 일자: 2026-07-31

원칙(§2.2 'SaveSystem 단일 창구 원칙') 확정 이후, 실제 구현 시 다음 세부사항이 정해졌다.

- API는 `PlayerPrefs`가 네이티브로 지원하는 타입(Int/Float/String)만 1:1로 래핑한다(`SetInt/GetInt`, `SetFloat/GetFloat`, `SetString/GetString`).
	- `Bool`은 인코딩 방식(int 0/1 등)을 새로 정해야 하는 타입인데 지금 쓰는 곳이 없어, 미리 넣는 건 불필요한 선행 설계로 판단해 제외했다.
	- 3.3(칭호) 구현 시 실제로 필요해지면 그 시점 요구사항에 맞춰 추가한다.
- Flush 정책: `Set` 계열 호출 시 내부에서 즉시 `PlayerPrefs.Save()`까지 실행(자동 flush), 별도 `Flush()` 메서드는 두지 않는다.
	- 이 프로젝트에서 저장 이벤트 빈도가 낮아(런 종료 등) 매번 flush해도 비용 부담이 없고, 호출부가 flush를 잊어 저장이 유실되는 실패 유형을 원천 차단하는 쪽이 이 창구를 만든 목적(저장 동작의 단일 창구화)에 더 부합한다고 판단했다.
- 키 이름 상수는 `SaveSystem`이 소유하지 않고 각 호출부(`RunStats` 등)가 소유한다.
	- `SaveSystem`은 전체 키 레지스트리가 아니라 순수 통로 역할만 담당한다.
- `Core/RunStats.cs`의 기존 `PlayerPrefs` 직접 호출(`HandlePlayerDeath()`)을 `SaveSystem` 경유로 리팩터했다.
	- 저장 키 이름(`SurvivalTimeKey`/`KillCountKey`)은 변경이 없다.

---

### 2.4 등급(Rarity) 시스템: 최종 확정

> 일자: 2026-07-31

과거 이력(참고용, 순서대로 두 번 정정됨): ① '필드에서 순수 확률로 조우' → ② '스폰마다 랜덤'으로 잘못 구현 → ③ '등급 = 무기 고유 고정값'으로 정정 → ④(이번) '등급별 최종 스탯을 코드 공식이 아니라 직접 authored 값으로' 재확정. 아래가 최종 상태다.

- **티어**: 일반 / 희귀 / 영웅 / 전설 (4단계).
	- 해금 대상 아님(위 논의 2 참고).
	- **무기 종류(정의) 자체에 고정되는 값**이지 스폰마다 바뀌는 랜덤이 아니다.
	- 스폰 인스턴스마다 달라지는 랜덤 편차는 별도의 '컨디션' 항목이 담당한다.
- **스탯 결정 방식(2026-07-31 재확정)**: 등급별 최종 스탯(공격력/쿨다운/최대내구도)은 **런타임 코드가 '총 보너스율 × 카테고리 비중' 공식으로 계산하지 않는다.**
	- 대신 등급은 '이 등급 무기의 총 스탯 값(예산)이 대략 얼마인가'만 가이드라인으로 제시하고, 그 예산을 공격력/공격속도/내구도 중 무엇에 얼마나 배분할지는 무기 정체성(화력형/연사형 등)에 따라 **사람이 `WeaponDefinition`에 직접 입력**하거나, 추후 외부 밸런싱 도구가 계산해 SO에 자동 주입한다.
	- 즉 `WeaponDefinition.Damage`/`Cooldown`/`MaxDurability` 필드 자체가 이미 등급이 반영된 최종값이며, `WeaponBase`는 이 값을 그대로 쓸 뿐 배율 계산을 하지 않는다.
- **총 스탯 값 가이드라인(placeholder, 사람이 손으로 입력할 때 참고할 대략적인 범위. 튜닝/자동화 대책은 후속 과제)**:

| 등급  | 총 스탯 값(가이드) |
| --- | ----------- |
| 일반  | 100 ~ 120   |
| 희귀  | 150 ~ 170   |
| 영웅  | 200 ~ 220   |
| 전설  | 250 ~ 270   |

  영웅/전설 구간은 일반→희귀의 +50 간격을 그대로 연장한 placeholder 추정치이며, 사용자가 직접 확정한 값이 아니다. 실제 밸런싱 시 다시 검토 필요.

  **주의(2026-07-31 추가)**: 위 표는 순전히 '이런 형태로 입력하면 된다'는 예시일 뿐, 실제로 검증된 밸런스 수치가 아니다. 예컨대 일반의 최댓값(120)과 희귀의 최솟값(150) 사이에 30만큼의 빈 구간이 존재하는데, 이게 의도된 설계(등급 간 확실한 단절)인지 그냥 예시라서 생긴 우연의 공백인지조차 확정된 바 없다. 사용자가 직접 확인차 지적한 부분. 실제 밸런싱(사람이 손으로 입력하든 외부 도구가 계산하든) 시점에 구간을 이어붙일지, 겹치게 할지, 지금처럼 띄울지부터 다시 정해야 한다.
- **세부 스탯 배분**: 코드가 관여하지 않는다.
	- 과거에 코드로 강제하던 '근접(화력형) 60/20/20, 원거리(연사형) 20/50/30' 비중은 이제 **집행되는 로직이 아니라, 사람이 수동으로 스탯을 입력할 때 참고하는 디자인 가이드라인**으로만 남는다(무기 정체성에 맞게 배분하라는 취지 자체는 유지).
- **적용 범위**: 사거리·투사체 속도(정체성 스탯)는 등급과 무관하게 고정.
	- 이 원칙은 유지한다.
- **현재 상태**: 무기가 2종뿐이라 지금은 둘 다 일반 등급 그대로(`WeaponDefinition` 기본값).
	- 4단계(무기 서브타입 확장) 때 새 무기들을 만들면서 상위 등급 스탯을 실제로 입력하게 될 골격만 이번 라운드에 마련한다.
- **시각적 구분**: 등급별 placeholder 색상 틴트(일반=흰색, 희귀=파랑, 영웅=보라, 전설=주황/금).
	- `Weapons/WeaponGradeData`가 담당하며 코드 상 유일한 역할은 이 틴트뿐이다(스탯 계산 역할이 없다).
- **틴트 적용 시점(2026-07-31 확정)**: 처음엔 `WeaponSpawner.SpawnAt()`이 스폰마다 `definition.Rarity`를 읽어 인스턴스 전용 머티리얼에 런타임으로 칠했으나, 등급이 무기 고유의 고정값이라 스폰마다 다시 읽을 이유가 없다는 지적으로 **프리팹 빌드 시점(`Stage1SceneBuilder`)에 한 번만 굽는 방식으로 변경**했다.
	- 등급이 정해지는 authored 시점(=프리팹 생성 시점)에 그 결과를 이미 알 수 있으므로, 6번 항목(등급 스탯을 런타임 공식이 아니라 authored 값으로)과 동일한 원칙이다.
	- `WeaponSpawner`는 이제 틴트 관련 코드를 전혀 갖지 않는다.
	- **트레이드오프**: `WeaponDefinition.Rarity`를 빌더 재실행 없이 Inspector에서 나중에 바꾸면, 캐시된 프리팹 머티리얼 색은 자동으로 안 따라온다(수동으로 빌더를 재실행하거나 머티리얼을 직접 맞춰야 한다).
		- 런타임 방식이었다면 자동으로 반영됐을 부분.
	- 무기가 아직 2종뿐이고 전부 일반 등급인 골격 단계라 현재는 실질적 리스크가 낮다고 판단해 감수.

---

### 2.5 컨디션(Condition): 최종 확정

> 일자: 2026-07-31

3.2 구현 중 '등급이 스폰마다 랜덤'으로 잘못 구현했던 걸 사용자가 정정하는 과정에서 나온 신규 개념. 등급은 무기 고유 고정값으로 되돌리되, 같은 무기라도 개체(스폰 인스턴스)마다 편차를 주고 싶다는 요구는 유효했으므로 별도 축으로 분리했다.

- **정의**: 무기 인스턴스가 스폰될 때마다 무작위로 결정되는 상태값이다.
	- 등급을 대체하는 게 아니라 그 위에 얹히는 훨씬 작은 변주다.
- **적용 범위는 공격력에만 한정(2026-07-31 확정)**: 최초 설계는 등급과 동일하게 공격력/공격속도/내구도 3종에 비중대로 분배하는 방식이었으나, '내구도까지 컨디션이 좌우하면 불공평함이 너무 커진다'(내구도는 무기 수명 자체를 좌우하므로 편차의 체감이 과함), '공격속도는 컨디션(개체의 상태/품질)이라는 개념과 어울리지 않는다'는 사용자 판단으로 공격력 하나로 범위를 좁혔다.
	- 그 결과 카테고리별 비중 분배(`WeaponStatWeights`)가 더 이상 필요 없어져 해당 파일은 삭제했다.
	- 컨디션 값 자체가 곧 공격력 배율이다.
- **티어(placeholder)**: 매우 나쁨 -10% / 나쁨 -5% / 보통 0% / 좋음 +5% / 매우 좋음 +10% (5단계, `WeaponCondition`: VeryBad/Bad/Normal/Good/VeryGood).
	- 균등 분포로 스폰된다.
	- 밸런스 테스트하며 불공평하게 느껴지면 폐지 검토 대상이다.
	- 사용자가 명시적으로 '테스트 후 언페어하면 폐지'라고 조건부로 확정했다.
- **롤링 주체(2026-07-31 확정)**: `WeaponSpawner`가 아니라 **`WeaponBase.Awake()`가 스스로 컨디션을 굴린다.**
	- 스포너는 '어떤 프리팹을 언제 어디에 스폰할지'만 알면 되고 Condition의 존재 자체를 몰라도 된다.
		- 등급이 무기 정의에 고정될 때와 동일한 논리다.
	- 이 덕분에 과거에 있었던 '스폰 이후 `AddComponent`로 붙이고 `ApplyCondition()`을 명시적으로 호출해야 하는' Awake 순서 워크어라운드 자체가 통째로 사라졌다(컨디션이 완전히 `WeaponBase` 내부에서 자기 완결적으로 처리된다).
- **맨손 무기 제외**: `WeaponDefinition`에 `hasCondition`(bool, 기본 true) 필드를 추가해 `WeaponBase.Awake()`가 이 값을 보고 컨디션을 굴릴지 결정한다.
	- 맨손(`UnarmedFists`) 정의만 `hasCondition = false`로 명시 설정한다.
	- 대안으로 '`WeaponPickup` 컴포넌트 유무로 판단'도 검토했으나, 맨손 오브젝트엔 `WeaponPickup`이 없다.
	- 필드 하나 추가가 `rarity`/`moveSpeedPenalty`처럼 이미 `WeaponDefinition`에 있는 '무기별 특성 플래그' 패턴과 일관되고 의도가 더 명시적이라 채택했다.
- **컴포넌트 구조 단순화**: 컨디션이 공격력 하나에만 영향을 주는 단순한 값이 되고, 롤링도 무기 스스로 하게 되면서, 별도 컴포넌트(`WeaponConditionInstance`)를 만들 필요가 없어졌다.
	- `WeaponBase`가 `Condition`(enum, public getter)과 내부 배율 하나만 필드로 들고 있는다.
- **미해결 이슈 (3.4만 남음, 2026-07-31 일부 해결)**: 무기 도감(3.4, 8칸 = 무기 2종 × 등급 4단계)과 칭호 '전설 등급 무기 첫 획득'(3.3)은 원래 '같은 무기가 랜덤 등급으로 조우된다'는(잘못된) 전제로 설계됐다.
	- 등급이 무기 고유 고정값으로 되돌아갔고 현재 무기가 2종뿐이므로, 이 전제가 깨진다.
	- **칭호 #5는 3.3 착수 시 사용자 확인으로 해결**: '전설 등급 무기 첫 획득' → '컨디션이 매우 좋음(VeryGood)으로 스폰된 무기 첫 획득'으로 교체(아래 '칭호. 최소 세트' #5 참고).
		- 등급과 달리 컨디션은 스폰마다 실제로 랜덤 굴려지므로 '발견/변주' 컨셉이 그대로 성립한다.
	- **도감(3.4)의 슬롯 구성 축은 아직 미결정이다.**
		- 3.4 착수 시 다시 논의 필요(후보: ① 무기 종류 × 컨디션, ② 등급 축은 4단계로 미루고 무기 종류만).

---

### 2.6 코드 구조/네이밍 정리

> 일자: 2026-07-31

3.2 구현 리뷰 중 사용자 피드백으로 반영:

- `WeaponCategory`/`WeaponRarity`/`WeaponCondition` enum을 각각 `WeaponCategory.cs`/`WeaponRarity.cs`/`WeaponCondition.cs`로 분리했다.
	- 이전엔 `WeaponDefinition.cs`/`WeaponConditionData.cs`에 얹혀 있어 '새 항목을 추가하려 할 때 어디 있는지 헷갈린다'는 지적이 있었다.
	- C# 관례(파일명=타입명)에도 맞춘다.
- `DurabilityTracker` → `WeaponDurability`로 개명했다.
	- 'Tracker'라는 이름이 수동 관찰만 하는 것처럼 오해를 부른다는 지적이 있었다.
	- 실제로는 `Consume()`으로 상태를 능동적으로 변경하고 `OnDepleted`까지 발행하는, '무기의 내구도 상태 그 자체'에 가까운 컴포넌트라 이름을 그에 맞게 바꿨다.
- `WeaponGradeData`/`WeaponConditionData`는 각각 등급 틴트, 컨디션 공격력 배율만 담당하는 순수 데이터 클래스로 축소했다.

---

### 2.7 네이밍 컨벤션: `~System` / `~Manager`

> 일자: 2026-08-01
> 상태: 확정

3.4(무기 도감) 클래스명을 정하는 과정에서 나온 프로젝트 전역 규칙이다. **신규 클래스에 한해 적용한다**(기존 클래스 소급 리네이밍은 하지 않는다, 아래 예외 참고):

- **static으로만 접근하는 클래스** → `~System` (예: `SaveSystem`, 신규 `WeaponCollectionSystem`, `AchievementSystem`).
- **static은 아니지만 씬에 유일하게 존재하는 클래스**(`public static X Instance { get; private set; }` 싱글턴 패턴) → `~Manager`.

**예외로 명시적으로 남겨둠**(2026-08-01, 사용자 확인). 아래 클래스들은 규칙을 소급 적용하면 오히려 어색해져, 규칙 적용 대상에서 제외한다.
- `WeaponGradeData`/`WeaponConditionData`. 상태·로직 없이 룩업 테이블 하나만 담당해 'System'이라 부르기엔 너무 얇다.
- `Stage1SceneBuilder`/`PlayerInputActionsSetup`. '시스템'이 아니라 에디터 전용 빌드 스크립트라 규칙과 성격이 다름.
- `ProjectilePool`/`DamageNumberPool`. 'Pool'이 이미 역할을 명확히 전달해 'Manager'를 덧붙이면 중복.

이 규칙 도입으로 3.3 계획의 `AchievementManager`(static class)도 착수 전에 `AchievementSystem`으로 이름을 맞춘다. 아직 코드가 없어 지금 바꾸는 데 비용이 없다. `HANDOFF.md`가 다음 라운드 갱신 시 이 이름으로 반영되어야 한다.

---

### 2.8 네이밍 컨벤션: 4분류로 재정의

> 일자: 2026-08-02

위 'static 접근 클래스 → `~System`' / '씬 유일 존재 Instance 싱글턴 → `~Manager`' 이분법이, 사실은 'static 접근 가능 여부'와 '씬에 컴포넌트로 실체화됐는지 여부'라는 서로 다른 두 축을 하나로 뭉쳐놓은 것이었다. 칭호 시스템(`AchievementSystem`/`AchievementTracker`) 재설계를 논의하다 이 모호함이 드러났다. 두 축을 분리해 4분류로 재정의한다.

| 접미사 | static 접근 | 씬 컴포넌트로 실체화 | 개수 제약 |
|---|---|---|---|
| `~System` | 가능 | 됨(예: `Instance` 싱글턴 패턴) | - |
| `~Handler` | 가능 | 안 됨(순수 static 클래스) | - |
| `~Manager` | 불가(참조를 들고 있어야 접근 가능) | 됨 | 단일 |
| `~Controller` | 불가 | 됨 | 여러 개 존재 가능 |

- 위 접미사보다 더 적절한 어휘가 있으면(`Data`/`Definition`/`Pool` 등) 그대로 쓴다.
	- 기존 예외 목록(`WeaponGradeData`/`WeaponConditionData`/`Stage1SceneBuilder`/`PlayerInputActionsSetup`/`ProjectilePool`/`DamageNumberPool`)은 그대로 유효.
- **적용 범위. 2026-08-04 해소**: `SaveSystem`/`WeaponCollectionSystem`은 실제로 'System'이 아니라 'Handler'로 재분류해 `SaveHandler`/`WeaponCollectionHandler`로 개명 완료(아래 '코드 구조/네이밍 정리' §2단계 후속 참고).
	- 소급 리네이밍 여부는 '신규 클래스에 한해 적용'을 깨지 않는 선에서, 리팩터링 라운드에서 사용자가 명시적으로 요청한 경우에만 진행하기로 했다.
	- 자동/일괄 소급 적용은 여전히 하지 않는다.
- **이 표는 확정됐으나 코드에는 아직 적용하지 않았다(문서화만 완료, 2026-08-02)**.
	- 칭호 시스템 재설계(`AchievementSystem`/`AchievementTracker`) 실제 구현 시 처음으로 적용될 예정.

---

### 2.9 씬 전용 오브젝트는 직렬화 필드 대신 static `Instance` 접근

> 일자: 2026-08-03
> 비고: `PlayerAugments.cs`에서 승격

`RunStats`/`EnemySpawner`/`AchievementSystem`/`PlayerAugments`처럼 씬에 유일하게 존재하는 컴포넌트를, 소비 측이 `[SerializeField]` 참조 대신 `public static X Instance { get; private set; }` 싱글턴 패턴으로 접근하는 이유(개별 클래스 주석에 반복 기록하지 않고 여기 한 번만 남김):

- 소비 측(`MeleeWeapon`/`RangedWeapon`/`HealthComponent` 등)은 씬 오브젝트뿐 아니라 **프로젝트 프리팹 애셋**(무기 필드 스폰 프리팹, `Enemy_Basic`)에도 존재한다.
- 프리팹은 애셋이므로, 씬에만 존재하는 오브젝트(플레이어의 `PlayerAugments` 등)에 대한 참조를 직렬화할 수 없다.
	- 직렬화 필드로 두면 프리팹 상태에서 그 필드가 항상 비어 있게 된다.
- 그래서 씬 유일 컴포넌트는 static `Instance`로 접근하고, `Awake`에서 할당·`OnDestroy`에서 해제한다.

---

### 2.10 칭호: 최소 세트

> 일자: 2026-07-30
> 상태: placeholder

시스템 검증용 최소 8개. 문구는 나중에 다듬어도 되는 수준이다. 각 조건이 **단일 런 기준**인지 **전체 런 누적(평생)** 기준인지 명시한다(2026-07-30 추가 확정). 이게 갈리면 어떤 카운터를 어디(런 단위 vs SaveSystem)에 둘지가 달라지므로 반드시 구분해서 구현해야 한다.

1. 첫 무기 획득. 누적(평생 최초 1회)
2. 첫 처치. 누적(평생 최초 1회)
3. 생존 5분 달성. **단일 런 기준**(그 판에서 5분 도달 시 트리거)
4. 누적 처치 100마리 달성. 누적
5. ~~전설 등급 무기 첫 획득~~ → **컨디션이 매우 좋음(VeryGood)으로 스폰된 무기 첫 획득**. 누적(평생 최초 1회). (2026-07-31 재정의: 원래 전제였던 '등급이 스폰 확률로 결정된다'가 5.5 정정으로 깨져 사용자 확인 하에 컨디션 기준으로 교체. 상세 경위는 위 '컨디션(Condition). 최종 확정' 미해결 이슈 절 참고.)
6. 무기 파괴(내구도 0) 10회 경험. **누적**(전체 런 합산 통산 10회. 단일 런 기준이면 진입장벽이 너무 높아짐)
7. 맨손으로 1분 생존. **단일 런 내 연속 시간**(무기를 주우면 타이머 리셋, 다시 맨손이 되면 처음부터 재기산)
8. 누적 10판 플레이. 누적

조건 판정은 매 프레임 전수 폴링이 아니라, 해당 상황에서만 활성화되는 효율적인 방식으로 구현한다(예: #7은 무기 슬롯이 맨손 상태일 때만 타이머 진행). 구체 구현은 Claude Code 재량이다.

---

### 2.11 칭호: SO 기반 구현으로 재설계

> 일자: 2026-08-01
> 상태: 확정

착수 전 계획은 `AchievementId`(enum) + `AchievementSystem`이 조건마다 하드코딩된 분기로 판정하는 구조였다. '칭호가 늘어날수록 enum 관리가 불편해진다'는 문제 제기로 착수 직전에 재설계해 아래 구조로 확정했다(코드 반영 완료, 3.3 구현 라운드).

- **폴더**: `Assets/Scripts/Achievements/`(네임스페이스 `WeaponMaster.Achievements`)를 신설해 칭호 관련 코드를 전부 모았다.
	- 칭호는 무기 도메인 개념이 아니라 메타 진행 개념이라 `Weapons` 폴더에 끼워 넣는 게 어색하다는 지적을 반영했다.
	- `Assets/Scripts/Augments/`가 이미 `Core`+`Player`를 참조하는 독자적 네임스페이스로 존재하는 선례를 그대로 따랐다(`Achievements`도 `Core`/`Weapons`를 참조하되 아무도 거꾸로 참조하지 않아 순환이 없다).
- **데이터 구조**(`WeaponDefinition`/`WeaponRoster`와 동일 패턴):
	- `AchievementMetric`(enum, 7개). 칭호 각각이 아니라 '무엇을 재는가'만 나열: `WeaponEquipCount`/`EnemyKillCount`/`WeaponDestroyedCount`/`RunPlayedCount`(전부 평생 누적) / `SurvivalSeconds`/`UnarmedSeconds`(단일 런, 구간형) / `WeaponConditionTier`(순간값).
		- 이름은 처음에 `AchievementSignal`이었으나 '역할을 잘 못 드러낸다'는 지적으로 `Metric`으로 변경했다.
		- `threshold`와 짝지었을 때 '이 값이 threshold 이상이면 달성'이라는 문장이 그대로 성립하는 이름을 택했다.
	- `AchievementDefinition`(SO).
		- 칭호 하나 = 애셋 하나다.
		- `displayName`/`description`/`metric`/`threshold` 4필드뿐이다.
		- 저장/조회 키는 `WeaponDefinition`과 동일하게 `Object.name` 재사용, 별도 id 필드가 없다.
	- `AchievementRoster` + `AchievementRosterEditor`.
		- `WeaponRoster`/`WeaponRosterEditor`(3.4)와 완전히 동일한 패턴(재스캔 버튼, `Assets/Data/Achievements` 폴더 스캔).
- **판정 로직**: 칭호마다 다른 코드를 짜지 않고 '측정값이 threshold 이상이면 달성'이라는 규칙 하나로 처리한다.
	- `AchievementSystem`(static).
		- 해금 여부 저장/조회 + 측정값 보고(`IncrementMetric`/`ReportMetric`) + `OnMetricReported` 이벤트 발행만 담당한다.
		- 로스터도, 칭호가 몇 개인지도 모른다(`WeaponCollectionSystem`이 맨손의 존재를 몰라도 되는 것과 같은 이유).
	- `AchievementTracker`(MonoBehaviour, `Achievements` 폴더).
		- 유일하게 로스터를 아는 곳이다.
		- `WeaponSlot`의 이벤트 3개(`WeaponEquipped`/`WeaponBecameUnarmed`/`WeaponDestroyed`)와 `HealthComponent.OnDeath`를 구독해 측정값을 보고하고, `OnMetricReported`를 구독해 '이 metric을 쓰는 항목 중 threshold를 넘겼는데 아직 안 열린 것'을 찾아 해금한다.
		- 칭호별 분기 코드는 없다.
- **생존시간/맨손시간은 폴링하지 않음**: 두 값 다 '그 구간이 끝날 때까지 계속 늘어나기만 하는' 성질이 있어서, 중간에 매번 확인하지 않고 구간이 끝나는 시점에 최종값 한 번만 확인해도 결과가 같다.
	- 생존시간의 '끝' = 사망(`HealthComponent.OnDeath`), 맨손시간의 '끝' = 무기 재장착 또는 맨손인 채 사망.
	- 둘 다 원래 있는 이벤트라 별도의 주기적 확인(폴링)이 필요 없다.
- **`WeaponSlot.cs` 수정**(구현 중 발견한 버그): `WeaponBecameUnarmed` 이벤트를 `DropCurrentWeapon()`이 아니라, 수동 드롭과 내구도 소진 두 경로가 공유하는 `ClearCurrentWeaponState()`에서 발행하도록 위치를 옮겼다.
	- 안 그러면 무기가 파괴돼 맨손이 되는 경로에서 '맨손 연속 1분' 타이머가 안 켜지는 틈이 있었다.
- **`AchievementTracker`의 맨손 시간 추적 필드**: `isUnarmed`(bool) + 시작 시각(float) 두 필드 대신 `float? unarmedSecondsStart` 하나로 통일했다.
	- `null`이면 무장 상태, 값이 있으면 그 시각부터 맨손이라는 뜻이다.
	- 두 필드가 항상 같이 움직여야 한다는 규칙을 별개 필드 두 개로 표현하면 코드에 안 드러나는데, nullable 값 하나로 합치면 타입 자체가 그 규칙을 강제한다.
	- 필드 이름도 `AchievementMetric.UnarmedSeconds`와 바로 연결되도록 `unarmedSecondsStart`로 지었다(사용자 피드백: 필드가 왜 존재하는지 메서드 본문까지 안 내려가도 이름만 보고 알 수 있어야 한다).
- **검토 후 채택하지 않은 것**: 어떤 칭호가 이미 해금되면 그 metric 추적(예: 맨손 시간 타이머)을 멈추는 최적화다.
	- 이벤트 빈도가 낮고 로스터도 소규모(현재 8개)라 비용이 사실상 0에 가까운데, 이걸 하려면 '이 metric을 쓰는 칭호가 아직 남아있는가'라는 새 질의 개념을 추가해야 해서 실익 대비 복잡도가 안 맞다고 판단해 보류했다.
- **칭호 8개 최종 데이터**(`Stage1SceneBuilder.BuildAchievementDefinitions`):

| 이름 | 설명 | 측정값 | threshold |
|---|---|---|---|
| 첫 무기 획득 | 무기를 처음으로 손에 넣었다. | WeaponEquipCount | 1 |
| 첫 처치 | 적을 처음으로 쓰러뜨렸다. | EnemyKillCount | 1 |
| 생존자 | 한 판에서 5분 동안 생존했다. | SurvivalSeconds | 300 |
| 학살자 | 누적 100마리를 처치했다. | EnemyKillCount | 100 |
| 명품 | 컨디션이 매우 좋은 무기를 처음으로 손에 넣었다. | WeaponConditionTier | 4(VeryGood) |
| 소모품 | 무기를 누적 10회 파괴했다. | WeaponDestroyedCount | 10 |
| 맨몸 파이터 | 맨손으로 1분 동안 연속 생존했다. | UnarmedSeconds | 60 |
| 단골 | 누적 10판을 플레이했다. | RunPlayedCount | 10 |
- **현재 상태(2026-08-01)**: 코드 구현을 완료했다(`Achievements/` 폴더 5개 파일, `Editor/AchievementRosterEditor.cs`, `WeaponSlot.cs`/`EnemyXPReward.cs`/`Stage1SceneBuilder.cs` 수정).
	- **Unity 에디터 Play 테스트로 8개 조건을 실제로 검증하는 작업은 사용자 결정으로 스킵했다**.
	- 코드는 미검증 상태로 남아있음을 인지하고 다음 단계로 진행한다.

---

### 2.12 칭호: AchievementSystem/AchievementTracker 책임 재분배

> 일자: 2026-08-02

`ReportMetric`/`OnMetricReported` 네이밍을 검토하다가, 'Tracker가 System에 값을 보고하면 System이 이벤트로 다시 Tracker를 불러 판정하는' 왕복 구조가 사실상 자기 자신을 부르는 것과 다름없다는 문제가 드러나 재설계했다(구독자와 호출자가 둘 다 `AchievementTracker` 하나뿐이었다). 위 '칭호. SO 기반 구현으로 재설계'의 '판정 로직' 절을 아래 내용으로 대체한다(코드 반영 완료).

- **로스터 소유권을 `AchievementTracker` → `AchievementSystem`으로 이전**: `AchievementSystem`이 씬에 유일하게 존재하는 MonoBehaviour 싱글턴(`Instance` 패턴)이 되어 로스터를 직접 들고, `IncrementMetric`/`UpdateMetric` 내부에서 저장 직후 곧바로 threshold 판정(private `CheckThreshold`)까지 처리한다.
	- `OnMetricReported` 이벤트는 완전히 삭제했다.
	- System→Tracker 방향 통신 자체가 없어졌다(Tracker→System 단방향 호출만 남는다).
- **`AchievementTracker`는 그대로 유지**: '이 게임에서 어떤 이벤트가 일어났는지'(무기 장착/파괴/맨손 전환, 사망)를 감지해 `AchievementSystem`의 정적 메서드를 호출만 하는 역할로 축소됐다.
	- 로스터도 판정 로직도 모른다.
	- `AchievementSystem`도 여전히 `WeaponSlot`/`HealthComponent`가 뭔지 몰라도 된다.
	- 원래의 분리 자체는 유지하되, 그 분리 기준이 '로스터를 Tracker가 갖는다'에서 '게임 이벤트 구독을 Tracker가 갖는다'로 재정의됐다.
- **공개 API는 계속 static 유지**: `AchievementSystem`이 MonoBehaviour가 됐어도 `IncrementMetric`/`UpdateMetric`/`IsUnlocked`/`GetCounter`/`GetMetricBest`는 전부 `public static`으로 노출한다.
	- 호출부(`EnemyXPReward`, `DebugOverlay` 등)가 참조를 들고 있지 않아도 되게 하기 위해서다(내부적으로 `Instance?.CheckThreshold(...)`에 위임한다).
	- 이 형태가 네이밍 컨벤션 4분류(위 '네이밍 컨벤션. 4분류로 재정의' 참고)의 'static 접근 가능 + 씬 실체화' 칸('System')에 정확히 해당해, 개명 없이 `AchievementSystem` 이름을 그대로 유지한다.
- **`ReportMetric` → `UpdateMetric` 개명 + 의미 변경**: 기존 `ReportMetric`은 저장 없이 값을 그냥 이벤트로 흘려보내기만 해서 'Report'라는 이름과 실제 동작(아무 상태도 안 바꿈)이 애매하게 겹친다는 지적이 나왔다.
	- 이제 `UpdateMetric`은 `SaveSystem`에 '이 metric의 역대 최고값'(신규 키 프리픽스 `achievement_best_`)을 저장해두고, 새로 들어온 값이 그보다 클 때만 갱신 + 판정을 진행한다.
		- 진짜로 저장된 값을 갱신하므로 이름이 정확해졌다.
	- 목적은 칭호 화면에서 누적형과 동일하게 순간값(생존시간 등)도 '현재값/목표값'으로 보여줄 수 있게 하기 위해서다(예: '역대 최고 생존시간 3분 / 목표 5분').
	- 이 진행도 UI 자체는 4단계 몫이라 아직 안 만들었고, 데이터만 `SaveSystem`에 쌓이도록 조회 전용 `GetMetricBest(metric)`만 추가해두었다.
- **해금 알림 이벤트는 아직 만들지 않았다**: 칭호가 해금됐을 때 UI에 알리는 용도의 이벤트(`OnAchievementUnlocked` 등)가 필요해질 수 있으나, 지금은 그런 UI 자체가 없어 만들지 않는다.
	- 4단계에서 필요해지면 그때 추가한다.
- `DebugOverlay.cs`에 순간값 3종(`SurvivalSeconds`/`UnarmedSeconds`/`WeaponConditionTier`)의 `GetMetricBest` 표시 줄을 추가했다.
	- `Stage1SceneBuilder.BuildAchievements`가 `AchievementSystem`/`AchievementTracker` 두 GameObject를 각각 생성하도록 변경했다(`CleanupPreviousBuild`에도 `AchievementSystem` 정리 라인 추가).
- **현재 상태(2026-08-02)**: 코드 반영을 완료했다.
	- Unity 에디터 컴파일/Play 테스트는 아직 안 했다(다음 세션 우선 확인 대상).

---

### 2.13 무기 도감: 최소 구조

> 최초 설계: 2026-07-30
> 재확정: 2026-08-01

- **최종 확정(2026-08-01)**: 도감 슬롯은 조합이 아니라 **고유한 무기 정의(`WeaponDefinition`) 하나당 하나**다.
	- 컨디션은 런 내에서만 유효한 휘발성 값이라 도감에 기록하지 않는다(등급은 무기 정의에 고정된 속성이라 슬롯에 함께 표시되지만, 슬롯을 나누는 축은 아니다. 슬롯 자체가 무기 정의 단위).
	- 현재 로스터는 맨손을 포함해 3종이므로 도감도 3칸(맨주먹/방망이/평범한 권총, 전부 등급: 일반)이다.
	- 해금 조건은 기존과 동일하게 '필드에서 실제로 획득(장착)'이며, `WeaponSlot.Equip()` 시점에 해당 무기 정의를 최초로 만났는지만 보고 컨디션 값은 판정에 관여하지 않는다.
- **확장 가능한 구조**: 하드코딩된 조합표(enum × enum)가 아니라 무기 정의 로스터를 그대로 스캔/참조하는 방식으로 구현한다.
	- 나중에 새 등급/새 무기가 `WeaponDefinition` 애셋으로 추가되면, 로스터 목록에 그 애셋을 등록하는 것만으로 도감 슬롯이 자동으로 늘어난다(칭호 8개처럼 조건마다 코드를 새로 써야 하는 구조가 아님).
- **경위(2026-07-30 → 2026-07-31 → 2026-08-01)**: 최초 설계는 '무기 종류 × 등급' 8칸이었으나, '등급도 필드 조우 확률로 결정된다'는(잘못된) 전제로 만들어진 것이었다.
	- 이후 등급이 무기 종류에 고정된 값으로 바로잡히며 이 전제가 깨졌다(§2.4 참고. 현재 로스터 2종 모두 일반 등급이라 8칸 중 최대 2칸만 도달 가능).
	- 중간에 '무기 종류 × 컨디션(10칸)' 안이 잠깐 검토됐으나, 컨디션이 런 한정 값이라 영구 기록인 도감과 어울리지 않는다는 사용자 판단으로 폐기되고, 2026-08-01 최종적으로 '무기 정의 단위' 안으로 확정됐다.
	- 등급 축(4단계) 자체를 도감 분류에 쓰는 안은 채택하지 않는다(`GDD.md` 11장 12번 참고).
	- 재검토 후보는 `Backlog.md` #7 참고.
- **맨손의 '항상 발견됨' 처리(2026-08-01 확정)**: 맨손은 `WeaponPickup`/`WeaponSlot.Equip()`을 절대 거치지 않아 SaveSystem 기반 발견 기록 대상이 될 수 없다(한 번도 플레이 안 해도 도감엔 이미 맨손이 등록돼 있어야 한다는 요구와 충돌).
	- SaveSystem에 쓰지 않고 `WeaponRoster`/`WeaponCollectionSystem`에 '예외 목록'을 별도로 두지도 않는 대신, `WeaponDefinition.hasCondition`과 완전히 같은 패턴으로 `WeaponDefinition.startsDiscovered`(bool, 기본 false) 필드를 추가해 등록 시점(`Stage1SceneBuilder`가 `CreateWeaponDefinition` 호출 시 맨손에만 `startsDiscovered: true` 전달)에 결정한다.
	- `WeaponRoster.IsDiscovered()`가 `definition.StartsDiscovered || WeaponCollectionSystem.IsDiscovered(definition)`로 흡수하므로 `WeaponCollectionSystem`은 맨손의 존재를 몰라도 된다.
	- 실용적 선택으로 확정한다.
		- '오직 맨손만을 위한 필드'라는 지적은 유효하나 `hasCondition`도 동일한 모양(맨손만 예외)이라 이미 받아들여진 패턴과 일관된다고 판단했다.
- **`hasCondition`/`startsDiscovered` 구조 정리는 의도적으로 미룸(2026-08-01)**: `WeaponDefinition.hasCondition`도 `startsDiscovered`와 동일한 문제(사실상 맨손만을 위한 필드)를 안고 있다.
	- 사용자가 이 문제를 인지한 채로, 3.4의 나머지 작업량과 우선순위를 고려해 지금 당장 손대지 않고 미뤘다.
	- 구조를 몰라서 넘어간 게 아니라는 점만 남겨둔다.
	- 재검토 후보는 `Backlog.md` #2 참고.

---

### 2.14 칭호/도감 뷰(화면) 배치: 3단계는 로직·저장까지만

> 일자: 2026-07-30

칭호/도감을 실제로 확인할 진입 지점(메인 메뉴 등)은 아직 없다. `DevPlan.md` 4단계 '재시작/메뉴 화면'에서야 생긴다. 3단계에서 임시 화면을 만들면 4단계에서 정식 메뉴가 생길 때 다시 걷어내고 통합해야 하는 이중 작업이 생기므로, **3단계는 조건 판정 로직과 SaveSystem 저장까지만 구현하고 화면은 아예 만들지 않는다.** 이번 라운드의 동작 검증은 `Debug.Log` 또는 Play 모드 Inspector 값 확인으로 대신한다. 칭호 목록/도감 그리드를 실제로 보여주는 화면(그리고 그 진입 지점을 메인 메뉴로 할지 등)은 4단계에서 정의한다.

**대신 개발자 전용 디버그 오버레이를 별도로 둔다(2026-07-30, 일회용)**. 플레이어 대상 뷰가 없어 테스트 주기가 길어지는 문제를 보완하기 위함이며, 4단계 정식 메뉴로 이어지는 게 아니라 순수 일회용 도구라 위 '이중 작업 방지' 논리와 충돌하지 않는다. `UNITY_EDITOR`/`Debug.isDebugBuild` 조건으로 감싸 실빌드에는 노출되지 않게 하고, 구식 `OnGUI` 기반 텍스트+버튼 정도로 충분하다(폴리싱 불필요). 칭호 8개/도감 8칸 상태와 원본 카운터를 표시하고, 누적형 조건(#4, #6, #8)을 그라인딩 없이 바로 테스트할 수 있도록 카운터를 강제로 증가시키는 치트 버튼을 포함한다.

---

### 2.15 이후 단계 재검토 후보

> 일자: 2026-07-30

- **신규 무기 서브타입**: GDD 5.1의 중량형 근접/강격형 원거리 등 도입은 메타 해금과의 연결고리가 사라지며 3단계 범위에서 빠졌다.
	- 도입 여부는 아직 미결정이다.
	- 재검토 후보는 `Backlog.md` #5 참고.
- **난이도 스스로 올리기(Dead Cells Boss Cell식)**: 파워 대신 난이도가 성장하는 방식이다.
	- 3단계 재설계 논의 중 검토됐으나 이번 라운드엔 미채택이다.
	- 재검토 후보는 `Backlog.md` #6 참고.
- **`AchievementTracker._unarmedStreakStart` 일반화 여부 (2026-08-02)**: 지금은 '맨손 연속 유지 시간'이라는 단일 스트릭만 이 필드로 추적한다.
	- 필드 하나가 다른 상태 없이 단독으로 있어서 '왜 여기 있는지' 파악하려면 `Start`/`HandleWeaponEquipped`/`HandleWeaponBecameUnarmed`/`HandleRunEnded` 네 곳을 다 훑어야 한다는 지적이 나옴.
	- 만약 '특정 무기로 n분 연속 버티기'류 칭호(예: '나뭇가지로 5분 버티기')가 실제로 채택되면 이 타이머를 일반화할 유인이 생기는데, 진짜 병목은 타이머 구조가 아니라 `AchievementMetric`/`AchievementDefinition`이 '어떤 무기가 대상인지'를 표현 못 한다는 점이다.
	- `AchievementMetric`은 칭호가 늘어도 안 늘어나야 하는 작고 안정적인 목록으로 설계됐는데(위 '칭호. SO 기반 구현으로 재설계' 참고), 무기별로 metric을 늘리면 그 원칙이 깨진다.
	- 그래서 타이머 일반화는 이런 칭호가 실제로 채택되고 `AchievementDefinition`이 '대상 무기'를 참조하는 방식까지 함께 정해지는 시점에 같이 검토하기로 한다.
	- 스키마 결정 없이 타이머만 먼저 일반화하면 나중에 다시 뜯어야 할 가능성이 크다고 판단해 의도적으로 보류했다.
	- 최소 비용 대안(참고용, 아직 적용 안 함): 필드를 그대로 두되 `BeginUnarmedStreak()`/`EndUnarmedStreak()` 같은 이름 있는 메서드 쌍으로 감싸서 '왜 있는지'만 즉시 보이게 하는 방법도 있다.
- **`PlayerAugmentSystem`을 아는 소비자가 많은 것에 대한 결합도 우려 (2026-08-03)**: `WeaponBase`/`PlayerMovement`/`HealthComponent`/`AugmentSelectionController`가 전부 `PlayerAugmentSystem`을 알고 있어 불안하다는 지적이 나옴.
	- 검토 결과 대부분 기능 자체의 본질로 판단했다.
	- 증강이 '전역 스탯 적용'으로 설계된 이상(위 '증강 효과 적용 구조' 참고), 그 스탯을 쓰는 코드가 전부 이 클래스를 아는 건 필연.
	- 다만 '안다'의 결이 다르다: `WeaponBase`/`PlayerMovement`는 `GetMultiplierSafe(StatType)` 정적 호출 한 줄뿐이라 `roster`/`instances`/`Awake` 타이밍 같은 내부 구현은 몰라도 되는 얕은 결합이고, 참조를 직접 드는 곳은 `HealthComponent`(적과 공유해 static에 못 기대는 예외, 코드 주석에 이유 명시)와 `AugmentSelectionController`(`GetEligibleCandidates`/`LevelUp`처럼 static으로 못 만드는 진짜 인스턴스 동작 필요) 단 2곳뿐이라 이미 최소화된 상태로 판단했다.
	- 인터페이스 추상화(`IStatSource` 등)로 더 줄이는 안도 검토했으나, 이 프로젝트엔 테스트 어셈블리도 없고 구현체가 여러 개 될 계획도 없어 지금 도입하면 득 없는 간접화(`CLAUDE.md`의 '가상의 미래 요구사항을 위해 설계하지 않는다' 원칙에 반한다).
	- **결론(당시): 현재 상태 유지, 조치 없다.**
	- **갱신(같은 날, 더 큰 논의 끝에): 해결됐다.**
	- 아래 '증강 배율 적용 구조 개편' 항목에서 실제로 구현까지 완료했다.
	- `WeaponBase`/`PlayerMovement`/`HealthComponent`가 `PlayerAugmentSystem`을 아예 모르게 되어, 이 타입을 아는 파일이 5개에서 2개(`AugmentSelectionController`/`Stage1SceneBuilder`)로 줄었다.
	- 인터페이스 추상화보다 이 방향이 나은 이유: 가상의 미래 요구사항이 아니라 지금 이미 있는 결합도 문제를 실제로 줄였기 때문.

---

### 2.16 증강 배율 적용 구조 개편

> 일자: 2026-08-03
> 비고: 같은 날 구현까지 완료

`PlayerAugmentSystem`을 아는 소비자가 많다는 위 우려에서 출발해, '핵심 시스템(무기/이동/체력)이 부가 시스템(증강)을 아예 모르게 하자'는 방향으로 설계를 확정하고 같은 날 구현까지 완료했다(원래는 컴파일 검증 전이라 다음 라운드로 미루려 했으나, 이후 사용자가 바로 진행을 요청해 이어서 구현했다. 아직 Unity 컴파일/플레이 확인은 안 된 상태이므로 다음에 에디터 켤 때 반드시 확인해야 한다).

**핵심 방향**: pull(무기/이동/체력이 필요할 때마다 `PlayerAugmentSystem`에 물어봄) → push(무기/이동/체력이 배율을 자기 내부에 들고 노출만 하고, `PlayerAugmentSystem`이 레벨업 시 값을 밀어넣어 줌)로 전환했다.

- **`HealthComponent`**: `MaxHealthMultiplier`를 보관한다(기본값 반드시 `1f`. `0f`면 최대체력이 0이 되는 실버그이므로 주의).
	- 의미 있는 이름의 메서드로 외부에서 갱신한다(단순 public setter 아님. `WeaponDurability.Consume()`/`PlayerAugmentSystem.LevelUp()`과 동일한 관례).
	- `PlayerAugmentSystem` 참조·`using WeaponMaster.Augments`를 완전히 제거했다.
- **`PlayerMovement`**: `SpeedMultiplier`를 보관한다.
	- 동일한 패턴이다.
	- `PlayerAugmentSystem` 참조를 제거했다.
- **`WeaponSlot`**: `attackDamageMultiplier`/`attackSpeedMultiplier` 딱 2개만 보관한다(쿨타임 배율은 별도로 존재하지 않는다. 공격속도 배율을 쿨다운 계산식에 나눗셈으로 적용하는 것뿐, `WeaponBase.EffectiveCooldown` 참고).
	- **왜 무기 인스턴스(`WeaponBase`)가 아니라 `WeaponSlot`에 두는지**: 장착된 무기 인스턴스는 교체될 때마다 바뀌지만 `WeaponSlot`은 Player에 고정돼 안 바뀌는 대상이라, 레벨업마다 '지금 장착된 무기가 뭔지 찾아서 다시 밀어넣기'가 필요 없어졌다.
	- 무기가 몇 번을 바뀌든 `WeaponSlot`에 한 번 저장해두면 끝이다.
- **`IWeapon.TryAttack`/`WeaponBase.TryAttack` 시그니처에 배율 파라미터를 추가했다**(대안: `WeaponBase`가 `WeaponSlot`을 직접 참조하는 방식도 검토했으나 기각. 무기가 자기 컨테이너를 위로 참조하는 방향이라 지금 없애려는 결합과 같은 냄새가 남, 성능도 파라미터 전달이 더 저렴함을 확인했다).
	- `IWeapon` 구현체가 `WeaponBase` 하나·호출부가 `WeaponSlot.Update()` 하나뿐이라 실제 변경 범위는 작다(`IWeapon.cs`/`WeaponBase.cs`/`WeaponSlot.cs` 3개).
	- `WeaponBase`는 이제 '기본 스탯 + 외부에서 받은 배율 → 최종 값' 계산만 하는 순수 계산기가 되어 `WeaponSlot`의 존재도 몰라도 된다.
- **밀어넣기 로직은 별도 클래스로 빼지 않았다**: 애초에 분리를 고려했던 이유(`WeaponEquipped` 이벤트 구독까지 필요해서 `AchievementTracker`급 오케스트레이션이 될 것 같았음)가, 배율을 `WeaponSlot`에 저장하기로 하면서 사라졌다.
	- 트리거가 `LevelUp()` 하나뿐이라, `PlayerAugmentSystem.LevelUp()` 안에서 해당 StatType에 대응하는 대상(`weaponSlot`/`healthComponent`/`playerMovement`, 3개 직렬화 참조로 보관)에 새 값을 바로 호출해주는 정도로 충분하다.
	- `AchievementSystem`/`AchievementTracker`를 쪼갠 것과는 달리 여기선 '여러 이질적 게임 이벤트를 번역'하는 책임이 없어서(트리거가 자기 자신의 메서드 하나뿐) 분리할 근거가 약하다.
- **구현 결과(계획과 달라진 점)**: `WeaponSlot`의 세터는 계획 문서엔 `SetAugmentMultipliers(damage, speed)` 하나로 적었으나, 실제로는 `SetAttackDamageMultiplier`/`SetAttackSpeedMultiplier` 2개로 분리 구현했다(`LevelUp()`이 한 번에 StatType 하나만 바꾸는데 굳이 안 바뀐 값까지 매번 재조회해서 같이 넘길 이유가 없어서 더 단순한 쪽을 선택).
	- `PlayerAugmentSystem.GetMultiplierSafe`는 실제로 호출부가 완전히 사라져(grep으로 확인) 제거를 완료했다.
- **완료 후 상태**: `PlayerAugmentSystem`을 아는 파일이 실제로 5개(`WeaponBase`/`PlayerMovement`/`HealthComponent`/`AugmentSelectionController`/`Stage1SceneBuilder`)에서 2개(`AugmentSelectionController`/`Stage1SceneBuilder`)로 줄었다.
	- 위 '결합도 우려' 항목 **해결됐다**.
	- `PlayerAugmentSystem._augmentInstancesByStat`의 'Awake 아니라 필드 초기화 시점' 타이밍 주석은 이제 실질적으로 불필요해졌으나(씬 로드 중 파고드는 소비자가 없어졌다. `AugmentSelectionController`도 런타임 이벤트로만 호출), 아직 손대지 않았다.
	- 배열을 다시 `Awake()` 안에서 만들도록 되돌리고 주석을 정리하는 건 사소한 후속 정리로 남겨둔다(정확성엔 영향 없음, 지금 방식도 여전히 안전하게 동작).

---

### 2.17 `PlayerAugmentSystem._augmentInstancesByStat` 정리

> 일자: 2026-08-03

- 필드명 `instances` → `_augmentInstances` → `_augmentInstancesByStat`로 최종 확정했다(StatType 인덱싱 구조라는 게 이름에서 드러나도록).
	- `Instance`/`instances` 시각적 충돌 문제(Q1)는 이걸로 해결했다.
- 'Dictionary 대신 배열' 주석과 'Awake 아니라 필드 초기화 시점' 주석은 둘 다 유지하기로 재확정했다.
	- 전자는 '왜 이렇게 짰지?'라는 인지 과정 자체를 매번 없애주는 게 코드 가독성에 낫다는 판단이다.
	- 후자는 실제로 한 번 터진 프로덕션 버그(HealthBarUI.OnEnable → HealthComponent.MaxHealth)의 재발 방지 문서라 제거 시 정보 손실이 크다.
- `Awake()`에 'StatType당 AugmentDefinition은 정확히 하나'라는 전제 위반 감지용 `Debug.LogWarning`을 추가했다(같은 슬롯에 덮어쓰기 발생 시 경고).
	- 에디터 시점 검증(`AugmentRosterEditor` 쪽)은 지금은 생략한다.
	- 런타임 경고 하나로 충분하다고 판단했으며, 필요해지면 추가로 검토한다.
- 위 경고 로직이 `Awake()`를 '싱글턴 등록'과 'roster 순회 채우기'라는 결이 다른 두 일로 섞어버려서, 후자를 `PopulateInstancesFromRoster()`로 분리했다(`AchievementSystem.Awake()`가 `Instance = this;` 한 줄뿐인 것과 동일한 모양으로 맞춤).
	- 필드 주석도 'Stat 기반 증강 전용' 문장 제거(필드명 `ByStat`이 이미 그 뜻을 담고 있어 중복 판단) 등으로 압축했다.

---

### 2.18 공격속도: GDD 정의(초당 공격 횟수)와 내부 표현(쿨다운 초)이 다른 이유

> 일자: 2026-08-03

`GDD.md` 5.1(표)은 공격속도를 **'초당 공격 횟수'**로 정의한다. 반면 코드(`WeaponDefinition.cooldown`, `WeaponBase.EffectiveCooldown`)는 정반대로 **'다음 공격까지 걸리는 시간(초)'**을 저장·계산한다. 예: `cooldown = 1.34`는 '1.34초에 한 번 공격'(초당 약 0.746회)이라는 뜻이지 '초당 1.34회'가 아니다.

- **왜 모순이 아닌가**: 속도(횟수/초) = 1 / 시간(초)이므로 두 표현은 수학적으로 동치다.
	- 증강 배율 적용도 이 관계를 그대로 따른다.
	- `EffectiveCooldown = definition.Cooldown / attackSpeedMultiplier`(곱셈이 아니라 나눗셈).
	- '쿨다운을 배율만큼 곱해 줄이는' 방식(예: ×0.9)과 '쿨다운을 배율로 나누는' 방식(÷1.1)은 근사치일 뿐 정확히 같지 않은데, GDD가 정의한 '초당 횟수 × 배율'과 정확히 일치하는 쪽은 나눗셈이다.
	- 즉 내부 구현이 나눗셈을 쓰는 것 자체가 GDD 정의를 정확히 만족시키기 위한 선택이다.
- **왜 내부 저장 단위를 굳이 '초'로 뒤집어서 쓰는가(사용자 확인 완료)**: 실제 공격 루프가 `Time.time < nextAttackTime` 같은 '다음 시각까지 대기' 패턴이라 초 단위가 코드와 자연스럽게 맞아떨어진다.
	- 만약 필드 자체를 '초당 횟수'로 저장하면 매 공격마다 `1f / attacksPerSecond`로 변환하는 불필요한 단계가 매번 추가된다.
	- 기획자가 인스펙터에서 값을 입력할 때도 '이 무기는 0.5초마다 한 번 휘두른다'가 '초당 2회 휘두른다'보다 감이 잡히는 경우가 많다.
- **주의(향후 스탯 UI 구현 시)**: 플레이어에게 '공격속도' 수치를 화면에 노출하게 되면, 내부 `cooldown` 값을 그대로 보여주면 안 되고 GDD 정의(초당 횟수)대로 `1f / cooldown`으로 변환해서 표시해야 한다.
	- 지금은 그런 UI가 없어 실제로 틀릴 여지는 없지만, 나중에 놓치기 쉬운 지점이라 기록해 둔다.

---

### 2.19 증강 시스템: 식별 키를 StatType에서 AugmentDefinition으로 전환 + 카테고리 기반 효과 적용

> 일자: 2026-08-03

기존 `PlayerAugmentManager`(당시 `PlayerAugmentSystem`)는 `_augmentInstancesByStat`(StatType 개수만큼의 고정 배열)로 증강을 관리했다. 이 방식은 'StatType 하나 = 증강 하나'라는 1:1 결합을 구조적으로 강제한다. 새 스탯이 생기면 반드시 대응 증강을 만들어야 하고, 스탯과 무관한 증강(유니크 효과 등)은 이 구조에 아예 들어올 수 없어 별도 시스템으로 관리해야 한다. 코드를 훑어보던 중 사용자가 이 결합을 지적하며 재설계가 논의됐다. **가상의 미래 대비가 아니라 `DevPlan.md` 4단계에 이미 예정된 '무기 특화 증강'이 이 구조에 안 맞아서 나온 문제**임을 확인하고 재설계에 착수했다.

**근본 원인**: `StatType`이 두 역할을 겸하고 있었다. (1) 증강을 찾는 식별 키, (2) 효과를 어디에 적용할지 정하는 디스패치 키다. 이 둘을 하나의 enum으로 묶은 것이 1:1 결합의 원인이다.

결정된 구조는 다음과 같다.
- **식별 키를 `AugmentDefinition` 자체로 교체했다**.
	- `WeaponDefinition`/`AchievementDefinition`과 동일한 패턴이다.
	- `PlayerAugmentManager`는 `Dictionary<AugmentDefinition, AugmentInstance>`(선택된 것만 담는 sparse 구조)로 관리하며, Awake 시점의 전체 roster 선(先)채움(`PopulateInstancesFromRoster`)은 완전히 제거했다.
	- 이제 `LevelUp()`이 처음 호출될 때 그 자리에서 인스턴스를 생성한다.
- **효과 적용은 `AugmentCategory` enum + switch로 분기한다**.
	- 상속 기반(전략 패턴)도 검토했으나 기각했다.
	- 근거: 이 프로젝트는 `MeleeWeapon`/`RangedWeapon`처럼 '동작 자체가 복잡하게 갈리는' 경우에만 상속을 써왔는데, 증강 효과 적용은 '배율 하나를 어느 세터에 꽂을지' 수준이라 상속의 이점(복잡한 동작 캡슐화)이 크지 않다.
	- 카테고리도 GDD상 증강을 '캐주얼하고 cross-run 가중치 낮게' 유지한다는 설계 의도상 한없이 늘어날 가능성이 낮다(스탯형 + 무기특화형, 많아야 2~3개 선).
	- **재검토 기준을 명시적으로 남긴다: 카테고리가 3~4개를 넘거나 카테고리별 전용 필드가 쌓여 `AugmentDefinition`이 지저분해지기 시작하면 그때 상속으로 전환을 검토한다.**
- **같은 StatType을 겨냥하는 StatBoost 증강이 여러 개일 수 있다는 전제로, 배율을 곱해서 합산한 뒤 푸시한다**.
	- 1:1 결합을 풀면서 생기는 자연스러운 귀결이다.
	- 지금은 스탯 4개 각각에 증강이 1개뿐이라 동작상 차이는 없지만(합산 대상이 하나뿐이므로), 나중에 같은 스탯을 겨냥하는 증강이 늘어나도 코드 변경 없이 정확하게 동작하도록 미리 반영했다.
- **Roster 소유권은 `PlayerAugmentManager` → `AugmentSelectionManager`로 이전했다**.
	- '선택 UI가 전체 카탈로그를 아는 게 자연스럽다'는 사용자 직관을 반영했다.
	- `PlayerAugmentManager`는 신규 조회 메서드 `GetCurrentLevel(AugmentDefinition)` 하나만 노출하고, `AugmentSelectionManager`가 후보 산출 시(`ShowPopup()`) roster를 순회하며 매번 이 메서드로 현재 레벨을 물어본다.
	- **'소스가 2개 아닌가' 우려에 대한 결론**: 아니다.
		- 진짜 상태(선택 여부+레벨)의 소스는 `PlayerAugmentManager`의 딕셔너리 하나뿐이고, `AugmentSelectionManager`는 그 값을 캐싱 없이 팝업 한 번 그릴 때마다 다시 조회할 뿐이라 어긋날 여지가 없다.
		- 이건 이미 이 코드베이스에 있는 `AchievementSystem.IsUnlocked`/`WeaponRoster.IsDiscovered`와 동일한 '밖에서 물어보면 사실을 알려주는' 조회 패턴이다.
- **`AugmentPopupView`가 `AugmentInstance`/`WeaponMaster.Augments`를 더 이상 몰라도 된다**.
	- 표시용 DTO `AugmentPopupOption`(UI 네임스페이스 소속, DisplayName+Level만 담음)을 신규 도입해 `AugmentSelectionManager`가 조립해서 넘겨준다.
	- 뷰가 도메인 타입을 아예 몰라도 되는 방향으로 결합이 한 번 더 줄어든다.

**영향받는 파일**:
- `AugmentCategory.cs`(신규)
- `AugmentDefinition.cs`(category 필드 추가)
- `PlayerAugmentManager.cs`(딕셔너리 전환, `LevelUp(StatType)`→`LevelUp(AugmentDefinition)`, roster 필드 제거)
- `AugmentSelectionManager.cs`(roster 필드 추가, 후보 산출 로직 변경)
- `AugmentPopupView.cs`+`AugmentPopupOption.cs`(신규)
- `Stage1SceneBuilder.cs`(배선 갱신)

**후속: 레벨업 완전 회복을 `AugmentSelectionManager`에서 분리했다(같은 날)**. GDD 7.1의 '레벨업하면 완전 회복'은 팝업 UI 흐름과 무관한 별개의 반응인데, `PlayerXP.OnLevelUp`을 구독하는 클래스가 `AugmentSelectionManager`뿐이라 회복 호출(`FullHeal()`)이 그 안 두 곳(후보 0개일 때, 선택 확정 시)에 흩어져 있던 것을 발견해 분리했다. `HealthComponent`가 직접 구독하지 않는 이유는 기존 증강 배율 개편과 동일하다(적과 공유하는 컴포넌트라 `PlayerXP`를 알면 안 된다). 대신 `Assets/Scripts/Player/LevelUpFullHeal.cs`(신규, `EnemyXPReward`/`DamagePopupSpawner`와 동일한 '이벤트 하나 → 반응 하나' 패턴)를 만들어 `playerXP`/`playerHealth` 참조만 들고 독립적으로 구독하게 했다. 부수 효과로 `AugmentSelectionManager`는 `playerHealth` 필드가 완전히 사라졌다(5개 → 4개 필드). 밸런스 조정 시 이 컴포넌트 하나만 떼거나 교체하면 되는 것도 이점이다.

---

### 2.20 `RunStats` → `RunRecordManager`(`RunStatsUI` → `RunRecordUI`) 개명 + static `Instance` 제거

> 일자: 2026-08-04

리팩터링 검토 중 사용자 지적으로 반영. 두 단계로 개명했다:

1. 1차로 `RunStatsTracker`를 검토했으나(매 프레임 능동적으로 값을 갱신하고 사망 시 `SaveSystem`에 저장까지 하는데 이름이 수동적인 데이터 홀더처럼 읽힌다는 문제 제기, `WeaponDurability`가 정반대 이유로 `~Tracker`에서 벗어난 선례와 대비됨), 곧이어 'Stat'이라는 어휘가 이미 `StatType`(공격력/공격속도/이동속도/최대체력. 증강 대상 스탯)으로 이 프로젝트에서 선점돼 있어 한 어휘가 두 뜻으로 겹친다는 추가 지적으로 `RunRecordManager`로 최종 확정했다. 'Record'는 개발 노트가 이미 이 데이터를 부르던 말('런 결과')과도 자연스럽게 맞고, static `Instance`를 걷어낸 뒤라 `~Manager`(static 접근 불가 + 씬 컴포넌트로 실체화)라는 접미사도 §3단계 네이밍 컨벤션 4분류 표와 정확히 맞아떨어졌다.
2. 짝인 `RunStatsUI`도 같은 이유로 `RunRecordUI`로 함께 개명했다(사용자 확인).

**static `Instance`도 함께 제거했다**. 위 '씬 전용 오브젝트는 직렬화 필드 대신 static `Instance` 접근'에 정리된 근본 원인(`EnemyXPReward`가 `Enemy_Basic` **프리팹**에 살아서 씬 전용 오브젝트를 직렬화 참조로 못 들고 있음)을 다시 보니, `EnemyXPReward`가 실제로 프리팹에서 건드리는 씬 싱글턴은 `EnemySpawner`(풀 반납 때문에 어차피 static이어야 함) 하나뿐이었다. 옛 `RunStats.Instance?.AddKill()` 호출은 프리팹 제약과 무관하게 그냥 같은 관례를 답습한 것이다.

- `EnemySpawner`에 `public event Action OnEnemyKilled`를 신설했다.
	- `ReturnEnemy()`(현재 유일한 호출부가 사망 시에만 호출)에서 함께 발행한다.
	- '씬의 모든 적을 관장하는 컴포넌트'가 이미 프리팹↔씬 경계를 넘나드는 유일한 지점이므로, 그 경계를 넘는 통보도 여기 한 곳으로 모았다.
- `EnemyXPReward.HandleDeath()`는 이제 별도 호출 없이 기존 `EnemySpawner.Instance?.ReturnEnemy(...)` 호출 하나로 끝난다.
- `RunRecordManager`는 static `Instance`/`Awake`/`OnDestroy`를 전부 제거하고, `[SerializeField] private EnemySpawner enemySpawner`로 씬↔씬 직렬화 참조를 든 뒤 `OnEnable`/`OnDisable`에서 `OnEnemyKilled` 구독/해제한다.
	- `AddKill()`도 외부에서 더 이상 직접 호출하지 않으므로 `public` → `private`로 좁혔다.
- `RunRecordUI`/`AchievementTracker`도 `RunStats.Instance` 대신 `[SerializeField] private RunRecordManager runRecord` 직렬화 참조로 전환했다(둘 다 씬 오브젝트라 정상적인 직렬화 참조가 가능했다. 애초에 static일 이유가 없었다).
- **범위는 `RunRecordManager`/`RunRecordUI`로 한정했다**.
	- `PlayerXP`/`AchievementSystem`/`PlayerAugmentSystem` 등 나머지 static `Instance` 싱글턴은 각자 다른 이유(프리팹에서 직접 참조되거나, 호출부가 참조를 안 들어도 되게 하려는 의도적 설계)가 있어 이번 라운드에서 함께 정리하지 않았다.
	- `Stage1SceneBuilder`의 씬 조립 순서를 `EnemySpawner → RunManager → RunRecordManager(enemySpawner 배선) → UI(runRecord 배선) → Achievements(runRecord 배선)` 순으로 재배치했다.
	- 새로 생긴 직렬화 의존성 때문에 빌드 순서 자체가 의미를 갖게 됐다(기존에는 대부분 순서 무관이었다).

**부수 작업: '~와 동일한 관례' 식 주석 정리**. 위 개명 과정에서 여러 파일의 주석이 `RunStats`를 이름만으로 언급하고 있어(`PlayerXP와 동일한 관례` 류) 개명할 때마다 같이 손봐야 했다. 이 기회에 원칙을 명확히 하자면: 주석에서 다른 클래스 이름을 지우면 문장이 비거나 무의미해지는 '그거랑 똑같음' 식 순수 상호참조(`DamageNumberPool.cs`/`ProjectilePool.cs`/`EnemySpawner.cs`/`PlayerXPUI.cs`/`Stage1SceneBuilder.cs`(XP바 배치부)에서 발견된 사례들)는 삭제한다. static `Instance` 패턴을 쓰는 이유 자체는 이미 위 '씬 전용 오브젝트는...' 절에 한 번만 기록돼 있으므로(2026-08-03 확정) 각 파일에서 반복 언급할 필요가 없고, 이런 이름 언급은 리네이밍 때마다 깨지는 대가만 지불한다(이번 라운드가 실제 사례). 반면 클래스 이름을 지우면 설명 자체가 무너지는 진짜 근거(예: `EnemyXPReward.cs`의 'HealthComponent와 분리해 둔 이유는 HealthComponent가 플레이어에게도 쓰이는데...')는 그대로 유지한다.

---

### 2.21 `SaveSystem` → `SaveHandler`, `WeaponCollectionSystem` → `WeaponCollectionHandler` 개명

> 일자: 2026-08-04

위 `RunStats` 리팩터링 라운드 도중, '여러 곳에서 호출되는 무상태 유틸이니 static은 유지해야 맞지 않냐'는 질문에서 출발해 §3단계 '네이밍 컨벤션. 4분류로 재정의(2026-08-02)'가 남겨뒀던 미확정 항목('`SaveSystem`/`WeaponCollectionSystem`은 표대로면 `Handler`인데 소급 리네이밍할지 미정')을 실제로 해소했다.

- **static을 유지하는 근거가 `RunStats`와는 다르다는 점을 명확히 하면**: `RunStats`(→`RunRecordManager`)의 static `Instance`는 'MonoBehaviour인데 프리팹이 씬 전용 오브젝트를 직렬화 참조로 못 든다'는 유니티 제약을 우회하는 workaround였던 반면, `SaveSystem`/`WeaponCollectionSystem`은 애초에 MonoBehaviour가 아니고 씬에 존재한 적도 없는 순수 `static class`(내부 상태 없이 `PlayerPrefs`/`SaveHandler`만 감싸는 무상태 유틸)라 static이 정공법이다.
	- 두 케이스를 같은 'static 문제'로 뭉뚱그리지 않도록 구분해 기록한다.
- 4분류 표 기준 `~Handler` = static 접근 가능 + 씬 컴포넌트로 실체화 안 된다(순수 static 클래스).
	- `SaveSystem`/`WeaponCollectionSystem` 둘 다 정확히 이 칸이었고 이름만 `~System`을 달고 있었던 상태였다.
	- `SaveHandler`/`WeaponCollectionHandler`로 개명해 실제 구조와 이름을 일치시켰다.
- `WeaponCollectionHandler.cs`의 'static으로만 접근하는 클래스라 ~System 접미사를 쓴다' 주석도 낡은 2분류 시절 근거였으므로 4분류 기준 설명으로 갱신했다.
- **범위**: `AchievementSystem`은 그대로 유지한다.
	- 씬에 `Instance` 싱글턴으로 실체화돼 있어(`AchievementRoster` SO 필드를 인스펙터에서 배선해야 함) `~System` 분류가 정확하다.
	- 소급 리네이밍은 여전히 '자동/일괄 적용은 하지 않고, 리팩터링 라운드에서 사용자가 명시적으로 짚은 케이스에 한해 진행'이라는 원칙을 유지한다.

---

### 2.22 로스터 에디터 3종을 제네릭 베이스로 통합

> 일자: 2026-08-04

`WeaponRosterEditor`/`AchievementRosterEditor`/`AugmentRosterEditor`가 '폴더 다시 스캔' 버튼 로직을 완전히 동일하게(로스터/정의 타입, 스캔 폴더 경로, 직렬화 필드 이름 3가지만 다름) 복붙해 갖고 있던 것을 사용자 제안으로 통합했다. '로스터가 더 늘어날지 불확실하다'는 망설임이 있었지만, 가상의 미래 대비가 아니라 **이미 3개가 완전히 동일한 코드로 존재한다는 사실 자체**가 통합 근거였다(코드 주석에도 이미 'WeaponRosterEditor와 동일한 패턴'이라고 스스로 적어놨을 정도).

- `Assets/Scripts/Editor/RosterInspectors/RosterEditor.cs`를 신설했다.
	- `public abstract class RosterEditor<TRoster, TDefinition> : Editor`(`where TRoster/TDefinition : UnityEngine.Object`, `System`도 `using` 중이라 `System.Object`와 충돌하지 않도록 반드시 `UnityEngine.Object`로 명시 한정해야 한다).
	- `OnInspectorGUI`/`Rescan` 공통 로직을 여기 하나로 모으고, 서브클래스는 `FolderPath`/`FieldName` 두 abstract 프로퍼티만 오버라이드한다.
- **`[CustomEditor]`는 제네릭 베이스가 아니라 각 구체 서브클래스에 달아야 유니티가 인식한다**.
	- 베이스는 어트리뷰트 없이 추상으로만 두고, `WeaponRosterEditor : RosterEditor<WeaponRoster, WeaponDefinition>` 같은 얇은 서브클래스(필드 2개 오버라이드뿐, 10줄 안팎)가 각자 `[CustomEditor(typeof(X))]`를 소유한다.
- 세 서브클래스 합쳐 약 160줄 → 베이스 57줄 + 서브클래스 3×13줄로 축소했다.
	- 스캔 로직 버그가 있었다면 이제 한 곳만 고치면 된다.

---

### 2.23 `EnemyXPReward`/`EnemyAI`/`AchievementTracker`: '적 처치' 반응 재분배 + `EnemySpawner` static `Instance` 제거

> 일자: 2026-08-04

`EnemyXPReward`가 이름과 달리 XP 지급·칭호 카운터 증가·풀 반납 세 가지 반응을 한 메서드에 몰아 갖고 있던 것을 정리했다. `LevelUpFullHeal`/`DamagePopupSpawner`에서 이미 확립한 '이벤트 하나 → 반응 하나, 각자 독립 구독' 원칙을 그대로 따랐다. `AchievementTracker`의 클래스 주석이 스스로 '유일한 담당자'라고 선언하고 있었는데 `EnemyXPReward`가 `AchievementSystem`을 직접 호출해 그 선언을 어기고 있던 것도 이번에 바로잡았다.

- **`EnemyXPReward`**: `PlayerXP.Instance.AddXP(xpValue)` 하나만 남았다.
	- `AchievementSystem`/`EnemySpawner`/`EnemyAI`를 더 이상 모른다(`using WeaponMaster.Achievements;` 제거).
- **`EnemyAI`**: `[RequireComponent(typeof(HealthComponent))]`를 추가했다.
	- `_health.OnDeath`를 `OnEnable`/`OnDisable`로 구독한다(기존 `Active` 리스트 등록/해제와 같은 '생애주기 관리' 책임으로 묶었다).
	- 죽으면 풀 반납한다.
	- XP 지급과는 별개 축의 관심사라 병합하지 않기로 했다(더 자세한 근거는 대화 중 논의. 이동/생애주기 vs 보상 경제는 다른 축).
- **`AchievementTracker`**: `EnemySpawner.OnEnemyKilled`를 구독해 `EnemyKillCount`를 증가시킨다.
	- 이제 클래스 주석의 '유일한 담당자' 선언이 실제로 참이 됐다.
- **`EnemySpawner`의 static `Instance` 제거**: `EnemyAI`(프리팹)가 `EnemySpawner`(씬 전용)를 참조해야 하는 이유는 여전히 유효하지만, `RunStats` 리팩터링 때와 마찬가지로 `SpawnEnemy()`가 이미 갓 스폰된 인스턴스를 쥐고 있는 시점에 `ai.SetTarget(player)`와 완전히 같은 패턴으로 `ai.SetSpawner(this)`를 주입하면 static이 필요 없다는 걸 재확인했다.
	- `EnemySpawner.Instance`의 유일한 호출부가 `EnemyAI.HandleDeath()`(이전엔 `EnemyXPReward.HandleDeath()`) 하나뿐이었던 게 이 전환을 쉽게 만들었다.
	- **검토했으나 채택 안 한 대안(C안. 스포너가 직접 `EnemyAI`/`HealthComponent`의 `OnDeath`를 구독)**: 풀링 재사용 시 `HealthComponent.ResetForReuse()`가 `OnDeath` 구독자를 안 지워서, 스포너가 매 스폰마다 구독만 걸면 재사용 때 구독이 누적돼 사망 시 콜백이 중복 발동하는 문제가 있음을 확인했다.
		- self-unsubscribe 클로저나 `HealthComponent.TakeDamage()`에서 `OnDeath?.Invoke()` 직후 `OnDeath = null`로 청소하는 방안도 검토했으나(둘 다 기술적으로는 동작함을 순서 추적으로 확인), (a) 죽지 않고 강제로 비활성화되는 미래 경로엔 두 방안 다 대응 못 하고(그러려면 `OnDisable()` 기반의 별도 이벤트가 필요. 더 복잡), (b) `HealthComponent`는 플레이어와 적이 공유하는 범용 컴포넌트라 적 풀링 전용 관심사를 얹는 게 결이 안 맞는다는 점에서 최종적으로 기각했다.
		- 주입(B안)이 이런 이벤트 생애주기 고민 자체를 없애줘서 더 단순하다.

---

### 2.24 `AchievementSystem` → `AchievementManager` 개명 + static 전면 제거

> 일자: 2026-08-04

`EnemySpawner`를 static 없이 정리한 김에 `AchievementSystem`도 검토했다. 이 케이스는 오히려 더 명확했다:

- **호출부 전수 조사 결과 `AchievementTracker.cs`/`DebugOverlay.cs` 딱 둘뿐이고, 둘 다 씬 오브젝트다**(프리팹 아님).
	- `RunStats`/`EnemySpawner`가 원래 내세우던 '프리팹이 씬 전용 오브젝트를 직렬화 참조로 못 든다'는 이유 자체가 처음부터 해당하지 않는다.
- `AchievementSystem.cs` 자신의 기존 주석이 '호출부가 참조를 들고 있지 않아도 되게 한다'고 static의 근거를 스스로 '편의성'이라고 밝히고 있었다.
	- 구조적 필요가 아니라 순전한 편의였다는 뜻이라 'static은 안 만들 수 있으면 안 만든다' 원칙에 가장 먼저 걸리는 케이스다.
- **개명**: `AchievementSystem`을 `AchievementManager`로 개명했다.
	- static `Instance` 제거 후 'static 접근 불가 + 씬 컴포넌트로 실체화 + 단일'이 되므로 4분류 표상 정확히 `~Manager` 칸이다.
	- `PlayerAugmentManager`(§2단계, 동일한 이유로 이미 개명된 선례)와 완전히 같은 패턴이다.
- `IsUnlocked`/`GetCounter`/`GetMetricBest`는 사실 `roster` 인스턴스 상태를 안 쓰는 순수 조회 함수라 static으로 남겨도 됐지만, 같은 클래스에서 일부만 static이면 호출부가 헷갈리므로 API 일관성을 위해 전부 인스턴스 메서드로 통일했다.
- `AchievementTracker`/`DebugOverlay`에 `[SerializeField] private AchievementManager achievementManager` 필드를 추가했다.
	- `Stage1SceneBuilder.BuildAchievements`가 이제 생성한 `AchievementManager` 인스턴스를 반환하고, `BuildDebugOverlay`가 그걸 파라미터로 받아 배선한다.
	- `BuildDebugOverlay`는 원래 'SO 애셋은 경로로 재로드해서 시그니처를 안 건드린다'는 관례였는데, `AchievementManager`는 씬 오브젝트라 재로드가 안 되므로 이번만 예외로 파라미터를 받는다(주석에 이유 명시).

---

### 2.25 `WeaponSlot` → `PlayerWeaponController` 개명

> 일자: 2026-08-04

'Slot'이 수동적인 보관 칸을 연상시키는데, 클래스 자신의 주석이 스스로 '플레이어의 단일 장착 무기 상태 머신'이라고 부를 만큼 실제로는 능동적이라는 지적이 나왔다(장착/교체/드롭/파괴 복귀 로직 관장, `Update()`에서 입력을 받아 현재 무기에 공격 위임, 이벤트 3종 발행). `RunStats`/`SaveSystem` 때와 같은 결의 '이름이 수동적 데이터처럼 읽히는데 실제로는 능동적' 패턴이다.

- static 접근성과 무관한 순수 어휘 문제라 System/Handler/Manager/Controller 4분류 표는 적용 대상이 아니다(`WeaponSlot`은 애초에 static Instance를 쓴 적이 없다).
	- `HealthComponent`/`PlayerMovement`처럼 플레이어에 붙는 평범한 컴포넌트 계열로 보고, 'Controller'가 게임 개발에서 흔히 갖는 뉘앙스(입력→행동 변환, 상태 관리)에 맞춰 개명했다.
- 영향받는 파일(전부 필드/타입/주석 갱신):
	- `AchievementTracker.cs`(`weaponSlot`→`playerWeaponController`)
	- `PlayerAugmentManager.cs`(동일)
	- `WeaponPickup.cs`(`TryGetComponent(out WeaponSlot slot)`→`out PlayerWeaponController controller`)
	- `IWeapon.cs`/`WeaponBase.cs`/`WeaponDefinition.cs`(주석만)
	- `Stage1SceneBuilder.cs`(`PlayerRefs.WeaponSlot`→`PlayerRefs.PlayerWeaponController` 포함 전체 배선)

---
