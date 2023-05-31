# PinBot

A Discord Bot that provides a better experience when pinning messages. PinBot allows you to designate a channel to house your pins, rather than Discord's built-in pin feature, which means you won't be limited to the 100 pins you can store in a channel.

Now written in C# and .NET 7.

[Invite PinBot to your Discord Server](https://discord.com/api/oauth2/authorize?client_id=830875816300380210&permissions=2684873936&scope=bot%20applications.commands)

[Discord Support Server](https://discord.gg/Za4NAtJJ9v)

![PinBot](https://github.com/rarDevelopment/pin-bot-dotnet/assets/4060573/f54f2c1c-9c3f-47a3-86a3-e7afae6cffe7)

## Getting Started

- Invite the bot to your server using the invite link above.
- Designate a channel for your pins to be housed within.
- If desired, run `/catch-up` and specify the channel to catch up. This will remove all pins in that channel and move them into the newly designated pin channel.
- Decide which of the modes you want enabled/disabled (next section).

## Modes

Note: These modes are not exclusive from each other.

### Auto Mode

When Auto Mode is enabled, PinBot will automatically pin a message you've pinned to the designated channel. This means when a message is pinned like normal, PinBot will re-post that pin in the designated pin channel, and then unpin the existing pin to keep your standard pins empty.

If Auto Mode is disabled, pins will behave like normal Discord pins, and you can pin to the designated channel by replying to the messasge with 📌. You can enable or disable Auto Mode using the `/set-auto-mode` command.

NOTE: Auto Mode is **ON** by default.

### Pin Voting

Ordinarily, only users with the "Manage Messages" permission can pin messages in a channel. If you enable Pin Voting, users can "vote" to pin a message by adding a 📌 reaction to that message. If the required number of reaction votes are on a message, that message will be pinned. You cannot unpin a message by removing your votes.

NOTE: Pin Voting is **OFF** by default.

## Commands

**Note:** All slash commands can only be used by the administrators.

---

`/set-channel`

Sets the channel that will house pins (pins from any existing pin channel will NOT be moved).

---

`/catch-up`

Processes all messages currently pinned to the existing channel and pins them in PinBot's designated pin channel before removing the pins from the channel itself. Use carefully as this cannot be undone!

---
