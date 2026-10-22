# 트러블슈팅 02: PlayerInputReader AimPoint NullReferenceException

> 공개 편집본 p1 · 2026-10-23 · 원본 `703affa`. 현재 안내는 [공개본 문서 색인](../README.md)을 따른다. 과거 기록은 당시 상태이며 현재 코드 전체의 검증 결과가 아니다.

> 공통 작성 규칙: [README.md](README.md) 참고.

이 문서는 `PlayerInputReader.Update()`에서 반복적으로 발생하던 NullReferenceException을 조사하고 해결한 과정을 기록한다. `DevNotes/`가 "최종 결정과 그 이유"만 남기는 것과 달리, 이 문서는 원인을 잘못 짚었던 경로까지 포함해 **조사 과정 전체**를 남기는 것이 목적이다. 특히 "에디터 스크립트로 애셋을 수정하고 저장했는데 왜 반영이 안 되는가"라는 함정은 이 프로젝트의 다른 에디터 자동화 스크립트에도 재발할 수 있어, 원인 규명 경위를 상세히 남긴다.

---

## 1. 증상

- `PlayerInputReader.Update()` (`PlayerInputReader.cs:61`, `_aimPointAction.ReadValue<Vector2>()`)에서 NullReferenceException 발생.
- `Tools/Build Scene`을 한 번 실행하면 사라진다.
- 유니티 에디터를 재시작하면 다시 발생한다. 재현이 "에디터 세션 단위"로 반복되는 게 특이점이었다.

---

## 2. 원인 진단

### 2.1 1차 확인: null이 된 지점

- `_moveAction.ReadValue<Vector2>()`(60번째 줄)는 예외 없이 통과하고, 바로 다음 줄인 `_aimPointAction.ReadValue<Vector2>()`(61번째 줄)에서만 터졌다.
- 이는 `inputActions` 필드 자체나 `Player` 액션맵 조회는 정상이고, `AimPoint`라는 이름의 액션만 못 찾는다는 뜻이다(`PlayerInputReader.Awake()`의 `map.FindAction("AimPoint")`가 null 반환).

---

### 2.2 근본 원인: 애셋 원본에 액션이 없음

- `Assets/InputSystem_Actions.inputactions` 원본에는 `AimPoint`/`DropWeapon` 액션이 없었다(템플릿 기본 액션만 존재).
- 이 두 액션은 `Assets/Scripts/Editor/PlayerInputActionsSetup.cs`(에디터 스크립트)가 `Stage1SceneBuilder.BuildScene()` 실행 시 코드로 주입하도록 설계돼 있었다.
- `EnsureActions()`는 액션을 추가한 뒤 `EditorUtility.SetDirty(asset)` + `AssetDatabase.SaveAssets()`로 저장을 시도했다.

---

### 2.3 왜 저장이 실패했는가

- `.inputactions`는 일반 `.asset`이 아니라 **ScriptedImporter가 JSON 텍스트를 파싱해서 만드는 애셋**이다.
- `AssetDatabase.LoadAssetAtPath`로 받은 `InputActionAsset` 객체를 메모리에서 수정하고 `SetDirty`+`SaveAssets`를 호출해도, 이 저장 경로는 **소스 JSON 파일 자체에는 반영되지 않는다.**
- `git log`로 확인한 결과 `Assets/InputSystem_Actions.inputactions`는 2026-07-27 커밋 이후 한 번도 수정된 적이 없었고, 파일에도 `AimPoint`/`DropWeapon`이 없었다. `EnsureActions()`가 지금까지 디스크에 실제로 저장된 적이 한 번도 없었다는 뜻이다.
- 재현 경로가 "에디터 세션 단위"였던 이유가 여기서 설명된다: `Build Scene` 실행 시 인메모리 객체에는 액션이 추가되어 그 세션 동안은 NRE가 사라지지만, 다음 에디터 재시작 시 디스크의(수정되지 않은) JSON을 다시 리임포트하면서 원상태로 돌아갔다.
- **빌드에서는 더 확실하게 재현되는 문제였다.** `PlayerInputActionsSetup.cs`는 `Assets/Scripts/Editor/`에 있어 플레이어 빌드(실행 빌드 등)에는 아예 포함되지 않는다. 즉 빌드에는 "한 세션만 임시로 고쳐지는" 경로 자체가 없어, 고치치 않고 빌드했다면 항상 이 NRE가 발생했을 것이다.

---

## 3. 해결

### 3.1 채택한 방법: Input Actions 에디터 창에서 직접 추가

- `Assets/InputSystem_Actions.inputactions`를 Input Actions 에디터 창에서 열어 `Player` 액션맵에 다음을 직접 추가하고 저장했다.
	- `AimPoint`: `Action Type: Value`, `Control Type: Vector 2`, 바인딩 `<Mouse>/position`, 바인딩 그룹 `Keyboard&Mouse`.
	- `DropWeapon`: `Action Type: Button`, 바인딩 `<Keyboard>/q`, 바인딩 그룹 `Keyboard&Mouse`.
