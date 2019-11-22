using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using JazzBot.Data;

namespace JazzBot.Commands
{
	[Group("Emoji")]
	[Description("Команды связанные с использованием эмодзей")]
	[Aliases("e")]
	[RequireBotPermissions(Permissions.UseExternalEmojis)]
	public sealed class EmojiCommands : BaseCommandModule
	{
		[Command("React")]
		[Description("Создает реакцию на сообщение с данным Id и заданной эмодзей")]
		[Aliases("r")]
		[RequirePermissions(Permissions.AddReactions)]
		public async Task React(CommandContext context, [Description("Id сообщения")] ulong messageId, [RemainingText, Description("Название или id эмодзи")] DiscordEmojiWrapper emojiWrapper)
		{
			var emoji = emojiWrapper.Value;
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
		public async Task ExecuteGroup(CommandContext context, [RemainingText, Description("Название или id эмодзи")] DiscordEmojiWrapper emojiWrapper)
		{
			var emoji = emojiWrapper.Value;
			await context.RespondAsync(emoji).ConfigureAwait(false);
		}
	}
}
