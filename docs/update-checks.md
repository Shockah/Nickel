[← back to readme](README.md)  
[← back to player guide](player-guide.md)

# Update checks

Nickel comes with pre-installed mods which do automatic update checks for your mods (including Nickel itself), but to make these work (correctly, or even at all, depending on the update source), they need to be configured.

1. Start Nickel.
2. Go into the Main Menu of the game, if you are not currently in it.
3. Click the "Mod Settings" button in the top-right corner of the menu. If you do not see that button, make sure you installed Nickel correctly.
4. Continue with steps for the update source you want to configure - preferably all of them.

## GitHub

GitHub **does not require** any configuration, but **it is highly recommended** you do. If you do not, any GitHub update checks Nickel does will be rate limited by IP address instead of just you, which makes it possible for you to share these limits with other people, which could stop update checks from working.

GitHub allows you to generate new tokens with a set expiry date. The maximum time a token can be valid for is 3 months (or 1 year if you choose the date manually). After that time, Nickel should be able to tell that your token has expired and will notify you to update your token.

To configure GitHub update checks:

1. Create a [GitHub](https://github.com/) account, if you do not have one yet.
1. Make sure you have followed the steps from the main *Update checks* section.
2. Click "Nickel: GitHub update checks".
3. Double-check the "Enabled" option is ticked, and if not, click on it.
4. Click on the "Setup" button next to the "Token" setting. This will open your web browser. If it does not, open this link manually: https://github.com/settings/tokens?type=beta
5. If needed, log in to your GitHub account.
6. Click the "Generate new token" button.
7. Fill out the fields:
	1. Give the token any name - it is only used to help you identify the token.
	2. Choose the expiration time for the token.
	3. Leave the other options as-is. Nickel does not require any additional permissions.
8. Click the "Generate token" button at the bottom.
9. Copy the generated token - the token starts with the `github_pat_` or a similar sequence.
10. Go back into Nickel.
11. Click on the "Paste" button next to the "Token" setting.
12. You are all set to go!

## NexusMods

NexusMods update checks **require** configuration. If not configured, Nickel will not be able to check for any mod updates from NexusMods.

NexusMods allows you to generate an API key. These keys do not expire.

To configure NexusMods update checks:

1. Create a [NexusMods](https://www.nexusmods.com/) account, if you do not have one yet.
1. Make sure you have followed the steps from the main *Update checks* section.
2. Click "Nickel: NexusMods update checks".
3. Double-check the "Enabled" option is ticked, and if not, click on it.
4. Click on the "Setup" button next to the "API key" setting. This will open your web browser. If it does not, open this link manually: https://next.nexusmods.com/settings/api-keys
5. If needed, log in to your NexusMods account.
6. Scroll the page to the "Nickel"/"The community-created mod loader for Cobalt Core." entry, which should be somewhere near the bottom.
7. Click the "REQUEST API KEY" button next to Nickel's entry.
8. Copy the generated API key after it appears.
9. Go back into Nickel.
10. Click on the "Paste" button next to the "API key" setting.
11. You are all set to go!