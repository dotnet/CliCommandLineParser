
namespace Microsoft.DotNet.Cli.CommandLine
{
    public class HelpViewOptions
    {
        public static HelpViewOptions Default = new HelpViewOptions();

        public HelpViewOptions(bool displayRawAliases = false)
        {
            DisplayRawAliases = displayRawAliases;
        }

        public bool DisplayRawAliases { get; }
    }
}
