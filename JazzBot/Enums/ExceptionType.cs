namespace JazzBot.Enums
{
	/// <summary>
	/// Provides "type" of <see cref="CustomJbException"/> that happened.
	/// </summary>
	public enum ExceptionType
	{
		/// <summary>
		/// Default type or none provided.
		/// </summary>
		Default = 0,


		/// <summary>
		/// Connected with work of Database.
		/// </summary>
		DatabaseException = 1,


		/// <summary>
		/// Connected with some playlist idk.
		/// </summary>
		PlaylistException = 2,

		/// <summary>
		/// Reason of <see cref="CustomJbException"/> is unknown.
		/// </summary>
		Unknown = 3,


		/// <summary>
		/// <see cref="CustomJbException"/> of this type should be displayed only in <see cref="JazzBot.Data.Bot.ErrorChannel"/>.
		/// </summary>
		ForInnerPurposes = 4,
	}

}
