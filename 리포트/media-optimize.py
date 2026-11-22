"""Prepare immutable original objects and verified same-format public renditions.

Requires Pillow, imageio-ffmpeg and Gifsicle/gifdiff. Never writes source media.
"""
import argparse, concurrent.futures, hashlib, json, pathlib, re, shutil, subprocess, time, zipfile
import imageio_ffmpeg
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('--inventory',required=True)
parser.add_argument('--work',required=True)
parser.add_argument('--gifsicle',required=True)
parser.add_argument('--gifdiff',required=True)
parser.add_argument('--tag',required=True)
args=parser.parse_args()
work=pathlib.Path(args.work).resolve()
originals=work/'originals'; public=work/'public'
originals.mkdir(parents=True,exist_ok=True);public.mkdir(exist_ok=True)
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
records=json.loads(pathlib.Path(args.inventory).read_text(encoding='utf-8'))
unique={row['oid']:row for row in records}
def run(command,timeout=240):
 return subprocess.run(command,stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=timeout)
def digest(file):
 with file.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def audio_hash(file):
 r=run([ffmpeg,'-v','error','-i',str(file),'-map','0:a','-c','copy','-f','hash','-hash','sha256','-'])
 return r.stdout.decode().strip() if r.returncode==0 else None
def gif_info(file):
 with Image.open(file) as im:
  duration=0
  for n in range(im.n_frames):im.seek(n);duration+=im.info.get('duration',0)
  return {'width':im.width,'height':im.height,'frames':im.n_frames,'durationMs':duration,'loop':im.info.get('loop')}
def optimize(row):
 ext=pathlib.Path(row['file']).suffix.lower();source=originals/(row['oid']+ext)
 if not source.exists():
  with source.open('wb') as out:subprocess.run(['git','cat-file','blob',row['oid']],stdout=out,check=True)
 sha=digest(source);key=sha+ext;target=public/key
 verification={'format':ext[1:]};candidate=work/(row['oid']+'.candidate'+ext)
 started=time.monotonic()
 try:
  if ext=='.gif':
   r=run([args.gifsicle,'--careful','-O3',str(source),'-o',str(candidate)],180)
   if r.returncode!=0:raise RuntimeError('Gifsicle failed')
   check=run([args.gifdiff,str(source),str(candidate)],180)
   before,after=gif_info(source),gif_info(candidate)
   if check.returncode!=0 or before['durationMs']!=after['durationMs'] or before['loop']!=after['loop']:raise RuntimeError('GIF pixel/timeline verification failed')
   verification.update(before);verification['pixelEquivalent']=True
  else:
   r=run([ffmpeg,'-v','error','-y','-i',str(source),'-map','0:v:0','-map','0:a?','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-fps_mode','passthrough','-c:a','copy','-movflags','+faststart','-threads','2',str(candidate)],300)
   if r.returncode!=0:raise RuntimeError('Video encode failed')
   check=run([ffmpeg,'-hide_banner','-i',str(source),'-i',str(candidate),'-filter_complex_threads','1','-lavfi','[0:v]setpts=PTS-STARTPTS[a];[1:v]setpts=PTS-STARTPTS[b];[a][b]ssim','-an','-f','null','-'],300)
   match=re.findall(rb'All:([0-9.]+)',check.stderr)
   ssim=float(match[-1]) if match else 0
   before=imageio_ffmpeg.count_frames_and_secs(str(source));after=imageio_ffmpeg.count_frames_and_secs(str(candidate))
   source_audio=audio_hash(source);result_audio=audio_hash(candidate)
   if check.returncode!=0 or ssim<0.99 or before[0]!=after[0] or abs(before[1]-after[1])>0.1 or source_audio!=result_audio:raise RuntimeError(f'Video quality/timing guard (SSIM={ssim})')
   verification.update({'ssim':ssim,'frames':before[0],'durationSeconds':before[1],'hasAudio':source_audio is not None,'audioPacketHash':source_audio,'audioUnchanged':True})
  if candidate.stat().st_size<source.stat().st_size*0.98:
   candidate.replace(target);method='lossless-gif' if ext=='.gif' else 'h264-crf18-ssim99-audio-copy'
  else:shutil.copyfile(source,target);method='original-already-efficient'
 except Exception as error:
  shutil.copyfile(source,target);method='original-preserved';verification['fallbackReason']=str(error)
 finally:
  candidate.unlink(missing_ok=True)
 result={'sourceOid':row['oid'],'sourceSha256':sha,'sourceBytes':source.stat().st_size,'object':key,'publicSha256':digest(target),'publicBytes':target.stat().st_size,'method':method,'verification':verification,'originalUrl':f'https://github.com/MDJ0126/codex-config/releases/download/{args.tag}/{key}','elapsedSeconds':round(time.monotonic()-started,2)}
 (work/(row['oid']+'.result.json')).write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8')
 print(json.dumps({'oid':row['oid'],'before':result['sourceBytes'],'after':result['publicBytes'],'method':method}),flush=True)
 return result

results={}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
 for result in pool.map(optimize,unique.values()):results[result['sourceOid']]=result
entries=[{'path':row['file'].removeprefix('리포트/'),**results[row['oid']]} for row in records]
manifest={'schemaVersion':1,'releaseTag':args.tag,'publicBundleUrl':f'https://github.com/MDJ0126/codex-config/releases/download/{args.tag}/public-media.zip','entries':entries}
with zipfile.ZipFile(work/'public-media.zip','w',compression=zipfile.ZIP_STORED) as archive:
 for key in sorted({e['object'] for e in entries}):archive.write(public/key,key)
manifest['publicBundleSha256']=digest(work/'public-media.zip')
(work/'media-registry.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'complete':True,'paths':len(entries),'objects':len(results),'sourceBytes':sum(e['sourceBytes'] for e in entries),'publicBytes':sum(e['publicBytes'] for e in entries),'methods':{m:sum(e['method']==m for e in entries) for m in sorted({e['method'] for e in entries})}}),flush=True)
