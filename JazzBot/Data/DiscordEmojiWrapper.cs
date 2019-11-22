using DSharpPlus.Entities;

namespace JazzBot.Data
{
	public struct DiscordEmojiWrapper
	{
		public DiscordEmoji Value { get; }

		public DiscordEmojiWrapper(DiscordEmoji emoji)
			=> this.Value = emoji;
	}
}
