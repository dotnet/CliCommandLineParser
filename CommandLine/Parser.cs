﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Cli.CommandLine
{
    public class Parser
    {
        private static readonly char[] defaultTokenSplitDelimiters = { '=', ':' };

        private readonly char[] tokenSplitDelimiters = null;

        public Parser(params Option[] options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!options.Any())
            {
                throw new ArgumentException("You must specify at least one option.");
            }

            DefinedOptions.AddRange(options);
            tokenSplitDelimiters = defaultTokenSplitDelimiters;
        }

        public Parser(char[] delimiters, params Option[] options) : this(options)
        {
            tokenSplitDelimiters = delimiters ?? defaultTokenSplitDelimiters;
        }

        public OptionSet DefinedOptions { get; } = new OptionSet();

        public ParseResult Parse(string[] args) => Parse(args, false);

        internal ParseResult Parse(
            IReadOnlyCollection<string> rawArgs,
            bool isProgressive)
        {
            var knownTokens = new HashSet<Token>(
                DefinedOptions
                    .FlattenBreadthFirst()
                    .SelectMany(
                        o =>
                            o.RawAliases.Select(
                                a =>
                                    new Token(a, o.IsCommand
                                                     ? TokenType.Command
                                                     : TokenType.Option))));

            var unparsedTokens = new Queue<Token>(
                NormalizeRootCommand(rawArgs)
                    .Lex(knownTokens, tokenSplitDelimiters));
            var rootAppliedOptions = new AppliedOptionSet();
            var allAppliedOptions = new List<AppliedOption>();
            var errors = new List<OptionError>();
            var unmatchedTokens = new List<string>();

            while (unparsedTokens.Any())
            {
                var token = unparsedTokens.Dequeue();

                if (token.Value == "--")
                {
                    // stop parsing further tokens
                    break;
                }

                if (token.Type != TokenType.Argument)
                {
                    var definedOption =
                        DefinedOptions.SingleOrDefault(o => o.HasAlias(token.Value));

                    if (definedOption != null)
                    {
                        var appliedOption = allAppliedOptions
                            .LastOrDefault(o => o.HasAlias(token.Value));

                        if (appliedOption == null)
                        {
                            appliedOption = new AppliedOption(definedOption, token.Value);
                            rootAppliedOptions.Add(appliedOption);
                        }

                        allAppliedOptions.Add(appliedOption);

                        continue;
                    }
                }

                var added = false;

                foreach (var appliedOption in Enumerable.Reverse(allAppliedOptions))
                {
                    var option = appliedOption.TryTakeToken(token);

                    if (option != null)
                    {
                        allAppliedOptions.Add(option);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    unmatchedTokens.Add(token.Value);
                }
            }

            errors.AddRange(
                unmatchedTokens.Select(UnrecognizedArg));

            return new ParseResult(
                rawArgs,
                rootAppliedOptions,
                isProgressive,
                unparsedTokens.Select(t => t.Value).ToArray(),
                unmatchedTokens,
                errors);
        }

        public IReadOnlyCollection<string> NormalizeRootCommand(IReadOnlyCollection<string> args)
        {
            var firstArg = args.First();

            if (DefinedOptions.Count != 1)
            {
                return args;
            }

            var commandName = DefinedOptions
                .SingleOrDefault(o => o.IsCommand)
                ?.Name;

            if (commandName == null ||
                firstArg.Equals(commandName, StringComparison.OrdinalIgnoreCase))
            {
                return args;
            }

            if (firstArg.Contains(Path.DirectorySeparatorChar) &&
                (firstArg.EndsWith(commandName, StringComparison.OrdinalIgnoreCase) ||
                 firstArg.EndsWith($"{commandName}.exe", StringComparison.OrdinalIgnoreCase)))
            {
                args = new[] { commandName }.Concat(args.Skip(1)).ToArray();
            }
            else
            {
                args = new[] { commandName }.Concat(args).ToArray();
            }

            return args;
        }

        private static OptionError UnrecognizedArg(string arg) =>
            new OptionError(
                $"Option '{arg}' is not recognized.", arg);
    }
}