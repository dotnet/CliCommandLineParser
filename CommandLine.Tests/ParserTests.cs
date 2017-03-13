﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Cli.CommandLine.Accept;
using static Microsoft.DotNet.Cli.CommandLine.Create;

namespace Microsoft.DotNet.Cli.CommandLine.Tests
{
    public class ParserTests
    {
        private readonly ITestOutputHelper output;

        public ParserTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void An_option_without_a_long_form_can_be_checked_for_using_a_prefix()
        {
            var result = new Parser(
                    Option("--flag", ""))
                .Parse("--flag");

            result.HasOption("--flag").Should().BeTrue();
        }

        [Fact]
        public void An_option_without_a_long_form_can_be_checked_for_without_using_a_prefix()
        {
            var result = new Parser(
                    Option("--flag", ""))
                .Parse("--flag");

            result.HasOption("flag").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_short_form_an_option_with_an_alias_can_be_checked_for_by_its_short_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o");

            result.HasOption("o").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_long_form_an_option_with_an_alias_can_be_checked_for_by_its_short_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("--one");

            result.HasOption("o").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_short_form_an_option_with_an_alias_can_be_checked_for_by_its_long_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_long_form_an_option_with_an_alias_can_be_checked_for_by_its_long_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("--one");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void Two_options_are_parsed_correctly()
        {
            var result = new Parser(
                    Option("-o|--one", ""),
                    Option("-t|--two", ""))
                .Parse("-o -t");

            result.HasOption("o").Should().BeTrue();
            result.HasOption("one").Should().BeTrue();
            result.HasOption("t").Should().BeTrue();
            result.HasOption("two").Should().BeTrue();
        }

        [Fact]
        public void Parse_result_contains_arguments_to_options()
        {
            var result = new Parser(
                    Option("-o|--one", "", ExactlyOneArgument()),
                    Option("-t|--two", "", ExactlyOneArgument()))
                .Parse("-o args_for_one -t args_for_two");

            result["one"].Arguments.Single().Should().Be("args_for_one");
            result["two"].Arguments.Single().Should().Be("args_for_two");
        }

        [Fact]
        public void When_no_options_are_specified_then_an_error_is_returned()
        {
            Action create = () => new Parser();

            create.ShouldThrow<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("You must specify at least one option.");
        }

        [Fact]
        public void Two_options_cannot_have_conflicting_aliases()
        {
            Action create = () =>
                new Parser(
                    Option("-o|--one", ""),
                    Option("-t|--one", ""));

            create.ShouldThrow<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Alias 'one' is already in use.");
        }

        [Fact]
        public void A_double_dash_delimiter_specifies_that_no_further_command_line_args_will_be_treated_options()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o \"some stuff\" -- -x -y -z");

            result.HasOption("o")
                  .Should()
                  .BeTrue();

            result.AppliedOptions
                  .Should()
                  .HaveCount(1);
        }

        [Fact]
        public void The_portion_of_the_command_line_following_a_double_slash_is_accessible_as_UnparsedTokens()
        {
            var result = new Parser(
                    Option("-o", ""))
                .Parse("-o \"some stuff\" -- x y z");

            result.UnparsedTokens
                  .Should()
                  .ContainInOrder("x", "y", "z");
        }

