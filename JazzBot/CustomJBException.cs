using System;
using JazzBot.Enums;

namespace JazzBot
{
	public sealed class CustomJBException : Exception
	{
		public ExceptionType ExceptionType {get;}

		public CustomJBException(string message, ExceptionType exceptionType) :base(message)
			=>	this.ExceptionType = exceptionType;


		public CustomJBException(string message) : base(message)
			=> this.ExceptionType = ExceptionType.Default;
		
	}
}
