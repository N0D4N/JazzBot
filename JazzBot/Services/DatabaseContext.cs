using JazzBot.Data;
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

		//public DbSet<Music> Music { get; set; }

		public DbSet<Tag> Tags { get; set; }

		public DbSet<Configs> Configs { get; set; }

		private string EntityFrameworkConnectionString { get; }

		public DatabaseContext()
		{
			this.EntityFrameworkConnectionString = Program.Cfgjson.Database.EntityFrameworkConnectionString;
			Database.EnsureCreated();
		}

		public DatabaseContext(DbContextOptions<DatabaseContext> options) :base(options)
		{
			this.EntityFrameworkConnectionString = Program.Cfgjson.Database.EntityFrameworkConnectionString;
			Database.EnsureCreated();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlServer(this.EntityFrameworkConnectionString);
			
		}

		//protected override void OnModelCreating(ModelBuilder modelBuilder)
		//{
		//	//modelBuilder.Entity<DGuild>().Property(g => g.IdOfCurrentSong).HasDefaultValue(1);
		//	//modelBuilder.Entity<DGuild>().Property(g => g.IsLoggingEnabled).HasDefaultValue(false);
		//	//modelBuilder.Entity<DGuild>().Property(g => g.PlaylistName).HasDefaultValue("Jazz");
		//	//modelBuilder.Entity<DGuild>().Property(g => g.Seed).HasDefaultValue(150);
		//	////delBuilder.Entity<DGuild>().Property(g=> g.)
		//}
	}
}
