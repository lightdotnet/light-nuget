$name = "Lightsoft"
$packageFolders = Get-ChildItem -Path "src" -Directory | Select-Object -ExpandProperty Name

foreach ($folder in $packageFolders) {

  $packageId = "$name.$folder"
  
  Unlist-Package($packageId)

}

$apiKey = "<your_api_key_with_unlist_permission>"

function Unlist-Package {
    param (
        [string]$PackageId
    )

    $json = Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$PackageId/index.json" | ConvertFrom-Json

  if ($json.error -ne $null) {
        Write-Host "Error when get $packageId : $json.code $json.message"
        return;
    }
	
  foreach($version in $json.versions) {
	Write-Host "Unlisting $packageId, Ver $version"
	dotnet nuget delete $packageId $version --source https://api.nuget.org/v3/index.json --non-interactive --api-key $apiKey

    Start-Sleep -Seconds 1
  }
}