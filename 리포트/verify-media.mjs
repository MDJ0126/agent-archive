import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.dirname(fileURLToPath(import.meta.url)),registry=JSON.parse(await fs.readFile(path.join(root,'media-registry.json'),'utf8'));
const base='https://mdj0126.github.io/codex-config/';let cursor=0,verified=0;
await Promise.all(Array.from({length:4},async()=>{while(cursor<registry.entries.length){const entry=registry.entries[cursor++],url=new URL(entry.path.split('/').map(encodeURIComponent).join('/'),base);let error;
 for(let attempt=0;attempt<3;attempt++)try{const response=await fetch(url,{headers:{'cache-control':'no-cache'}});if(!response.ok)throw new Error(`HTTP ${response.status}`);const type=response.headers.get('content-type')||'',expected=entry.path.endsWith('.gif')?'image/gif':'video/mp4';if(!type.includes(expected))throw new Error(`Unexpected Content-Type ${type}`);const hash=createHash('sha256');let bytes=0;for await(const chunk of response.body){hash.update(chunk);bytes+=chunk.length;}if(hash.digest('hex')!==entry.publicSha256||bytes!==entry.publicBytes)throw new Error('Public rendition hash/size mismatch');error=null;verified++;break;}catch(e){error=e;if(attempt<2)await new Promise(resolve=>setTimeout(resolve,5000));}
 if(error)throw new Error(`${entry.path}: ${error.message}`);
}}));
console.log(`PASS: ${verified} existing GIF/MP4 URLs returned the expected type, complete bytes and SHA-256.`);
