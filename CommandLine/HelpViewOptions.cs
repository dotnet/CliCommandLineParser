
namespace Microsoft.DotNet.Cli.CommandLine
{
    public class HelpViewOptions
    {
        public static HelpViewOptions Default = new HelpViewOptions();

        public HelpViewOptions(string optionAliasSeparator = ", ")
        {
            OptionAliasSeparator = optionAliasSeparator;
        }

        public string OptionAliasSeparator {get;}
    }
}
