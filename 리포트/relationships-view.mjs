import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { escapeHTML as escape, renderMarkdown } from './render.mjs';

const repository = 'https://github.com/MDJ0126/codex-config/blob/main/';
const treeHeading = '\n## Document Tree\n';
const exceptionFile = '관계도/보고 예외 프로젝트.md';

function readDocumentTree(source) {
  const normalized = source.replaceAll('\r\n', '\n');
  const start = normalized.indexOf(treeHeading);
  if (start < 0) throw Error('Missing Document Tree in AGENTS.md');
  const body = normalized.slice(start + treeHeading.length).split(/\n## /, 1)[0];
  const nodes = [];
  const ancestors = [];
  const seen = new Set();
  for (const line of body.split('\n').filter(line => line.trim())) {
    const match = /^( *)- \[([^\]]+)\]\(([^)]+)\) — (.+)$/.exec(line);
    if (!match || match[1].length % 2 || !match[3].startsWith(repository)) throw Error('Invalid Document Tree line: ' + line);
    const [, indentation, label, link, description] = match;
    const source = decodeURIComponent(link.slice(repository.length));
    const depth = indentation.length / 2;
    const guide = '리포트/guide/';
    const isRoot = nodes.length === 0;
    if (seen.has(source) || (isRoot
      ? depth !== 0 || source !== 'AGENTS.md'
      : depth === 0 || depth > ancestors.length || !source.startsWith(guide)
        || source.split('/').includes('..') || source.includes('\\') || !source.endsWith('.md'))) {
      throw Error('Invalid Document Tree parent or source: ' + source);
    }
    const file = isRoot ? null : source.slice(guide.length);
    const id = isRoot ? 'global' : file === exceptionFile ? 'reporting-exceptions' : `doc-${nodes.length}`;
    const parent = ancestors[depth - 1];
    nodes.push({ id, label, link, description, file, depth, parent });
    ancestors.length = depth;
    ancestors.push(id);
    seen.add(source);
  }
  if (!nodes.length) throw Error('Empty Document Tree');
  return nodes;
}

export async function buildRelationships(root, _projects, shell) {
  const instructions = await readFile(path.join(root, '../AGENTS.md'), 'utf8');
  const nodes = readDocumentTree(instructions);
  const exceptionRoot = nodes.find(node => node.file === exceptionFile);
  if (!exceptionRoot || exceptionRoot.depth !== 1) throw Error('Reporting exceptions must be a direct child of AGENTS.md');
  const exceptionNodes = nodes.filter(node => node.parent === exceptionRoot.id);
  const sidebar = new Set([exceptionRoot.id, ...exceptionNodes.map(node => node.id)]);
  if (nodes.some(node => sidebar.has(node.parent) && !sidebar.has(node.id))) throw Error('Only direct children are supported in the reporting exception branch');
  const entries = await Promise.all(nodes.map(async node => {
    const { id, file, label, link, description } = node;
    const raw = file ? await readFile(path.join(root, 'guide', file), 'utf8') : instructions;
    const markdown = raw.replace(/^---\r?\n[\s\S]*?\r?\n---\r?\n\s*/, '');
    const base = file ? file.split('/').map(encodeURIComponent).join('/') : link;
    const title = `${label}.md`;
    const { html, toc } = renderMarkdown(markdown, { bodyMode: true });
    const sections = /^#\s/.test(markdown) ? toc.slice(1) : toc;
    const contents = file && sections.length > 3
      ? `<details class="relations-contents"><summary>이 문서의 목차</summary><ul>${sections.map(({ id, label }) =>
        `<li><a href="#${id}" data-jump="${id}">${label}</a></li>`).join('')}</ul></details>`
      : '';
    const card = `<button type="button" class="relations-node" data-node="${escape(id)}" data-preview="${escape(base)}" data-source="${escape(link)}" data-title="${escape(title)}"><strong>${escape(title)}</strong><small>${escape(description)}</small></button>`;
    return { ...node, card, preview: `<template id="preview-${escape(id)}">${contents}${html}</template>` };
  }));
  const rows = [];
  for (const entry of entries) {
    if (!sidebar.has(entry.id)) (rows[entry.depth] ??= []).push(entry.card);
  }
  const rowHtml = rows.map(row => `<div class="relations-row">${row.join('')}</div>`);
  const exceptionCards = entries.filter(entry => sidebar.has(entry.id));
  const edges = nodes.slice(1).map(({ parent, id, depth }) => {
    const kind = depth === 1 ? 'required' : parent === exceptionRoot.id ? 'branch' : 'reference';
    return `<span data-from="${escape(parent)}" data-to="${escape(id)}" data-kind="${kind}"></span>`;
  }).join('');
  const body = `<main class="relations-page">
    <div class="relations-heading"><h1>AGENTS.md 관계도</h1><p>전역 AGENTS.md의 문서 트리에서 공통 가이드와 프로젝트별 보고 예외를 확인하세요.</p></div>
    <section class="relations-panel" aria-label="AGENTS.md 관계도">
      <div class="relations-toolbar"><div class="relations-legend"><span>적용 지침</span><span>참조 문서</span></div></div>
      <div class="relations-scroll" tabindex="0" aria-label="AGENTS.md 관계도, 가로 스크롤 가능">
        <div class="relations-board"><canvas aria-hidden="true"></canvas>${rowHtml[0]}
          <div class="relations-main-branch">${rowHtml.slice(1).join('')}</div>
          <div class="relations-exception-branch">${exceptionCards[0].card}<div class="relations-exceptions">${exceptionCards.slice(1).map(entry => entry.card).join('')}</div></div>
        </div>
      </div>
    </section>
    <dialog class="relations-dialog" aria-labelledby="relations-title">
      <div class="relations-dialog-bar"><div><strong id="relations-title"></strong><small id="relations-location"></small></div>
      <a class="relations-source" target="_blank" rel="noopener">Markdown 원문 ↗</a><form method="dialog"><button autofocus>닫기 <span aria-hidden="true">×</span></button></form></div>
      <article class="relations-document"></article>
    </dialog>
    <template id="relations-edges">${edges}</template>${entries.map(entry => entry.preview).join('')}
  </main><script defer src="../assets/relationships.js"></script>`;
  const html = shell('AGENTS.md 관계도', body, '../').replace('</head>',
    '<link rel="stylesheet" href="../assets/relationships.css"></head>');
  await writeFile(path.join(root, 'guide/relationships.html'), html);
}
