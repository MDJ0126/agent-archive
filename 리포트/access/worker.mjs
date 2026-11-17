const prefix = '/codex-config';
const encoder = new TextEncoder();
const denied = () => new Response('<!doctype html><html><meta charset="utf-8"><title>403 Forbidden</title><h1>403 Forbidden</h1></html>', {status:403, headers:{'content-type':'text/html;charset=utf-8','cache-control':'private, no-store','x-robots-tag':'noindex, nofollow','referrer-policy':'no-referrer'}});
const digest = async value => Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', encoder.encode(value))), b => b.toString(16).padStart(2,'0')).join('');
function deviceCookie(request) {
  const raw=request.headers.get('cookie')||'';
  // Use the same device identifier as the portfolio/blog; retain existing connections.
  for(const name of ['site_device_id','__Host-tracefolio_device']){
    const match=raw.match(new RegExp('(?:^|;\\s*)'+name+'=([^;]+)'));
    if(match){try{const id=decodeURIComponent(match[1]);if(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(id))return id;}catch{}}
  }
  return '';
}
function deviceHeader(id){return 'site_device_id='+id+'; Max-Age=31536000; Path=/; Secure; HttpOnly; SameSite=Lax';}

const escapeHtml = value => String(value).replace(/[&<>"']/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const linking=url.pathname==='/_access'&&['GET','HEAD','POST'].includes(request.method);
    const managing=url.pathname===prefix+'/admin/rule'&&request.method==='POST';
    if (!['GET','HEAD'].includes(request.method)&&!linking&&!managing) return denied();
    if (!linking&&url.pathname !== '/'&&url.pathname !== prefix && !url.pathname.startsWith(prefix+'/')) return denied();
    try {
      const store = env.ACCESS.get(env.ACCESS.idFromName('registered-devices'));
      const result = await store.fetch('https://internal/policies');
      if (!result.ok) return denied();
      const {rules} = await result.json();
      const allowed = new Set(JSON.parse(env.ALLOWED_RULE_HASHES || '[]'));
      const registered = [];
      for (const rule of rules) {
        if (rule.scope!=='device' || rule.enabled===false || rule.type !== 'ignore' || (rule.expiresAt && Date.parse(rule.expiresAt)<=Date.now())) continue;
        if (allowed.has(await digest(`${rule.scope}:${rule.value}`))) registered.push(rule);
      }
      if(linking){
        let device='';
        if(request.method==='POST'){
          if(request.headers.get('origin')!==url.origin||!request.headers.get('content-type')?.startsWith('application/x-www-form-urlencoded'))return denied();
          const input=await request.text();if(input.length>256)return denied();
          device=new URLSearchParams(input).get('device')||'';
        }else device=url.searchParams.get('device')||'';
        if(!registered.some(rule=>rule.value===device))return denied();
        const connected='<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="referrer" content="no-referrer"><title>보고 사이트 연결</title><body><p>보고 사이트에 연결하고 있습니다.</p><p><a href="'+prefix+'/">보고 사이트 열기</a></p><script>history.replaceState(null,"",location.pathname);location.replace('+JSON.stringify(prefix+'/')+')</script></body></html>';
        return new Response(request.method==='HEAD'?null:connected,{status:200,headers:{'content-type':'text/html;charset=utf-8','cache-control':'private, no-store','referrer-policy':'no-referrer','x-robots-tag':'noindex, nofollow','set-cookie':deviceHeader(device)}});
      }
      const device = deviceCookie(request);
      if (!device||!registered.some(rule=>rule.value===device)) return denied();
      if(url.pathname==='/')return Response.redirect(url.origin+prefix+'/',302);
      if(managing){
        if(request.headers.get('origin')!==url.origin||!request.headers.get('content-type')?.startsWith('application/x-www-form-urlencoded'))return denied();
        const body=await request.text();if(body.length>256)return denied();
        const form=new URLSearchParams(body);
        const changed=await store.fetch('https://internal/rule',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({id:form.get('id'),enabled:form.get('enabled')==='1'})});
        if(!changed.ok)return new Response('변경할 수 없습니다. 마지막 허용 항목은 해제할 수 없습니다.',{status:400,headers:{'content-type':'text/plain;charset=utf-8','cache-control':'no-store'}});
        return new Response(null,{status:303,headers:{location:prefix+'/admin','cache-control':'no-store'}});
      }
      if (url.pathname === prefix+'/admin' || url.pathname === prefix+'/admin/') {
        const rows=rules.filter(rule=>rule.scope==='device').map(rule=>`<li><strong>${escapeHtml(rule.label || '관리 기기')}</strong> · ${rule.scope==='device'?'기기':'IP'} · ${rule.enabled===false?'해제':'허용'}${rule.scope==='device'&&rule.enabled!==false?` · <a href="/_access?device=${encodeURIComponent(rule.value)}">이 기기 연결</a>`:''}<form method="post" action="${prefix}/admin/rule"><input type="hidden" name="id" value="${escapeHtml(rule.id)}"><input type="hidden" name="enabled" value="${rule.enabled===false?'1':'0'}"><button>${rule.enabled===false?'다시 허용':'접근 해제'}</button></form></li>`).join('');
        const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tracefolio 접근 관리</title><style>body{font:16px system-ui;background:#f4f6fa;color:#172033;max-width:760px;margin:8vh auto;padding:24px}main{background:white;padding:32px;border:1px solid #dce2ec;border-radius:16px}li{padding:14px 0}a{color:#2459b3}p{line-height:1.7}</style><main><h1>보고 사이트 접근 관리</h1><p>등록 기기 2대를 중앙 보고 사이트에 독립적으로 저장했습니다.</p><ul>${rows}</ul><p>PC와 iPhone의 등록 기기 ID만 허용합니다. IP 주소는 허용 기준으로 사용하지 않습니다.</p><p><a href="${prefix}/">보고 사이트 열기</a></p><p>접근 해제를 누르면 다음 요청부터 권한이 취소됩니다. 포폴과 블로그의 설정에는 영향을 주지 않습니다. 기기 연결 링크는 해당 기기에서만 열고 공유하지 마세요.</p></main></html>`;
        return new Response(request.method==='HEAD'?null:html,{headers:{'content-type':'text/html;charset=utf-8','cache-control':'private, no-store','x-robots-tag':'noindex, nofollow'}});
      }
      if (url.pathname===prefix) return new Response(null,{status:302,headers:{location:prefix+'/','cache-control':'no-store'}});
      url.pathname=url.pathname.slice(prefix.length);
      const sourcePath=url.pathname.endsWith('/')?url.pathname+'index.html':url.pathname;
      const extension=sourcePath.split('.').at(-1).toLowerCase();
      const mime={html:'text/html;charset=utf-8',css:'text/css;charset=utf-8',js:'text/javascript;charset=utf-8',mjs:'text/javascript;charset=utf-8',json:'application/json;charset=utf-8',md:'text/markdown;charset=utf-8'};
      const sourceUrl='https://raw.githubusercontent.com/MDJ0126/codex-config/main/'+encodeURIComponent('리포트')+sourcePath;
      let asset;
      // Read current published reports only after authorization; retain packaged assets as fallback.
      if(mime[extension]){
        try{const current=await fetch(sourceUrl,{signal:AbortSignal.timeout(5000)});if(current.ok){const currentHeaders=new Headers();currentHeaders.set('content-type',mime[extension]);asset=new Response(current.body,{status:current.status,headers:currentHeaders});}}catch{}
      }
      if(!asset)asset=await env.ASSETS.fetch(new Request(url,request));
      if(asset.status===404&&!mime[extension]){try{const current=await fetch(sourceUrl,{signal:AbortSignal.timeout(5000)});if(current.ok)asset=current;}catch{}}
      if(asset.status===404&&extension==='html')return new Response(null,{status:302,headers:{location:prefix+'/?missing='+encodeURIComponent(sourcePath),'cache-control':'private, no-store'}});
      const headers=new Headers(asset.headers);
      headers.set('cache-control','private, no-store');
      if(asset.ok)headers.append('set-cookie',deviceHeader(device));
      headers.set('x-robots-tag','noindex, nofollow');
      const location=headers.get('location');
      if(location){const target=new URL(location,url);if(target.origin===url.origin&&!target.pathname.startsWith(prefix+'/')){target.pathname=prefix+target.pathname;headers.set('location',target.pathname+target.search+target.hash)}}
      return new Response(asset.body,{status:asset.status,headers});
    } catch {
      return denied();
    }
  }
};

export class AccessStore {
  constructor(state,env){this.state=state;this.env=env;}
  async fetch(request){
    let rules=await this.state.storage.get('rules');
    if(!rules){
      if(!this.env.INITIAL_RULES)return new Response('Not configured',{status:503});
      rules=JSON.parse(this.env.INITIAL_RULES);
      await this.state.storage.put('rules',rules);
    }
    if(rules.some(rule=>rule.scope!=='device')){rules=rules.filter(rule=>rule.scope==='device');await this.state.storage.put('rules',rules);}
    if(request.method==='GET')return Response.json({rules});
    if(request.method==='POST'&&new URL(request.url).pathname==='/rule'){
      const {id,enabled}=await request.json();
      const rule=rules.find(rule=>rule.id===id);
      if(!rule||typeof enabled!=='boolean'||(!enabled&&rule.enabled!==false&&rules.filter(rule=>rule.enabled!==false).length<=1))return new Response('Invalid change',{status:400});
      rule.enabled=enabled;
      await this.state.storage.put('rules',rules);
      return Response.json({ok:true});
    }
    return new Response('Not found',{status:404});
  }
}