- 저장(`Ctrl+S`/Save Asset)까지 완료해, `git status` 기준으로 실제 파일 변경이 반영됨을 확인했다.
- 이 방식은 기존에 이미 잘 동작하던 `Move`/`Attack` 등도 전부 거쳐간 정식 경로다. 별도의 검증이 필요 없는 안전한 방법이다.

---

### 3.2 후속 정리: 불필요해진 자동화 스크립트 제거

- `PlayerInputActionsSetup.cs`는 3.1로 액션이 애셋에 영구히 존재하게 되면서 더 이상 할 일이 없어졌다(멱등 가드라 실행해도 아무 것도 안 하는 상태). 삭제했다.
- `Stage1SceneBuilder.BuildScene()`에서 `PlayerInputActionsSetup.EnsureActions()` 호출도 함께 제거했다.

---

### 3.3 검토했으나 채택하지 않은 대안: 코드로 올바르게 저장하기

- `EnsureActions()`의 저장 로직만 고쳐서 살리는 방법도 기술적으로는 가능했다: `EditorUtility.SetDirty`+`AssetDatabase.SaveAssets()` 대신 `InputActionAsset.ToJson()`으로 직렬화한 문자열을 `File.WriteAllText()`로 파일에 쓰고 `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`로 리임포트하면 실제로 디스크에 반영된다.
- 다만 이 API에는 추가 함정이 있다.
	- `InputActionAsset.ToJson()`은 이 클래스 전용 직렬화 포맷을 만든다. 흔히 쓰는 범용 `JsonUtility.ToJson(asset)`은 포맷이 달라 호환되지 않는다. 이름이 비슷해 혼동하기 쉽다.
	- Play 모드 중에 API로 수정한 내용은 Edit 모드 복귀 시 사라진다(유니티가 Play 모드 종료 시 디스크에서 다시 로드하기 때문). 반드시 Edit 모드에서 실행해야 한다.
- 이 프로젝트에는 3.1(직접 추가)로 이미 해결됐으므로 이 대안은 채택하지 않았다. 다만 **새 프로젝트를 시작할 때마다 액션을 반복해서 만드는 수고를 줄이는 재사용 도구**를 만들 때는 이 방법이 유효한 선택지로 남아 있다. 구체적인 도구 설계는 당시 미확정 제안으로 남았다.

---

## 4. 관련 리서치: 문자열 기반 액션 조회의 구조적 한계

- 이번 버그는 `PlayerInputReader.Awake()`가 액션을 문자열(`FindAction("AimPoint")`)로 조회하는 방식이라, 이름이 없거나 틀리면 컴파일은 통과하고 런타임에야 null로 드러나는 구조적 약점 때문에 오래 발견되지 않았다.
- 웹 리서치 결과, Unity 공식 문서와 다수 실무 가이드는 프로덕션 코드에 문자열 기반 `FindAction()`보다 **"Generate C# Class"**(애셋 임포터에서 액션을 타입 세이프한 C# 프로퍼티로 자동 생성하는 기능)를 권장한다고 확인했다. 근거는 타입 안전성(오타/누락이 컴파일 에러로 잡힘), 문자열 조회/메시징 시스템을 거치지 않는 성능 이점, 리매핑/멀티플레이어 등 세밀한 제어 용이성이다.
- 이 절은 당시 검토 기록이다. 이후 타입 안전 입력 클래스를 도입했으며 [개발 노트3.10](../DevNotes/03-PlayerAnimation.md#310-playerinputreader-nre-해결--입력-액션-조회를-generate-c-class로-전환)을 따른다.
- Sources
	- [Best practices for using Input System - Unity Discussions](https://discussions.unity.com/t/best-practices-for-using-input-system/1704430)
	- [Input Action Assets | Input System | 1.8.2 (공식 매뉴얼)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/ActionAssets.html)
	- [Class InputActionAsset | Input System | 1.15.0 (공식 API 문서)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.15/api/UnityEngine.InputSystem.InputActionAsset.html)

---

## 5. 결론

- 원인은 "액션이 없어서"가 아니라 "액션을 코드로 추가했다고 믿었지만 저장 API가 틀려 실제로는 한 번도 저장된 적이 없어서"였다. 에디터 스크립트가 애셋을 수정한 뒤 `AssetDatabase.SaveAssets()`만 호출하고 넘어가는 패턴은, 대상이 ScriptedImporter 기반 애셋(`.inputactions` 등)일 때 조용히 실패할 수 있다는 점을 이번에 확인했다.
- 최종 해결은 애셋을 GUI에서 직접 편집하는 정공법이었고, 이를 대체하려던 에디터 자동화 스크립트는 제거했다.
- 파생된 두 가지 개선 논의 중, 액션 조회를 타입 세이프하게 바꾸는 것(Generate C# Class 전환)은 같은 라운드에 바로 적용해 `DevNotes/03-PlayerAnimation.md` §3.10으로 승격됐다. 새 프로젝트용 재사용 도구를 올바른 API로 다시 만드는 것은 당시 미확정 제안으로 남았다.
