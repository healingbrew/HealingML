namespace HealingML
{
    public class TabIndentHelper : IndentHelperBase
    {
        protected override string TabCharacter { get; } = "\t";

        protected override IndentHelperBase Clone()
        {
            return new TabIndentHelper
            {
                TabSize = TabSize
            };
        }
    }
}
