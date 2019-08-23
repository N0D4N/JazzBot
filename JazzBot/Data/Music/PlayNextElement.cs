namespace JazzBot.Data.Music
{
	public sealed class PlayNextElement
	{
		/// <summary>
		/// Path to song.
		/// </summary>
		public string PathToFile { get; }

		/// <summary>
		/// Title of the song.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Coefficient of similarity of this song to song that should be played.
		/// </summary>
		public double Coefficient { get; set; }

		public PlayNextElement(string path, string name, double coef)
		{
			this.PathToFile = path;
			this.Title = name;
			this.Coefficient = coef;
		}


	}
}
