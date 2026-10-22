# OrangePlatformer — 1스테이지 3D 플랫포머 프로토타입 기획서

- 엔진: Unity 6, URP 17.6, Input System 1.20, uGUI 2.6 (TMP 미사용 — TMP Essentials 미임포트 상태라 레거시 `UnityEngine.UI.Text` + 내장 `LegacyRuntime.ttf` 사용, UI 문구는 영어)
- 씬: `Assets/Scenes/OrangePlatformer.unity` (Build Settings index 0 — 재시작이 `SceneManager.LoadScene`을 쓰므로 필수)
- 현재 프로젝트 상태(조사 결과): URP 템플릿 그대로. 게임 코드·UI·디자인 문서 없음, `Assets/InputSystem_Actions.inputactions`는 템플릿 기본값(사용하지 않음 — 입력은 코드에서 생성), Cinemachine 없음 → 자체 궤도 카메라.

## 1. Purpose (플레이어 경험)
귤 캐릭터로 짧은 직선 코스를 건너며 마리오 64 / 어 햇 인 타임식 액션(이단 점프·높이뛰기·다이브·벽 점프·엉덩방아)을 **각각 한 번씩 반드시 쓰게** 만든다. 각 구간은 해당 액션 없이는 통과할 수 없도록 수치로 막는다(아래 3.4 검증표). 3~5분 내 클리어 가능한 분량.

## 2. 게임 상태와 루프
`GameState`: `Intro → Playing ⇄ Paused`, `Playing → Won | GameOver`, `Won/GameOver/Paused → (Restart) → 씬 재로드 → Intro`.

| 상태 | 입력 | 타이머 | timeScale | 커서 | 플레이어 조작 |
|---|---|---|---|---|---|
| Intro | Enter / START 버튼 → Playing | 정지(0) | 1 | 해제 | 비활성 |
| Playing | Esc → Paused | 진행 | 1 | 잠금 | 활성 |
| Paused | Esc / RESUME → Playing, RESTART → 재시작 | 정지 | 0 | 해제 | 비활성 |
| Won / GameOver | Enter / RESTART → 재시작 | 정지(최종값 유지) | 1 | 해제 | 비활성 |

## 3. Rules

### 3.1 이동·점프 (CharacterController 기반 운동학 컨트롤러)
- 이동은 카메라 yaw 기준: `dir = normalize(camForwardXZ * move.y + camRightXZ * move.x)`, 목표속도 `dir * moveSpeed`(웅크림 시 `crouchMoveSpeed`). 지상 가속 `groundAcceleration`, 감속 `groundDeceleration`, 공중 `airAcceleration`. 캐릭터는 이동 방향으로 `turnSpeedDeg`°/s 회전.
- 중력 `gravity`(양수 크기), 낙하속도 하한 `-maxFallSpeed`.
- 점프 초속 공식: `v = sqrt(2 * gravity * h)`.
  - 예: 점프 h=1.6, g=30 → v = sqrt(96) = **9.80 m/s**, 정점 도달 0.327 s.
  - 이단 점프 h=1.3 → **8.83 m/s**. 높이뛰기 h=4.0 → **15.49 m/s**. 벽 점프 수직 10 m/s → 상승 1.67 m.
