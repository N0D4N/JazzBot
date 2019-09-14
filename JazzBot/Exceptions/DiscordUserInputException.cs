using System;

namespace JazzBot.Exceptions
{
	public sealed class DiscordUserInputException : Exception
	{
		public string ArgumentName { get; private set; }

		public DiscordUserInputException(string message) : base(message)
			=> this.ArgumentName = string.Empty;

		public DiscordUserInputException(string message, string argumentName) : base(message)
			=> this.ArgumentName = argumentName;
	}
}
