// Use the upload result directly; do not rediscover it through ListArtifacts.
export default async function deployPages({github, core, context, artifactId}) {
 if(!/^\d+$/.test(String(artifactId)))throw new Error('Upload did not return a valid artifact ID');
 const repo=context.repo;
 // Upload finalization and REST visibility are not atomic. A just-uploaded ID
 // can return 404 before it is indexed; do not fail the entire run on that race.
 async function awaitVisible(label,request){
  const deadline=Date.now()+180000;
  let attempt=0;
  while(true){
   try{return await request();}catch(error){
    const status=Number(error.status);
    if(![404,429,500,502,503,504].includes(status)||Date.now()>=deadline)throw error;
    const delay=Math.min(20000,2000*2**Math.min(attempt++,4),deadline-Date.now());
    core.info(`${label}: HTTP ${status}; waiting ${delay}ms for uploaded artifact ${artifactId} (attempt ${attempt})`);
    await new Promise(resolve=>setTimeout(resolve,delay));
   }
  }
 }
 const artifact=await awaitVisible('Artifact visibility',()=>github.rest.actions.getArtifact({...repo,artifact_id:Number(artifactId)}));
 if(artifact.data.expired||artifact.data.workflow_run?.id!==context.runId)throw new Error('Artifact is expired or belongs to another run');
 core.info(`Deploying uploaded artifact ${artifactId} from run ${context.runId}`);
 core.info(`Artifact size: ${artifact.data.size_in_bytes} bytes`);
 const overallDeadline=Date.now()+600000;
 for(let deploymentAttempt=1;deploymentAttempt<=2;deploymentAttempt++){
 const oidcToken=await core.getIDToken();
 // Retry only an explicit 404 before a deployment exists, never an ambiguous
 // server/transport failure which could already have created a deployment.
 const createDeadline=Date.now()+180000;
 let deployment;
 while(!deployment){
  try{({data:deployment}=await github.request('POST /repos/{owner}/{repo}/pages/deployments',{
   ...repo,artifact_id:Number(artifactId),pages_build_version:context.sha,oidc_token:oidcToken
  }));}catch(error){
   if(Number(error.status)!==404||Date.now()>=createDeadline)throw error;
   core.info(`Pages artifact visibility: HTTP 404; waiting for artifact ${artifactId}`);
   await new Promise(resolve=>setTimeout(resolve,10000));
  }
 }
 core.setOutput('page_url',deployment.page_url);
 const deadline=overallDeadline;
 let retryDeployment=false;
 while(Date.now()<deadline){
  const {data}=await awaitVisible('Pages status',()=>github.request('GET /repos/{owner}/{repo}/pages/deployments/{deployment_id}',{...repo,deployment_id:deployment.id}));
  if(data.status==='succeed'){core.info('Pages deployment succeeded');return;}
  if(['deployment_failed','deployment_content_failed','deployment_cancelled','deployment_lost','failed','cancelled'].includes(data.status)){
   core.info(`Pages terminal response: ${JSON.stringify(data)}`);
   if(data.status==='deployment_failed'&&deploymentAttempt===1&&Date.now()+15000<deadline){
    core.warning(`Pages explicitly failed deployment ${deployment.id}; retrying the same artifact once after 15s`);
    await new Promise(resolve=>setTimeout(resolve,15000));retryDeployment=true;break;
   }
   throw new Error(`Pages deployment ${deployment.id}: ${data.status}; artifact ${artifactId}; attempt ${deploymentAttempt}`);
  }
  core.info(`Pages deployment ${deployment.id}: ${data.status}`);
  await new Promise(resolve=>setTimeout(resolve,10000));
 }
 if(retryDeployment)continue;
 await github.request('POST /repos/{owner}/{repo}/pages/deployments/{deployment_id}/cancel',{...repo,deployment_id:deployment.id});
 throw new Error(`Pages deployment ${deployment.id} timed out`);
 }
}
