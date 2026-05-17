# Update checks

Nickel includes pre-installed mods that provide automatic update checks for installed mods (including Nickel itself).

When updates are found, they are shown in a dedicated full-screen update screen in-game. This screen lists all available updates and allows you to take actions such as:
* Remind later
* Ignore update
* View details

Update information is also shown in the console and written to Nickel's log files. When available, these outputs include direct links to the update pages.

Some update sources require additional setup before Nickel can check for updates.

1. Start Nickel.
2. Go into the Main Menu of the game, if you are not currently in it.
3. Click the "Mod Settings" button in the top-right corner of the menu. If you do not see that button, make sure you installed Nickel correctly.
4. Continue with the steps for each update source you want to use.

## [GitHub](https://github.com/)

GitHub **does not require** any configuration, but **it is highly recommended** that you do. If you do not, GitHub update checks will be rate limited by shared IP address instead of your own account. This means you may end up sharing rate limits with other people, which can cause update checks to stop working.

GitHub allows you to generate new tokens with a configurable expiry date. Tokens can be valid for up to 3 months (or 1 year if you choose the date manually). Nickel will notify you if your token expires.

To configure GitHub update checks:

1. Create a GitHub account, if you do not have one yet.
2. Make sure you have followed the steps from the main *Update checks* section.
3. Click "Nickel: GitHub update checks".
4. Make sure the "Enabled" option is ticked.
5. Click the "Setup" button next to the "Token" setting. This will open your web browser. If it does not, open this link manually: https://github.com/settings/tokens?type=beta
6. If needed, log in to your GitHub account.
7. Click the "Generate new token" button.
8. Fill out the fields:
	1. Give the token any name - it is only used to help you identify the token.
	2. Choose the expiration time for the token.
	3. Leave the other options as-is. Nickel does not require any additional permissions.
9. Click the "Generate token" button at the bottom.
10. Copy the generated token. The token usually starts with `github_pat_` or a similar sequence.
11. Go back into Nickel.
12. Click the "Paste" button next to the "Token" setting.
13. You are all set to go!

## [NexusMods](https://www.nexusmods.com/)

NexusMods update checks require configuration before Nickel can check for updates hosted on NexusMods.

NexusMods allows you to generate an API key. These keys do not expire.

To configure NexusMods update checks:

1. Create a NexusMods account, if you do not have one yet.
2. Make sure you have followed the steps from the main *Update checks* section.
3. Click "Nickel: NexusMods update checks".
4. Make sure the "Enabled" option is ticked.
5. Click the "Setup" button next to the "API key" setting. This will open your web browser. If it does not, open this link manually: https://next.nexusmods.com/settings/api-keys
6. If needed, log in to your NexusMods account.
7. Scroll the page to the "Nickel"/"The community-created mod loader for Cobalt Core." entry, which is usually somewhere halfway through the page.
8. Click the "REQUEST API KEY" button next to Nickel's entry.
9. Copy the generated API key after it appears.
10. Go back into Nickel.
11. Click the "Paste" button next to the "API key" setting.
12. You are all set to go!