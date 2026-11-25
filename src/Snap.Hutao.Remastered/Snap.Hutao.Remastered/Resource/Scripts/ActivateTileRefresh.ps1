# Snap Hutao Remastered - 后台刷新磁贴激活脚本。
# 由计划任务 "Snap Hutao Tile Refresh" 通过 TileRefresh.vbs 静默调用，
# 用 AUMID 激活应用，使新进程获得 MSIX 包身份（磁贴 API 的必要条件）。
#
# 自愈：应用卸载后激活必然失败，此时连同计划任务与暂存脚本一起清理，避免残留。
param(
    [Parameter(Mandatory = $true)][string]$Aumid,
    [Parameter(Mandatory = $false)][string]$Arguments = '--refresh-tile'
)

$ErrorActionPreference = 'Stop'

# 必须与 TileRefreshActivation.ResultFileName 一致：被激活的进程把刷新结果写在这里。
$resultFileName = 'TileRefresh.last'

$source = @'
using System;
using System.Runtime.InteropServices;

public static class HutaoAppActivator
{
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        // PreserveSig：直接返回 HRESULT。没有它，COM 会把返回值变成隐藏的
        // out 参数，成功激活会被当成失败。
        [PreserveSig]
        int ActivateApplication(string appUserModelId, string arguments, int options, out uint processId);
    }

    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager
    {
    }

    public static uint Activate(string appUserModelId, string arguments)
    {
        IApplicationActivationManager manager = (IApplicationActivationManager)new ApplicationActivationManager();
        uint processId;
        // options = AO_NOERRORUI(2)：激活失败时禁止 Shell 弹出错误界面。
        // 否则隐藏窗口下的错误对话框无人点击，任务会一直挂住。
        int hr = manager.ActivateApplication(appUserModelId, arguments, 2, out processId);
        Marshal.ThrowExceptionForHR(hr);
        return processId;
    }
}
'@

function Remove-OrphanedSchedule
{
    # 应用已不在：删除计划任务和残留文件。
    & schtasks.exe /Delete /TN 'Snap Hutao Tile Refresh' /F 2>&1 | Out-Null

    # 本脚本执行期间无法删除自身。拼一条完整的 cmd 命令行交给隐藏进程，
    # 它延后几秒再删，那时本进程已经退出。
    # 名字必须与 TileRefreshScheduler 暂存时的一致（TileRefresh.ps1 / TileRefresh.vbs）。
    $targets = @('TileRefresh.ps1', 'TileRefresh.vbs') |
        ForEach-Object { Join-Path $PSScriptRoot $_ } |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { "del /f /q `"$_`"" }

    if ($targets.Count -eq 0)
    {
        return
    }

    $commandLine = '/c "ping 127.0.0.1 -n 4 >nul & ' + ($targets -join ' & ') + '"'
    Start-Process -FilePath 'cmd.exe' -ArgumentList $commandLine -WindowStyle Hidden -ErrorAction SilentlyContinue | Out-Null
}

function Test-PackageRegistered
{
    # 通过包注册表判断应用是否已卸载。Get-AppxPackage 在计划任务上下文
    # 中不总是可用，这里直接查 Repository\Packages。
    # 键名是 PackageFullName：Name_<Version>_<Arch>__<PublisherId>，
    # 而 AUMID 前段是 FamilyName：Name_PublisherId。两者之间隔着版本与架构，
    # 因此拆出 Name（首段）与 PublisherId（末段）分别匹配，版本变化（升级）
    # 不影响判定。
    $familyName = $Aumid.Split('!')[0]
    $namePart = $familyName.Split('_')[0]
    $publisherPart = $familyName.Split('_')[-1]
    $repoRoot = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages'
    $keys = @(Get-ChildItem $repoRoot -Name -ErrorAction SilentlyContinue)
    foreach ($key in $keys)
    {
        if ($key.StartsWith($namePart) -and $key.EndsWith($publisherPart))
        {
            return $true
        }
    }

    return $false
}

if (-not ('HutaoAppActivator' -as [type]))
{
    Add-Type -TypeDefinition $source -Language CSharp
}

function Read-RefreshResult
{
    # 被激活的进程无法把退出码回传给调用方，因此它把结果写进本脚本旁的
    # TileRefresh.last（首行是结果码）。缺失说明进程没能正常收尾。
    $resultPath = Join-Path $PSScriptRoot $resultFileName
    if (-not (Test-Path -LiteralPath $resultPath))
    {
        return $null
    }

    $first = Get-Content -LiteralPath $resultPath -TotalCount 1 -ErrorAction SilentlyContinue
    $code = 0
    if ([int]::TryParse($first, [ref]$code))
    {
        return $code
    }

    return $null
}

# 上一次的结果不能当作这一次的结论。
$staleResult = Join-Path $PSScriptRoot $resultFileName
if (Test-Path -LiteralPath $staleResult)
{
    Remove-Item -LiteralPath $staleResult -Force -ErrorAction SilentlyContinue
}

try
{
    $processId = [HutaoAppActivator]::Activate($Aumid, $Arguments)
}
catch
{
    # 激活失败可能是瞬时错误（应用正在更新等），重试一次再判定。
    Start-Sleep -Seconds 3
    try
    {
        $processId = [HutaoAppActivator]::Activate($Aumid, $Arguments)
    }
    catch
    {
        # 连续两次失败后仍需确认包确实不在：更新期间激活被阻塞属于暂时
        # 状态，此时删除任务会把后台刷新永久关掉。只有包已从注册表消失
        # （真正卸载）才自愈清理。
        if (-not (Test-PackageRegistered))
        {
            Remove-OrphanedSchedule
        }

        exit 1
    }
}

# 等被激活的进程收尾：内部超时 2 分钟，这里留出余量。
$target = Get-Process -Id $processId -ErrorAction SilentlyContinue
if ($target)
{
    try
    {
        $target | Wait-Process -Timeout 150 -ErrorAction Stop
    }
    catch
    {
        # 等不到就按超时处理，交由下面的结果判定。
    }
}

$result = Read-RefreshResult
if ($null -eq $result)
{
    # 进程没写出结果：当作刷新失败，避免把异常当成成功。
    exit 6
}

# 结果码转成计划任务的退出码。1（脚本宿主/编译失败）与 5（刷新失败）分开：
#   0 → 成功
#   4 → 需要人机验证（进程无法完成，需用户在应用内刷新一次）→ 7
#   其他 → 5（刷新失败）
if ($result -eq 4)
{
    exit 7
}

if ($result -ne 0)
{
    exit 5
}

exit 0
