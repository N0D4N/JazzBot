using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using JazzBot.Attributes;
using JazzBot.Utilities;

namespace JazzBot
{
	/// <summary>
	/// Custom help formatter for CommandsNext
	/// </summary>
	public class JazzBotHelpFormatter : BaseHelpFormatter
	{
		public DiscordEmbedBuilder EmbedBuilder { get; }
		private Command Command { get; set; }

		/// <summary>
		/// Creates a new help formatter.
		/// </summary>
		/// <param name="ctx">Context in which this formatter is being invoked.</param>
		public JazzBotHelpFormatter(CommandContext ctx)
			: base(ctx)
		{
			this.EmbedBuilder = new DiscordEmbedBuilder()
				.WithTitle("Help")
				.WithColor(Helpers.RandomColor());
		}

		/// <summary>
		/// Sets the command this help message will be for.
		/// </summary>
		/// <param name="command">Command for which the help message is being produced.</param>
		/// <returns>This help formatter.</returns>
		public override BaseHelpFormatter WithCommand(Command command)
		{
			this.Command = command;

			this.EmbedBuilder.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "Описание отсутствует."}");

			if (command is CommandGroup cGroup && cGroup.IsExecutableWithoutSubcommands)
				this.EmbedBuilder.WithDescription($"{this.EmbedBuilder.Description}\n\nЭта группа может быть выполнена как отдельная команда.");

			if (command.Aliases?.Any() == true)
				this.EmbedBuilder.AddField("Синонимы-сокращения", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)), false);

			if (command.Overloads?.Any() == true)
			{
				var sb = new StringBuilder();

				foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
				{
					sb.Append('`').Append(command.QualifiedName);

					foreach (var arg in ovl.Arguments)
						sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

					sb.Append("`\n");

					foreach (var arg in ovl.Arguments)
						sb.Append('`').Append(arg.Name).Append(" (").Append(this.CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "Описание отсутствует.").Append('\n');

					sb.Append('\n');
				}

				this.EmbedBuilder.AddField("Аргументы", sb.ToString().Trim(), false);
			}

			if (command.ExecutionChecks.Any())
			{
				var exChecksSb = new StringBuilder();

				if (command.ExecutionChecks.Any(x => x is OwnerOrPermissionAttribute))
				{
					var oOpa = command.ExecutionChecks.Single(x => x is OwnerOrPermissionAttribute) as OwnerOrPermissionAttribute;
					exChecksSb.AppendLine($"Чтобы использовать эту команду вы должны быть владельцем бота или иметь права {Formatter.InlineCode(oOpa.Permissions.ToPermissionString())}.");
				}

				if (command.ExecutionChecks.Any(x => x is RequireOwnerAttribute))
				{
					exChecksSb.AppendLine("Чтобы использовать эту команду вы должны быть владельцем бота.");
				}

				if (command.ExecutionChecks.Any(x => x is CooldownAttribute))
				{
					var cooldown = command.ExecutionChecks.Single(x => x is CooldownAttribute) as CooldownAttribute;
					exChecksSb.AppendLine($"Эта команда может быть использована {Formatter.InlineCode(cooldown.MaxUses.ToString())} раз/а в течении {Formatter.InlineCode(cooldown.Reset.TotalSeconds.ToString())} секунд в {Formatter.InlineCode(cooldown.BucketType.ToString())}.");
				}

				if (command.ExecutionChecks.Any(x => x is RequirePermissionsAttribute))
				{
					var perm = command.ExecutionChecks.Single(x => x is RequirePermissionsAttribute) as RequirePermissionsAttribute;
					exChecksSb.AppendLine($"Требует у бота и участника прав {Formatter.Underline(perm.Permissions.ToPermissionString())}");
				}

				if(command.ExecutionChecks.Any(x=> x is RequireBotPermissionsAttribute))
				{
					var perm = command.ExecutionChecks.Single(x => x is RequireBotPermissionsAttribute) as RequireBotPermissionsAttribute;
					exChecksSb.AppendLine($"Требует у бота наличие прав {Formatter.Underline(perm.Permissions.ToPermissionString())}");
				}

				this.EmbedBuilder.AddField("Предусловия выполнения команды", exChecksSb.ToString().Trim(), false);
			}

			return this;
		}

		/// <summary>
		/// Sets the subcommands for this command, if applicable. This method will be called with filtered data.
		/// </summary>
		/// <param name="subcommands">Subcommands for this command group.</param>
		/// <returns>This help formatter.</returns>
		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
		{
			this.EmbedBuilder.AddField(this.Command != null ? "Подкоманды" : "Команды", string.Join(", ", subcommands.Select(x => Formatter.InlineCode(x.Name))), false);

			return this;
		}

		/// <summary>
		/// Construct the help message.
		/// </summary>
		/// <returns>Data for the help message.</returns>
		public override CommandHelpMessage Build()
		{
			if (this.Command == null)
				this.EmbedBuilder.WithDescription("Список всех команд высшего уровня. Выберите чтобы увидеть больше информации.");

			return new CommandHelpMessage(embed: this.EmbedBuilder.Build());
		}
	}
}