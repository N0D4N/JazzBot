using System;
using JazzBot.Enums;

namespace JazzBot
{
	public sealed class CustomJbException : Exception
	{
		public ExceptionType ExceptionType { get; }

		public CustomJbException(string message, ExceptionType exceptionType) : base(message)
			=> this.ExceptionType = exceptionType;


		public CustomJbException(string message) : base(message)
			=> this.ExceptionType = ExceptionType.Default;

	}
}
