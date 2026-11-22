import fs from 'node:fs/promises';
import path from 'node:path';
import {mediaRegistry} from './media-registry.mjs';
const textTypes=new Set(['.html','.css','.js','.mjs','.json','.md','.svg']);
export async function publicPackage(root){
 const candidates=new Map();
 const media=await mediaRegistry(root);
 async function walk(relative){let entries;try{entries=await fs.readdir(path.join(root,relative),{withFileTypes:true});}catch(e){if(e.code==='ENOENT')return;throw e;}
  for(const entry of entries){const file=path.posix.join(relative,entry.name);if(entry.isSymbolicLink())throw new Error(`Public package symlink: ${file}`);if(entry.isDirectory())await walk(file);else candidates.set(file,(await fs.stat(path.join(root,file))).size);}
 }
 for(const entry of ['index.html','assets','reports','guide','write','discussions','manuals']){if(entry.endsWith('.html'))candidates.set(entry,(await fs.stat(path.join(root,entry))).size);else await walk(entry);}
 try{candidates.set('sitemap.xml',(await fs.stat(path.join(root,'sitemap.xml'))).size);}catch(e){if(e.code!=='ENOENT')throw e;}
 try{candidates.set('media-registry.json',(await fs.stat(path.join(root,'media-registry.json'))).size);}catch(e){if(e.code!=='ENOENT')throw e;}
 const projects=JSON.parse(await fs.readFile(path.join(root,'projects.json'),'utf8'));
 for(const id of Object.keys(projects)){for(const entry of ['posts','reports','assets'])await walk(`projects/${id}/${entry}`);for(const file of ['guidelines.html','guidelines.md']){const relative=`projects/${id}/${file}`;candidates.set(relative,(await fs.stat(path.join(root,relative))).size);}}
 const originalSizes=new Map(candidates);
 for(const entry of media.entries){originalSizes.set(entry.path,entry.sourceBytes);candidates.set(entry.path,entry.publicBytes);}
 const retained=new Set(),queue=[];
 function include(file){if(candidates.has(file)&&!retained.has(file)){retained.add(file);queue.push(file);}}
 // Preserve every public report URL, including historical redirects. Asset
 // imports are followed only when linked, never treated as public entrypoints.
 for(const file of candidates.keys())if(file.endsWith('.html')&&!file.includes('/assets/')&&!file.startsWith('assets/'))include(file);
 include('sitemap.xml');
 function reference(raw,from){
  let value=raw.replaceAll('&amp;','&').replaceAll('&quot;','"').replace(/^\$\{prefix\}/,'');
  if(!value||value.startsWith('#')||/^(?:data|mailto|javascript|tel):/i.test(value))return;
  if(/^https?:/i.test(value)){let u;try{u=new URL(value);}catch{return;}if(u.origin!=='https://mdj0126.github.io'||!u.pathname.startsWith('/codex-config/'))return;value=u.pathname.slice('/codex-config/'.length);from='index.html';}
  if(value.startsWith('//')||value.includes('${'))return;
  value=value.split(/[?#]/)[0];try{value=decodeURIComponent(value);}catch{return;}
  if(value.startsWith('/codex-config/'))value=value.slice('/codex-config/'.length);
  const relative=path.posix.normalize(value.startsWith('/')?value.slice(1):path.posix.join(path.posix.dirname(from),value));
  if(!relative.startsWith('../')){include(relative);include(relative.replace(/\/$/,'')+'/index.html');}
  // JSON registries and script strings also contain site-root-relative URLs.
  include(value.replace(/^\.\//,''));
 }
 for(let i=0;i<queue.length;i++){
  const file=queue[i];if(!textTypes.has(path.extname(file)))continue;
  const source=await fs.readFile(path.join(root,file),'utf8');
  for(const match of source.matchAll(/(?:href|src|poster)\s*=\s*["']([^"']+)["']/gi))reference(match[1],file);
  for(const match of source.matchAll(/srcset\s*=\s*["']([^"']+)["']/gi))for(const entry of match[1].split(','))reference(entry.trim().split(/\s+/)[0],file);
  for(const match of source.matchAll(/url\(\s*["']?([^\s)"']+)["']?\s*\)/gi))reference(match[1],file);
  for(const match of source.matchAll(/!?\[[^\]]*\]\((?:<([^>]+)>|([^\s)]+))(?:\s+[^)]*)?\)/g))reference(match[1]||match[2],file);
  for(const match of source.matchAll(/["'`]([^"'`\r\n<>]+\.[a-z0-9]{1,8}(?:[?#][^"'`\r\n<>]*)?)["'`]/gi))reference(match[1],file);
  if(file.endsWith('.md'))for(const match of source.matchAll(/^payload:\s*(.+)$/gm))reference(match[1].trim(),file);
 }
 const bytes=[...retained].reduce((s,f)=>s+candidates.get(f),0),sourceBytes=[...originalSizes.values()].reduce((a,b)=>a+b,0);
 const originalRetained=[...retained].reduce((s,f)=>s+originalSizes.get(f),0),mediaSavingsBytes=originalRetained-bytes;
 const largest=[...retained].map(file=>({file,bytes:candidates.get(file)})).sort((a,b)=>b.bytes-a.bytes).slice(0,15);
 return {files:[...retained].sort(),sourceFiles:candidates.size,sourceBytes,bytes,excludedFiles:candidates.size-retained.size,excludedBytes:sourceBytes-originalRetained,mediaSavingsBytes,archivedMediaPaths:media.entries.length,largest};
}
