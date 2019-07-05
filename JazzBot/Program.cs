using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using JazzBot.Attributes;
using JazzBot.Commands;
using JazzBot.Entities;
using JazzBot.Exceptions;
using JazzBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JazzBot
{
	#region unused
	/*public static class IListExtensions
	 {
		 private static RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
		 /// <summary>
		 /// Shuffles the element order of the specified list.
		 /// </summary>
		 public static void Shuffle<T>(this IList<T> list)
		 {
			 int n = list.Count;
			 while (n > 1)
			 {
				 byte[] box = new byte[sizeof(int)];
				 provider.GetBytes(box);
				 int bit = BitConverter.ToInt32(box, 0);
				 int k = Math.Abs(bit) % (int)n;
				 n--;s
				 T value = list[k];
				 list[k] = list[n];
				 list[n] = value;
			 }
		 }

		 public static void AddToListFromDB(this IList<string> list, string connectionstring)
		 {
			 SqlConnection conn = new SqlConnection(connectionstring);
			 SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT PathToFile FROM Playlist", conn);
			 DataSet PlaylistDataSet = new DataSet();
			 dataAdapter.Fill(PlaylistDataSet, "Playlist");
			 foreach (DataRow dataRow in PlaylistDataSet.Tables["Playlist"].Rows)
			 {
					 list.Add(dataRow[0].ToString());
			 }
		 }

		 public static int Cryptorandom(int minValue, int maxValue)
		 {
			 byte[] _uint32Buffer = new byte[4];
			 if (minValue > maxValue)
				 throw new ArgumentOutOfRangeException("minValue");
			 if (minValue == maxValue) return minValue;
			 Int64 diff = maxValue - minValue;
			 while (true)
			 {
				 provider.GetBytes(_uint32Buffer);
				 UInt32 rand = BitConverter.ToUInt32(_uint32Buffer, 0);

				 Int64 max = (1 + (Int64)UInt32.MaxValue);
				 Int64 remainder = max % diff;
				 if (rand < max - remainder)
				 {
					 return (Int32)(minValue + (rand % diff));
				 }
			 }
		 }

	 }*/
	#endregion
	public sealed class Program
    {
        public DiscordClient Client { get; set; }
        public CommandsNextExtension Commands { get; set; }
		public InteractivityExtension interactivity;
		public LavalinkExtension Lavalink { get; set; }
		public LavalinkNodeConnection LavalinkNode { get; set; }
		public static JazzBotConfig Cfgjson { get; set; }
		
		public Bot Bot { get; set; }

		private IServiceProvider Services { get; set; }

		public static void Main(string[] args)
        {
            // since we cannot make the entry method asynchronous,
            // let's pass the execution to asynchronous code
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();

        }

        public async Task RunBotAsync()
        {


            
            Cfgjson = new JazzBotConfig();

			var cfg = new DiscordConfiguration
            {
                Token = Cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            // then we want to instantiate our client
            Client = new DiscordClient(cfg);

            // next, let's hook some events, so we know
            // what's going on
            Client.Ready += this.Client_Ready;
            Client.GuildAvailable += this.Client_GuildAvailable;
            Client.ClientErrored += this.Client_ClientError;
            Client.SocketClosed += this.Socket_Closed;
            Client.SocketErrored += this.Socket_Error;
            Client.VoiceServerUpdated += this.Voice_Server_Update;
			Client.MessageReactionAdded += this.Client_ReactionAdded;
			Bot = new Entities.Bot(Cfgjson, this.Client);


			this.Services = new ServiceCollection()
				.AddSingleton(Cfgjson)
				.AddSingleton(this.Bot)
				.AddSingleton<MusicService>()
				.AddSingleton(new LavalinkService(Cfgjson, this.Client))
				.AddSingleton(this)
				.BuildServiceProvider(true);

			//Client.GuildDownloadCompleted += this.Client_Guild_Downdload_Complete;

			// up next, let's set up our commands
			var ccfg = new CommandsNextConfiguration
			{
				// let's use the string prefix defined in config.json
				StringPrefixes = Cfgjson.CommandPrefixes,

				EnableDefaultHelp = true,

				CaseSensitive = false,

				// enable responding in direct messages
				EnableDms = false,

				// enable mentioning the bot as a command prefix
				EnableMentionPrefix = true,

				Services = this.Services
            };

            // and hook them up
            this.Commands = Client.UseCommandsNext(ccfg);

			

            // let's hook some command events, so we know what's 
            // going on
            this.Commands.CommandExecuted += this.Commands_CommandExecuted;
            this.Commands.CommandErrored += this.Commands_CommandErrored;

			// up next, let's register our commands

			this.Commands.RegisterCommands<Ungrupped>();
            this.Commands.RegisterCommands<MusicCommands>();
			this.Commands.RegisterCommands<OwnerCommands>();
			this.Commands.RegisterCommands<TagCommands>();
			this.Commands.SetHelpFormatter<JazzBotHelpFormatter>();


			// and let's enable it
			var icfg = new InteractivityConfiguration
			{
				Timeout = TimeSpan.FromSeconds(60)
			};
			interactivity = Client.UseInteractivity(icfg);

			this.Lavalink = Client.UseLavalink();
			

			// finally, let's connect and log in
			await Client.ConnectAsync();

            Client.Ready += async e =>
            {
				var db = new DatabaseContext();
				
				var config = await db.Configs.SingleOrDefaultAsync(x => x.Id == e.Client.CurrentUser.Id);
				if(config == null)
				{
					config = new Data.Configs
					{
						Id = e.Client.CurrentUser.Id,
						Presence = "Music"
					};
					await db.Configs.AddAsync(config);
					if (await db.SaveChangesAsync() <= 0)
						throw new CustomJBException("Не удалось обновить БД", ExceptionType.DatabaseException);
					await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

				}
				else
				{
					if(e.Client.CurrentUser.Presence?.Activity.Name == null)					
						await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

				}
				db.Dispose();
			};

            //this.Client.MessageReactionAdded += this.ReactionAdded_OnOwnerMessage;

            // and this is to prevent premature quitting
            await Task.Delay(-1);
		} 



        private Task Socket_Closed(SocketCloseEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, e.Client.CurrentUser.Username, $"Socket was closed with message - '\"{e.CloseMessage}'\" and code - '\"{e.CloseCode}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Socket_Error(SocketErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, e.Client.CurrentUser.Username, $"Socket was errored with exception - '\"{e.Exception.Message}'\"", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Voice_Server_Update(VoiceServerUpdateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, e.Client.CurrentUser.Username, $"Voice server in {e.Guild.Name} changed to {e.Endpoint}", DateTime.Now);
            return Task.CompletedTask;
        }

		//private async Task Client_Guild_Downdload_Complete(GuildDownloadCompletedEventArgs e)
		//{

		//}


		private  Task Client_Ready(ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            e.Client.DebugLogger.LogMessage(LogLevel.Info, e.Client.CurrentUser.Username, "Бот готов к работе.", DateTime.Now);

            Console.Title = e.Client.CurrentUser.Username;


			//bot.errorChannel = await Client.GetChannelAsync(ErrorChannel);
			//bot.reportChannel = await Client.GetChannelAsync(ReportChannel);
			//bot.requestChannel = await Client.GetChannelAsync(RequestChannel);
			//await Task.Delay(1000);
			//bot.coverArtsChannel = await Client.GetChannelAsync(CoverArtsChannel);
			//await bot.FillCertainPlaylistAsync("Jazz");

			// since this method is not async, let's return
			// a completed task, so that no additional work
			// is done
			return Task.CompletedTask;
        }

        private  Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            // let's log the name of the guild that was just
            // sent to our client
            e.Client.DebugLogger.LogMessage(LogLevel.Info, e.Client.CurrentUser.Username, $"Доступен сервер: {e.Guild.Name}", DateTime.Now);
			return Task.CompletedTask;
        }

        private async Task Client_ClientError(ClientErrorEventArgs e)
        {
            // let's log the details of the error that just 
            // occured in our client
            e.Client.DebugLogger.LogMessage(LogLevel.Error, e.Client.CurrentUser.Username, $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);


			DiscordChannel chn = Bot.ErrorChannel;


            var embedError = new DiscordEmbedBuilder
            {
                Title = "Exception",
                Description = $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}",
                Color = DiscordColor.Rose
            };

            

            await chn.SendMessageAsync(embed: embedError).ConfigureAwait(false);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done

        }

		private async Task Client_ReactionAdded(MessageReactionAddEventArgs e)
		{
			if (e.User.Id != e.Client.CurrentApplication.Owner.Id || e.Message.Author.Id != e.Client.CurrentUser.Id)
				return;
			DiscordEmoji deleteEmoji = DiscordEmoji.FromName(e.Client, ":no_entry_sign:");
			if (e.Emoji.GetDiscordName() != deleteEmoji.GetDiscordName())
				return;
			await e.Message.DeleteAsync().ConfigureAwait(false);
		}

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, e.Context.Client.CurrentUser.Username, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

		private async Task Commands_CommandErrored(CommandErrorEventArgs e)
		{
			// let's log the error details
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, e.Context.Client.CurrentUser.Username, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);


			var ex = e.Exception;
			while (ex is AggregateException)
				ex = ex.InnerException;

			// let's check if the error is a result of lack
			// of required permissions
			if (ex is ChecksFailedException exep)
			{
				// yes, the user lacks required permissions, 
				// let them know

				string permissionsLacking = "";

				foreach (var failedchecks in exep.FailedChecks)
				{
					if (failedchecks is RequireBotPermissionsAttribute reqbotperm)
					{
						permissionsLacking += reqbotperm.Permissions.ToPermissionString();
						var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

						// let's wrap the response into an embed
						var embed = new DiscordEmbedBuilder
						{
							Title = "Access denied",
							Description = $"{emoji} Боту не хватает прав." + permissionsLacking,
							Color = new DiscordColor(0xFF0000) // red
															   // there are also some pre-defined colors available
															   // as static members of the DiscordColor struct
						}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
						.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
						await e.Context.RespondAsync("", embed: embed).ConfigureAwait(false);
						return;
					}
					if (failedchecks is RequireUserPermissionsAttribute requserperm)
					{
						permissionsLacking += requserperm.Permissions.ToPermissionString();
						var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

						// let's wrap the response into an embed
						var embed = new DiscordEmbedBuilder
						{
							Title = "Access denied",
							Description = $"{emoji} Вам не хватает прав." + permissionsLacking,
							Color = new DiscordColor(0xFF0000) // red
															   // there are also some pre-defined colors available
															   // as static members of the DiscordColor struct
						}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
						.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
						await e.Context.RespondAsync("", embed: embed).ConfigureAwait(false);
						return;
					}
					if(failedchecks is RequireOwnerAttribute reqowner)
					{
						var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
						var embed = new DiscordEmbedBuilder
						{
							Title = "Access denied",
							Description = $"{emoji} Команда доступна только владельцу",
							Color = new DiscordColor(0xFF0000) // red
															   // there are also some pre-defined colors available
															   // as static members of the DiscordColor struct
						}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
						.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
						await e.Context.RespondAsync("", embed: embed).ConfigureAwait(false);
						return;
					}
					if(failedchecks is OwnerOrPermissionAttribute ownerOrPermission)
					{
						permissionsLacking = "";
						permissionsLacking += ownerOrPermission.Permissions.ToPermissionString();
						var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
						var embed = new DiscordEmbedBuilder
						{
							Title = "Access denied",
							Description = $"{emoji} Вы не являетесь владельцем или вам не хватает прав:\n{permissionsLacking}\n",
							Color = new DiscordColor(0xFF0000) // red
															   // there are also some pre-defined colors available
															   // as static members of the DiscordColor struct
						}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
						.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
						await e.Context.RespondAsync("", embed: embed).ConfigureAwait(false);
						return;
					}
					if(failedchecks is CooldownAttribute cooldown)
					{
						await e.Context.RespondAsync(embed: new DiscordEmbedBuilder
						{
							Title = $"Вы пытаетесь вызывать комманду слишком часто, таймеры - " +
									$"не больше {cooldown.MaxUses} раз в {cooldown.Reset.TotalMinutes} минут",
							Color = new DiscordColor(0xFF0000)
						}).ConfigureAwait(false);
						return;
					}
				}
			}
			else if(ex is ArgumentException argEx)
			{
				var embed = new DiscordEmbedBuilder
				{
					Title = "Argument exception",
					Description = $"Произошла ошибка в работе команды {e.Command}, скорее всего, связанная с данными вводимыми пользователями, с сообщением: \n{Formatter.InlineCode(argEx.Message)}\n в \n{Formatter.InlineCode(argEx.Source)}",
					Color = new DiscordColor(0xFF0000) // red
													   // there are also some pre-defined colors available
													   // as static members of the DiscordColor struct
				}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
				.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
				await e.Context.RespondAsync(embed: embed).ConfigureAwait(false);
				return;
			}
			else if(ex is CustomJBException jbex)
			{
				switch(jbex.ExceptionType)
				{
					case ExceptionType.DatabaseException:
						var embedDB = new DiscordEmbedBuilder
						{
							Title = "Database exception",
							Description = $"Произошла ошибка связанная с работой БД с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"Команда {e.Command.Name}");
						await e.Context.RespondAsync(embed: embedDB).ConfigureAwait(false);
						await this.Bot.ErrorChannel.SendMessageAsync(embed: embedDB).ConfigureAwait(false);
						break;

					case ExceptionType.PlaylistException:
						var embedPL = new DiscordEmbedBuilder
						{
							Title = "Playlist exception",
							Description = $"Произошла ошибка с работой плейлистов (скорее всего плейлиста не существует) с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"По запросу {e.Context.User.Username}#{e.Context.User.Discriminator}", e.Context.User.AvatarUrl)
						.AddField("Заметка", $"Если вы считаете что бот неправ воспользуйтесь командой {Formatter.InlineCode("report")}");
						await e.Context.RespondAsync(embed: embedPL).ConfigureAwait(false);
						break;

					case ExceptionType.ForInnerPurposes:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: new DiscordEmbedBuilder
						{
							Title = "Inner Purposes Exception",
							Description = $"Произошла внутренняя ошибка с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"Команда {e.Command.Name}")).ConfigureAwait(false);
						break;

					case ExceptionType.Unknown:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: new DiscordEmbedBuilder
						{
							Title = "Неизвестная или дефолтная ошибка",
							Description = $"с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"Команда {e.Command.Name}")).ConfigureAwait(false);
						break;

					case ExceptionType.Default:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: new DiscordEmbedBuilder
						{
							Title = "Неизвестная или дефолтная ошибка",
							Description = $"с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"Команда {e.Command.Name}")).ConfigureAwait(false);
						break;
					default:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: new DiscordEmbedBuilder
						{
							Title = "Неизвестная или дефолтная ошибка",
							Description = $"с сообщением: \n{Formatter.InlineCode(jbex.Message)}\n в \n{Formatter.InlineCode(jbex.Source)}",
							Color = new DiscordColor(0xFF0000)
						}.WithFooter($"Команда {e.Command.Name}")).ConfigureAwait(false);
						break;
				}
			}
			else if(ex is CommandNotFoundException commandNotFoundException)
			{
				//Ignore
				return;
			}
			else
			{
				var embed = new DiscordEmbedBuilder
				{
					Title = "Произошла непредвиденная ошибка в работе команды",
					Description = $"Message: \n{Formatter.InlineCode(ex.Message)}\n в \n{Formatter.InlineCode(ex.Source)}",
					Color = new DiscordColor(0xFF0000)
				}.WithFooter($"Команда {e.Command.Name}");
				await e.Context.RespondAsync(embed: embed).ConfigureAwait(false);
				await this.Bot.ErrorChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
			}

		}

	}
}


/*	// this structure will hold data from config.json
	public  struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

		[JsonProperty("ConnectionString")]
		public string ConnectionString { get; private set; }

		[JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }

        [JsonProperty("ReportChannelID")]
        public ulong ReportChannelID { get; private set; }

        [JsonProperty("ErrorChannelID")]
        public ulong ErrorChannelID { get; private set; }

        [JsonProperty("RequestChannelID")]
        public ulong RequestChannelID { get; private set; }

		[JsonProperty("CoverArtsChannelID")]
		public ulong CoverArtsChannelID { get; private set; }

		[JsonProperty("PathToDirectoryWithPlaylists")]
		public string PathToDirectoryWithPlaylists { get; private set; }

		[JsonProperty("EFCS")]
		public string EFCS { get; private set; }

		[JsonProperty("UsualVoiceChannelID")]
		public ulong UsualVoiceChannelID { get; private set; }

	}
}
*/