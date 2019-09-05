using System;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace JazzBot.Utilities
{
	/// <summary>
	/// Class with predefined <see cref="DiscordEmbedBuilder"/>.
	/// </summary>
	static class EmbedTemplates
	{
		/// <summary>
		/// Create <see cref="DiscordEmbedBuilder"/> with predefined <see cref="DiscordColor"/> and <see cref="DiscordEmbedFooter"/> indicating which member triggered this embed.
		/// </summary>
		/// <param name="executedBy">Member which executed command</param>
		/// <param name="currentMember">Current member of the guild, is needed for <see cref="Helpers.ExtendedColor(DiscordMember, DiscordMember)"/></param>
		public static DiscordEmbedBuilder ExecutedByEmbed(DiscordMember executedBy, DiscordMember currentMember)
		{
			return new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"{executedBy.Username}#{executedBy.Discriminator}",
					IconUrl = executedBy.AvatarUrl
				},
				Timestamp = DateTimeOffset.Now,
				Color = Helpers.ExtendedColor(executedBy, currentMember),
			};
		}

		/// <summary>
		/// General <see cref="DiscordEmbedBuilder"/> for errors.
		/// </summary>
		public static DiscordEmbedBuilder ErrorEmbed()
		{
			return new DiscordEmbedBuilder
			{
				Timestamp = DateTimeOffset.Now,
				Color = DiscordColor.Red
			};
		}

		/// <summary>
		/// <see cref="DiscordEmbedBuilder"/> for errors happened in commands.
		/// </summary>
		/// <param name="member">Member which tried executing <see cref="Command"/> which failed</param>
		/// <param name="command">Command in which error happened</param>
		/// <returns></returns>
		public static DiscordEmbedBuilder CommandErrorEmbed(DiscordMember member, Command command)
		{
			return ErrorEmbed()
				.WithAuthor($"{member.Username}#{member.Discriminator}", iconUrl: member.AvatarUrl)
				.WithFooter($"Комманда \"{command.QualifiedName}\"");
		}


	}
}
