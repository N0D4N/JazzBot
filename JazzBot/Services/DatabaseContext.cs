using JazzBot.Data;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;

namespace JazzBot.Services
{
	public class DatabaseContext : DbContext
	{
		/// <summary>
		/// Guilds in which bot is in.
		/// </summary>
		public DbSet<DGuild> Guilds { get; set; }

		/// <summary>
		/// Playlists.
		/// </summary>
		public DbSet<Songs> Playlist { get; set; }

		public DbSet<Tag> Tags { get; set; }

		public DbSet<Configs> Configs { get; set; }

		private string PgSqlCS { get; }

		public DatabaseContext()
		{
			this.PgSqlCS = Program.CfgJson.Database.NpgSqlConnectionString();
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
		{
			this.PgSqlCS = Program.CfgJson.Database.NpgSqlConnectionString();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseNpgsql(this.PgSqlCS);

		}

	}
}
