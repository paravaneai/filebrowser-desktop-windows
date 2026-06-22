namespace FileBrowserDesktop;

public sealed class ConnectionProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Server";
    public string SshUser { get; set; } = "";
    public string SshHost { get; set; } = "";
    public int SshPort { get; set; } = 22;
    public string SshIdentityFile { get; set; } = "";
    public string LocalHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 18080;
    public string RemoteHost { get; set; } = "127.0.0.1";
    public int RemotePort { get; set; } = 8080;

    public string SshTarget => string.IsNullOrWhiteSpace(SshUser)
        ? SshHost.Trim()
        : $"{SshUser.Trim()}@{SshHost.Trim()}";

    public string LocalUri => $"http://{LocalHost}:{LocalPort}/";

    public ConnectionProfile Clone()
    {
        return new ConnectionProfile
        {
            Id = Id,
            Name = Name,
            SshUser = SshUser,
            SshHost = SshHost,
            SshPort = SshPort,
            SshIdentityFile = SshIdentityFile,
            LocalHost = LocalHost,
            LocalPort = LocalPort,
            RemoteHost = RemoteHost,
            RemotePort = RemotePort,
        };
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? SshTarget : Name;
    }
}
