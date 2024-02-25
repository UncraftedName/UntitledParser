namespace System
{
    /* Polyfill for System.Index from .NET Core, to allow C# 8.0 [^n] notation */
    public readonly struct Index
    {
        private readonly int _val;
        public Index(int val, bool fromend = false)
        {
            _val = val;
            if (fromend) _val = ~_val;
        }
        public int GetOffset(int len)
        {
            if (_val < 0) return len + _val;
            return _val;
        }

        public static implicit operator Index(int i) => new Index(i);
    }
}