- 가변 점프: 일반/이단 점프 상승 중(vy>0) Jump를 떼면 `vy *= jumpCutMultiplier`(0.5). 높이뛰기·벽 점프에는 적용 안 함.
- **코요테 타임**: 지면을 떠난 뒤 `coyoteTime`(0.12 s) 동안 첫 점프 가능(점프가 아닌 낙하로 떠난 경우만).
- **점프 버퍼**: 착지 전 `jumpBufferTime`(0.15 s) 이내에 누른 Jump는 착지 순간 실행.
- **이단 점프**: 공중에서 1회. 지상 착지·벽 점프 시 다시 충전. 높이뛰기·다이브·엉덩방아 후에는 사용 불가.
- **웅크리기**: 지상에서 Crouch 유지 → `Crouching`, 컨트롤러 높이 `crouchHeight`, 속도 `crouchMoveSpeed`.
- **높이뛰기**: `Crouching` 상태에서 Jump → vy = 15.49, 수평 속도는 `highJumpForwardSpeed`로 캡(제자리 높이뛰기 성격). 이후 이단 점프 불가.
- **다이브**: 공중(Airborne, 이단 점프 후 포함, 벽 점프 후 포함)에서 Dive 1회 → 수평 속도 = 캐릭터 정면 × `diveSpeed`(12), vy = `diveUpwardSpeed`(4). 다이브 중 조향 불가, 이후 점프·엉덩방아 불가. 착지 시 `diveLandSlideTime`(0.35 s) 동안 배 미끄럼(감속 `groundDeceleration`) 후 Grounded. 벽에 정면 충돌 시 수평 속도 0(튕김), 낙하 지속.
- **벽 슬라이드/벽 점프**: `WallJumpSurface` 컴포넌트가 붙은 콜라이더에만 동작(다른 벽 남용 방지).
  - 공중에서 `OnControllerColliderHit`의 법선 |n.y| < `wallNormalMaxY`(0.3)인 WallJumpSurface 접촉 → 접촉 시각 기록, 법선 저장.
  - 접촉 중 vy ≤ 0 이면 `WallSlide`: 낙하속도 하한 `-wallSlideMaxFallSpeed`(3), 벽 쪽으로 1 m/s 밀어 접촉 유지, 캐릭터는 벽을 등지는 방향(=법선)을 바라봄.
  - 마지막 접촉 후 `wallContactGraceTime`(0.12 s) 이내 Jump → vy = 10, 수평 = 법선 × 7, `wallJumpInputLockTime`(0.2 s) 동안 이동 입력 무시. 이단 점프 재충전.
- **엉덩방아**: 공중(Airborne/WallSlide 제외, 다이브 중 제외)에서 Crouch 누름 → `GroundPoundHang` `groundPoundHangTime`(0.25 s) 동안 속도 0 정지(공중 회전 연출) → `GroundPoundFall` vy = −`groundPoundSpeed`(25), 수평 0. 아래 방향(n.y > 0.7) 충돌 대상에 `IGroundPoundable`이 있으면 `OnGroundPound(hitPoint)` 호출(상자는 파괴되고 낙하 계속). 실제 지면 착지 시 `groundPoundLandStunTime`(0.2 s) 이동 불가.
- 플레이어 상태 `PlayerMotorState`: Grounded, Crouching, Airborne, WallSlide, Dive, DiveSlide, GroundPoundHang, GroundPoundFall, GroundPoundLand.

### 3.2 절차적 연출 (애니메이션은 Idle·Walk만)
- Animator float `Speed` = 수평 속도(m/s). Idle→Walk Speed>0.1, Walk→Idle Speed<0.1.
- 점프 시작: Visual 스케일 (0.9, `jumpStretch` 1.25, 0.9) → `squashRecoverSharpness`로 1 복귀. 착지: (1.15, `landSquash` 0.7, 1.15).
- 이단 점프: Spin을 로컬 X축 360° 앞구르기 `flipDuration`(0.35 s). 높이뛰기: Y축 720° 회전 0.5 s. 벽 점프: Y축 180° 즉시 반전 + stretch.
- 웅크림: 스케일 Y `crouchScaleY`(0.7), XZ 1.15.
- 다이브: Spin X축 `diveTiltDeg`(80°) 전방 기울임(0.1 s 보간), DiveSlide 동안 유지, 종료 시 0.
- 엉덩방아: Hang 동안 X축 360° 회전, Fall 동안 stretch(0.8,1.3,0.8), 착지 squash(1.4,0.55,1.4).
- 벽 슬라이드: Spin Z축 15° 기울기.

### 3.3 수집·체력·체크포인트·골
- 귤 픽업: 트리거 반경 `pickupRadius` 0.6 m, Player 태그가 들어오면 `CollectOrange()` 후 비활성. 회전 `pickupSpinDegPerSec`, 상하 `pickupBobAmplitude`/1 Hz.
- 스테이지 배치 귤 = **정확히 8개** = `orangesRequired`. 그중 2개는 상자 안(엉덩방아 필수).
- `collected == orangesRequired` 순간 `GoalOpen = true`, `GoalOpened` 1회 발생 → 믹서 뚜껑이 `lidOpenDuration` 1.0 s 동안 `lidOpenHeight` 0.8 m 상승 + 힌지 `lidOpenAngle` 110° 회전, 시작 시 뚜껑 콜라이더 비활성.
- 용기 내부 트리거(`GoalTrigger`)에 Player 진입 && GoalOpen && Playing → `Won`. 뚜껑 닫힘 동안에는 물리적으로 진입 불가.
- 낙사: `KillZone` 트리거 진입 → `PlayerFell()`: Playing이 아니면 무시. hearts −1. 0이면 즉시 `GameOver`. 아니면 조작 비활성 후 `respawnDelay`(0.6 s) 뒤 마지막 체크포인트 위치·방향으로 `Respawn`(속도 0, Grounded). 수집한 귤은 유지.
- 체크포인트: 트리거 진입 시 해당 위치를 현재 체크포인트로(이전보다 앞이든 뒤든 마지막 진입 기준), 깃발 색 노랑→초록. 초기 체크포인트 = 스폰.
- 경과 시간: Playing 동안 `Time.deltaTime` 누적, `mm:ss.ff` 표시.

