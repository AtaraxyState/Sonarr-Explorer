# Sonarr Flow Launcher Plugin Submission

This plugin is ready for submission to the official Flow Launcher plugin repository.

## Submission Details

**File to submit:** `SonarrFlowLauncher-D29D1AA0-3F6A-4F2E-8D0A-A5B7C9A5EFCF.json`

## Requirements Checklist

✅ **JSON submission file created** - Contains all required fields (ID, Name, Description, Author, Version, Language, Website, UrlDownload, UrlSourceCode, IcoPath)

✅ **GitHub Actions workflow setup** - Automated build and release pipeline configured in `.github/workflows/release.yml`

✅ **CDN icon URL** - Using jsdelivr.com CDN for global accessibility

✅ **Plugin Store policy compliance** - Plugin contains no malicious code, piracy, inappropriate content, etc.

## Next Steps

1. **Create a release**: Tag a version (e.g., `v1.0.0`) to trigger the GitHub Actions workflow
2. **Fork Flow-Launcher/Flow.Launcher.Plugins**: Fork the official plugins repository
3. **Add submission file**: Copy `SonarrFlowLauncher-D29D1AA0-3F6A-4F2E-8D0A-A5B7C9A5EFCF.json` to the `plugins/` directory
4. **Submit pull request**: Create a PR to the official repository

## Release Process

To create a release:
```bash
git tag v1.0.0
git push origin v1.0.0
```

This will trigger the GitHub Actions workflow to build and create a release with the plugin package.

## Manual Installation (before official approval)

Users can manually install using:
```
pm install https://github.com/AtaraxyState/Sonarr-Explorer/releases/latest/download/SonarrFlowLauncher.zip
``` 