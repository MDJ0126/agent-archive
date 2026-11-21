// Use the upload result directly; do not rediscover it through ListArtifacts.
export default async function deployPages({github, core, context, artifactId}) {
 if(!/^\d+$/.test(String(artifactId)))throw new Error('Upload did not return a valid artifact ID');
 const repo=context.repo;
 const artifact=await github.rest.actions.getArtifact({...repo,artifact_id:Number(artifactId)});
 if(artifact.data.expired||artifact.data.workflow_run?.id!==context.runId)throw new Error('Artifact is expired or belongs to another run');
 core.info(`Deploying uploaded artifact ${artifactId} from run ${context.runId}`);
 const {data:deployment}=await github.request('POST /repos/{owner}/{repo}/pages/deployments',{
  ...repo,artifact_id:Number(artifactId),pages_build_version:context.sha,oidc_token:await core.getIDToken()
 });
 core.setOutput('page_url',deployment.page_url);
 const deadline=Date.now()+600000;
 while(Date.now()<deadline){
  const {data}=await github.request('GET /repos/{owner}/{repo}/pages/deployments/{deployment_id}',{...repo,deployment_id:deployment.id});
  if(data.status==='succeed'){core.info('Pages deployment succeeded');return;}
  if(['deployment_failed','deployment_content_failed','deployment_cancelled','deployment_lost','failed','cancelled'].includes(data.status))throw new Error(`Pages deployment ${deployment.id}: ${data.status}`);
  core.info(`Pages deployment ${deployment.id}: ${data.status}`);
  await new Promise(resolve=>setTimeout(resolve,10000));
 }
 await github.request('POST /repos/{owner}/{repo}/pages/deployments/{deployment_id}/cancel',{...repo,deployment_id:deployment.id});
 throw new Error(`Pages deployment ${deployment.id} timed out`);
}
