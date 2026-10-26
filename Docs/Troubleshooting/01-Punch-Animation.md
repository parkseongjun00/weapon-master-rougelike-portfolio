# 트러블슈팅 01: 정면 펀치 제작과 IK 베이크 실패

> 공개 편집본 p3 · 2026-10-27 · 문서 원본 `37ba4c4`. 공개 범위와 코드 기준은 [문서 색인](../README.md)을 따른다.


<a id="트러블슈팅-01-스트레이트-펀치-애니메이션-시행착오"></a>

| 항목 | 내용 |
|---|---|
| 버전 | v0.2 |
| 수정일 | 2026-10-25 |

> 공통 작성 규칙: [README.md](README.md) 참고.

---

## 1. 문제 요약

<a id="12-제약-조건"></a>

<a id="11-목표"></a>

<a id="1-배경과-목표"></a>

| 항목 | 당시 기록 |
|---|---|
| 목표 | 캐릭터가 조준 방향을 유지하고 머리·상체·발의 회전 없이 팔만 앞으로 뻗는 펀치 확보 |
| 증상 | 생성된 복싱 모션의 상체 회전·사선 스탠스, 인게임에서 좌우 주먹의 방향 오차 |
| 환경·제약 | Character_Boy·Humanoid. Hips/Spine 직접 키프레임 제외. 공격 중 이동을 유지하고 상체 레이어에 펀치 적용 |
| 당시 조작 | 마우스 좌클릭 공격·키보드 이동, 공격은 당시 설명상 캐릭터 이동 방향 기준. 현재 조작 명세와 구분 |
| 확인된 결론 | 당시 Animation Rigging 공식 베이크는 Humanoid 미지원. UI 제한을 우회한 본 회전 베이크도 사용자 재생 시험에서 실패 |
| 조사 상태 | 런타임 IK를 선택한 뒤 승인 없는 구현을 취소·복구하고, 런타임 IK와 직접 본 회전 제작의 선택 지점에서 중단 |
| 연결이 불명확한 작업 | Generic 클립 honest_left_punch를 Humanoid 캐릭터에 적용할 방법은 기록에 없음 |

