param(
    [Parameter(Mandatory=$true)][string]$Unity,
    [ValidateSet('Generate','EditMode','PlayMode','AndroidDev','AndroidTest','Android','iOS')][string]$Check = 'Generate',
    [string]$TestFilter = ''
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Unity -PathType Leaf)) { throw 'Supply the full path to a working Unity 6 Editor executable.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$results = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$resultName=if($TestFilter){$Check+'-filtered'}else{$Check}
$unityArguments = @('-batchmode','-projectPath',$projectRoot,'-logFile',(Join-Path $results ($resultName + '.log')))
switch ($Check) {
    'Generate' { $unityArguments += @('-quit','-executeMethod','BallQuest.Editor.ProjectGenerator.GenerateProject') }
    'EditMode' { $unityArguments += @('-runTests','-testPlatform','EditMode','-testResults',(Join-Path $results ($resultName+'.xml'))) }
    'PlayMode' { $unityArguments += @('-runTests','-testPlatform','PlayMode','-testResults',(Join-Path $results ($resultName+'.xml'))) }
    'AndroidDev' { $unityArguments += @('-quit','-buildTarget','Android','-executeMethod','BallQuest.Editor.BuildAutomation.AndroidDevelopment') }
    'AndroidTest' { $unityArguments += @('-quit','-buildTarget','Android','-executeMethod','BallQuest.Editor.BuildAutomation.AndroidTesting') }
    'Android' { $unityArguments += @('-quit','-buildTarget','Android','-executeMethod','BallQuest.Editor.BuildAutomation.Android') }
    'iOS' { $unityArguments += @('-quit','-buildTarget','iOS','-executeMethod','BallQuest.Editor.BuildAutomation.IOS') }
}
if($TestFilter -and $Check -in @('EditMode','PlayMode')) { $unityArguments += @('-testFilter',$TestFilter) }
$quotedArguments = $unityArguments | ForEach-Object { '"' + ($_ -replace '"','\"') + '"' }
$process = Start-Process -FilePath $Unity -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru
# Wait for the Editor itself. An Android ADB service may legitimately outlive a failed Editor run.
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw ('Unity failed. See ' + (Join-Path $results ($resultName + '.log'))) }
