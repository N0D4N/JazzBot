using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace JazzBot.Commands
{
	[Group("Emoji")]
	[Description("Комманды связанные с использованием эмодзей")]
	[Aliases("e")]
	[RequireBotPermissions(Permissions.UseExternalEmojis)]
	public sealed class EmojiCommands : BaseCommandModule
	{
		[Command("React")]
		[Description("Создает реакцию на сообщение с данным Id заданной эмодзей")]
		[Aliases("r")]
		[RequirePermissions(Permissions.AddReactions)]
		[Priority(1)]
		public async Task React(CommandContext context, [Description("Id сообщения")] ulong messageId, [Description("Id эмодзи")] ulong emojiId)
		{
			if(!this.TryGetEmojiFromId(context.Client, emojiId, out DiscordEmoji emoji))
			{
				await context.RespondAsync($"Емодзи с Id {emojiId} не найдено").ConfigureAwait(false);
				return;
			}
			DiscordMessage msg;
			try
			{
				msg = await context.Channel.GetMessageAsync(messageId);
			}
			catch(Exception)
			{
				await context.RespondAsync($"Сообщения с Id {messageId} в данном канале не найдено").ConfigureAwait(false);
				return;
			}
			await msg.CreateReactionAsync(emoji).ConfigureAwait(false);
		}

		[Command("React")]
		[Priority(0)]
		public async Task React(CommandContext context, [Description("Id сообщения")] ulong messageId, [RemainingText, Description("Название эмодзи")] string emojiName)
		{
			if(!this.TryGetEmojiFromName(context.Client, emojiName, out DiscordEmoji emoji))
			{
				await context.RespondAsync($"Емодзи с названием {emojiName} не найдено").ConfigureAwait(false);
				return;
			}
			DiscordMessage msg;
			try
			{
				msg = await context.Channel.GetMessageAsync(messageId);
			}
			catch(Exception)
			{
				await context.RespondAsync($"Сообщения с Id {messageId} в данном канале не найдено").ConfigureAwait(false);
				return;
			}
			await msg.CreateReactionAsync(emoji).ConfigureAwait(false);
		}

		[GroupCommand]
		[Priority(1)]
		public async Task ExecuteGroup(CommandContext context, [Description("Id эмодзи")] ulong emojiId)
		{
			if(!this.TryGetEmojiFromId(context.Client, emojiId, out DiscordEmoji emoji))
			{
				await context.RespondAsync($"Емодзи с Id {emojiId} не найдено").ConfigureAwait(false);
				return;
			}
			await context.RespondAsync(emoji).ConfigureAwait(false);
		}

		[GroupCommand]
		[Priority(0)]
		public async Task ExecuteGroup(CommandContext context, [RemainingText, Description("Id эмодзи")] string emojiName)
		{
			if(!this.TryGetEmojiFromName(context.Client, emojiName, out DiscordEmoji emoji))
			{
				await context.RespondAsync($"Емодзи с названием {emojiName} не найдено").ConfigureAwait(false);
				return;
			}
			await context.RespondAsync(emoji).ConfigureAwait(false);
		}

		#region HelpingMethods
		private bool TryGetEmojiFromName(DiscordClient client, string emojiName, out DiscordEmoji resultEmoji)
		{
			try
			{
				resultEmoji = DiscordEmoji.FromName(client, $":{emojiName}:");
			}
			catch(Exception)
			{
				resultEmoji = null;
				return false;
			}
			return true;
		}

		private bool TryGetEmojiFromId(DiscordClient client, ulong emojiId, out DiscordEmoji resultEmoji)
		{
			try
			{
				resultEmoji = DiscordEmoji.FromGuildEmote(client, emojiId);
			}
			catch(Exception)
			{
				resultEmoji = null;
				return false;
			}
			return true;
		}
		#endregion
	}
}
