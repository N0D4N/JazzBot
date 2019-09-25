using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace JazzBot.Attributes
{
	/// <summary>
	/// Defines that command or group of commands can only be executed if user is connected to voice channel.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class RequireVoiceConnectionAttribute	: CheckBaseAttribute
	{
		/// <summary>
		/// Check if user must be connected to the same voice channel as bot.
		/// </summary>
		public bool SameVoiceChannelAsBot { get; }

		/// <summary>
		/// Defines that command or group of commands can only be executed if user is connected to voice channel.
		/// </summary>
		/// <param name="sameVoiceChannelAsBot">Check if user must be connected to the same voice channel as bot.</param>
		public RequireVoiceConnectionAttribute(bool sameVoiceChannelAsBot)
		{
			this.SameVoiceChannelAsBot = sameVoiceChannelAsBot;
		}

		public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
		{

			var memberVoiceChannel = context.Member?.VoiceState?.Channel;

			if(memberVoiceChannel == null)
				return Task.FromResult(false);

			if(!this.SameVoiceChannelAsBot)
				return Task.FromResult(true);

			var botVoiceChannel = context.Guild.CurrentMember?.VoiceState?.Channel;

			/* 
			 * It's needed for some commands that can be executed if bot is only in the same voice channel as user 
			 * OR
			 * bot is not connected to voice channels at all.
			 * You may want to change it.
			*/
			if(botVoiceChannel == null)
				return Task.FromResult(true);

			return Task.FromResult(memberVoiceChannel.Id == botVoiceChannel.Id);
		}
	}
}
