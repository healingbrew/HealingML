namespace HealingML
{
    public class SpaceIndentHelper : IndentHelperBase
    {
        protected override string TabCharacter { get; } = "  ";

        protected override IndentHelperBase Clone()
        {
            return new SpaceIndentHelper
            {
                TabSize = TabSize
            };
        }
    }
}
