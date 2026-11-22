import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {Readable} from 'node:stream';
import {pipeline} from 'node:stream/promises';
import {createWriteStream,createReadStream} from 'node:fs';
const root=path.dirname(fileURLToPath(import.meta.url)),cache=path.join(root,'.media-cache'),source=await fs.readFile(path.join(root,'media-registry.json'),'utf8'),registry=JSON.parse(source);
const digest=async file=>{const hash=createHash('sha256');for await(const chunk of createReadStream(file))hash.update(chunk);return hash.digest('hex');};
await fs.mkdir(cache,{recursive:true});
const bundle=path.join(cache,'public-media.zip'),objects=path.join(cache,'public');
let valid=false;try{valid=await digest(bundle)===registry.publicBundleSha256;}catch{}
if(!valid){const response=await fetch(registry.publicBundleUrl);if(!response.ok)throw new Error(`Media bundle HTTP ${response.status}`);await pipeline(Readable.fromWeb(response.body),createWriteStream(bundle));if(await digest(bundle)!==registry.publicBundleSha256)throw new Error('Media bundle SHA-256 mismatch');}
await fs.mkdir(objects,{recursive:true});
const extract="import pathlib,re,sys,zipfile; root=pathlib.Path(sys.argv[2]).resolve(); z=zipfile.ZipFile(sys.argv[1]); assert all(re.fullmatch(r'[a-f0-9]{64}\\.(gif|mp4)',i.filename) for i in z.infolist()),'unsafe media archive'; z.extractall(root)";
execFileSync(process.env.MEDIA_PYTHON||(process.platform==='win32'?'python':'python3'),['-c',extract,bundle,objects]);
const ref=process.env.MEDIA_SOURCE_REF||'HEAD',repo=execFileSync('git',['rev-parse','--show-toplevel'],{cwd:path.resolve(root,'..')}).toString('utf8').trim();
const tree=execFileSync('git',['ls-tree','-r','-z',ref,'--','리포트'],{cwd:repo,maxBuffer:32*1024*1024}).toString('utf8');
const current=new Map();for(const line of tree.split('\0')){const match=/^\d+ blob ([a-f0-9]+)\t(.+)$/.exec(line);if(match&&/\.(gif|mp4)$/i.test(match[2]))current.set(match[2].slice('리포트/'.length),match[1]);}
const entries=[];
for(const entry of registry.entries){if(current.get(entry.path)!==entry.sourceOid)continue;const file=path.join(objects,entry.object);if(!/^[a-f0-9]{64}\.(gif|mp4)$/.test(entry.object)||await digest(file)!==entry.publicSha256)throw new Error(`Public rendition integrity failed: ${entry.path}`);entries.push(entry);}
const registered=new Set(entries.map(e=>e.path));let restored=0;
for(const [relative,oid]of current){if(registered.has(relative))continue;const target=path.resolve(root,relative);if(!target.startsWith(root+path.sep))throw new Error('Invalid media source path');await fs.mkdir(path.dirname(target),{recursive:true});await fs.writeFile(target,execFileSync('git',['cat-file','blob',oid],{cwd:repo,maxBuffer:100*1024*1024}));restored++;}
if(restored)console.log(`::warning::${restored} new/changed media files were preserved as originals; run the media archive optimizer before adding more large media.`);
await fs.writeFile(path.join(cache,'active.json'),JSON.stringify({registrySha256:createHash('sha256').update(source).digest('hex'),entries}));
console.log(`Verified ${entries.length} archived media paths; preserved ${restored} unregistered originals. Source media was not overwritten.`);
