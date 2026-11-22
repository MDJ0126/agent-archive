"""Upload a verified media snapshot; credentials stay in memory, never in files."""
import argparse, concurrent.futures, hashlib, http.client, json, pathlib, subprocess, urllib.error, urllib.parse, urllib.request
parser=argparse.ArgumentParser();parser.add_argument('--work',required=True);args=parser.parse_args()
work=pathlib.Path(args.work).resolve();manifest=json.loads((work/'media-registry.json').read_text(encoding='utf-8'))
credential=subprocess.check_output(['git','credential','fill'],input=b'protocol=https\nhost=github.com\n\n').decode()
token=dict(line.split('=',1) for line in credential.splitlines() if '=' in line)['password'];del credential
repo='MDJ0126/codex-config';headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','User-Agent':'codex-media-archive'}
def api(method,route,data=None):
 body=None if data is None else json.dumps(data).encode()
 req=urllib.request.Request('https://api.github.com/repos/'+repo+route,data=body,headers={**headers,'Content-Type':'application/json'},method=method)
 with urllib.request.urlopen(req,timeout=120) as response:return json.load(response)
tag=manifest['releaseTag']
try:release=api('GET','/releases/tags/'+tag)
except urllib.error.HTTPError as error:
 if error.code!=404:raise
 release=api('POST','/releases',{'tag_name':tag,'target_commitish':subprocess.check_output(['git','rev-parse','origin/main']).decode().strip(),'name':'Media originals and public renditions '+tag,'draft':True,'body':'Content-addressed original media and verified same-format public renditions. Original filenames and SHA-256 mappings are recorded in media-registry.json. Existing website media URLs continue serving compatible renditions. Never replace an existing snapshot asset.'})
existing={}
for page in range(1,10):
 assets=api('GET',f"/releases/{release['id']}/assets?per_page=100&page={page}")
 existing.update({a['name']:a for a in assets})
 if len(assets)<100:break
unique={e['object']:e for e in manifest['entries']}
uploads=[(name,work/'originals'/(e['sourceOid']+pathlib.Path(name).suffix),e['sourceSha256']) for name,e in unique.items()]
def digest(file):
 with file.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
uploads.extend([('public-media.zip',work/'public-media.zip',manifest['publicBundleSha256']),('media-registry.json',work/'media-registry.json',digest(work/'media-registry.json'))])
def upload(item):
 name,file,expected=item
 if name in existing:
  asset=existing[name]
  if asset.get('digest')!='sha256:'+expected or asset['size']!=file.stat().st_size:raise RuntimeError('Existing immutable snapshot asset differs: '+name)
  return asset
 content_type='image/gif' if name.endswith('.gif') else 'video/mp4' if name.endswith('.mp4') else 'application/json' if name.endswith('.json') else 'application/zip'
 conn=http.client.HTTPSConnection('uploads.github.com',timeout=300)
 try:
  route=f"/repos/{repo}/releases/{release['id']}/assets?name={urllib.parse.quote(name)}"
  conn.putrequest('POST',route)
  for key,value in {**headers,'Content-Type':content_type,'Content-Length':str(file.stat().st_size)}.items():conn.putheader(key,value)
  conn.endheaders()
  with file.open('rb') as stream:
   while chunk:=stream.read(4*1024*1024):conn.send(chunk)
  response=conn.getresponse();data=json.loads(response.read())
  if response.status!=201:raise RuntimeError(f'Asset upload HTTP {response.status}: {name}')
  if data.get('digest')!='sha256:'+expected or data['size']!=file.stat().st_size:raise RuntimeError('Server asset integrity mismatch: '+name)
  print(json.dumps({'uploaded':name,'bytes':data['size'],'sha256Verified':True}),flush=True);return data
 finally:conn.close()
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:published=list(pool.map(upload,uploads))
if release['draft']:release=api('PATCH',f"/releases/{release['id']}",{'draft':False})
(work/'release-receipt.json').write_text(json.dumps({'releaseUrl':release['html_url'],'assets':len(published),'allServerDigestsVerified':True},indent=2),encoding='utf-8')
print(json.dumps({'published':release['html_url'],'assets':len(published),'allServerDigestsVerified':True}),flush=True)
