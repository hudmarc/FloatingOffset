namespace FloatingOffset.Runtime
{
    public static class Functions
    {
        const string HEX = "X";
        /// <summary>
        /// Converts the given integer to hex for easy display.
        /// </summary>
        /// <param name="scene">The integer to convert.</param>
        /// <returns>The integer in Hex code.</returns>
        public static string ToHex(this int integer) => integer.ToString(HEX);

        /// <summary>
        /// Next power of 2, from https://stackoverflow.com/questions/31997707/rounding-value-to-nearest-power-of-two
        /// </summary>
        /// <param name="x">The number whose next power of 2 we want to find</param>
        /// <returns></returns>
        public static int GetNextPowerOfTwo(int x)
        {
            if (x < 0) { return 0; }
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
    }
}
