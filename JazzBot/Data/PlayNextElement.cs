namespace JazzBot.Data
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

		public PlayNextElement(string path, string name)
		{
			this.PathToFile = path;
			this.Title = name;
			this.Coefficient = 0.0;
		}

		//public static int CompareByCoefficient(PlayNextElement pne1, PlayNextElement pne2)
		//{
		//	return pne1.Coefficient.CompareTo(pne2.Coefficient);
		//}
	}
}
