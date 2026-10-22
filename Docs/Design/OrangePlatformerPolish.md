# OrangePlatformerPolish — 1스테이지 폴리시(지터 수정 · 장식 모델 · 캐릭터 애니메이션)

- 대상: 기존 `OrangePlatformer` 1스테이지(기획서 `Docs/Design/OrangePlatformer.md`). **규칙·레벨 수치·조작·OrangePlatformerConfig 값은 변경 없음.**
- 씬: 기존 `Assets/Scenes/OrangePlatformer.unity`를 그대로 수정 (Build Settings index 0). 계획 승인 시 씬 복제안은 제외됨 — 이미 새 조명·주스 바다·배경 섬이 들어간 씬이라 한 씬만 유지.
- 새 코드: `Assets/Scripts/OrangePlatformerPolish/` (asmdef `OrangePlatformerPolish` → refs `OrangePlatformer`). 기존 파일 수정은 `PlayerSignals.cs`, `PlayerController.cs`, `OrbitCamera.cs`, `PlayerVisuals.cs` 네 개만.

## 1. Purpose
- 걷기·달리기 때 캐릭터가 화면에서 떨리지 않고 매끄럽게 움직인다(조작감 신뢰).
- 액션마다 전용 동작이 재생돼 "지금 무슨 기술을 썼는지" 몸으로 읽힌다.
- 귤 과수원 섬 + 오렌지주스 바다 테마를 생성 모델로 채워 화면이 풍성해진다.

## 2. 지터 수정 — 결정과 근거
- **결정: 모터는 FixedUpdate(50 Hz) 유지 + 렌더 보간.** 모터를 Update로 옮기면 가변 dt로 점프 정점·이단/높이뛰기 높이·코요테/버퍼 타이밍이 프레임률에 따라 흔들리고(§3.4 차단 검증표가 고정 dt 기준), 기존 EditMode 테스트(`PlayerMovementMathTests`)는 순수 수학이라 어느 쪽이든 통과하지만 플레이 수치 보존은 고정 틱이 안전하다.
- 원인: 루트는 20 ms마다 한 번 이동, 렌더는 vsync 0·무제한 → 대부분 프레임에서 이동 0, 일부 프레임에서 한 틱만큼 점프. 카메라는 LateUpdate에서 계단식 루트를 지수 추적 → 화면상 캐릭터가 앞뒤로 떨림.
- 해결 (Rigidbody Interpolate와 동일 방식, 한 틱 지연):
  - `PlayerRenderInterpolator`(루트에 부착, `[DefaultExecutionOrder(-200)]`): `FixedUpdate`에서 이번 틱 이동 **전** 루트 위치를 `prevTick`으로 저장(PlayerController보다 먼저 실행). `LateUpdate`에서 `currTick = root.position`, `alpha = RenderInterpolation.Alpha(Time.time, Time.fixedTime, Time.fixedDeltaTime)` = clamp01((time − fixedTime)/fixedDt), `render = Lerp(prevTick, currTick, alpha)`, `visual.localPosition = baseLocalPos + root.InverseTransformVector(render − root.position)`.
  - 예: fixedDt 0.02, 6 m/s → 틱당 0.12 m. 렌더 240 fps에서 프레임당 시각 이동 ≈ 0.025 m 균일(보간 전: 0,0,0,0,0.12 반복).
  - 텔레포트: `IPlayerMotorView.Teleported`(Respawn 끝에서 발생) 수신 시 `prevTick = currTick = root.position`, Visual 즉시 원위치. 보조 안전장치: |currTick − prevTick| > `teleportSnapDistance`(3 m; 정상 최대 틱 이동 25 m/s×0.02 = 0.5 m)면 스냅.
  - `interpolationEnabled=false`면 Visual은 `baseLocalPos` 고정(디버그 비교용).
