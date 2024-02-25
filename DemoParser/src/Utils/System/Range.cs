namespace System
{
    /* Polyfill for System.Index from .NET Core, to allow C# 8.0 [start..end] notation */
    public readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }
        public Range(Index start, Index end) { Start = start; End = end; }

        public static Range StartAt(Index start) => new Range(start, new Index(-1));
        public static Range EndAt(Index end) => new Range(new Index(0), end);
        public static Range All => new Range(new Index(0), new Index(-1));
    }
}