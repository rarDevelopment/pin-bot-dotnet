using System.Text.RegularExpressions;
using Discord.Webhook;
using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot;

public class PinHandler
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly IDiscordFormatter _discordFormatter;
    private readonly ILogger<DiscordBot> _logger;
    private const int MaxMessageContentLength = 800;
    private const string TruncateSuffix = "[...]";
    private const string MessageLinkText = "View the message";

    public PinHandler(IPinBusinessLayer pinBusinessLayer, IDiscordFormatter discordFormatter, ILogger<DiscordBot> logger)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _discordFormatter = discordFormatter;
        _logger = logger;
    }

    public IList<Embed> BuildPinEmbeds(IMessage messageToPin, string username)
    {
        var embedsToReturn = new List<Embed>();

        var channelMention = $"<#{messageToPin.Channel.Id}>";
        var title = $":pushpin: Pinned by **{username}** in {channelMention}";

        var embedValue =
            $"{TruncateMessage(messageToPin.Content, MaxMessageContentLength, TruncateSuffix)}\n[{MessageLinkText}]({messageToPin.GetJumpUrl()})";
        var embedField = new EmbedFieldBuilder
        {
            Name = messageToPin.Author.Username,
            Value = embedValue,
            IsInline = false
        };

        var textEmbed =
            _discordFormatter.BuildRegularEmbed("", title, null, new List<EmbedFieldBuilder> { embedField });

        embedsToReturn.Add(textEmbed);

        var attachmentEmbeds = new List<Embed>();
        if (messageToPin.Attachments.Any())
        {
            var currentAttachmentCount = 1;
            var totalAttachmentCount = messageToPin.Attachments.Count;

            foreach (var attachment in messageToPin.Attachments)
            {
                var type = GetTypeOfAttachment(attachment);
                var embedNumberOfTotalText = $"{currentAttachmentCount}/{totalAttachmentCount}";
                if (type == "image")
                {
                    var imageEmbed = new EmbedBuilder
                    {
                        Title = $"Attached image ({embedNumberOfTotalText})",
                        ImageUrl = attachment.Url,
                    };
                    attachmentEmbeds.Add(imageEmbed.Build());
                }
                else if (!string.IsNullOrEmpty(attachment.Url))
                {
                    var attachmentEmbed = new EmbedBuilder
                    {
                        Title = $"Attached Media ({embedNumberOfTotalText})",
                        Url = attachment.Url
                    };
                    attachmentEmbeds.Add(attachmentEmbed.Build());
                }
                currentAttachmentCount += 1;
            }
        }

        embedsToReturn.AddRange(attachmentEmbeds);

        return embedsToReturn;
    }

    public IList<Embed> BuildPinEmbedsFromExisting(IMessage messageToPin, IMessage existingMessage)
    {
        var embedsToPreserve = existingMessage.Embeds.TakeLast(existingMessage.Embeds.Count - 1);
        var existingEmbed = existingMessage.Embeds.First();
        var embedsToReturn = new List<Embed>();
        var firstEmbed = new EmbedBuilder
        {
            Description = existingEmbed.Description,
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = existingEmbed.Fields.First().Name,
                    Value = $"{TruncateMessage(messageToPin.Content, MaxMessageContentLength, TruncateSuffix)}\n[{MessageLinkText}]({messageToPin.GetJumpUrl()})",
                    IsInline = false
                }
            }
        };
        embedsToReturn.Add(firstEmbed.Build());
        embedsToReturn.AddRange(embedsToPreserve.Select(e => e as Embed)!);
        return embedsToReturn;
    }

    /// <summary>
    ///     Pins using the webhook and saves to the databases
    /// </summary>
    /// <param name="messageToPin">The message to pin</param>
    /// <param name="pinningUserName">The name of the user or process performing the pin (for display only)</param>
    /// <param name="pinSystemMessage"></param>
    /// <returns><see cref="PinHandlerResult"/></returns>
    public async Task<PinHandlerResult> HandlePin(IMessage messageToPin, string pinningUserName, IMessage? pinSystemMessage = null)
    {
        if (messageToPin.Channel is not IGuildChannel guildChannel)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "Channel on Message was not a valid GuildChannel"
            };
        }

        var pinWebhook = await _pinBusinessLayer.GetWebhook(guildChannel.GuildId.ToString());
        if (pinWebhook == null)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "No pin webhook found in the database for the specified Guild."
            };
        }

        var getExistingPinnedMessage = await _pinBusinessLayer.GetPinnedMessage(messageToPin.Id.ToString(), guildChannel.GuildId.ToString());

        if (getExistingPinnedMessage != null)
        {
            var channelWithPin = await guildChannel.Guild.GetTextChannelAsync(Convert.ToUInt64(getExistingPinnedMessage.ChannelId));
            var existingPin = await channelWithPin.GetMessageAsync(Convert.ToUInt64(getExistingPinnedMessage.NewPinMessageId));
            if (existingPin != null)
            {
                var existingEmbed = _discordFormatter.BuildRegularEmbed(
                    $"{pinningUserName} pinned a message",
                    $"This [message]({messageToPin.GetJumpUrl()}) was already pinned! Check out [the pin]({existingPin.GetJumpUrl()})");
                return new PinHandlerResult
                {
                    IsSuccess = true,
                    EmbedToSend = existingEmbed
                };
            }

            await _pinBusinessLayer.DeleteByMessageId(messageToPin.Id.ToString(), guildChannel.GuildId.ToString());
        }

        var pinChannel = await guildChannel.Guild.GetChannelAsync(Convert.ToUInt64(pinWebhook.ChannelId));
        if (pinChannel is not SocketTextChannel pinTextChannel)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = $"Could not retrieve pin channel with identifier {pinWebhook.ChannelId}"
            };
        }

        var webhookEmbeds = BuildPinEmbeds(messageToPin, pinningUserName);

        var discordWebhook = await guildChannel.Guild.GetWebhookAsync(Convert.ToUInt64(pinWebhook.WebhookId));
        if (discordWebhook == null)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "No webhook found with that identifier in this Guild."
            };
        }

        var webhookClient = new DiscordWebhookClient(discordWebhook);

        var newPinMessageId = await webhookClient.SendMessageAsync(embeds: webhookEmbeds);

        if (newPinMessageId == default)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to send the new pin to the server."
            };
        }

        var newPinMessage = await pinTextChannel.GetMessageAsync(newPinMessageId);
        if (newPinMessage == null)
        {
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = $"Could not retrieve pin message with identifier {newPinMessageId}."
            };
        }

        var didSave = await _pinBusinessLayer.SavePin(messageToPin.Id.ToString(),
            guildChannel.GuildId.ToString(),
            guildChannel.Id.ToString(),
            messageToPin.Author.Id.ToString(),
            DateTime.Now,
            newPinMessageId.ToString(),
            pinWebhook.ChannelId);

        if (!didSave)
        {
            _logger.LogError($"Failed to save pin of message {messageToPin.Id} to database. Removing pin.");
            await newPinMessage.DeleteAsync();
            return new PinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to save pin."
            };
        }

        var embedToReturn = _discordFormatter.BuildRegularEmbed(
            $"{pinningUserName} pinned a message",
            $"{pinningUserName} just pinned [a message]({messageToPin.GetJumpUrl()}). Check out [the pin]({newPinMessage.GetJumpUrl()})");

        if (pinSystemMessage != null)
        {
            await pinSystemMessage.DeleteAsync();
        }

        return new PinHandlerResult
        {
            IsSuccess = true,
            EmbedToSend = embedToReturn
        };
    }

    public async Task<BulkPinHandlerResult> ProcessPinBacklogInChannel(SocketTextChannel channel)
    {
        var pinWebhook = await _pinBusinessLayer.GetWebhook(channel.Guild.Id.ToString());
        if (pinWebhook == null)
        {
            return new BulkPinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "No pin channel configured! Use the config command to set your pin channel."
            };
        }

        var pinChannel = channel.Guild.GetTextChannel(Convert.ToUInt64(pinWebhook.ChannelId));
        if (pinChannel == null)
        {
            return new BulkPinHandlerResult
            {
                IsSuccess = false,
                ErrorMessage = "Pin channel is gone or missing! Please set a new pin channel."
            };
        }

        var pins = await channel.GetPinnedMessagesAsync();
        if (!pins.Any())
        {
            return new BulkPinHandlerResult
            {
                IsSuccess = true
            };
        }

        var sortedPins = pins.Reverse();

        foreach (var pin in sortedPins)
        {
            try
            {
                if (pin == null)
                {
                    continue;
                }

                var result = await HandlePin(pin, "BetterPinBot");
                if (!result.IsSuccess)
                {
                    continue;
                }

                if (pin is IUserMessage messageToUnpin)
                {
                    await messageToUnpin.UnpinAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not pin message with identifier {pin?.Id.ToString() ?? "[no identifier]"} - {ex.Message}");
            }
        }

        return new BulkPinHandlerResult
        {
            IsSuccess = true
        };
    }

    private static string TruncateMessage(string messageContent, int length, string trailingEnding)
    {
        return messageContent.Length > length
            ? $"{messageContent.Substring(0, length - trailingEnding.Length)}{trailingEnding}"
            : messageContent;
    }

    private string? GetTypeOfAttachment(IAttachment attachment)
    {
        if (!string.IsNullOrEmpty(attachment.ContentType))
        {
            var mimeTypeSplit = attachment.ContentType.Split("/");
            return mimeTypeSplit[0];
        }

        if (!string.IsNullOrEmpty(attachment.Filename))
        {
            var imageExtensions = new List<string> { "jpg", "gif", "png", "jpeg" };
            var fileName = attachment.Filename.ToLower();
            var fileNameRegex = new Regex("\\.[0-9a-z]+$");
            var matches = fileNameRegex.Matches(fileName);
            if (matches.Any())
            {
                var match = matches.First().Value;
                return imageExtensions.Contains(match.Replace(".", "")) ? "image" : null;
            }
        }

        _logger.LogError("Could not get content_type or filename for this attachment {0}", attachment.Id);

        return null;
    }
}