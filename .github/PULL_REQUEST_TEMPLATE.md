## Summary

Describe the change and why it is needed.

## Verification

- [ ] Built locally with `dotnet build src\FileBrowserDesktop.csproj`
- [ ] Tested affected user flow manually
- [ ] Updated documentation if behavior changed

## Security Review

- [ ] Does not store secrets in JSON, command arguments, logs, or source
- [ ] Does not disable SSH host-key checking
- [ ] Does not expose File Browser publicly
- [ ] Credential or SSH behavior changes are explained above
