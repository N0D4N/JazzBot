using JazzBot.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

		private string EntityFrameworkConnectionString { get; }

		private string PgSqlCS { get; set; }

		public DatabaseContext()
		{
			this.EntityFrameworkConnectionString = Program.Cfgjson.Database.EntityFrameworkConnectionString;
			this.PgSqlCS = new NpgsqlConnectionStringBuilder
			{
				Host = Program.Cfgjson.Database.Hostname,
				Port = Program.Cfgjson.Database.Port,

				Username = Program.Cfgjson.Database.Username,
				Password = Program.Cfgjson.Database.Password,
				Database = Program.Cfgjson.Database.Database
			}.ConnectionString;
			Database.EnsureCreated();
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options) :base(options)
		{
			this.EntityFrameworkConnectionString = Program.Cfgjson.Database.EntityFrameworkConnectionString;
			this.PgSqlCS = new NpgsqlConnectionStringBuilder
			{
				Host = Program.Cfgjson.Database.Hostname,
				Port = Program.Cfgjson.Database.Port,

				Username = Program.Cfgjson.Database.Username,
				Password = Program.Cfgjson.Database.Password,
				Database = Program.Cfgjson.Database.Database
			}.ConnectionString;
			Database.EnsureCreated();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseNpgsql(this.PgSqlCS);
			
		}

	}
}
