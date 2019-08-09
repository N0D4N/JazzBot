namespace JazzBot.Enums
{
	/// <summary>
	/// Provides "type" of <see cref="JazzBot.CustomJBException"/> that happened.
	/// </summary>
	public enum ExceptionType : int
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
		/// Reason of <see cref="JazzBot.CustomJBException"/> is unknown.
		/// </summary>
		Unknown = 3,


		/// <summary>
		/// <see cref="JazzBot.CustomJBException"/> of this type should be displayed only in <see cref="JazzBot.Entities.Bot.ErrorChannel"/>.
		/// </summary>
		ForInnerPurposes = 4,
	}

}