        [Fact]
        public void Parser_options_can_supply_context_sensitive_matches()
        {
            var parser = new Parser(
                Option("--bread", "",
                       AnyOneOf("wheat", "sourdough", "rye")),
                Option("--cheese", "",
                       AnyOneOf("provolone", "cheddar", "cream cheese")));

            var result = parser.Parse("--bread ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("rye", "sourdough", "wheat");

            result = parser.Parse("--bread wheat --cheese ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("cheddar", "cream cheese", "provolone");
        }

        [Fact]
        public void Short_form_arguments_can_be_specified_using_equals_delimiter()
        {
            var parser = new Parser(Option("-x", "", ExactlyOneArgument()));

            var result = parser.Parse("-x=some-value");

            result.Errors.Should().BeEmpty();

            result["x"].Arguments.Should().ContainSingle(a => a == "some-value");
        }

        [Fact]
        public void Long_form_arguments_can_be_specified_using_equals_delimiter()
        {
            var parser = new Parser(Option("--hello", "", ExactlyOneArgument()));

            var result = parser.Parse("--hello=there");

            result.Errors.Should().BeEmpty();

            result["hello"].Arguments.Should().ContainSingle(a => a == "there");
        }

        [Fact]
        public void Short_form_arguments_can_be_specified_using_colon_delimiter()
        {
            var parser = new Parser(Option("-x", "", ExactlyOneArgument()));

            var result = parser.Parse("-x:some-value");

            result.Errors.Should().BeEmpty();

            result["x"].Arguments.Should().ContainSingle(a => a == "some-value");
        }

        [Fact]
        public void Long_form_arguments_can_be_specified_using_colon_delimiter()
        {
            var parser = new Parser(Option("--hello", "", ExactlyOneArgument()));

            var result = parser.Parse("--hello:there");

            result.Errors.Should().BeEmpty();

            result["hello"].Arguments.Should().ContainSingle(a => a == "there");
        }

        [Fact]
        public void Argument_short_forms_can_be_bundled()
        {
            var parser = new Parser(
                Option("-x", "", NoArguments()),
                Option("-y", "", NoArguments()),
                Option("-z", "", NoArguments()));

            var result = parser.Parse("-xyz");

            result.AppliedOptions
                  .Select(o => o.Name)
                  .Should()
                  .BeEquivalentTo("x", "y", "z");
        }

        [Fact]
        public void Parser_root_Options_can_be_specified_multiple_times_and_their_arguments_are_collated()
        {
            var parser = new Parser(
                Option("-a|--animals", "", ZeroOrMoreArguments()),
                Option("-v|--vegetables", "", ZeroOrMoreArguments()));

            var result = parser.Parse("-a cat -v carrot -a dog");

            result["animals"]
                .Arguments
                .Should()
                .BeEquivalentTo("cat", "dog");

            result["vegetables"]
                .Arguments
                .Should()
                .BeEquivalentTo("carrot");
        }
        
        [Fact]
        public void Command_Options_can_be_specified_multiple_times_and_their_arguments_are_collated()
        {
            var parser = new Parser(
                Command("the-command", "", Option("-a|--animals", "", ZeroOrMoreArguments()),
                        Option("-v|--vegetables", "", ZeroOrMoreArguments())));

            var result = parser.Parse("the-command -a cat -v carrot -a dog");

            result["the-command"]["animals"]
                .Arguments
                .Should()
                .BeEquivalentTo("cat", "dog");

            result["the-command"]["vegetables"]
                .Arguments
                .Should()
                .BeEquivalentTo("carrot");
        }

        [Fact]
        public void Options_need_not_be_respecified_in_order_to_add_arguments()
        {
            var parser = new Parser(
                Option("-a|--animals", "", ZeroOrMoreArguments()),
                Option("-v|--vegetables", "", ZeroOrMoreArguments()));

            var result = parser.Parse("-a cat dog -v carrot");

            result["animals"]
                .Arguments
                .Should()
                .BeEquivalentTo("cat", "dog");

            result["vegetables"]
                .Arguments
                .Should()
                .BeEquivalentTo("carrot");
        }

        [Fact]
        public void Option_with_multiple_nested_options_allowed_is_parsed_correctly()
        {
            var option = Command("outer", "",
                                 Option("--inner1", "", ExactlyOneArgument()),
                                 Option("--inner2", "", ExactlyOneArgument()));

            var parser = new Parser(option);

            var result = parser.Parse("outer --inner1 argument1 --inner2 argument2");

            output.WriteLine(result.Diagram());

            var applied = result.AppliedOptions.Single();

            applied
                .ValidateAll()
                .Should()
                .BeEmpty();

            applied.AppliedOptions
                   .Should()
                   .ContainSingle(o =>
                                      o.Name == "inner1" &&
                                      o.Arguments.Single() == "argument1");
            applied.AppliedOptions
                   .Should()
                   .ContainSingle(o =>
                                      o.Name == "inner2" &&
                                      o.Arguments.Single() == "argument2");
        }

        [Fact]
        public void Relative_order_of_arguments_and_options_does_not_matter()
        {
            var parser = new Parser(
                Command("move", "",
                        OneOrMoreArguments(),
                        Option("-x", "", ExactlyOneArgument())));

            // option before args
            var result1 = parser.Parse(
                "move -x the-option arg1 arg2");

            // option between two args
            var result2 = parser.Parse(
                "move arg1 -x the-option arg2");

            // option after args
            var result3 = parser.Parse(
                "move arg1 arg2 -x the-option");

            // arg order reversed
            var result4 = parser.Parse(
                "move arg2 arg1 -x the-option");

            // all should be equivalent
            result1.ShouldBeEquivalentTo(result2);
            result1.ShouldBeEquivalentTo(result3);
            result1.ShouldBeEquivalentTo(result4);
        }

        [Fact]
        public void An_outer_command_with_the_same_name_does_not_capture()
        {
            var command = Command("one", "",
                                  Command("two", "",
                                          Command("three", "")),
                                  Command("three", ""));

            var result = command.Parse("one two three");

            result.Diagram().Should().Be("[ one [ two [ three ] ] ]");
        }

        [Fact]
        public void An_inner_command_with_the_same_name_does_not_capture()
        {
            var command = Command("one", "",
                                  Command("two", "",
                                          Command("three", "")),
                                  Command("three", ""));

            var result = command.Parse("one three");

            result.Diagram().Should().Be("[ one [ three ] ]");
        }

        [Fact]
        public void When_nested_commands_all_acccept_arguments_then_the_nearest_captures_the_arguments()
        {
            var command = Command("outer", "",
                                  ZeroOrOneArgument(),
                                  Command("inner", "",
                                          ZeroOrOneArgument()));

            var result = command.Parse("outer arg1 inner arg2");

            result["outer"].Arguments.Should().BeEquivalentTo("arg1");

            result["outer"]["inner"].Arguments.Should().BeEquivalentTo("arg2");
        }

        [Fact]
        public void Nested_commands_with_colliding_names_cannot_both_be_applied()
        {
            var command = Command("outer", " ",
                                  ExactlyOneArgument(),
                                  Command("non-unique", "",
                                          ExactlyOneArgument()),
                                  Command("inner", "",
                                          ExactlyOneArgument(),
                                          Command("non-unique", "",
                                                  ExactlyOneArgument())));

            var result = command.Parse("outer arg1 inner arg2 non-unique arg3 ");

            output.WriteLine(result.Diagram());

            result.Diagram().Should().Be("[ outer [ inner [ non-unique <arg3> ] <arg2> ] <arg1> ]");
        }

        [Fact]
        public void When_child_option_will_not_accept_arg_then_parent_can()
        {
            var parser = new Parser(
                Command("the-command", "",
                        ZeroOrMoreArguments(),
                        Option("-x", "", NoArguments())));

            var result = parser.Parse("the-command -x two");

            var theCommand = result["the-command"];
            theCommand["x"].Arguments.Should().BeEmpty();
            theCommand.Arguments.Should().BeEquivalentTo("two");
        }

        [Fact]
        public void When_parent_option_will_not_accept_arg_then_child_can()
        {
            var parser = new Parser(
                Command("the-command", "",
                        NoArguments(),
                        Option("-x", "", ExactlyOneArgument())));

            var result = parser.Parse("the-command -x two");

            var theCommand = result["the-command"];

            theCommand["x"].Arguments.Should().BeEquivalentTo("two");
            theCommand.Arguments.Should().BeEmpty();
        }

        [Fact]
        public void When_the_same_option_is_defined_on_both_outer_and_inner_command_and_specified_at_the_end_then_it_attaches_to_the_inner_command()
        {
            var parser = new Parser(Command("outer", "", NoArguments(),
                                            Command("inner", "",
                                                    Option("-x", "")),
                                            Option("-x", "")));

            var result = parser.Parse("outer inner -x");

            result["outer"]
                .AppliedOptions
                .Should()
                .NotContain(o => o.Name == "x");
            result["outer"]["inner"]
                .AppliedOptions
                .Should()
                .ContainSingle(o => o.Name == "x");
        }

        [Fact]
        public void When_the_same_option_is_defined_on_both_outer_and_inner_command_and_specified_in_between_then_it_attaches_to_the_outer_command()
        {
            var parser = new Parser(Command("outer", "",
                                            Command("inner", "",
                                                    Option("-x", "")),
                                            Option("-x", "")));

            var result = parser.Parse("outer -x inner");

            result["outer"]["inner"]
                .AppliedOptions
                .Should()
                .BeEmpty();
            result["outer"]
                .AppliedOptions
                .Should()
                .ContainSingle(o => o.Name == "x");
        }

        [Fact]
        public void Subsequent_occurrences_of_tokens_matching_command_names_are_parsed_as_arguments()
        {
            var command = Command("the-command", "",
                                  Command("complete", "",
                                          ExactlyOneArgument(),
                                          Option("--position", "",
                                                 ExactlyOneArgument())));

            var result = command.Parse("the-command",
                                       "complete",
                                       "--position",
                                       "7",
                                       "the-command");

            var complete = result["the-command"]["complete"];

            output.WriteLine(result.Diagram());

            complete.Arguments.Should().BeEquivalentTo("the-command");
        }

        [Fact]
        public void A_root_command_can_be_omitted_from_the_parsed_args()
        {
            var command = Command("outer",
                                  "",
                                  Command("inner", "", Option("-x", "", ExactlyOneArgument())));

            var result1 = command.Parse("inner -x hello");
            var result2 = command.Parse("outer inner -x hello");

            result1.Diagram().Should().Be(result2.Diagram());
        }

        [Fact]
        public void A_root_command_can_match_a_full_path_to_an_executable()
        {
            var command = Command("outer",
                                  "",
                                  Command("inner", "", Option("-x", "", ExactlyOneArgument())));

            var result1 = command.Parse("inner -x hello");

            var exePath = Path.Combine("dev", "outer.exe");
            var result2 = command.Parse($"{exePath} inner -x hello");

            result1.Diagram().Should().Be(result2.Diagram());
        }

        [Fact]
        public void Absolute_unix_style_paths_are_lexed_correctly()
        {
            var command =
                @"rm ""/temp/the file.txt""";

            var parser = new Parser(
                Command("rm", "", ZeroOrMoreArguments()));

            var result = parser.Parse(command);

            result.AppliedOptions["rm"]
                  .Arguments
                  .Should()
                  .OnlyContain(a => a == @"/temp/the file.txt");
        }

        [Fact]
        public void Absolite_Windows_style_paths_are_lexed_correctly()
        {
            var command =
                @"rm ""c:\temp\the file.txt\""";

            var parser = new Parser(
                Command("rm", "", ZeroOrMoreArguments()));

            var result = parser.Parse(command);

            Console.WriteLine(result);

            result.AppliedOptions["rm"]
                  .Arguments
                  .Should()
                  .OnlyContain(a => a == @"c:\temp\the file.txt\");
        }

        [Fact]
        public void When_a_default_argument_value_is_not_provided_then_the_default_value_can_be_accessed_from_the_parse_result()
        {
            var option = Command("command", "",
                                 ExactlyOneArgument().With(defaultValue: () => "default"),
                                 Command("subcommand", "",
                                         ExactlyOneArgument()));

            var result = option.Parse("command subcommand subcommand-arg");

            output.WriteLine(result.Diagram());

            result["command"].Arguments.Should().BeEquivalentTo("default");
        }

        [Fact]
        public void Unmatched_options_are_not_split_into_smaller_tokens()
        {
            var command = Command("command",
                                  "",
                                  ExactlyOneArgument(),
                                  Option("-o", "", NoArguments()));

            var result = command.Parse("command argument -o /p:RandomThing=random",
                                       new char[0]);

            result["command"].Arguments.Should().BeEquivalentTo("argument");
            result.UnmatchedTokens.Should().BeEquivalentTo("/p:RandomThing=random");
        }

        [Fact]
        public void Argument_names_can_collide_with_option_names()
        {
            var command = Command("the-command", "",
                                  Option("--one", "",
                                         ExactlyOneArgument()));

            var result = command.Parse("the-command --one one");

            result["the-command"]["one"]
                .Arguments
                .Should()
                .BeEquivalentTo("one");
        }
    }
}