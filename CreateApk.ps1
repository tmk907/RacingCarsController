Param(
    [Parameter(Mandatory=$true)]
    [string]
    $newVersionName,
    [Parameter(Mandatory=$true)]
    [string]
    $signingKeyPass
)

Function ChangeVersion {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $newVersionName
    )

    # Get current version code and increment
    $manifestPath = Resolve-Path '.\RacingCarsControllerAndroid\AndroidManifest.xml'
    [xml]$manifest = Get-Content -Path $manifestPath
    $newVersionCode = ([int]$manifest.manifest.versionCode + 1).ToString()

    $csprojPath = Resolve-Path '.\RacingCarsControllerAndroid\RacingCarsControllerAndroid.csproj'

    # Update manifest
    (Get-Content $manifestPath) -Replace 'android:versionCode="\d+"', "android:versionCode=`"${newVersionCode}`"" | Set-Content $manifestPath
    (Get-Content $manifestPath) -Replace 'android:versionName="[\w\d.-]+"', "android:versionName=`"$newVersionName`"" | Set-Content $manifestPath

    # Update project
    (Get-Content $csprojPath) -Replace '<ApplicationVersion>\d+</ApplicationVersion>', "<ApplicationVersion>$newVersionCode</ApplicationVersion>" | Set-Content $csprojPath
    (Get-Content $csprojPath) -Replace '<ApplicationDisplayVersion>[\w\d.-]+</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$newVersionName</ApplicationDisplayVersion>" | Set-Content $csprojPath

    Write-Host "New versionName: $newVersionName, new versionCode: $newVersionCode"
}

Function Build {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $signingKeyPass
    )

    dotnet publish -f:net7.0-android -c:Release /p:AndroidSigningKeyPass=$signingKeyPass /p:AndroidSigningStorePass=$signingKeyPass
}

Function CopyToLocalAppStore {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $versionName
    )

    $publishDirectory = '.\RacingCarsControllerAndroid\bin\Release\net7.0-android\publish'
    $appStoreDirectory = 'C:\Source\AppStore'
    $appName = 'RacingCarsController'

    $package = 'com.tmk907.RacingCarsControllerAndroid'
    $architectures = @('arm64-v8a','armeabi-v7a','x86_64')

    foreach($arch in $architectures){
        $apkName = "$package-$arch-Signed.apk"
        $publishedApkName = "$appName-v$versionName-$arch.apk"

        Copy-Item -Path "$publishDirectory\$apkName" -Destination "$appStoreDirectory\$publishedApkName"
        Write-Host "$publishedApkName copied to  $appStoreDirectory"
    }
}

ChangeVersion -newVersionName $newVersionName

Build -signingKeyPass $signingKeyPass

CopyToLocalAppStore -versionName $newVersionName