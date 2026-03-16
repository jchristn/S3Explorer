$ErrorActionPreference = 'Stop'

$packageArgs = @{
    packageName    = $env:ChocolateyPackageName
    fileType       = 'exe'
    # TODO: Update this URL to point to your hosted installer (e.g. GitHub Releases)
    url64bit       = 'https://github.com/jchristn/S3Explorer/releases/download/v1.0.0/S3ExplorerSetup-1.0.0.exe'
    softwareName   = 'S3 Explorer*'
    checksum64     = 'TODO_REPLACE_WITH_SHA256_CHECKSUM'
    checksumType64 = 'sha256'
    silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
    validExitCodes = @(0)
}

Install-ChocolateyPackage @packageArgs
