import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.dirname(fileURLToPath(import.meta.url)),budget=JSON.parse(await fs.readFile(path.join(root,'performance-budget.json'),'utf8'));
const index=await fs.readFile(path.join(root,'index.html'),'utf8'),stats=JSON.parse(await fs.readFile(path.join(root,'.package-stats.json'),'utf8'));
const metrics={homeHtmlBytes:Buffer.byteLength(index),trackerDataBytes:(await fs.stat(path.join(root,'assets/tracker-data.json'))).size,initialRecordRows:(index.match(/class="record issue-row"/g)||[]).length,packageBytes:stats.bytes,excludedFiles:stats.excludedFiles,excludedBytes:stats.sourceBytes-stats.bytes,packageMs:stats.packageMs};
let previous;try{previous=JSON.parse(await fs.readFile(path.join(root,'.build-cache/performance.json'),'utf8'));}catch{}
const failures=[];for(const key of ['homeHtmlBytes','trackerDataBytes','initialRecordRows'])if(metrics[key]>budget[key])failures.push(`${key}: ${metrics[key]} > ${budget[key]}`);
if(metrics.packageBytes>budget.packageMaxBytes)failures.push('Public package exceeds project capacity budget');
const warnings=[];if(metrics.packageBytes>budget.packageWarnBytes)warnings.push('Historical media exceeds 1 GiB; plan compatible archival hosting before further growth');
if(previous){metrics.packageGrowthBytes=metrics.packageBytes-previous.packageBytes;if(metrics.packageGrowthBytes>budget.packageGrowthMaxBytes)failures.push('Public package grew by more than 100 MB in one publication');else if(metrics.packageGrowthBytes>budget.packageGrowthWarnBytes)warnings.push('Public package grew by more than 20 MB; review new media');}
try{Object.assign(metrics,JSON.parse(await fs.readFile(path.join(root,'.build-metrics.json'),'utf8')));if(metrics.buildMs>budget.buildWarnMs)warnings.push('Build exceeded 60 seconds');}catch{}
for(const warning of warnings)console.log(`::warning::${warning}`);
console.log(JSON.stringify({metrics,warnings,failures},null,2));
if(process.env.GITHUB_STEP_SUMMARY)await fs.appendFile(process.env.GITHUB_STEP_SUMMARY,`\n### Publication performance\n\n\`\`\`json\n${JSON.stringify({metrics,warnings},null,2)}\n\`\`\`\n`);
if(failures.length)throw new Error(failures.join('; '));
await fs.mkdir(path.join(root,'.build-cache'),{recursive:true});await fs.writeFile(path.join(root,'.build-cache/performance.json'),JSON.stringify(metrics));
