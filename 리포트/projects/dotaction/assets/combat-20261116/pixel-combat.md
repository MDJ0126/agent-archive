# 3D 픽셀 전투 프로토타입

`Assets/Scenes/PixelCombat.unity`를 열고 Play를 누릅니다. 기존 `ArtReference.unity`는 정적 아트 기준으로 보존합니다.

- 마우스 왼쪽 클릭 또는 누르기: 목적지 셀로 이동합니다.
- WASD: 화면 방향을 기준으로 격자 이동합니다.
- 기본 공격: 사거리 안의 가장 가까운 적에게 청록 투사체를 자동 발사합니다.
- R: 전투를 처음부터 다시 시작합니다.
- 총 3웨이브를 처치하면 승리하고, 체력이 0이면 패배합니다.

## 저장된 구성

| 항목 | 위치 |
| --- | --- |
| 씬 | `Assets/Scenes/PixelCombat.unity` |
| 전투 수치 원본 | `Assets/Data/CombatStats.xlsx` |
| 픽셀 프리셋 | `Assets/Data/CombatPixels.asset` |
| 3D Forward 렌더러 | `Assets/Data/CombatRenderer.asset` |
| 캐릭터·투사체 프리팹 | `Assets/Prefabs/Combat` |
| 프로젝트 소유 재질 | `Assets/Materials/Combat` |
| 동작 코드 | `Assets/Scripts/Runtime` |

DDT에서 CombatStats 원본을 열어 저장하면 타입 코드와 바이너리를 재생성합니다. Id 1은 레인저, 2는 고블린, 3은 스켈레톤입니다. Health·Speed·Damage·Range·Cooldown을 조절합니다. 생성 코드를 직접 수정하지 않습니다. Addressables는 사용하지 않으며 등록된 바이너리를 직접 참조합니다.

XZ 평면의 1m 셀과 8방향 A* 경로를 사용합니다. 대각선 이동 시 벽의 모서리를 가로지르지 않습니다. 이동은 셀 사이를 보간하며 출발·도착 셀을 예약합니다. 같은 셀의 동시 점유를 막지만, 다수 적의 복잡한 교착을 해결하는 군중 내비게이션은 이번 프로토타입의 범위가 아닙니다.

## 외부 에셋과 렌더링

Fantasya의 로컬 보유본에서 Pixel Camera, Design Data Table 2.0.2, 공용 TurnRoom Templete, Polyperfect의 Low Poly Animated Fantasy Creatures를 복사했습니다. 외부 원본은 직접 수정하지 않았습니다. 캐릭터는 프로젝트 소유 프리팹·Animator Controller·Toon 재질로 확장합니다. 보유 에셋의 별도 배포 권한을 의미하지 않으며 판매용 원본은 공개 보고서에 업로드하지 않습니다.

현재 도입한 PixelCameraEffect 소스는 선택 오브젝트 경로를 사용합니다. 문서에 남아 있는 Full Camera 설정을 동작 근거로 삼지 않고, 환경과 캐릭터에 PixelCameraObject를 연결했습니다. 720p 출력에서 2px 셀을 사용합니다. 조명·색 단계·윤곽선은 프로젝트 프리셋과 재질에서 편집합니다.

PC 16:9 화면에서 직교 카메라 크기 6.2의 가까운 사선 탑뷰를 사용하며 플레이어를 부드럽게 따라갑니다. 사용자 요청에 따라 제목·안내판·화면 상태 UI와 경로선·바닥 표식을 제거하고 월드 체력바만 남겼습니다. 승패 문구도 표시하지 않으며 3웨이브 종료 후 전투가 정지합니다. R로 다시 시작합니다.

기존 URP 2D 렌더러는 목록의 첫 항목으로 보존하고, 전투 카메라만 추가한 Forward 렌더러를 사용합니다. 기존 아트 규격의 옆모습 스프라이트는 이번 3D 캐릭터의 크기 기준으로 적용하지 않습니다.

## 실제 실행 근거

결과 보고서 원본은 `docs/reports/combat-prototype-v2.json`입니다. `CombatCapture.Run`은 저장된 씬을 Play Mode로 실행하여 동일한 목적지 입력 경로를 호출하고, 카메라 스택의 실제 출력과 이동·발사·처치 수치를 저장합니다. 자동 테스트가 아닌 플레이 기록 도구이며 최종 확인 결과는 보고서에 기록합니다.

열린 사용자 에디터와 충돌하면 `.tools/combat-preview` 사본에서 실행합니다. 이 경우 원본과 대조한 파일·실행 대상·미확인 범위를 구분해서 보고합니다. 게임 플레이어 빌드와 배포는 별도입니다.
