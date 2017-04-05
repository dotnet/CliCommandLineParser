using System;
using System.Linq;
using System.Text;
using static System.Environment;
using static Microsoft.DotNet.Cli.CommandLine.DefaultHelpViewText;

namespace Microsoft.DotNet.Cli.CommandLine
{
    public static class HelpViewExtensions
    {
        private static int columnGutterWidth = 3;

        public static string HelpView(this Command command, HelpViewOptions helpViewOptions = null)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (helpViewOptions == null)
            {
                helpViewOptions = HelpViewOptions.Default;
            }

            var helpView = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(command.HelpText))
            {
                helpView.AppendLine(command.HelpText);
                helpView.AppendLine();
            }

            WriteSynopsis(command, helpView);

            WriteArgumentsSection(command, helpView);

            WriteOptionsSection(command, helpView, helpViewOptions);

            WriteSubcommandsSection(command, helpView, helpViewOptions);

            WriteAdditionalArgumentsSection(command, helpView);

            return helpView.ToString();
        }

        private static void WriteAdditionalArgumentsSection(
            Command command,
            StringBuilder helpView)
        {
            if (command.TreatUnmatchedTokensAsErrors)
            {
                return;
            }

            helpView.Append(AdditionalArgumentsSection);
        }

        private static void WriteArgumentsSection(
            Command command,
            StringBuilder helpView)
        {
            var argName = command.ArgumentsRule.Name;
            var argDescription = command.ArgumentsRule.Description;

            var shouldWriteCommandArguments =
                !string.IsNullOrWhiteSpace(argName) &&
                !string.IsNullOrWhiteSpace(argDescription);

            var parentCommand = command.Parent as Command;

            var parentArgName = parentCommand?.ArgumentsRule?.Name;
            var parentArgDescription = parentCommand?.ArgumentsRule?.Description;

            var shouldWriteParentCommandArguments =
                !string.IsNullOrWhiteSpace(parentArgName) &&
                !string.IsNullOrWhiteSpace(parentArgDescription);

            if (shouldWriteCommandArguments ||
                shouldWriteParentCommandArguments)
            {
                helpView.AppendLine();
                helpView.AppendLine(ArgumentsSection.Title);
            }
            else
            {
                return;
            }

            var indent = "  ";
            var argLeftColumnText = $"{indent}<{argName}>";
            var parentArgLeftColumnText = $"{indent}<{parentArgName}>";
            var leftColumnWidth =
                Math.Max(argLeftColumnText.Length,
                         parentArgLeftColumnText.Length) +
                columnGutterWidth;

            if (shouldWriteParentCommandArguments)
            {
                WriteColumnizedSummary(
                    parentArgLeftColumnText,
                    parentArgDescription,
                    leftColumnWidth,
                    helpView);
            }

            if (shouldWriteCommandArguments)
            {
                WriteColumnizedSummary(
                    argLeftColumnText,
                    argDescription,
                    leftColumnWidth,
                    helpView);
            }
        }

        private static void WriteOptionsSection(
            Command command,
            StringBuilder helpView,
            HelpViewOptions helpViewOptions)
        {
            var options = command
                .DefinedOptions
                .Where(o => !o.IsCommand)
                .Where(o => !o.IsHidden())
                .ToArray();

            if (!options.Any())
            {
                return;
            }

            helpView.AppendLine();
            helpView.AppendLine(OptionsSection.Title);

            WriteOptionsList(options, helpView, helpViewOptions);
        }

        private static void WriteSubcommandsSection(
            Command command,
            StringBuilder helpView,
            HelpViewOptions helpViewOptions)
        {
            var subcommands = command
                .DefinedOptions
                .Where(o => !o.IsHidden())
                .OfType<Command>()
                .ToArray();

            if (!subcommands.Any())
            {
                return;
            }

            helpView.AppendLine();
            helpView.AppendLine(CommandsSection.Title);

            WriteOptionsList(subcommands, helpView, helpViewOptions);
        }

        private static void WriteOptionsList(
            Option[] options,
            StringBuilder helpView,
            HelpViewOptions helpViewOptions)
        {
            var leftColumnTextFor = options
                .ToDictionary(o => o, o => LeftColumnText(o, helpViewOptions));

            var leftColumnWidth = leftColumnTextFor
                                      .Values
                                      .Select(s => s.Length)
                                      .OrderBy(length => length)
                                      .Last() + columnGutterWidth;

            foreach (var option in options)
            {
                WriteColumnizedSummary(leftColumnTextFor[option],
                                       option.HelpText,
                                       leftColumnWidth,
                                       helpView);
            }
        }

        private static string LeftColumnText(Option option, HelpViewOptions helpViewOptions)
        {
            string leftColumnText;

            if (!helpViewOptions.DisplayRawAliases)
            {
                leftColumnText = "  " +
                                    string.Join(", ",
                                                option.Aliases
                                                      .OrderBy(a => a.Length)
                                                      .Select(a =>
                                                      {
                                                          if (option.IsCommand)
                                                          {
                                                              return a;
                                                          }
                                                          else
                                                          {
                                                              return a.Length == 1
                                                                         ? $"-{a}"
                                                                         : $"--{a}";
                                                          }
                                                      }));
            }
            else
            {
                leftColumnText = "  " +
                                    string.Join(", ",
                                                option.RawAliases
                                                      .OrderBy(a => a.Length)
                                                      .Select(a =>
                                                      {
                                                          if (option.IsCommand)
                                                          {
                                                              return a.TrimStart(new[] { '-' });
                                                          }
                                                          else
                                                          {
                                                              return a;
                                                          }
                                                      }));
            }


            var argumentName = option.ArgumentsRule.Name;

            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                leftColumnText += $" <{argumentName}>";
            }

            return leftColumnText;
        }

        private static void WriteColumnizedSummary(
            string leftColumnText,
            string rightColumnText,
            int width,
            StringBuilder helpView)
        {
            helpView.Append(leftColumnText);

            if (leftColumnText.Length <= width - 2)
            {
                helpView.Append(new string(' ', width - leftColumnText.Length));
            }
            else
            {
                helpView.AppendLine();
                helpView.Append(new string(' ', width));
            }

            var descriptionWithLineWraps = string.Join(
                NewLine + new string(' ', width),
                rightColumnText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));

            helpView.AppendLine(descriptionWithLineWraps);
        }

        private static void WriteSynopsis(
            Command command,
            StringBuilder helpView)
        {
            helpView.Append(Synopsis.Title);

            foreach (var subcommand in command
                .RecurseWhileNotNull(c => c.Parent as Command)
                .Reverse())
            {
                helpView.Append($" {subcommand.Name}");

                var argsName = subcommand.ArgumentsRule.Name;
                if (subcommand != command &&
                    !string.IsNullOrWhiteSpace(argsName))
                {
                    helpView.Append($" <{argsName}>");
                }
            }

            if (command.DefinedOptions
                       .Any(o => !o.IsCommand &&
                                 !o.IsHidden()))
            {
                helpView.Append(Synopsis.Options);
            }

            var argumentsName = command.ArgumentsRule.Name;
            if (!string.IsNullOrWhiteSpace(argumentsName))
            {
                helpView.Append($" <{argumentsName}>");
            }

            if (command.DefinedOptions.OfType<Command>().Any())
            {
                helpView.Append(Synopsis.Command);
            }

            if (!command.TreatUnmatchedTokensAsErrors)
            {
                helpView.Append(Synopsis.AdditionalArguments);
            }

            helpView.AppendLine();
        }
    }
}