- 카메라: `OrbitCamera.target`을 Player 루트 → **Visual**(보간된 위치)로 교체, `ignoreRoot` = Player 루트. 인터폴레이터 LateUpdate(-200)가 OrbitCamera LateUpdate(0)보다 먼저 실행. `OrbitCamera`는 `target.GetComponentInParent<IPlayerMotorView>().Teleported`를 구독해 `focus`를 즉시 목표로 스냅(리스폰 시 카메라가 바다 밑에서 끌려오지 않게).
- 회전: 루트 yaw는 이미 Update에서 `RotateTowards(turnSpeedDeg × Time.deltaTime)`로 렌더 프레임마다 갱신 → 유지(Visual이 상속). 모터는 `desiredFacing`만 사용하므로 회전 위치와 무관.
- 점프 높이 판정·테스트는 루트(물리) 위치 기준이라 보간 영향 없음.

## 3. 캐릭터 애니메이션
### 3.1 클립 (Orange_Character.fbx, Generic 리그 유지, 테이크 이름 `Orange_Character_Armature|<Clip>`)
| Clip | 루프 | 길이 목표 | 용도 |
|---|---|---|---|
| Idle | O | 기존 | 정지 |
| Walk | O | 기존 | 0.1 ≤ 수평속도 < 4.5 (웅크림 이동 2.5 m/s, 가속 중) |
| Run | O | 0.6–0.8 s | 수평속도 ≥ 4.5 (기본 이동 6 m/s) |
| Jump | X | 0.3–0.4 s | 상승(vy > 0.5) |
| Fall | O | 0.5–1 s | 하강 |
| Dive | X(마지막 포즈 유지) | 0.25 s | Dive·DiveSlide (배 깔고 앞으로 뻗음) |
| WallSlide | O | 0.5–1 s | 벽 슬라이드(벽을 등지고 한 손 짚음) |
| GroundPound | X | ≈0.25 s 감기 + 낙하 포즈 유지 | Hang·Fall |
| Victory | O | 1.5–2.5 s | Won |
- 루프 클립은 첫/끝 프레임 포즈 일치(Blender에서 끝 키를 첫 키 복사, Cycles 모디파이어 금지), 루트 수평 이동 제거(제자리).

### 3.2 상태 결정 (순수 함수 `PlayerAnimStateResolver.Resolve`, 우선순위 위→아래)
1. `won` → Victory
2. GroundPoundHang/GroundPoundFall → GroundPound
3. Dive/DiveSlide → Dive
4. WallSlide → WallSlide
5. Airborne: vy > `jumpRiseMinVy`(0.5) → Jump, 아니면 Fall
6. Grounded/Crouching/GroundPoundLand: hs < `idleSpeedThreshold`(0.1) → Idle, hs < `runSpeedThreshold`(4.5) → Walk, 아니면 Run
- hs = sqrt(vx²+vz²), `won` = `GameSessionLocator.Current?.State == GameState.Won`.
- `PlayerAnimationDriver`(Visual에 부착, Update): 결정값이 바뀌면 `animator.CrossFadeInFixedTime(<enum 이름>, fade)`(fade = Victory는 `victoryCrossFade` 0.25 s, 그 외 `animCrossFade` 0.1 s). `Jumped` 이벤트(이단·벽 점프 포함)마다 Jump를 normalizedTime 0부터 재시작. 컨트롤러 전이·파라미터 없음(스크립트가 단일 권한).
- 예: 정지에서 W 유지, 지상 가속 40 m/s² → t = 0.1/40 = 0.0025 s에 Walk, t = 4.5/40 = 0.1125 s에 Run, t = 6/40 = 0.15 s에 최고속 6 m/s. 웅크림+W는 2.5 m/s 상한이라 Walk 유지.

### 3.3 절차적 연출 조정 (`PlayerVisuals`)
- 제거: Animator `Speed` 설정(드라이버가 대체), 다이브 X축 80° 기울기, 벽 슬라이드 Z축 15° 기울기(클립이 포즈 담당).
- 유지: 점프 stretch / 착지 squash / 웅크림 scaleY 0.7, 이단 점프 X축 360° 앞구르기, 높이뛰기 Y축 720°, 엉덩방아 Hang 360°(클립 위에 Spin으로 겹침).
- `config.diveTiltDeg`는 사용처가 없어지나 OrangePlatformerConfig 필드는 유지(에셋 값 보존).

