import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { escapeHTML as escape, renderMarkdown } from './render.mjs';

const topic = (id, label) => [id, `관계도/${label}.md`, label];

const guideRows = [
  [topic('global', '전역 지침')],
  [
    topic('work', '작업별 지침'),
    topic('qa', '품질 점검 절차'),
    topic('authoring', '결과 보고 기준'),
    ['projects', null, '프로젝트 전용 지침'],
  ],
  [
    topic('naming', '네이밍 규칙'),
    topic('style', '코드 스타일'),
    topic('qa-record', '검수 기록'),
    topic('qa-evidence', '검수 증거'),
    ['workflow', 'workflow.md', '보고서 처리 가이드'],
  ],
  [
    ['report', 'template-report.md', '결과 보고서 양식'],
    ['bug', 'template-bug.md', '버그 보고서 양식'],
  ],
];

const edges = [
  ['global', 'work', 'required'],
  ['global', 'qa', 'required'],
  ['global', 'authoring', 'required'],
  ['global', 'projects', 'required'],
  ['global', 'reporting-exceptions', 'required'],
  ['work', 'naming', 'reference'],
  ['work', 'style', 'reference'],
  ['qa', 'qa-record', 'reference'],
  ['qa', 'qa-evidence', 'reference'],
  ['authoring', 'qa-evidence', 'reference'],
  ['authoring', 'workflow', 'reference'],
  ['workflow', 'report', 'reference'],
  ['workflow', 'bug', 'reference'],
];

const projectReporting = {
  'sampleprojectforunity': '샘플 프로젝트 보고 예외',
  'dotaction': '캠프 가디언 보고 예외',
  'snsmanager': '소셜 게시 보고 예외',
  'fantasya': '판타지아 보고 기준',
  'codex-config': '중앙 아카이브 보고 기준',
};

export async function buildRelationships(root, projects, shell) {
  const previews = [];
  async function documentNode(id, file, label, source, project = '', projectLabel = '') {
    const raw = await readFile(path.join(root, source), 'utf8');
    const markdown = raw.replace(/^---\r?\n[\s\S]*?\r?\n---\r?\n\s*/, '');
    const base = file.split('/').map(encodeURIComponent).join('/');
    const title = `${label}.md`;
    const { html, toc } = renderMarkdown(markdown, { bodyMode: true });
    const caption = projectLabel || `제목 ${toc.length}개`;
    const sections = /^#\s/.test(markdown) ? toc.slice(1) : toc;
    const contents = sections.length > 3
      ? `<details class="relations-contents"><summary>이 문서의 목차</summary><ul>${sections.map(({ id, label }) =>
        `<li><a href="#${id}" data-jump="${id}">${label}</a></li>`).join('')}</ul></details>`
      : '';
    previews.push(`<template id="preview-${escape(id)}">${contents}${html}</template>`);
    return `<button type="button" class="relations-node" data-node="${escape(id)}" data-preview="${escape(base)}" data-project="${escape(project)}" data-title="${escape(title)}"${project && project !== 'codex-config' ? ' hidden' : ''}><strong>${escape(title)}</strong><small>${escape(caption)}</small></button>`;
  }

  const projectDocuments = Object.entries(projects).sort(([, a], [, b]) => a.name.localeCompare(b.name, 'ko'));
  const projectNodes = await Promise.all(projectDocuments.map(([id, project]) =>
    documentNode(`project-${id}`, `../projects/${id}/guidelines.md`, '프로젝트 전용 지침', `projects/${id}/guidelines.md`, id, project.name)));
  const reportingProjects = projectDocuments.filter(([id]) => Object.hasOwn(projectReporting, id));
  const exceptionNodes = await Promise.all(reportingProjects.map(([id, project]) => {
    const label = projectReporting[id];
    const file = `관계도/${label}.md`;
    return documentNode(`reporting-${id}`, file, label, `guide/${file}`, '', project.name);
  }));
  const exceptionFile = '관계도/보고 예외 프로젝트.md';
  const exceptionRoot = await documentNode('reporting-exceptions', exceptionFile, '보고 예외 프로젝트',
    `guide/${exceptionFile}`, '', `프로젝트 ${reportingProjects.length}개`);
  const rows = await Promise.all(guideRows.map(async (row, index) => {
    const nodes = await Promise.all(row.map(([id, file, label, source]) => file
      ? documentNode(id, file, label, source ?? `guide/${file}`)
      : `<a class="relations-node" data-node="${id}" href="project-instructions.html"><strong>${label}</strong><small>프로젝트별 문서 목록</small></a>`));
    if (index === 2) nodes.push(`<div class="relations-project">${projectNodes.join('')}</div>`);
    return `<div class="relations-row">${nodes.join('')}</div>`;
  }));
  const options = projectDocuments.map(([id, project]) =>
    `<option value="${escape(id)}"${id === 'codex-config' ? ' selected' : ''}>${escape(project.name)}</option>`).join('');
  const renderedEdges = [...edges,
    ...projectDocuments.map(([id]) => ['projects', `project-${id}`, 'reference']),
    ...reportingProjects.map(([id]) => ['reporting-exceptions', `reporting-${id}`, 'branch']),
  ]
    .map(([from, to, kind]) => `<span data-from="${escape(from)}" data-to="${escape(to)}" data-kind="${kind}"></span>`).join('');
  const body = `<main class="relations-page">
    <div class="relations-heading"><h1>관계도 관리</h1><p>중앙 아카이브의 지침과 가이드 연결을 살펴보고, 문서를 눌러 원문을 읽으세요.</p></div>
    <section class="relations-panel" aria-label="Markdown 문서 관계도">
      <div class="relations-toolbar"><div class="relations-legend"><span>적용 지침</span><span>참조 문서</span></div>
        <label for="relations-project">전용 지침 프로젝트 <select id="relations-project">${options}</select></label>
      </div>
      <div class="relations-scroll" tabindex="0" aria-label="문서 관계도, 가로 스크롤 가능">
        <div class="relations-board"><canvas aria-hidden="true"></canvas>${rows[0]}
          <div class="relations-main-branch">${rows.slice(1).join('')}</div>
          <div class="relations-exception-branch">${exceptionRoot}<div class="relations-exceptions">${exceptionNodes.join('')}</div></div>
        </div>
      </div>
    </section>
    <dialog class="relations-dialog" aria-labelledby="relations-title">
      <div class="relations-dialog-bar"><div><strong id="relations-title"></strong><small id="relations-location"></small></div>
      <a class="relations-source" target="_blank" rel="noopener">Markdown 원문 ↗</a><form method="dialog"><button autofocus>닫기 <span aria-hidden="true">×</span></button></form></div>
      <article class="relations-document"></article>
    </dialog>
    <template id="relations-edges">${renderedEdges}</template>${previews.join('')}
  </main><script defer src="../assets/relationships.js"></script>`;
  const html = shell('관계도 관리', body, '../').replace('</head>',
    '<link rel="stylesheet" href="../assets/relationships.css"></head>');
  await writeFile(path.join(root, 'guide/relationships.html'), html);
}
