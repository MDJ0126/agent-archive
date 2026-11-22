import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import {createRequire} from 'node:module';
const arg=name=>process.argv[process.argv.indexOf(name)+1];
const output=path.resolve(arg('--output'));await fs.mkdir(output,{recursive:true});
const require=createRequire(process.env.CODEX_NODE_MODULES?path.join(process.env.CODEX_NODE_MODULES,'package.json'):import.meta.url);
const {chromium}=require('playwright');let server;
let url=arg('--url');
if(process.argv.includes('--site-root')){
 const root=path.resolve(arg('--site-root'));
 server=http.createServer(async(req,res)=>{try{let relative=decodeURIComponent(new URL(req.url,'http://local').pathname);if(relative.endsWith('/'))relative+='index.html';const target=path.resolve(root,'.'+relative);if(!target.startsWith(root+path.sep)){res.writeHead(403).end();return;}const bytes=await fs.readFile(target),ext=path.extname(target);res.setHeader('Content-Type',({'.html':'text/html; charset=utf-8','.js':'text/javascript','.mjs':'text/javascript','.json':'application/json','.css':'text/css','.png':'image/png','.gif':'image/gif','.svg':'image/svg+xml','.webp':'image/webp'})[ext]||'application/octet-stream');res.end(bytes);}catch{res.writeHead(404).end();}});
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));url=`http://127.0.0.1:${server.address().port}/`;
}
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe'});
try{
 const page=await browser.newPage({viewport:{width:1365,height:900}}),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('response',response=>{if(response.status()>=400&&new URL(response.url()).origin===new URL(url).origin)errors.push(`HTTP ${response.status()} ${response.url()}`);});
 await page.addInitScript(()=>{window.lab={lcp:0,cls:0,longTasks:[]};for(const [type,apply]of [['largest-contentful-paint',e=>window.lab.lcp=e.startTime],['layout-shift',e=>{if(!e.hadRecentInput)window.lab.cls+=e.value}],['longtask',e=>window.lab.longTasks.push(e.duration)]])new PerformanceObserver(list=>list.getEntries().forEach(apply)).observe({type,buffered:true});});
 await page.goto(url,{waitUntil:'networkidle'});
 await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>setTimeout(resolve,250)))));
 const metrics=await page.evaluate(()=>({...window.lab,domNodes:document.querySelectorAll('*').length,recordNodes:document.querySelectorAll('.record').length,htmlBytes:performance.getEntriesByType('navigation')[0].decodedBodySize,fcp:performance.getEntriesByName('first-contentful-paint')[0]?.startTime,domContentLoaded:performance.getEntriesByType('navigation')[0].domContentLoadedEventEnd,resourceBytes:performance.getEntriesByType('resource').reduce((s,r)=>s+r.transferSize,0)}));
 await page.locator('.tools').evaluate(element=>element.scrollIntoView({block:'start'}));await page.screenshot({path:path.join(output,'result.png')});
 await page.screenshot({path:path.join(output,'frame-0.png')});
 const started=Date.now();await page.locator('#search').fill('codex');await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));metrics.searchToPaintMs=Date.now()-started;await page.screenshot({path:path.join(output,'frame-1.png')});
 await page.locator('#search').fill('');await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));await page.screenshot({path:path.join(output,'frame-2.png')});
 const next=page.locator('#page-next');if(await next.count()){await next.click();await page.screenshot({path:path.join(output,'frame-3.png')});metrics.nextPage=await page.locator('#page-status').textContent();}
 metrics.errors=errors;metrics.url=url;await fs.writeFile(path.join(output,'metrics.json'),JSON.stringify(metrics,null,2));console.log(JSON.stringify(metrics));
}finally{await browser.close();if(server)await new Promise(resolve=>server.close(resolve));}
