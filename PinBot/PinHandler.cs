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

    public async Task<PinHandlerResult> HandlePin(IMessage messageToPin, string pinningUserName, IMessage pinSystemMessage)
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

        await pinSystemMessage.DeleteAsync();

        return new PinHandlerResult
        {
            IsSuccess = true,
            EmbedToSend = embedToReturn
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