## 4. Parameters → `OrangePlatformerPolishConfig` (`Assets/Settings/OrangePlatformerPolish/OrangePlatformerPolishConfig.asset`)
| name | type | default | range | unit |
|---|---|---|---|---|
| interpolationEnabled | bool | true | — | — |
| teleportSnapDistance | float | 3 | 1–10 | m |
| idleSpeedThreshold | float | 0.1 | 0.05–0.5 | m/s |
| runSpeedThreshold | float | 4.5 | 3–5.5 | m/s |
| jumpRiseMinVy | float | 0.5 | 0–2 | m/s |
| animCrossFade | float | 0.1 | 0–0.3 | s |
| victoryCrossFade | float | 0.25 | 0–0.5 | s |

## 5. 장식 모델 (VARCO 3D, 카툰·둥근 형태, 오렌지 #FF9A1F / 잎 초록 #6CC24A / 크림 #FFF1D0)
| 이름 | 크기(m) | 피벗 | 삼각형 | 배치 |
|---|---|---|---|---|
| Deco_OrangeTree (열매 달린 귤나무) | 높이 4 | 밑면 중앙 | ≤8k | Tree_1/Tree_2 교체 + 배경 섬마다 1–2그루 (총 ≤24) |
| Prop_FruitCrate (나무 과일 상자) | 기존 BreakableCrate BoxCollider에 맞춤 | 밑면 중앙 | ≤4k | Crate_L/Crate_R 외형 교체(콜라이더·스크립트 유지) |
| Deco_JuiceCarton (주스 팩) | 높이 1.6 | 밑면 중앙 | ≤3k | G_Landing 옆, 시작 지점 |
| Deco_OrangeHalfFloat (귤 반쪽 단면) | 지름 3 | 밑면 중앙(수면) | ≤6k | 주스 바다에 6개 |
| Deco_JuicerLandmark (착즙기) | 높이 12 | 밑면 중앙 | ≤20k | 골 뒤 배경 섬 1개 |
| Deco_SignBoard (귤 그림 나무 표지판) | 높이 1.6 | 밑면 중앙 | ≤3k | 시작·수직갱·높은 단·넓은 틈 앞 4개 |
| Deco_FlowerBush (꽃덤불) | 지름 1.2 | 밑면 중앙 | ≤4k | 코스 가장자리 10개 |
| Deco_StrawFlag (빨대 깃대) | 높이 2.5 | 밑면 중앙 | ≤3k | CP0–CP4 옆 장식(기존 체크포인트 깃발·색 변경은 그대로) |
- 장식은 콜라이더 없음, Static. 코스 중심선(x=0)에서 |x| ≥ 2.5 m(상자 x ±2.5 제외) 이격, 점프 궤적 위 공중에 두지 않음.

## 6. Dependencies
- `OrangePlatformer.Core.IPlayerMotorView`에 `event Action Teleported;` 추가 → PlayerController.Respawn 마지막에 발생 → 인터폴레이터·OrbitCamera·(선택)AnimationDriver 구독.
- Animator 상태 이름 = `PlayerAnimState` enum 이름(정확히 일치). 컨트롤러는 씬 단계가 새로 만든 `Assets/Art/OrangePlatformerPolish/OrangeCharacterPolish.controller`(파라미터·전이 없음, 9 상태).
- 세션 상태는 기존 `GameSessionLocator.Current`.

## 7. Acceptance
최종 파이프라인 답변의 `## Acceptance`와 동일(지터 수치, 애니메이션 상태, 장식 표시, 기존 루프 회귀).

## 8. Assumptions / Open questions
- VARCO Animate 카탈로그에 벽 슬라이드·엉덩방아 전용 동작이 없을 수 있음 → 가장 가까운 동작 + Blender 수작업 키 보정(같은 리그)으로 대체.
- 넘어짐/부활 클립(KillZone 낙사 시 Hurt, 리스폰 시 등장)은 nice-to-have(이번 범위 제외).
- Walk/Run 재생 속도를 수평속도에 맞추는 발 미끄럼 보정, 귤 반쪽 상하 흔들림은 nice-to-have.
- 기존 수용 기준 1–19 원문은 저장소에 없음(OrangePlatformer.md §8은 18개로 표기) → 핵심 루프를 회귀 항목으로 재기술.
