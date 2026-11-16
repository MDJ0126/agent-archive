# DotAction Sprite Studio

로컬 도트 보정·픽셀 드로잉·프레임 애니메이션 도구입니다. Node.js 20 이상과 설치된 의존성을 사용합니다. Unity 실행 없이 편집할 수 있습니다.

## 실행

프로젝트 루트에서 `powershell -ExecutionPolicy Bypass -File tools/Open-SpriteStudio.ps1`을 실행합니다. 기본 브라우저에 `http://127.0.0.1:43127`을 엽니다. 화면 전환 없이 서버만 실행하려면 `-NoBrowser`를 붙입니다. 종료는 실행 콘솔의 Ctrl+C입니다. 서버 종료 전 문서를 저장하세요.

현재 의존성은 설치되어 있습니다. 다른 PC에서는 `tools/sprite-studio`에서 `pnpm install --frozen-lockfile` 또는 `npm install` 후 실행합니다. 소스는 Node 내장 기능, pngjs 7, 공식 MCP SDK 1.32 및 zod 4를 사용합니다. 의존성 버전은 pnpm-lock.yaml로 고정합니다.

## 작업 순서

1. 에셋 ID와 크기를 정하고 새 문서를 만듭니다. 기본은 32×32입니다. 배경·타일도 최대 256×256까지 사용할 수 있습니다.
2. 이미지를 가져옵니다. 생성 이미지에는 면적 평균, 이미 정렬된 픽셀 그림에는 최근접을 선택합니다. 원본 캔버스를 같은 크기로 유지해야 프레임별 크기가 달라지지 않습니다. 큰 그림을 32×32로 줄이면 작은 장식은 사라질 수 있습니다.
3. 단색 배경은 색상·허용 차이를 지정해 제거합니다. 체크무늬가 실제로 그려진 이미지나 복잡한 배경은 자동 분리하지 않습니다.
4. 팔레트 버튼과 지우개로 픽셀을 수정합니다. 드래그 드로잉, 실행 취소·다시 실행을 지원합니다.
5. 승인한 프레임을 기준으로 지정하고 복제한 뒤 새 자세를 그립니다. 이전 프레임·기준 겹치기와 실루엣 차이를 함께 확인합니다. 통째 이동은 포즈 제작을 대신하지 않습니다.
6. 프레임 이름·동작 태그·시간(ms), 순서를 편집합니다. 재생에서 선택 동작만 반복할 수 있습니다. 같은 이름의 태그가 떨어져 있으면 출력 메타데이터는 각각의 연속 구간으로 분리됩니다.
7. 저장하고 시트를 출력합니다. `.sprite.json`이 Studio 편집 기준 원본입니다. PNG와 Aseprite JSON 형식의 프레임 배열·태그·시간을 출력합니다. 프로젝트의 Aseprite 빌드가 있으면 `.aseprite`도 함께 만들고 재열어 프레임 수·시간을 확인합니다.

여러 파일은 입력 순서대로 처리합니다. 시트를 가져올 때 분할 열/행은 이미지 크기를 정확히 나눠야 합니다. 첫 셀을 현재 프레임에 넣으려면 ‘새 프레임으로 추가’를 끕니다. 이미지에서 시간 정보는 복구되지 않으므로 별도로 정합니다.

## 저장 위치와 동시 편집

| 항목 | 위치 |
|---|---|
| 원본 이미지 | 가져온 원래 파일 보존; 생성 원본은 art/reference 권장 |
| Studio 편집 원본 | art/source/sprite-studio/에셋ID.sprite.json |
| Aseprite 편집 사본 | art/source/sprite-studio/native/에셋ID-리비전-시각.aseprite |
| PNG·JSON 출력 | art/exports/sprite-studio/에셋ID-리비전-시각.* |

출력은 새 파일명으로 남겨 이전 출력을 덮어쓰지 않습니다. Aseprite에서 한 수정은 Studio JSON으로 자동 역동기화되지 않습니다. Aseprite에서 계속 편집할 경우 해당 `.aseprite`를 새 기준 원본으로 삼으세요.

MCP 수정 전에는 브라우저 문서를 저장하세요. MCP 작업 후 ‘불러오기 / MCP 변경 새로고침’을 누릅니다. 저장 충돌은 revision 검사로 거부하며, 미저장 내용을 자동 덮어쓰지 않습니다. 실행 취소 기록은 메모리에 최대 40개 유지합니다. 페이지 재열기는 기록을 초기화합니다.

