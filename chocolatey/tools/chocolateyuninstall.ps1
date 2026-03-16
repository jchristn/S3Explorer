$ErrorActionPreference = 'Stop'

$packageArgs = @{
    packageName    = $env:ChocolateyPackageName
    softwareName   = 'S3 Explorer*'
    fileType       = 'exe'
    silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
    validExitCodes = @(0)
}

$uninstalled = $false
[array]$key = Get-UninstallRegistryKey -SoftwareName $packageArgs['softwareName']

if ($key.Count -eq 1) {
    $key | ForEach-Object {
        $packageArgs['file'] = "$($_.UninstallString)"
        Uninstall-ChocolateyPackage @packageArgs
    }
    $uninstalled = $true
} elseif ($key.Count -eq 0) {
    Write-Warning "$($packageArgs['packageName']) has already been uninstalled by other means."
} elseif ($key.Count -gt 1) {
    Write-Warning "$($key.Count) matches found!"
    Write-Warning "The following is a log of the keys found:"
    $key | ForEach-Object { Write-Warning "- $($_.DisplayName) $($_.DisplayVersion) ($($_.PSChildName))" }
    throw "Multiple uninstall registry keys found. Manual action may be required."
}
