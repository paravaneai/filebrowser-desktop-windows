using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace FileBrowserDesktop;

internal sealed class SshTunnelService : IDisposable
{
    private Process? _process;
    private readonly StringBuilder _sshOutput = new();

    public bool IsRunning => _process is { HasExited: false };

    public async Task StartAsync(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        Stop();
        _sshOutput.Clear();

        if (string.IsNullOrWhiteSpace(profile.SshTarget))
        {
            throw new InvalidOperationException("SSH target is not configured.");
        }

        if (await IsPortOpenAsync(profile.LocalHost, profile.LocalPort, cancellationToken))
        {
            throw new InvalidOperationException($"{profile.LocalHost}:{profile.LocalPort} is already in use.");
        }

        var sshPath = OperatingSystem.IsWindows()
            ? "ssh.exe"
            : "ssh";

        var startInfo = new ProcessStartInfo
        {
            FileName = sshPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        startInfo.ArgumentList.Add("-N");
        startInfo.ArgumentList.Add("-L");
        startInfo.ArgumentList.Add($"{profile.LocalHost}:{profile.LocalPort}:{profile.RemoteHost}:{profile.RemotePort}");

        if (profile.SshPort != 22)
        {
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(profile.SshPort.ToString());
        }

        if (!string.IsNullOrWhiteSpace(profile.SshIdentityFile))
        {
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(profile.SshIdentityFile);
        }

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("ExitOnForwardFailure=yes");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("BatchMode=yes");
        startInfo.ArgumentList.Add(profile.SshTarget);

        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not start ssh.exe. Make sure OpenSSH Client is installed and available on PATH.", ex);
        }

        if (_process is null)
        {
            throw new InvalidOperationException("Could not start ssh.exe.");
        }

        _process.OutputDataReceived += (_, e) => AppendOutput(e.Data);
        _process.ErrorDataReceived += (_, e) => AppendOutput(e.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        var ready = await WaitForPortAsync(profile.LocalHost, profile.LocalPort, TimeSpan.FromSeconds(12), cancellationToken);
        if (!ready)
        {
            var detail = GetOutputDetail();
            Stop();
            throw new TimeoutException($"SSH tunnel did not open in time.{detail}");
        }
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch
        {
            // The process may already be gone during app shutdown.
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void AppendOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (_sshOutput)
        {
            _sshOutput.AppendLine(line);
        }
    }

    private string GetOutputDetail()
    {
        lock (_sshOutput)
        {
            var detail = _sshOutput.ToString().Trim();
            return string.IsNullOrWhiteSpace(detail)
                ? ""
                : $"{Environment.NewLine}{detail}";
        }
    }

    private static async Task<bool> WaitForPortAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await IsPortOpenAsync(host, port, cancellationToken))
            {
                return true;
            }

            await Task.Delay(200, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
