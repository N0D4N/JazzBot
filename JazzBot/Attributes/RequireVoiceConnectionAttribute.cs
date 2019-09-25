using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace JazzBot.Attributes
{
	/// <summary>
	/// Specifies that command or group of commands can only be executed if user is connected to voice channel.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class RequireVoiceConnectionAttribute	: CheckBaseAttribute
	{
		/// <summary>
		/// Check if user must be connected to the same voice channel as bot.
		/// </summary>
		public bool SameVoiceChannelAsBot { get; }

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

			if(botVoiceChannel == null)
				return Task.FromResult(false);

			return Task.FromResult(memberVoiceChannel.Id == botVoiceChannel.Id);
		}
	}
}