프로세스가 비정상 종료되어 `.writer.lock`이 남으면 모든 Studio/MCP 쓰기 프로세스가 종료되었는지 확인한 뒤, 파일 안의 PID가 더 이상 실행 중이지 않을 때만 `art/source/sprite-studio/.writer.lock`을 삭제합니다. 원본 JSON은 삭제하지 않습니다.

## AI와 MCP 작업

Codex에 `dotaction-sprite-studio` stdio 서버가 등록되어 있습니다. 현재 대화에 도구가 나타나지 않으면 다음 연결에서 노출을 확인해야 합니다. 서버 등록과 현재 대화에서의 도구 노출은 별개입니다.

다른 MCP 호스트는 `tools/sprite-studio/mcp.json`을 참고합니다. 명령은 `node`, 인자는 이 프로젝트의 `tools/sprite-studio/mcp.mjs` 절대 경로입니다. 플러그인 폴더를 프로젝트 밖으로 옮기면 팔레트와 출력 경로도 바뀌므로 그대로 복사해서 독립 설치하지 않습니다.

도구는 목록, 생성, 읽기, 기준 이미지 보기, 픽셀 그리기, 복제, 정수 이동/반전, PNG 또는 로컬 PNG 가져오기, 전체 문서 저장, 시트 출력의 11개입니다. 원본·출력은 이 프로젝트에 제한됩니다. 로컬 PNG 가져오기는 art 내부 실제 경로만 허용합니다.

예시 요청:

> demo_scarf_idle을 기준으로 대기 애니메이션을 다듬어 줘. sprite_reference로 실제 기준 그림을 확인하고 얼굴·장비·팔레트·발 위치를 유지해 줘. 프레임을 복제해 스카프와 몸의 자세를 픽셀 단위로 수정하고, 총 4프레임으로 저장한 뒤 시트를 내보내 줘.

생성 모델을 사용할 경우에는 호스트 이미지 생성 도구에 **실제 기준 이미지**를 전달해 자세를 생성하고, 결과 원본을 art/reference에 보관한 뒤 sprite_import_file로 보정합니다. 이 MCP 자체에 이미지 생성 모델이나 유료 API 호출은 포함하지 않습니다. 모델이 만든 동일 캐릭터의 형태 일관성은 자동 보장하지 않습니다. 프레임 차이 수치는 검토 지표입니다.

## Unity 연결

1. 기존 `tools/Sync-Art.ps1`로 출력물을 `Assets/DotAction/Art`에 복사합니다.
2. `Art/sprite-studio`의 PNG를 선택합니다.
3. `DotAction > Sprite Studio > 선택한 시트에서 애니메이션 만들기`를 실행합니다.

시트가 Multiple·Point·PPU16으로 분할되고, 연속 태그별 AnimationClip·AnimatorController·SpriteRenderer 프리팹이 새 폴더에 생성됩니다. 원점은 프레임 하단 중앙입니다. 모든 클립은 기본 반복이며 공격처럼 한 번만 재생할 클립은 Inspector에서 Loop Time을 끄세요. 동작 간 전이는 게임 로직에 맞게 추가해야 합니다. 기존 씬은 변경하지 않습니다.

CLI에서도 Unity `-executeMethod DotAction.Editor.SpriteStudio.SpriteSheetBuilder.BuildFromCommandLine -spriteStudioSheet Assets/DotAction/Art/sprite-studio/파일.png`로 가져올 수 있습니다.

## 확인 근거

실제 MCP 연결로 `demo_scarf_idle` 4프레임을 그려 128×32 PNG·JSON을 출력했습니다. 브라우저에서 시트를 다시 4개로 분할하고 저장·재열기를 확인했습니다. Aseprite 원본 생성·재열기에서 프레임 수와 시간이 일치했습니다. 예제는 도구 사용 확인용 초안이며 최종 게임 캐릭터 승인이 아닙니다.

Unity 6000.0.83f1 배치 실행에서 `SPRITE_STUDIO_IMPORT_OK: 4 frames`와 종료 코드 0을 확인했습니다. 실제 `.anim`, `.controller`, `.prefab`이 생성되었습니다. 플레이 모드의 게임 화면과 동작 간 전이는 미확인입니다.

- [초기 화면](./before.jpg)
- [결과 화면](./after.jpg)
- [MCP 공식 도구 규격](https://modelcontextprotocol.io/specification/2025-06-18/server/tools)
- [Aseprite Sprite API](https://aseprite.org/api/sprite)

별도 자동 테스트나 회귀 테스트는 실행하지 않았습니다. 실제 편집·원본 저장·출력·통합 실행을 확인했습니다.
