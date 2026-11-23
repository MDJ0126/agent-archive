# 지침 동기화와 도구 관리

## 업데이트와 동기화

- Run `node <archive>/리포트/check-updates.mjs` at the start of each new user request, on resumption or context recovery, and before the final report. Pass `--since <ID>` for an ID already read in this session. Do not reuse another session's status or a shared marker. If the script is absent, read `리포트/guide/updates.json` directly from GitHub main. The [public update board](https://mdj0126.github.io/codex-config/guide/updates.html) presents the same records.
- Read newly linked source instructions and apply them to the current task. Proceed with already approved configuration, installation, or synchronization. Preserve and merge local unfinished work. Higher-priority instructions and the user's latest explicit request take precedence. An update does not expand authorization, costs, or publication scope. Do not automatically run remote code or overwrite local instructions.
- If the remote check fails, continue using known instructions and local records, report that the remote state is unverified, and retry at the next checkpoint. Avoid repeated retries at the same checkpoint. Report the latest update ID actually verified and any unapplied or unverified scope. Do not assume active sessions automatically reload instructions.
- When changing global or shared instructions, prepend a unique increasing ID, actual date, scope, summary, and source path to the update JSON, and advance `latest`. Preserve earlier records. Publish the instructions, update entry, and task report in the same commit.
- When editing global instructions, synchronize the installed copy and repository source and compare their hashes. Follow the synchronization section of the work instructions. Commit and push only approved files, preserving other work and secrets.

## 스킬과 도구 선택

- Assess SkillEvaluator and CLI-Anything for each task. Find their `SKILL.md` files under `$CODEX_HOME/skills` or `~/.codex/skills` even if absent from the skill list.
- Run `skillevaluator quality-check <skill-dir>` and `skillevaluator harness-check <skill-dir>` automatically only when first adding a new skill to the canonical installation. For edits, updates, reinstalls, another PC's restoration, synchronization, and routine checks, run assessments only within an explicit evaluation request. Preserve upstream sources and report scores, findings, scope, and exit codes. Static scores do not establish security or practical effectiveness. Model execution, cost, host access, and credentials follow the existing authorization.
- For GUI automation and repeated operations, check existing CLI-Anything harnesses and CLI-Hub first. Reuse a sufficient dedicated CLI, official API, or SDK. If needed, use the CLI-Anything methodology to implement a project-owned adapter. Verify the real backend, files, and output before claiming support. Follow the Affinity, upstream preservation, and focus rules.
- Use the isolated `uv tool` installs of `skillevaluator` and `cli-hub`. On Windows, if they are missing from PATH, try `~/.local/bin/<command>.exe` directly, then check installation status and errors. Existing authorization covers these global installs. Additional cost, credentials, and host execution follow existing authorization.
- The canonical `reverse-engineer-anything` is `~/.agents/skills/reverse-engineer-anything`. Preserve the older backup at `~/.codex/skill-backups/2026-11-20/reverse-engineer-anything` and keep it outside automatic discovery. Compare both before intentionally switching versions.
- Use `music-gen` for melody, harmony, arrangement, and MIDI rendering. Use `sound` for effects, UI audio, ambience, synthesis, and audio quality analysis. When both apply, use them for distinct parts of the work.
- For Cloudflare, prefer the feature-specific skill and supplement it with shared Cloudflare guidance as needed. Select stable, next, or migration from the actual dependency and requested transition. Choose native or Google-connected document and spreadsheet skills based on the real target and file format.
- Preserve official plugin caches and upstream skills. Static formatting or size warnings alone do not establish a loading failure or justify patching or deleting upstream material. Distinguish estimated descriptions and CLI profiles from what an actual session discovers. Check new-chat discovery separately from installation.

## 다른 PC 복원

- The installation manifest is `tools/skills/manifest.json` in the central repository. It records installation paths, source snapshots, SHA-256 hashes, pinned CLI versions, and app-managed plugins. Use the manifest for the current inventory. Initial registration covered 18 user skills, with 17 in Codex home and REA under `~/.agents/skills`, plus skillevaluator 1.0.1 and cli-anything-hub 0.4.1.
- On a new PC, in a new session, or when applying updates, run `tools/skills/Restore-Skills.ps1 -CheckOnly`. If a registered item is missing, run `Restore-Skills.ps1` immediately under the existing authorization for registered skills, CLIs, and required uv/Python. Resolve paths using the current home and `CODEX_HOME`. `Sync.ps1 -Direction Pull` performs restoration after instruction synchronization.
- For a new install, modification, upgrade, or intentional removal, run `Export-Skills.ps1` to update the manifest, snapshots, and hashes. Record and publish related instructions, the update board entry, and the individual result together. Keep manifest entries for other PCs unless removal was expressly requested.
- Preserve and review differing local skill content and CLI versions before merging. Do not copy `.system` skills, official plugin caches, login data, or tokens. Use Plugin Management to find and connect a missing app-managed plugin. Record authentication requirements as pending, and distinguish cache presence from connection.
- After installation, verify source hashes, CLI versions, and new-chat discovery separately. Apply the SkillEvaluator rule above. Treat any restoration not executed on the other PC as unverified. Do not claim that background synchronization exists unless demonstrated.

## 프로젝트 도입 절차

- 새 저장소의 초기화 또는 미적용 저장소의 첫 승인된 쓰기 작업에서 기존 루트·하위 지침을 조사합니다. 기존 규칙과 이력을 원문에 보존하고 루트에 공통 규칙과 작업별 트리 진입점을 둡니다. 기존 트리가 있으면 재사용합니다.
- 읽기 전용 작업에서는 현재 지침을 따르고 도입이 남아 있음을 알립니다. 충돌이나 바이트 예산 문제가 있으면 규칙을 임의로 덜어내기 전에 이유와 해결안을 사용자에게 알립니다.
- 수정 전 범위·예산·필수 규칙·원문 위치·검증 기준을 제시합니다. 모든 지침 수정에 요약과 원문 연결을 갱신하고 UTF-8 크기, 상대 링크, 충돌·예외 보존을 확인합니다. 공통 4 KiB는 설치된 전역 파일과 프로젝트 공통 진입 문서를 합산하고, 중앙 저장소에서 설치본과 같은 내용은 한 번만 셉니다.
- 자동 로딩 한도는 사용자 설정 최상위 `project_doc_max_bytes = 16384`입니다. 프로젝트별 파일을 바꾸기 전에 기존 설정과 지침을 확인합니다. 한도 변경이나 다른 기기의 설치가 필요하면 실제 반영 범위를 구분해 알립니다.

### 주제별 문서 분리

- 모든 프로젝트는 자동으로 읽는 공통 지침에서 주제 색인으로 연결합니다. 색인에는 주제 이름과 링크를 두고 세부 규칙은 소관 Markdown에서 관리합니다.
- 문서 안에서도 독립적으로 조회·수정·검수하는 주제가 여럿이면 하위 색인과 개별 Markdown으로 나눕니다. 함께 읽어야 의미가 통하는 짧은 절과 표는 같은 파일에 둡니다.
- 질문에 답할 때에는 색인에서 관련 경로 하나를 고르고 해당 하위 원문을 엽니다. 문서 이동 시 참조 링크·앵커, 기존 대시보드와 검색, 요약과 원문의 조건·예외를 확인합니다.
- 새 프로젝트는 첫 승인된 쓰기 작업에서 이 구조로 시작합니다. 기존 프로젝트는 지침 정리나 관련 문서 수정 때 해당 가지부터 적용합니다.
