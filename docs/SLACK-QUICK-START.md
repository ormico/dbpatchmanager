# Slack Integration Quick Start

## Required GitHub Secrets

Add these to **Settings → Secrets and variables → Actions**:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `SLACK_BOT_TOKEN` | Bot User OAuth Token | `xoxb-XXXXXXXXXX-...` |
| `SLACK_CHANNEL_ID` | Target channel ID | `C01234567AB` |

## Quick Setup (5 Minutes)

### 1. Create Slack App
→ https://api.slack.com/apps → **Create New App** → **From scratch**

### 2. Add Permissions
→ **OAuth & Permissions** → **Bot Token Scopes** → Add:
- `chat:write`
- `chat:write.public`

### 3. Install to Workspace
→ **Install to Workspace** → Copy **Bot User OAuth Token**

### 4. Get Channel ID
→ Open Slack channel → Click channel name → Copy **Channel ID** from bottom of About tab

### 5. Add to GitHub
→ Repository **Settings** → **Secrets and variables** → **Actions** → Add both secrets

✅ Done! Workflows will now send Slack notifications.

## What Gets Notified

| Workflow | Success | Failure |
|----------|---------|---------|
| Main Build | ❌ No | ✅ Yes |
| Release Build | ✅ Yes | ✅ Yes |

## Test It

```bash
# Test release success notification
git tag v1.0.0
git push origin v1.0.0
```

📖 **Full guide:** [SLACK-NOTIFICATION-SETUP.md](./SLACK-NOTIFICATION-SETUP.md)
