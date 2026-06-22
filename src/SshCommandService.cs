using System.Diagnostics;
using System.IO;
using System.Text;

namespace FileBrowserDesktop;

internal sealed record SshCommandResult(int ExitCode, string Output)
{
    public bool Success => ExitCode == 0;
}

internal static class SshCommandService
{
    public static Task<SshCommandResult> TestConnectionAsync(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        return RunCommandAsync(profile, "echo FILEBROWSER_DESKTOP_SSH_OK", null, cancellationToken);
    }

    public static Task<SshCommandResult> InstallOrConfigureFileBrowserAsync(
        ConnectionProfile profile,
        string serverRoot,
        bool alreadyInstalled,
        CancellationToken cancellationToken)
    {
        var scriptPath = FindServerInstallScript();
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Server setup script was not found.", scriptPath);
        }

        var script = File.ReadAllText(scriptPath);
        var prefix = string.Equals(profile.SshUser.Trim(), "root", StringComparison.OrdinalIgnoreCase)
            ? "bash -s --"
            : "sudo -n bash -s --";

        var args = new List<string>
        {
            "--root", serverRoot,
            "--address", profile.RemoteHost,
            "--port", profile.RemotePort.ToString(),
        };

        if (alreadyInstalled)
        {
            args.Add("--already-installed");
        }

        var remoteCommand = prefix + " " + string.Join(" ", args.Select(ShellQuote));
        return RunCommandAsync(profile, remoteCommand, script, cancellationToken);
    }

    public static async Task<SshCommandResult> RunCommandAsync(
        ConnectionProfile profile,
        string remoteCommand,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateBaseStartInfo(profile);
        startInfo.ArgumentList.Add(profile.SshTarget);
        startInfo.ArgumentList.Add(remoteCommand);

        if (standardInput is not null)
        {
            startInfo.RedirectStandardInput = true;
        }

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => AppendLine(output, e.Data);
        process.ErrorDataReceived += (_, e) => AppendLine(output, e.Data);

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Could not start ssh.exe.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not start ssh.exe. Make sure OpenSSH Client is installed and available on PATH.", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            process.StandardInput.Close();
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new SshCommandResult(process.ExitCode, output.ToString().Trim());
    }

    private static ProcessStartInfo CreateBaseStartInfo(ConnectionProfile profile)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "ssh.exe" : "ssh",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

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
        startInfo.ArgumentList.Add("BatchMode=yes");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("ConnectTimeout=10");

        return startInfo;
    }

    private static string FindServerInstallScript()
    {
        var publishedPath = Path.Combine(AppContext.BaseDirectory, "server", "install-filebrowser-localhost.sh");
        if (File.Exists(publishedPath))
        {
            return publishedPath;
        }

        var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "server", "install-filebrowser-localhost.sh"));
        return sourcePath;
    }

    private static string ShellQuote(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'") + "'";
    }

    private static void AppendLine(StringBuilder output, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup on cancellation.
        }
    }
}
