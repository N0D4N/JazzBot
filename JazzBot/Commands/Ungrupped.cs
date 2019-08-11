using System;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using JazzBot.Attributes;
using JazzBot.Data;
using JazzBot.Utilities;

namespace JazzBot.Commands
{
	public sealed class Ungrupped : BaseCommandModule
	{
		private Bot Bot { get; }

		public Ungrupped(Bot bot)
		{
			this.Bot = bot;
		}

		[Command("UserInfo")]
		[Description("Информация об этом участнике сервера")]
		[Aliases("memberinfo")]
		public async Task UserInfo(CommandContext context, [Description("Пользователь информацию о котором вы хотите увидеть")] DiscordMember member = null)
		{
			if (member == null)
				member = context.Member;
			await context.RespondAsync(embed: await this.MemberInfo(member, context).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Command("UserInfo")]
		public async Task UserInfo(CommandContext context, [RemainingText, Description("Имя пользователя")] string memberName)
		{
			if (string.IsNullOrWhiteSpace(memberName))
				throw new ArgumentException("Имя пользователя не может быть пустым или состоять из пробелов", nameof(memberName));
			var member = context.Guild.Members.Values.FirstOrDefault(x => x.DisplayName == memberName || x.Username == memberName);
			if (member != null)
				await context.RespondAsync(embed: await this.MemberInfo(member, context).ConfigureAwait(false)).ConfigureAwait(false);
			else
				throw new ArgumentException($"Пользователя с никнеймом или юзернеймом {memberName} не найдено", nameof(memberName));
		}

		[Command("Roll")]
		[Description("Выбирает случайное число между данными двумя целыми числами")]
		[Aliases("random", "r")]
		public async Task Roll(CommandContext context, [Description("Нижняя граница")] int? min = null, [Description("Верхняя граница")] int? max = null)
		{
			// No arguments were provided.
			if (min == null) 
			{
				min = 1;
				max = 10;
			}
			// Only one argument were provided, so it will be maximum and minimum will be default "1".
			else if (max == null) 
			{
				max = min.Value;
				min = 1;
			}
			int result = Helpers.Cryptorandom(min.Value, max.Value);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Случайное число в границах [{min};{max}] = {result}")).ConfigureAwait(false);
		}			

		

		[Command("Choice")]
		[Description("Выбирает случайный вариант из представленных")]
		[Aliases("Pick")]
		public async Task Choice(CommandContext context, [Description("Варианты среди которых нужно сделать выбор")] params string[] choices)
		{
			if (choices?.Any() != true)
				throw new ArgumentException("Вы должны предоставить хотя бы 1 вариант для выбора", nameof(choices));
			string x = choices[Helpers.Cryptorandom(0, choices.Length)].Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
			await context.RespondAsync(x).ConfigureAwait(false);
		}

		[Command("Ban")]
		[Description("Банит пользователя по его ID, пользователю не обязательно находиться на этом сервере")]
		[RequirePermissions(Permissions.BanMembers)]
		public async Task Hackban(CommandContext context, [Description("ID пользователя которого нужно забанить")]ulong id, [RemainingText, Description("Причина бана")] string reason = "")
		{
			if (context.Member.Id == id)
				throw new ArgumentException("Вы не можете забанить себя");
			string userString = $"{context.User.Username}#{context.User.Discriminator} ({context.User.Id})";
			string reasonString = string.IsNullOrWhiteSpace(reason) ? " отсутствует" : $": {reason}";

			// Member is still in the guild.
			if (context.Guild.Members.TryGetValue(id, out var member)) 
			{
				if (this.MemberRolePositionAndOwnerChecker(context.Member, member))
				{
					try
					{
						await context.Guild.BanMemberAsync(id, 7, $"Ответственный модератор {userString}. Причина{reasonString}").ConfigureAwait(false);
						await context.RespondAsync("Пользователь успешно забанен.").ConfigureAwait(false);
					}
					catch
					{
						await context.RespondAsync($"Не удалось забанить пользователя с ID = {id}, проверьте правильность ID, или положение высшей роли бота в отношении человека которого вы хотите забанить").ConfigureAwait(false);
					}
				}
				else
					await context.RespondAsync("Вы не можете забанить пользователя с ролью выше вашей и/или владельца сервера");
			}
			// Member is not in the guild.
			else
			{
				try
				{
					await context.Guild.BanMemberAsync(id, 7, $"Ответственный модератор {userString}. Причина{reasonString}").ConfigureAwait(false);
					await context.RespondAsync("Пользователь успешно забанен.").ConfigureAwait(false);
				}
				catch
				{
					await context.RespondAsync($"Не удалось забанить пользователя с ID = {id}, проверьте правильность ID, или положение высшей роли бота в отношении человека которого вы хотите забанить").ConfigureAwait(false);
				}
			}
		}

		[Command("Invite")]
		[Description("Ссылка с приглашением бота на ваш сервер")]
		[Aliases("inv")]
		public async Task Invite(CommandContext context)
		{

			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription(Formatter.MaskedUrl("Ссылка-приглашение бота на ваш сервер", 
				new Uri("https://discordapp.com/api/oauth2/authorize?client_id=" + context.Client.CurrentUser.Id + "&permissions=120966212&scope=bot"))))
				.ConfigureAwait(false);
		}

