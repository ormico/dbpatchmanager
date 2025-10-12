# Slack Notification Setup Guide

This guide will help you set up Slack notifications for your CI/CD workflows.

## Overview

The following workflows will send Slack notifications:

- **Main Build (`main-build.yml`)**: Sends notification on **failure**
  only
- **Release Build (`release-build.yml`)**: Sends notifications on both
  **failure** and **success**

## Prerequisites

- Admin access to your Slack workspace
- Admin access to your GitHub repository settings

## Step 1: Create a Slack App

1. Go to [Slack API Apps](https://api.slack.com/apps)
2. Click **"Create New App"**
3. Choose **"From scratch"**
4. Enter app name: `GitHub CI/CD Bot` (or your preferred name)
5. Select your workspace
6. Click **"Create App"**

## Step 2: Configure Bot Permissions

1. In your app settings, go to **"OAuth & Permissions"** (left sidebar)
2. Scroll down to **"Scopes"** → **"Bot Token Scopes"**
3. Add the following scopes:
   - `chat:write` - Send messages as the bot
   - `chat:write.public` - Send messages to public channels without joining
4. Scroll back to top and click **"Install to Workspace"**
5. Review permissions and click **"Allow"**
6. Copy the **"Bot User OAuth Token"** (starts with `xoxb-`)
   - ⚠️ Keep this token secure! You'll need it for GitHub secrets

## Step 3: Get Your Slack Channel ID

### Method 1: Via Slack Desktop/Web App

1. Open your Slack workspace
2. Navigate to the channel where you want notifications
3. Click the channel name at the top
4. In the "About" tab, scroll to the bottom
5. Copy the **Channel ID** (e.g., `C01234567AB`)

### Method 2: Via Slack API

1. Go to <https://api.slack.com/methods/conversations.list/test>
2. Select your app token
3. Click **"Test Method"**
4. Find your channel in the JSON response
5. Copy the `id` field

## Step 4: Add GitHub Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Add two secrets:

### Secret 1: SLACK_BOT_TOKEN

- **Name:** `SLACK_BOT_TOKEN`
- **Value:** Your Bot User OAuth Token (from Step 2)
  - Should start with `xoxb-`
  - Example: `xoxb-XXXXXXXXXX-XXXXXXXXXXXXX-XXXXXXXXXXXXXXXXXXXXXXXX`

### Secret 2: SLACK_CHANNEL_ID

- **Name:** `SLACK_CHANNEL_ID`
- **Value:** Your Channel ID (from Step 3)
  - Example: `C01234567AB`

## Step 5: Test the Integration

### Test Main Build Notifications (Failure Only)

To test a failure notification:

```bash
# Create a feature branch with a failing test
git checkout -b test/slack-notifications
# Make a change that breaks tests, then:
git add .
git commit -m "test: trigger notification"
git push origin test/slack-notifications
# Create PR and merge to main (if tests fail, Slack notifies)
```

### Test Release Build Notifications (Success & Failure)

To test a success notification:

```bash
# On main branch, create and push a tag
git tag v1.0.0
git push origin v1.0.0
# Watch for Slack notification when release completes
```

## Notification Examples

### Main Build Failure Notification

```
🔴 Main Branch Build Failed

Repository: ormico/dbpatchmanager
Branch: main
Commit: abc1234
Triggered by: username

Message: fix: update configuration

[View Workflow Run]
```

### Release Build Success Notification

```
✅ Release Published Successfully

Repository: ormico/dbpatchmanager
Version: 1.2.3
Tag: v1.2.3
Released by: username

🎉 A new release has been published and is ready for deployment!

[View Release] [View Workflow Run]
```

### Release Build Failure Notification

```
🔴 Release Build Failed

Repository: ormico/dbpatchmanager
Tag: v1.2.3
Commit: abc1234
Triggered by: username

❌ The release build has failed. Please check the workflow run for details.

[View Workflow Run]
```

## Customization

### Change Notification Channel

To send notifications to different channels for different workflows:

1. Create additional channel-specific secrets:
   - `SLACK_CHANNEL_ID_RELEASES`
   - `SLACK_CHANNEL_ID_BUILDS`

2. Update workflow files to use the appropriate secret:

   ```yaml
   channel-id: ${{ secrets.SLACK_CHANNEL_ID_RELEASES }}
   ```

### Customize Message Format

Edit the `payload` section in the workflow files:

- **Main Build:** `.GitHub/workflows/main-build.yml` (line ~330)
- **Release Build:** `.GitHub/workflows/release-build.yml` (line ~497 and
  ~562)

Use [Slack Block Kit Builder](https://app.slack.com/block-kit-builder)
to design custom messages.

### Add Mentions

To mention users or groups in notifications, add to the payload:

```yaml
{
  "type": "section",
  "text": {
    "type": "mrkdwn",
    "text": "<!channel> Build failed! <@U01234567> please investigate."
  }
}
```

- `<!channel>` - Mention @channel
- `<!here>` - Mention @here
- `<@U01234567>` - Mention specific user (use their Slack User ID)
- `<!subteam^S01234567>` - Mention user group

## Troubleshooting

### No Notifications Received

1. **Check Bot Token:**
   - Ensure token starts with `xoxb-` prefix
   - Verify secret name is exactly `SLACK_BOT_TOKEN`
   - Re-install app to workspace if token changed

2. **Check Channel ID:**
   - Verify it's the channel ID, not the channel name
   - Ensure secret name is exactly `SLACK_CHANNEL_ID`
   - Try inviting the bot to the channel: `/invite @GitHub CI/CD Bot`

3. **Check Workflow Logs:**
   - Go to Actions → Select the workflow run
   - Check the "Send Slack notification" step for errors

### Permission Errors

If you see "not_in_channel" or "channel_not_found":

1. Ensure bot has `chat:write.public` scope (allows posting to public channels)
2. For private channels, invite the bot: `/invite @GitHub CI/CD Bot`

### Rate Limiting

Slack has rate limits. If you have many failing builds:

- Consider adding a delay between notifications
- Use the existing duplicate issue prevention (already implemented)
- Notifications respect the same logic: won't spam for same commit

## Security Best Practices

- ✅ **Never commit tokens** to your repository
- ✅ **Use GitHub Secrets** for all sensitive values
- ✅ **Rotate tokens** if compromised
- ✅ **Grant minimum permissions** (only `chat:write` and `chat:write.public`)
- ✅ **Monitor bot activity** in Slack App Management

## Additional Resources

- [Slack API Documentation](https://api.slack.com/docs)
- [GitHub Actions Slack Integration](https://github.com/slackapi/slack-github-action)
- [Slack Block Kit Builder](https://app.slack.com/block-kit-builder)
- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## Support

If you encounter issues:

1. Check the [GitHub Action logs](../../actions)
2. Review Slack App management page for errors
3. Verify bot permissions and channel access
4. Test with Slack API test console
