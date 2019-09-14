using System.Data.Common;
using JazzBot.Enums;

namespace JazzBot.Exceptions
{
	public sealed class DatabaseException : DbException
	{
		public DatabaseActionType ActionType { get; }
		public DatabaseException(string message) : base(message)
			=> this.ActionType = DatabaseActionType.Default;

		public DatabaseException(string message, DatabaseActionType actionType) : base(message)
			=> this.ActionType = actionType;

		public DatabaseException(DatabaseActionType actionType)
			=> this.ActionType = actionType;
	}
}