- 상하체 레이어의 당시 구현은 [개발 노트 3.6](../DevNotes/03-PlayerAnimation.md#36-상하체-애니메이션-분리-upper-body-레이어--avatar-mask-구현)을 참고한다.

---

## 2. 조사 경로

<a id="2-진행-경로"></a>

### 2.1 펀치 모션 확보

<a id="21-펀치-소스-확보"></a>

<a id="21-분기-1-펀치-소스를-어떻게-확보할-것인가"></a>

- **가설**
	- 외부 생성·기성 모션에서 상체 회전 없는 정면 펀치를 확보할 수 있을 것으로 보았다.
- **시도**

	| 경로 | 수행·검토 내용 | 결과 |
	|---|---|---|
	| Meshy AI Text to Motion | 본 생성 시도, 1,500크레딧 사용 | 복싱 스타일 상체 회전이 남아 목표 미충족 |
	| Meshy Swift | 동일 프롬프트로 저비용 재시험 | 양발이 어긋난 사선 스탠스 등 결과 불일치 |
	| Mixamo | Avatar 설정 변경과 리타겟팅 | Hips 오류 해결 후에도 모션 이상이 남아 중단 |
	| ActorCore·Unity Asset Store의 MoCap Online | 소스 후보 검토 | 실행 결과 없이 미채택 |
	| Uthana·DeepMotion·Rokoko Create | 소스 후보 검토 | 실행 결과 없이 미채택 |
	| Blender | 수작업 도구 후보 검토 | 사용 경험이 없어 이번 작업에서 제외 |

- **결과**
	- Mixamo에서 Copy From Other Avatar 설정 시 `Transform 'Hips' for human bone 'Hips' not found`가 발생했고 Create From This Model로 해당 오류를 해결했다.
	- 이후 리타겟팅 이상은 구체적 증상 확인 전에 중단하고 Meshy로 돌아갔다. Mixamo 전체가 불가능하다고 확인한 것은 아니다.
- **판단**
	- Unity에서 수작업 포즈를 만들기로 했다. Animation Rigging의 Two Bone IK Constraint를 팔에만 적용하고 Hips/Spine은 건드리지 않는 방향을 선택했다.

---

### 2.2 생성 모션의 방향 오차

<a id="22-생성-모션의-방향-오차-조사"></a>

<a id="22-분기-1-a하위-분기-meshy-생성-결과가-정면을-보지-않는-이유"></a>

- **가설**
	- FBX 회전 설정·생성 프롬프트·상하체 마스킹을 조정하면 주먹의 방향 오차를 줄일 수 있다고 보았다.
- **시도**

	| 순서 | 방법 | 관찰·검토 결과 |
	|---|---|---|
	| 1 | Root Transform Rotation 조정 | 미리보기는 개선됐으나 인게임에서 좌우 손이 각각 바깥쪽으로 틀어짐 |
	| 2 | Meshy 프롬프트 조정 | jab→punch, square stance↔orthodox stance, without curving→like a taekwondo jireugi 등 표현 변경 |
	| 3 | Avatar Mask의 하반신 제외 | UpperBodyMask.mask를 확인했으나 Body에 포함된 Hips 회전을 다리 제외만으로 차단하지 못함 |
	| 4 | Animancer 검토 | 기본 Layer Mask는 AvatarMask 사용. Pro Weighted Mask Layers의 본별 가중치 가능성을 검토했으나 미채택 |

- **결과**
	- 당시에는 로코모션이 하반신을 정면으로 고정하는 반면 클립은 사선 스탠스의 하반신을 기준으로 팔 회전을 계산한 것을 원인으로 진단했다.
	- 부모 관절의 회전이 자식 관절의 월드 방향에 영향을 주므로 하반신 기준이 달라지면 주먹 방향도 달라진다는 설명이었다. 좌우 손의 반대 방향 오차도 손잡이에 따른 스탠스 차이로 설명했다.
	- 프롬프트를 평서형으로 바꾸고 머리·골반·몸통·다리 정지를 명시하는 단계까지 진행했으나 생성 경로를 중단하면서 더 시험하지 않았다.
	- 당시 조사에서는 Humanoid Avatar Mask의 Body에 척추·가슴·Hips가 묶여 있어 기본 UI로 Hips만 제외할 수 없다고 판단했다.
	- 커뮤니티의 유사 사례와 Spine 기준 Layered Blend Per Bone 방식을 조사했지만 원본 출처 링크는 이 기록에 남아 있지 않다. Animancer Pro도 실제 적용 시험은 하지 않았고 당시 최소 제작 범위에 비해 크다고 판단했다.
- **판단**
	- 런타임 마스킹을 추가하기보다 회전이 없는 소스를 직접 만드는 방향으로 진행했다.

---

### 2.3 Humanoid IK 베이크

<a id="23-ik-포즈의-클립-저장"></a>

<a id="23-분기-2-ik-포즈를-애니메이션-클립으로-어떻게-만들-것인가베이크-여부"></a>

- **가설**
	- IK로 만든 최종 본 회전을 클립에 저장하면 재생 시 IK를 제거할 수 있을 것으로 보았다. IK Target의 Transform만 저장하는 경우에는 재생 시에도 IK가 필요하다고 구분했다.
- **시도**

	| 방법 | 확인 내용 | 결과 |
	|---|---|---|
	| 공식 Bidirectional Motion Transfer | Rig·Two Bone IK Constraint 헤더의 Transfer motion to constraint / Transfer motion to skeleton 메뉴 확인 | 버튼 비활성화. 당시 공식 문서에서 Generic 지원·Humanoid 미지원 확인 |
	| Humanoid→Generic→베이크→Humanoid | 리그 타입을 임시로 바꿔 베이크 시도 | Generic 전환 시 기존 Humanoid 머슬 클립·로코모션이 재생되지 않음 |

- **결과**
	- 당시 확인한 문구는 "Bidirectional motion transfer can be used on generic character hierarchies. Humanoid is not supported at the moment."였다. Animation 창 설정 문제가 아니라 지원 범위의 제약으로 판단했다.
	- 리그 변경은 펀치 하나에 한정되지 않고 해당 캐릭터가 사용하는 Humanoid 모션 전체에 영향을 줬다.
	- 별도 작업으로 Generic 기반 honest_left_punch의 팔 본 6개 Rotation을 직접 키프레임한 기록이 있었다. Generic에서의 재생과 Character_Boy의 Humanoid 적용은 별개이며 후자의 연결 방법은 남아 있지 않았다.
- **판단**
	- 당시에는 이 펀치 외에 IK를 재사용할 계획이 없어 베이크의 이점이 작다고 보고 베이크·레코딩을 중단했다.
	- 웹챗에서 Two Bone IK Constraint와 Rig를 유지하고 런타임에 IK를 구동하는 방향을 선택했다. 다음 구현 세션은 이 결론을 전달받은 상태에서 시작했다.

---

### 2.4 베이크 재시도와 변경 복구

<a id="24-베이크-재조사와-구현-취소"></a>

<a id="24-분기-3-이-claude-code-세션에서-이미-끝난-분기를-다시-밟은-경위"></a>

- **가설**
	- Claude Code는 공식 메뉴의 isHuman 검사가 UI 제한일 뿐이며 BakeUtils.TransferMotionToSkeleton을 직접 호출하면 Humanoid에서도 동작한다고 잘못 판단했다.
- **시도**
	- 이미 중단한 베이크 문제를 다시 조사하며 BakeUtils.cs·TwoBoneIKConstraint.cs·RigConstraint.cs·RigBuilder.cs·TwoBoneIKConstraintEditor.cs를 확인하고 UI 검사를 우회하도록 안내했다.
	- 실패 확인 후 런타임 IK로 전환하면서 사용자 승인 없이 PunchAnimationAuthoringTool.cs를 삭제하고 PunchIKRigSetup.cs를 작성했다. 새 도구는 Character_Boy.prefab에 IK를 영구 설치하는 방식이었다.
	- 당시 임시 메모 Scratchpad.md도 해당 변경을 확정한 내용으로 수정했다.
- **결과**
	- 사용자 시험에서 마스크 유무와 관계없이 베이크 클립이 AnimatorController 재생에 반영되지 않았다. UI 제한만 우회하면 된다는 가설은 반박됐다.
	- 재조사에서는 Humanoid AnimatorController가 머슬 값으로 포즈를 재구성해 해당 raw 본 회전 커브가 적용되지 않는 것으로 판단했다.
	- 이미 문서로 확인한 제약을 패키지 소스 조사와 사용자 시험으로 반복 확인했고, 사용자가 중복 조사와 오판을 지적했다.
	- 사용자는 승인 없이 진행한 코드·문서 변경의 취소와 원상 복구를 요청했다.

	| 대상 | 복구 결과 |
	|---|---|
	| PunchIKRigSetup.cs | 삭제 |
	| PunchAnimationAuthoringTool.cs | 세션 기록에서 원본을 재구성해 복원 |
	| Character_Boy.prefab | 설치 스크립트 실행 전이어서 별도 복구 불필요 |
	| 당시 임시 메모 | 런타임 IK 확정 서술을 제거하고 베이크 실패와 선택 지점으로 되돌림 |

- **판단**
	- 기술 방향을 선택했던 사실과 프로덕션 프리팹을 변경할 실행 승인은 별개였다. 복구 후 다음 두 경로를 다시 비교하는 지점으로 돌아갔다.

---

### 2.5 런타임 IK와 직접 제작 비교

<a id="25-중단-시점의-대안-비교"></a>

<a id="25-분기-4현재-위치-다음-갈림길"></a>

- **가설**
	- 런타임 IK를 유지하거나 팔 본을 직접 키프레임하는 두 경로를 비교할 수 있다고 보았다.
- **시도**
	- 새 구현 없이 제작 방식과 프리팹 변경 범위를 비교했다.

	| 항목 | 런타임 IK 유지 | IK 없이 직접 본 회전 제작 |
	|---|---|---|
	| 방법 | Character_Boy에 TwoBoneIKConstraint 설치, Target Position을 키프레임 | RightUpperArm·RightLowerArm·RightHand 등 좌우 팔 본 6개를 포즈별로 회전 |
	| 장점 | IK로 포즈 제작, 당시 선택한 런타임 방식과 연결, 다리 IK 등의 확장 가능성 | 프로덕션 프리팹에 Animation Rigging 컴포넌트 불필요, Generic 클립 제작 경험 활용 가능성 |
	| 비용·제약 | 프리팹 영구 변경과 매 프레임 IK 계산. 당시에는 Two Bone IK의 closed-form 계산 비용을 낮게 평가했으나 측정값은 없음 | 포즈마다 본을 직접 회전해야 함. Generic 클립을 Humanoid에 적용하는 경로는 확인되지 않음 |

- **결과**
	- 사용자는 이 선택 지점에서 조사를 중단하고 다른 작업으로 전환했다.
- **판단**
	- 당시 재개 시 확인 대상으로 honest_left_punch의 활용 계획을 남겼다. 재사용 가능성을 실제 적용 성공으로 취급하지 않는다.

---

## 3. 결과

- **당시 결론**
	- Humanoid 베이크 경로를 중단했다. 런타임 IK를 선택한 이력은 있지만 승인 없는 구현을 복구한 뒤 최종 제작 경로를 다시 선택하기 전에 멈췄다.
- **남길 판단**
	- 지원 제한을 UI 제약으로 단정하지 않는다. 기존 조사에서 확인한 제약과 사용자 시험의 반박 결과를 먼저 확인한다.
	- Generic 클립 재생 성공을 Humanoid 적용 성공으로 확대하지 않는다.
	- 기술 방향 합의와 프리팹 영구 변경의 실행 승인을 구분한다.

---

## 4. 변경 이력

| 버전 | 날짜 | 내용 |
|---|---|---|
| 초기 기록 | 2026-08-29 | 웹챗 기록 3건과 구현 경위를 취합하고 원본 punch_animation_summary.md·punch_animation_issue_summary.md·ik-punch-animation-decision-log.md를 흡수 |
| v0.1 | 2026-10-25 | 문제 요약·조사 경로·결과로 재구성하고 실패 근거·취소와 복구·중단 지점 보존 |
