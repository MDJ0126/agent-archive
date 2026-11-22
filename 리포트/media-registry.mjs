import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export async function mediaRegistry(root){
 try{
  const source=await fs.readFile(path.join(root,'media-registry.json'),'utf8'),registry=JSON.parse(source);
  const active=JSON.parse(await fs.readFile(path.join(root,'.media-cache/active.json'),'utf8'));
  if(active.registrySha256!==createHash('sha256').update(source).digest('hex'))throw new Error('Run prepare-media.mjs for the current registry');
  return {...registry,entries:active.entries,byPath:new Map(active.entries.map(e=>[e.path,e])),objects:path.join(root,'.media-cache/public')};
 }catch(e){if(e.code==='ENOENT')return {entries:[],byPath:new Map()};throw e;}
}
export function archiveLinks(html,reportPath,registry){
 return html.replace(/<figure>[\s\S]*?<\/figure>/g,figure=>{
  const match=/\bsrc="([^"]+)"/.exec(figure);if(!match)return figure;
  let relative;try{relative=decodeURIComponent(new URL(match[1].replaceAll('&amp;','&'),'https://archive.local/'+reportPath).pathname.slice(1));}catch{return figure;}
  const entry=registry.byPath.get(relative);if(!entry)return figure;
  return figure.replace('</figure>',`<p class="media-original"><a class="report-link" href="${esc(entry.originalUrl)}" download>원본 다운로드</a> · <a class="report-link" href="../../../guide/media-archive.html">미디어 보관함</a></p></figure>`);
 });
}
export function archivePage(registry,page=1){
 const pages=Math.ceil(registry.entries.length/50),visible=registry.entries.slice((page-1)*50,page*50),navigation=`<nav aria-label="미디어 보관함 페이지">${page>1?`<a href="${page===2?"media-archive.html":`media-archive-${page-1}.html`}">이전</a> · `:""}${page} / ${pages}${page<pages?` · <a href="media-archive-${page+1}.html">다음</a>`:""}</nav>`;
 const total=registry.entries.reduce((s,e)=>s+e.sourceBytes,0),size=registry.entries.reduce((s,e)=>s+e.publicBytes,0);
 return `<main class="detail guide-page"><article><h1>미디어 보관함</h1><p>기존 주소에는 재생 가능한 최적화본을 제공합니다. 원본은 저장소와 별도 GitHub Release에 보존하며 아래 주소에서 내려받을 수 있습니다.</p><p>${registry.entries.length}개 경로 · 원본 ${(total/1e6).toFixed(1)} MB · 게시본 ${(size/1e6).toFixed(1)} MB</p><p><a class="report-link" href="../media-registry.json" download>전체 경로·원본 및 게시본 SHA-256 목록</a></p>${navigation}<div class="table-scroll"><table><thead><tr><th>기존 경로</th><th>게시본</th><th>원본</th></tr></thead><tbody>${visible.map(e=>`<tr><td>${esc(e.path)}</td><td><a class="report-link" href="../${e.path.split('/').map(encodeURIComponent).join('/')}">${(e.publicBytes/1e6).toFixed(2)} MB</a></td><td><a class="report-link" href="${esc(e.originalUrl)}">${(e.sourceBytes/1e6).toFixed(2)} MB · 원본</a></td></tr>`).join('')}</tbody></table></div>${navigation}</article></main>`;
}
