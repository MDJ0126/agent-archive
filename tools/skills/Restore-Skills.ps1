param([switch]$CheckOnly,[switch]$SkipCli,[string]$CodexDirectory=$(if($env:CODEX_HOME){$env:CODEX_HOME}else{Join-Path $env:USERPROFILE '.codex'}),[string]$AgentsDirectory=(Join-Path $env:USERPROFILE '.agents'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$manifest=Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw|ConvertFrom-Json
if($manifest.schemaVersion -ne 1){throw 'Unsupported skill manifest'}
$results=@();$issues=0
foreach($skill in $manifest.skills){
 if($skill.name -notmatch '^[a-z0-9-]+$' -or $skill.location -notin @('codex','agents')){throw 'Invalid skill identity'}
 $root=if($skill.location -eq 'codex'){$CodexDirectory}else{$AgentsDirectory}
 $destination=Join-Path (Join-Path $root 'skills') $skill.name
 $archive=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot $skill.archive))
 if(-not $archive.StartsWith(([IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'snapshots'))+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'Archive escapes snapshot directory'}
 if((Get-FileHash $archive).Hash.ToLowerInvariant() -ne $skill.archiveSha256){throw "Snapshot checksum mismatch: $($skill.name)"}
 $status='present'
 if(-not(Test-Path -LiteralPath $destination)){
  if($CheckOnly){$status='missing';$issues++}else{
   $zip=[IO.Compression.ZipFile]::OpenRead($archive)
   try{foreach($entry in $zip.Entries){$target=[IO.Path]::GetFullPath((Join-Path $destination $entry.FullName));if(-not $target.StartsWith(([IO.Path]::GetFullPath($destination)+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'Archive path escapes skill directory'}}}finally{$zip.Dispose()}
   New-Item -ItemType Directory -Force (Split-Path $destination)|Out-Null
   [IO.Compression.ZipFile]::ExtractToDirectory($archive,$destination)
   $status='installed'
  }
 }
 if($status -ne 'missing'){
  foreach($file in $skill.files){
   $target=[IO.Path]::GetFullPath((Join-Path $destination $file.path))
   if(-not $target.StartsWith(([IO.Path]::GetFullPath($destination)+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'File path escapes skill directory'}
   if(-not(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target).Hash.ToLowerInvariant() -ne $file.sha256){$status='different-preserved';break}
  }
  $actual=@(Get-ChildItem -LiteralPath $destination -File -Recurse -Force)
  if($actual.Count -ne @($skill.files).Count){$status='different-preserved'}
  if($status -eq 'different-preserved'){$issues++}
 }
 $results+=@{name=$skill.name;location=$skill.location;status=$status}
}
if(-not $SkipCli){
 $uv=Get-Command uv -ErrorAction SilentlyContinue
 if(-not $uv -and -not $CheckOnly){
  $winget=Get-Command winget -ErrorAction SilentlyContinue
  if(-not $winget){throw 'Install uv (Python tool manager), then rerun; winget is unavailable'}
  & $winget.Source install --id astral-sh.uv --exact --silent --accept-source-agreements --accept-package-agreements
  if($LASTEXITCODE -ne 0){throw 'uv installation failed'}
  $uvPath=Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Microsoft/WinGet/Packages/astral-sh.uv_*') -Filter uv.exe -Recurse|Select-Object -First 1 -ExpandProperty FullName
  if(-not $uvPath){throw 'uv installed but executable not located; reopen shell and rerun'}
 }elseif($uv){$uvPath=$uv.Source}else{$uvPath=$null}
 foreach($cli in $manifest.cli){
  $binary=Join-Path (Join-Path $env:USERPROFILE '.local/bin') ($cli.command+'.exe')
  $command=Get-Command $cli.command -ErrorAction SilentlyContinue
  if($command){$binary=$command.Source}
  $version=if(Test-Path $binary){(& $binary --version 2>&1|Out-String).Trim()}else{''}
  $matches=$version -match ('(?<![\d.])'+[regex]::Escape($cli.version)+'(?![\d.])')
  $status=if($matches){'present'}elseif($version){'different-preserved'}else{'missing'}
  if($status -eq 'missing' -and -not $CheckOnly -and $uvPath){
   & $uvPath tool install --python 3.13 "$($cli.package)==$($cli.version)"
   if($LASTEXITCODE -ne 0){throw "CLI install failed: $($cli.package)"}
   if(-not(Test-Path $binary)){throw "Installed CLI path unverified: $($cli.command)"}
   $version=(& $binary --version|Out-String).Trim()
   if($version -notmatch [regex]::Escape($cli.version)){throw 'Installed CLI version mismatch'}
   $status='installed'
  }
  if($status -in @('missing','different-preserved')){$issues++}
  $results+=@{name=$cli.command;location='uv-tool';status=$status}
 }
}
$plugins=@();foreach($plugin in $manifest.managedPlugins){$cache=Join-Path $CodexDirectory 'plugins/cache';$found=@('openai-bundled','openai-curated-remote','openai-primary-runtime')|Where-Object{Test-Path (Join-Path (Join-Path $cache $_) $plugin)};$plugins+=@{name=$plugin;status=$(if($found){'cached-connection-unverified'}else{'requires-plugin-connection'})}}
@{skillsAndCli=$results;managedPlugins=$plugins;issues=$issues;note='Plugin cache is not authentication or active connection evidence. Use Plugin Management to connect missing plugins; built-in skills follow the Codex app version.'}|ConvertTo-Json -Depth 6
if($issues){throw "$issues missing/different skills or CLI tools; existing files were preserved"}
