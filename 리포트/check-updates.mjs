import {readFile} from 'node:fs/promises';

// Read only. A session consumes the returned instructions; this never executes
// downloaded code, overwrites local instructions, or shares a seen-state file.
const base='https://raw.githubusercontent.com/MDJ0126/codex-config/main/';
const index=process.argv.indexOf('--since');
const since=index<0?null:process.argv[index+1];
if(index>=0&&!since)throw new Error('--since requires the last ID read by this session');
async function remote(file){
 const response=await fetch(`${base}${file}?t=${Date.now()}`,{cache:'no-store',signal:AbortSignal.timeout(15000)});
 if(!response.ok)throw new Error(`${file}: HTTP ${response.status}`);
 return response.text();
}
function validate(value){
 if(value.schemaVersion!==1||!Array.isArray(value.entries)||!value.entries.length||value.latest!==value.entries[0].id)throw new Error('Invalid update index');
 const ids=new Set();
 for(const e of value.entries){if(!/^\d{4}-\d{2}-\d{2}-\d{3}$/.test(e.id)||ids.has(e.id)||e.source!=='AGENTS.md')throw new Error('Invalid update entry');ids.add(e.id);}
 return value;
}
try{
 const board=validate(JSON.parse(await remote('리포트/guide/updates.json')));
 // Unknown checkpoints require a full read, rather than silently skipping.
 const checkpoint=board.entries.findIndex(e=>e.id===since);
 const pending=checkpoint<0?board.entries:board.entries.slice(0,checkpoint);
 const instructions=pending.length?await remote('AGENTS.md'):null;
 console.log(JSON.stringify({checkedAt:new Date().toISOString(),remoteVerified:true,latest:board.latest,pending,instructions,action:pending.length?'Read and apply applicable instructions in this session. Preserve local edits when synchronizing files.':'No updates since this session checkpoint.'},null,2));
}catch(error){
 let cached=null;try{cached=validate(JSON.parse(await readFile(new URL('./guide/updates.json',import.meta.url),'utf8')));}catch{}
 console.log(JSON.stringify({checkedAt:new Date().toISOString(),remoteVerified:false,error:error.message,cached,action:'Remote latest is unverified. Use known instructions, report the limitation, and retry at the next checkpoint.'},null,2));
 process.exitCode=1;
}
