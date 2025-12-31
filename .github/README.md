# .github

GitHub-specific configuration files for the PMDC repository.

## Files

| File | Description |
|------|-------------|
| `workflows/deploy.yml` | CI/CD workflow that builds and releases PMDC for Windows (x86/x64), Linux, and macOS on tag push |

## Deployment Workflow

The `deploy.yml` workflow triggers on any tag push and:
1. Builds PMDC for all platforms (win-x86, win-x64, linux-x64, osx-x64)
2. Builds WaypointServer for all platforms
3. Creates zipped release artifacts
4. Publishes a GitHub Release with all platform builds

## Related

- [../](../) - Repository root
