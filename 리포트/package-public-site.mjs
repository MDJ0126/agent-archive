import {mkdir,writeFile,rm,cp} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
const output=fileURLToPath(new URL('./.public-site/',import.meta.url));
await rm(output,{recursive:true,force:true});
await mkdir(output,{recursive:true});
// Keep public instruction links available; never include reports, posts or attachments.
await cp(new URL('./guide/',import.meta.url),new URL('./.public-site/guide/',import.meta.url),{recursive:true});
const html='<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="robots" content="noindex,nofollow"><title>보고 사이트 접근 제한</title><h1>보고 사이트는 등록된 관리 기기에서만 열 수 있습니다.</h1><p><a href="https://tracefolio-private.ehdwns0126.workers.dev/codex-config/">보호된 보고 사이트 열기</a></p></html>';
await writeFile(output+'/index.html',html);
await writeFile(output+'/404.html',html);
await writeFile(output+'/.nojekyll','');
console.log('Public package contains instructions and an access notice only.');
