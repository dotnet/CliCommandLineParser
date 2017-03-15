// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using static Microsoft.DotNet.Cli.CommandLine.Accept;

namespace Microsoft.DotNet.Cli.CommandLine
{
    public class Command : Option
    {
        public Command(
            string name,
            string help,
            Option[] options = null,
            ArgumentsRule arguments = null) :
            base(new[] { name }, help, arguments, options)
        {
        }

        public Command(
            string name,
            string help,
            params Command[] subcommands) :
            base(new[] { name }, help, options: subcommands)
        {
            var commandNames = subcommands.SelectMany(o => o.Aliases).ToArray();

            ArgumentsRule =
                ExactlyOneCommandRequired()
                    .WithSuggestionsFrom(commandNames)
                    .And(ArgumentsRule);
        }

        internal override bool IsCommand => true;
    }
}