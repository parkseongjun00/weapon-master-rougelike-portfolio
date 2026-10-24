# 공개본 문서 색인

- 공개 편집본: p2 / 2026-10-25
- 코드 원본: 703affae9ea3a2796f5d657dca5f368c07846b6c
- 문서 원본: ccf5060401deeba4b5ab9bb7ab53ecf4c9d05988

## 1. 읽는 순서와 역할

| 문서 | 역할 |
|---|---|
| [GDD](GDD.md) | 게임 규칙·설계 기준. 구현 완료 목록과 구분 |
| [Systems](Systems.md) | 구현 대상 기능 범위 |
| [DevPlan](DevPlan.md) | 구현 순서와 당시 진행 기록 |
| [DevNotes](DevNotes/README.md) | 확정된 구현 결정·사유·검증 경계 |
| [Backlog](Backlog.md) | 보류 항목과 처리 흔적 |
| [Troubleshooting](Troubleshooting/README.md) | 조사 과정·실패 경로 |
| [FirstMap](Art/FirstMap.md), [EnemyDesign](Art/EnemyDesign.md) | 환경·적 디자인 기준 |
| [아트 회의 기록](WorldArtMeetingNotes-2026-09-25.md) | 과거 논의. 현재 기준은 Art 문서 |
| [DocSystem](DocSystem.md), [WritingGuide](WritingGuide.md) | 문서별 책임과 작성 규칙 |
| [HANDOFF](HANDOFF.md) | 공개본 범위와 확인 경계 |
| [공개 작업 초안](DiscussionDraft.md) | 무기 종류 추가를 검토하는 작업 방식 예시 |
| [SourceMap](SourceMap.md) | 공개 코드의 원본 경로와 생략 범위 |

## 2. 공개 편집 원칙

- 문서 체계와 개발 기록을 보존하되, 배포 대상·이식 계획의 표현을 제거했습니다. 빌드·성능 미검증이라는 사실은 유지했습니다.
- 원본 문서의 버전은 원본 식별용으로 보존하고 공개 편집본 p2를 별도로 표시했습니다. 이 편집은 원본 게임 설계의 새로운 기능 확정이 아닙니다.
- 현재 규칙·계획에 남은 자동 픽업·별도 드랍 버튼·과거 타격 방식 설명을 현 코드에 맞췄습니다.
- 개발 노트의 과거 이름·실패·검증 결과는 해당 시점의 기록입니다. 현재 이동은 SlotFollower·NavigationMovement, 공통 전투는 WeaponController·CharacterAnimationController를 따릅니다.
- 초기 범위·과거 완료 표시는 최신 코드 전체의 완료·검증 보장이 아닙니다. 현재 구현 범위는 루트 README와 최신 개발 노트를 함께 확인합니다.
- 라이브 내부 초안과 로컬 도구·백업·로그·검증 결과 원본은 공개하지 않습니다. DiscussionDraft.md는 작업 방식을 보여 주기 위해 편집한 공개 예시입니다.
- 생략되거나 교체된 파일을 가리키는 링크는 원본 경로 표기로 전환했습니다. 링크가 남아 있는 파일은 이 저장소 안에서 열 수 있습니다.
- Private 저장소·개인 서비스의 접근 권한이 필요한 링크는 공개 안내에 사용하지 않습니다.

## 3. 최신 구현으로 이어지는 기록

- 이동 전환: [5.30](DevNotes/05-EnemyAnimation.md#530-navmesh-일반-이동과-역할-이름-전환)
- 반복 정지 진단과 사용자 확인: [5.31](DevNotes/05-EnemyAnimation.md#531-반경-계산-오차로-인한-슬롯-재구성-반복-수정), [5.32](DevNotes/05-EnemyAnimation.md#532-반복-정지-수정의-사용자-확인과-커밋)
- 공통 전투와 검증 경계: [5.33](DevNotes/05-EnemyAnimation.md#533-공통-무기모션과-기본-적-펀치), [5.34](DevNotes/05-EnemyAnimation.md#534-공통-공격의-사용자-확인과-커밋)
