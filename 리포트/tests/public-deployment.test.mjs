import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import {projectPagesBase, verifyPublic, verifyResponse} from '../verify-public.mjs';
const response=(status,text)=>({ok:status===200,status,arrayBuffer:async()=>Buffer.from(text)});
test('public verification accepts exact packaged bytes',async()=>{assert.equal((await verifyResponse('https://example.test/',Buffer.from('expected'),{request:async()=>response(200,'expected'),attempts:1})).toString(),'expected');});
test('HTTP 200 with stale content fails',async()=>{await assert.rejects(verifyResponse('https://example.test/',Buffer.from('expected'),{request:async()=>response(200,'stale'),attempts:1}),/differ/);});
test('404 is never reported as published',async()=>{await assert.rejects(verifyResponse('https://example.test/',Buffer.from('expected'),{request:async()=>response(404,'missing'),attempts:1}),/HTTP 404/);});
test('propagation delay retries and then requires exact bytes',async()=>{let called=0;await verifyResponse('https://example.test/',Buffer.from('expected'),{request:async()=>++called===1?response(404,'missing'):response(200,'expected'),attempts:2,delay:0});assert.equal(called,2);});

test('project Pages URL follows the current repository name',()=>{
 assert.equal(projectPagesBase('MDJ0126/agent-archive'),'https://mdj0126.github.io/agent-archive/');
 assert.equal(projectPagesBase('MDJ0126/next-name'),'https://mdj0126.github.io/next-name/');
 assert.equal(projectPagesBase('MDJ0126/mdj0126.github.io'),'https://mdj0126.github.io/');
 assert.throws(()=>projectPagesBase('MDJ0126/..'),/Invalid/);
});
test('manifest readback stays under the renamed project prefix and checks exact bytes',async(t)=>{
 const dir=await fs.mkdtemp(path.join(os.tmpdir(),'archive-public-check-'));
 t.after(()=>fs.rm(dir,{recursive:true,force:true}));
 const manifestPath=path.join(dir,'checks.json');
 const sha256=crypto.createHash('sha256').update('expected').digest('hex');
 const base='https://mdj0126.github.io/agent-archive';
 await fs.writeFile(manifestPath,JSON.stringify([{relative:'guide/index.html',sha256}]));
 assert.equal(await verifyPublic({base,manifestPath,request:async url=>{
  assert.equal(new URL(url).pathname,'/agent-archive/guide/index.html');
  return response(200,'expected');
 }}),1);
 await fs.writeFile(manifestPath,JSON.stringify([{relative:'../codex-config/index.html',sha256}]));
 await assert.rejects(verifyPublic({base,manifestPath,request:()=>assert.fail('outside project prefix')}),/escaped site/);
});
