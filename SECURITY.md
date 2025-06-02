# Security Guidelines

## API Key Protection

Your Sonarr API key should **NEVER** be committed to the repository. This project uses secure configuration methods to protect your credentials.

### 🔐 Setup Instructions

#### Method 1: Local Configuration File (Recommended)
1. Copy the example file:
   ```bash
   cp SonarrFlowLauncherPlugin/plugin.local.yaml.example SonarrFlowLauncherPlugin/plugin.local.yaml
   ```

2. Edit `plugin.local.yaml` and add your real API key:
   ```yaml
   ApiKey: "your-actual-sonarr-api-key-here"
   ```

3. The `plugin.local.yaml` file is automatically ignored by git and will never be committed.

#### Method 2: Environment Variable
Set the `SONARR_API_KEY` environment variable:

**Windows:**
```cmd
set SONARR_API_KEY=your-actual-sonarr-api-key-here
```

**PowerShell:**
```powershell
$env:SONARR_API_KEY="your-actual-sonarr-api-key-here"
```

**Linux/Mac:**
```bash
export SONARR_API_KEY="your-actual-sonarr-api-key-here"
```

### 🛡️ What's Protected

- `plugin.local.yaml` - Local config file (gitignored)
- `secrets.json` - Alternative secrets file (gitignored) 
- `.env` and `.env.local` - Environment files (gitignored)
- All build outputs (`bin/`, `obj/`) (gitignored)

### ⚠️ Important Notes

- **Never** put your API key in `plugin.yaml` (the main config file)
- **Never** hardcode API keys in source code
- The example files show the format but contain placeholder values only
- Test files and API tester automatically load your API key securely

### 🔍 Finding Your Sonarr API Key

1. Open your Sonarr web interface
2. Go to Settings → General
3. Look for "API Key" section
4. Copy the key and use it in your local configuration

### 🚨 If Your API Key is Exposed

If you accidentally committed your API key:

1. **Immediately regenerate** your API key in Sonarr settings
2. Update your local configuration with the new key
3. Consider the old key compromised and never use it again

### 💡 For Contributors

When contributing to this project:
- Use the example files as templates
- Never commit real API keys
- Test with your own local configuration
- Report any security issues privately to the maintainers 