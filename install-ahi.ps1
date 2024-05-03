#Requires -RunAsAdministrator
#Requires -Version 5.0

function GetLatestReleaseLink($repoURL) {
    return (Invoke-WebRequest $repoURL | ConvertFrom-Json).assets[0].browser_download_url
}

function Install-AHK {
    # Download it
    $installerURL = "https://www.autohotkey.com/download/ahk-install.exe"
    Invoke-WebRequest($installerURL) -OutFile ahk-install.exe

    # Install it
    .\ahk-install.exe /S
}

function Install-Interception {
    # Download it
    $repoURL = "http://api.github.com/repos/oblitum/Interception/releases/latest"
    Invoke-WebRequest(GetLatestReleaseLink($repoURL)) -OutFile Interception.zip

    # Unzip it
    Expand-Archive Interception.zip -DestinationPath . -Force

    # Install it
    & '.\Interception\command line installer\install-interception.exe' /install
}

function Install-AHI {
    # Download it
    $repoURL = "http://api.github.com/repos/evilC/AutoHotInterception/releases/latest"
    Invoke-WebRequest(GetLatestReleaseLink($repoURL)) -OutFile AutoHotInterception.zip

    # Unzip it
    Expand-Archive AutoHotInterception.zip -DestinationPath AutoHotInterception -Force
    
    # Copy the Interception libs to AHI libs
    robocopy Interception\library AutoHotInterception\Lib *.* /s /is /it

    # Unblock the DLLs
    AutoHotInterception\Lib\Unblocker.ps1

    # Copy libs to 'My Documents'
    $myDocsLibDir = Join-Path -Path ([Environment]::GetFolderPath("MyDocuments")) -ChildPath "AutoHotkey\lib"
    robocopy AutoHotInterception\Lib $myDocsLibDir *.* /s /is /it 
}


function Clean-Up() {
    Foreach ($item in "ahk-install.exe", "Interception.zip", "Interception", "AutoHotInterception.zip") {
        Remove-Item -Recurse -Force $item
    }
}

function main {
    # Change to script directory
    Set-Location $PSScriptRoot

    Install-AHK
    Install-Interception
    Install-AHI
    Clean-Up

    # Prompt for reboot
    Restart-Computer -Confirm
}

main
