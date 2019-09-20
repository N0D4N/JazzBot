$BotProjectDir = (get-item $PSScriptRoot).Parent.FullName;
$RI = "win10-x64";

Write-Host "Select build options: ";
Write-Host "[1] Linux x64";
Write-Host "[2] Linux Arm";
Write-Host "Type anything else for Windows 10 x64";
$Choice = Read-Host;
switch ($Choice)
{
    "1" {$RI = "linux-x64";}
    "2" {$RI = "linux-arm";}
    default{}
}

Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" -ArgumentList "publish -c Release -f netcoreapp2.2 -o .\Builds -r $RI" -PassThru -WorkingDirectory $BotProjectDir