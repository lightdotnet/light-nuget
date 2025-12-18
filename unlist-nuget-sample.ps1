<# Note:
    Must run this script with "Run Selection (F8)" 
#>

$name = "Lightsoft"
$packageFolders = Get-ChildItem -Path "src\blazor\src" -Directory | Select-Object -ExpandProperty Name

foreach ($folder in $packageFolders) {

  $packageId = "$name.$folder"
  
  Unlist-Package($packageId)

}

# Unlist-Package('Lightsoft.AspNetCore.Swagger')

$apiKey = "<your_api_key_with_unlist_permission>"

function Unlist-Package {
    param (
        [string]$PackageId
    )

    # NuGet APIs REQUIRE lowercase IDs
    $lowerId = $PackageId.ToLowerInvariant()
    $url = "https://api.nuget.org/v3/registration5-gz-semver2/$lowerId/index.json"

    try {
        $registration = Invoke-RestMethod -Uri $url -Method Get
    }
    catch {
        Write-Host "Failed to fetch registration data for $PackageId"
        return
    }

    foreach ($page in $registration.items) {

        # Some pages inline items, some require an extra fetch
        if ($page.items -eq $null -and $page.'@id') {
            $page = Invoke-RestMethod -Uri $page.'@id'
        }

        foreach ($item in $page.items) {
            $entry = $item.catalogEntry

            Write-Host $entry.version : $entry.listed

            if ($entry.listed -eq $true) {
                $version = $entry.version

                Write-Host "Unlisting $PackageId $version"

                dotnet nuget delete `
                    $PackageId `
                    $version `
                    --source https://api.nuget.org/v3/index.json `
                    --api-key $apiKey `
                    --non-interactive

                Start-Sleep -Seconds 2
            }  
        }

        
    }
}