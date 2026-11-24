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

$source = @'
using System;
using System.Runtime.InteropServices;

public static class HutaoAppActivator
{
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
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

if (-not ('HutaoAppActivator' -as [type]))
{
    Add-Type -TypeDefinition $source -Language CSharp
}

try
{
    [void][HutaoAppActivator]::Activate($Aumid, $Arguments)
    exit 0
}
catch
{
    # 激活失败可能是瞬时错误（应用正在更新等），重试一次再判定。
    Start-Sleep -Seconds 3
    try
    {
        [void][HutaoAppActivator]::Activate($Aumid, $Arguments)
        exit 0
    }
    catch
    {
        # 连续两次失败：应用大概率已卸载，自愈清理。
        Remove-OrphanedSchedule
        exit 1
    }
}
