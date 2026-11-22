import {readFile,writeFile,mkdir} from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {renderMarkdown} from './render.mjs';
export async function createRenderCache(root){
 const version=createHash('sha256').update(await readFile(path.join(root,'render.mjs'))).update(await readFile(path.join(root,'vendor/marked/marked.esm.js'))).digest('hex');
 const file=path.join(root,'.build-cache/render.json');let prior={};try{prior=JSON.parse(await readFile(file,'utf8'));}catch{}
 const current={};let hits=0,misses=0;
 return {render(source,options){const key=createHash('sha256').update(version).update(JSON.stringify(options)).update(source).digest('hex');if(prior[key]){hits++;return current[key]=prior[key];}misses++;return current[key]=renderMarkdown(source,options);},async save(){await mkdir(path.dirname(file),{recursive:true});await writeFile(file,JSON.stringify(current));console.log(`Markdown cache: ${hits} hits, ${misses} misses`);return {hits,misses};}};
}
