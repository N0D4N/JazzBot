using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using JazzBot.Data;

namespace JazzBot.Utilities
{
	public sealed class CustomEmojiConverter : IArgumentConverter<DiscordEmojiWrapper>
	{
		public Task<Optional<DiscordEmojiWrapper>> ConvertAsync(string value, CommandContext ctx)
		{
			DiscordEmoji emoji = null;
			DiscordEmojiWrapper wrapper;
			if(ulong.TryParse(value, out ulong result))
			{
				try
				{
					emoji = DiscordEmoji.FromGuildEmote(ctx.Client, result);
					wrapper = new DiscordEmojiWrapper(emoji);
					return Task.FromResult(Optional.FromValue(wrapper));
				}
				catch
				{
					return Task.FromResult(Optional.FromNoValue<DiscordEmojiWrapper>());
				}
			}
			else
			{
				try
				{
					emoji = DiscordEmoji.FromName(ctx.Client, $":{value}:");
					wrapper = new DiscordEmojiWrapper(emoji);
					return Task.FromResult(Optional.FromValue(wrapper));
				}
				catch
				{
					return Task.FromResult(Optional.FromNoValue<DiscordEmojiWrapper>());
				}
			}

		}
	}
}