### 3.4 구간 차단 검증(반드시 해당 액션 필요)
캡슐 반지름 0.3 → 가장자리 걸침으로 유효 틈은 약 0.6 m 짧아짐.
| 구간 | 장애 | 액션 없는 최대 | 필요 액션 |
|---|---|---|---|
| 벽 점프 수직갱 | 고원 높이 6.0 m | 점프+이단 1.6+1.3 = 2.9 m | 벽 점프 3회 내외(회당 순상승 ≈1.58 m: 수평 7 m/s로 2.9 m 횡단 0.41 s, 상승 10·0.41 − 15·0.41² ≈ 1.58) |
| 높은 단 | 3.5 m | 2.9 m(높이뛰기 후 이단 불가) | 높이뛰기 4.0 m |
| 넓은 틈 | 8.0 m | 6 m/s × 체공 1.06 s ≈ 6.4 m(+0.6) = 7.0 m | 점프+다이브 ≈ 7.8+0.6 = 8.4 m / 점프+이단+다이브 ≈ 11.4 m |
| 상자 2개 | 귤이 상자 안 | 점프·다이브로는 파괴 불가 | 엉덩방아 |

## 4. Parameters → `OrangePlatformerConfig` (ScriptableObject, `Assets/Settings/OrangePlatformer/OrangePlatformerConfig.asset`)
| name | type | default | range | unit |
|---|---|---|---|---|
| moveSpeed | float | 6 | 3–10 | m/s |
| crouchMoveSpeed | float | 2.5 | 1–4 | m/s |
| groundAcceleration | float | 40 | 10–80 | m/s² |
| groundDeceleration | float | 50 | 10–80 | m/s² |
| airAcceleration | float | 20 | 5–40 | m/s² |
| turnSpeedDeg | float | 720 | 180–1440 | deg/s |
| gravity | float | 30 | 15–50 | m/s² |
| maxFallSpeed | float | 25 | 15–40 | m/s |
| jumpHeight | float | 1.6 | 1–2.5 | m |
| doubleJumpHeight | float | 1.3 | 0.8–2 | m |
| highJumpHeight | float | 4.0 | 3.6–5 | m |
| highJumpForwardSpeed | float | 2 | 0–4 | m/s |
| jumpCutMultiplier | float | 0.5 | 0.3–1 | × |
| coyoteTime | float | 0.12 | 0–0.2 | s |
| jumpBufferTime | float | 0.15 | 0–0.25 | s |
| diveSpeed | float | 12 | 8–16 | m/s |
| diveUpwardSpeed | float | 4 | 0–6 | m/s |
| diveLandSlideTime | float | 0.35 | 0.1–0.6 | s |
| wallSlideMaxFallSpeed | float | 3 | 1–6 | m/s |
| wallJumpVerticalSpeed | float | 10 | 8–12 | m/s |
| wallJumpHorizontalSpeed | float | 7 | 4–10 | m/s |
| wallJumpInputLockTime | float | 0.2 | 0–0.4 | s |
| wallContactGraceTime | float | 0.12 | 0.05–0.2 | s |
| wallNormalMaxY | float | 0.3 | 0.1–0.5 | — |
| groundPoundHangTime | float | 0.25 | 0.1–0.5 | s |
| groundPoundSpeed | float | 25 | 15–35 | m/s |
| groundPoundLandStunTime | float | 0.2 | 0–0.5 | s |
| controllerHeight | float | 1.0 | — | m |
| crouchHeight | float | 0.6 | 0.4–0.8 | m |
| controllerRadius | float | 0.3 | — | m |
| cameraDistance | float | 6 | 3–10 | m |
| cameraMinDistance | float | 1 | 0.5–2 | m |
| cameraPitchMin | float | -20 | -45–0 | deg |
| cameraPitchMax | float | 65 | 30–85 | deg |
| cameraDefaultPitch | float | 20 | — | deg |
| mouseSensitivity | float | 0.12 | 0.02–0.5 | deg/pixel |
| stickSensitivity | float | 180 | 60–360 | deg/s |
| cameraCollisionRadius | float | 0.25 | 0.1–0.5 | m |
| cameraFollowSharpness | float | 12 | 1–30 | 1/s |
| cameraTargetHeight | float | 1.0 | 0.5–1.5 | m |
| orangesRequired | int | 8 | 1–8 | 개 |
| maxHearts | int | 3 | 1–5 | 개 |
| respawnDelay | float | 0.6 | 0–2 | s |
| lidOpenDuration | float | 1.0 | 0.3–2 | s |
| lidOpenHeight | float | 0.8 | 0–1.5 | m |
| lidOpenAngle | float | 110 | 0–180 | deg |
| pickupRadius | float | 0.6 | 0.3–1 | m |
| pickupSpinDegPerSec | float | 90 | 0–360 | deg/s |
| pickupBobAmplitude | float | 0.15 | 0–0.3 | m |
| jumpStretch | float | 1.25 | 1–1.5 | × |
| landSquash | float | 0.7 | 0.5–1 | × |
| crouchScaleY | float | 0.7 | 0.5–1 | × |
| squashRecoverSharpness | float | 12 | 4–30 | 1/s |
| flipDuration | float | 0.35 | 0.2–0.6 | s |
| diveTiltDeg | float | 80 | 45–90 | deg |

