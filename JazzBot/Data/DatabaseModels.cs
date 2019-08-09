using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazzBot.Data
{
	/// <summary>
	/// All info about guilds bot are in.
	/// </summary>
	[Table("Guild")]
	public class DGuild
	{
		/// <summary>
		/// ID of guild.
		/// </summary>
		[Key]
		[Required]
		[Column("Guild_ID")]
		public ulong IdOfGuild { get; set; }

		/// <summary>
		/// ID of song currentlyplaying.
		/// </summary>
		[Required]
		[Column("Current_Song_ID")]
		public int IdOfCurrentSong { get; set; }

		/// <summary>
		/// Name of current playlist on this guild.
		/// </summary>
		[Required]
		[Column("Playlist_Name")]
		[MaxLength(30)]
		public string PlaylistName { get; set; }

		/// <summary>
		/// Seed of the current guild playlist.
		/// </summary>
		[Required]
		[Column("Seed")]
		public int Seed { get; set; }
		
	}

	[Table("Songs")]
	public  class Songs
	{
		/// <summary>
		/// Id of song in table.
		/// </summary>
		[Key]
		[Required]
		[Column("Song_Id")]
		public int SongId { get; set; }

		/// <summary>
		/// Path to song.
		/// </summary>
		[Required]
		[Column("Path")]
		[MaxLength(300)]
		public string Path { get; set; }

		/// <summary>
		/// Title of the song.
		/// </summary>
		[Required]
		[Column("Name")]
		public string Name { get; set; }

		/// <summary>
		/// Name of playlist.
		/// </summary>
		[Required]
		[Column("Playlist_Name")]
		[MaxLength(30)]
		public string PlaylistName { get; set; }

		/// <summary>
		/// Used to differ songs in guild playlist.
		/// </summary>
		[NotMapped]
		public double Numing { get; set; }
	}

	//[Table("Music")]
	//public  class Music
	//{
	//	[Key]
	//	[Column("Id")]
	//	public int Id { get; set; }

	//	[Column("PlaylistName")]
	//	[MaxLength(25)]
	//	public string PlaylistName { get; set; }

	//	[Column("IdOfCurrentSong")]
	//	public int IdOfCurrentSong { get; set; }
	//}

	
	[Table("Tags")]
	public class Tag
	{
		/// <summary>
		/// ID of the tag.
		/// </summary>
		[Key]
		[Required]
		[Column("Id")]
		public ulong ID { get; set; }

		/// <summary>
		/// Name of the tag.
		/// </summary>
		[Column("Tag_name")]
		[Required]
		public string Name { get; set; }
		
		/// <summary>
		/// Content of the tag.
		/// </summary>
		[Column("Tag_content")]
		[Required]
		public string TagContent { get; set; }

		/// <summary>
		/// Date of creation of tag.
		/// </summary>
		[Column("Creation_date")]
		[Required]
		public DateTime CretionDate { get; set; }

		/// <summary>
		/// Date of last edit of tag.
		/// </summary>
		[Column("Latest_revision_date")]
		[Required]
		public DateTime RevisionDate { get; set; }

		/// <summary>
		/// Id of guild tag belongs to.
		/// </summary>
		[Column("GuildID")]
		[Required]
		public ulong GuildID { get; set; }

		/// <summary>
		/// Id of owner of the tag.
		/// </summary>
		[Column("OwnerID")]
		[Required]
		public ulong OwnerID { get; set; }

		/// <summary>
		/// How many times tag have been used.
		/// </summary>
		[Column("Amount_of_times_used")]
		[Required]
		public int TimesUsed { get; set; }
	}

	/// <summary>
	/// Configs for bots.
	/// </summary>
	[Table("Configs")]
	public class Configs
	{
		/// <summary>
		/// Id of bot.
		/// </summary>
		[Key]
		[Required]
		[Column("Id")]
		public ulong Id { get; set; }

		/// <summary>
		/// Status string of bot.
		/// </summary>
		[Column("Presence")]
		[Required]
		public string Presence { get; set; }
	}


}
