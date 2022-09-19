$dir = (Get-Location).Path
Write-Output "dir" $dir

function run($path, $name, $port) { 
  Write-Output "run" $name
  $lpath = Join-Path -Path $dir -ChildPath $path 
  $cpath = Join-Path -Path $dir -ChildPath "components"  
  return dapr run --app-port $port --app-id $name --components-path $cpath -- dotnet watch run --project=$lpath $port
}


function api() { 
  return run "dotnet/Eth/ScrapperAPI" "scrapper-api" 5005
}

function dispatcher() { 
  return run "dotnet/Eth/ScrapperDispatcherActor" "scrapper-dispatcher-actor" 5001
}

Start-Process powershell (api) 
Start-Process powershell (dispatcher) 