## 5. Input (코드에서 `InputAction` 생성, `.inputactions` 에셋 미사용)
| Action | 키보드/마우스 | 게임패드 |
|---|---|---|
| Move | WASD, 방향키 | Left Stick |
| Look | Mouse delta | Right Stick (×stickSensitivity×dt) |
| Jump | Space | South |
| Crouch (웅크리기/엉덩방아) | Left Shift, C | East, Left Trigger |
| Dive | E, Left Mouse | West |
| Pause | Escape | Start |
| Confirm (시작/결과 화면 재시작) | Enter, Numpad Enter | South (Intro/결과 상태에서만 의미) |

## 6. Dependencies (시스템 간 계약)
- 모든 공용 타입은 `Assets/Scripts/OrangePlatformer/Core/` (네임스페이스 `OrangePlatformer.Core`). UI는 Core만 참조.
- 세션 발견: `GameSessionLocator.Current` / `Registered` 이벤트. 플레이어 발견: 태그 `Player`.
- 레벨 오브젝트 → 세션: `CollectOrange`, `PlayerFell`, `SetCheckpoint`, `ReachGoal`.
- 세션 → 플레이어: `IRespawnable.Respawn`, `ControlsEnabled`.
- 플레이어 → 연출: `IPlayerMotorView` 이벤트/상태.
- 플레이어/카메라/세션 → 입력: `IPlayerInput`.
- 어셈블리: `OrangePlatformer.asmdef`(런타임, refs: Unity.InputSystem, UnityEngine.UI), `OrangePlatformer.Tests.asmdef`(Editor, UNITY_INCLUDE_TESTS).

## 7. 레벨·연출·씬 구성
최종 답변(파이프라인 계약)의 `## Level`, `## Look`, `## Scene wiring` 절과 동일. 좌표는 +Z 직선 코스, 상단면 y 기준.

## 8. Acceptance
최종 답변 `## Acceptance` 1–18번과 동일.

## 9. Assumptions / Open questions
- 믹서 용기 테두리 높이 ≈2.0 m 가정(이단 점프 2.9 m로 진입). 실측 2.7 m 초과 시 믹서 앞에 0.8 m 디딤 블록 추가.
- 캐릭터 FBX 정면이 +Z라고 가정. 아니면 Model 자식만 회전.
- UI 문구 영어(한글 폰트 에셋 부재). 한글 UI는 nice-to-have(맑은 고딕 Font 에셋 추가).
- Nice-to-have: 착지 먼지 파티클, 엉덩방아 카메라 셰이크, 효과음(VARCO Sound), 지상 다이브, 최고 기록 저장.
