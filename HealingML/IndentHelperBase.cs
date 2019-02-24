using System.Linq;

namespace HealingML
{
    public class IndentHelperBase
    {
        private string CachedTabs = "";
        protected int TabSize { get; set; }

        protected virtual string TabCharacter { get; } = "";

        public override string ToString()
        {
            return CachedTabs;
        }

        public static IndentHelperBase operator +(IndentHelperBase a, int b)
        {
            var c = a.Clone();
            c.TabSize += b;
            c.CachedTabs = c.Compile();
            return c;
        }

        public static IndentHelperBase operator -(IndentHelperBase a, int b)
        {
            var c = a.Clone();
            c.TabSize -= b;
            if (c.TabSize < 0) c.TabSize = 0;
            c.CachedTabs = c.Compile();
            return c;
        }

        protected virtual IndentHelperBase Clone()
        {
            return new IndentHelperBase
            {
                TabSize = TabSize
            };
        }

        public string Compile()
        {
            return string.Join(string.Empty, Enumerable.Repeat(TabCharacter, TabSize));
        }
    }
}