		[Command("say")]
		[Description("Отправляет \"embed\" в заданный канал с заданным текстом")]
		[OwnerOrPermission(Permissions.ManageGuild)]
		public async Task Say(CommandContext context, 
			[Description("Канал в который нужно отправить сообщение")]DiscordChannel channelToSayIn,
			[RemainingText,Description("Текст который нужно отправить")] string messageContent)
		{
			if (string.IsNullOrWhiteSpace(messageContent))
				throw new ArgumentException("Содержимое сообщения не должно быть пустым", nameof(messageContent));
			var embed = new DiscordEmbedBuilder
			{
				Description = messageContent.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere"),
				Timestamp = DateTime.Now,
				Color = Helpers.ExtendedColor(context.Member, context.Guild.CurrentMember),
			}.WithAuthor($"{context.Member.Username}#{context.Member.Discriminator}", iconUrl: context.Member.AvatarUrl);
			try
			{
				await channelToSayIn.SendMessageAsync(embed: embed).ConfigureAwait(false);
			}
			catch { }
		}

		[Command("ErrorReport")]
		[Description("Если произошла ошибка и вы хотите сообщить о ней - напишите подробное(или не очень) описание ошибки и репорт будет передан соответствующим лицам")]
		[Aliases("report")]
		[Cooldown(1,60,CooldownBucketType.User)]
		public async Task ErrorReport(CommandContext context, [RemainingText, Description("Описание ошибки")]string reportMessage)
		{
			if (string.IsNullOrWhiteSpace(reportMessage))
				throw new ArgumentException("Сообщение об ошибке не может быть пустым или содержать только пробелы", nameof(reportMessage));
			var interactivity = context.Client.GetInteractivity();

			var errmsg = await this.Bot.ReportChannel.SendMessageAsync(embed: new DiscordEmbedBuilder
			{
				Title = "Пришел юзер-репорт",
				Description = reportMessage.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere"),
				Timestamp = DateTimeOffset.Now
			}.WithAuthor($"{context.User.Username}#{context.User.Discriminator} ({context.User.Id})", iconUrl: context.User.AvatarUrl ?? context.User.DefaultAvatarUrl)
			.AddField("Id сервера", $"{context.Guild.Id}", false)
			.AddField("Id канала", $"{context.Channel.Id}", false)
			.AddField("Стоит ли сообщать о фиксе", "Да", false)).ConfigureAwait(false);

			var cancelReportEmoji = DiscordEmoji.FromName(context.Client, ":no_entry_sign:");

			var chsmsg = await context.RespondAsync(embed: new DiscordEmbedBuilder
			{
				Description = $"Спасибо за репорт как только ошибка будет исправлена и вам придет об этом сообщение, если вы не хотите получать сообщение с отчетом о фиксе - поставьте емодзи {cancelReportEmoji} (:no_entry_sign) под это сообщение",
				Timestamp = DateTimeOffset.Now,
			}.WithFooter($"По запросу {context.User.Username}#{context.User.Discriminator}", context.User.AvatarUrl)).ConfigureAwait(false);
			await chsmsg.CreateReactionAsync(cancelReportEmoji).ConfigureAwait(false);
			var intResult = await interactivity.WaitForReactionAsync(x => x.Emoji.GetDiscordName() == cancelReportEmoji.GetDiscordName(), chsmsg, context.User, TimeSpan.FromSeconds(25));
			if (intResult.TimedOut)
				return;
			else
			{
				await errmsg.ModifyAsync(embed: new DiscordEmbedBuilder(errmsg.Embeds[0])
					.AddField("Стоит ли сообщать об ошибке", "Уже нет", false).Build()).ConfigureAwait(false);

			}
		}

		[Command("Update")]
		[Description("Дает ссылку на информацию о последнем обновлении")]
		public async Task Update(CommandContext context)
		{
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription(Formatter.MaskedUrl("Ссылка на последнее обновление",new Uri(this.Bot.Config.Miscellaneous.UpdateLink)))).ConfigureAwait(false);
		}

		private Task<DiscordEmbed> MemberInfo(DiscordMember member, CommandContext context)
		{
			var roles = new StringBuilder();
			if (member.Roles.Count() > 0)
			{
				var roleslist = member.Roles.OrderByDescending(x => x.Position).Select(x => x.Name).ToList();
				roleslist.Add("everyone");
				roles.AppendJoin(',', roleslist.Select(x => $"{Formatter.InlineCode($"@{x}")}"));
			}
			else
				roles.AppendLine(Formatter.InlineCode("@everyone"));
			return Task.FromResult(EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Информация об участнике")
			.AddField("Пользователь", $"{ member.Username}#{member.Discriminator}", true)
			.AddField("ID", member.Id.ToString(), true)
			.AddField("Отображаемое имя", member.DisplayName, true)
			.AddField("Аккаунт создан", $"{(DateTimeOffset.Now - member.CreationTimestamp).Days} дней назад ({member.CreationTimestamp.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})", false)
			.AddField("Участник присоединился", $"{(DateTimeOffset.Now - member.JoinedAt).Days} дней назад ({member.JoinedAt.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})", false)
			.AddField("Роли", roles.ToString(), false)
			.WithThumbnailUrl(member.AvatarUrl).Build());
		}

		private bool MemberRolePositionAndOwnerChecker(DiscordMember memberInvoked, DiscordMember memberToBan)
		{
			return ((memberToBan?.Roles?.Any() == true && memberInvoked?.Roles?.Any() == true
						// Moderator have higher role than member he tries to ban.
						&& memberInvoked.Roles.OrderByDescending(x => x.Position).First().Position > memberToBan.Roles.OrderByDescending(x => x.Position).First().Position) 
							|| memberToBan?.Roles?.Any() != true)
								&& !memberToBan.IsOwner;
		}
	}
}