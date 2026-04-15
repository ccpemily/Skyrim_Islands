const { execSync } = require('child_process');
const path = require('path');

const workingDir = path.resolve(__dirname, '..', '..', '..');
const rimWorldPath = path.resolve(workingDir, 'RimWorldWin64.exe');

// 使用 PowerShell 获取或启动 RimWorld 进程，并通过 .NET 的 WaitForExit() 阻塞等待
// execSync 会让 Node 进程同步等待 PowerShell 结束，因此 VS Code 调试栏会一直保持
const command = `
    $p = Get-Process RimWorldWin64 -ErrorAction SilentlyContinue | Select-Object -First 1;
    if (-not $p) {
        $p = Start-Process -FilePath '${rimWorldPath.replace(/'/g, "''")}' -WorkingDirectory '${workingDir.replace(/'/g, "''")}' -PassThru;
    }
    $p.WaitForExit();
`.trim().replace(/\r?\n/g, ' ');

try {
    execSync(`powershell -NoProfile -Command "${command}"`, { stdio: 'inherit' });
} catch (e) {
    console.error('Failed to launch or wait for RimWorld:', e.message);
    process.exit(1);
}
