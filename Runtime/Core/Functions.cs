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
        /// Next power of 2 using bit twiddling
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int GetNextPowerOfTwo(int n)
        {
            // Handle edge cases (0 or negative numbers)
            if (n <= 0) return 1;

            // Step 1: Subtract 1
            n--;

            // Step 2: "Smear" the highest set bit downwards
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            // Step 3: Add 1 to get the actual power of two
            return n + 1;
        }
    }
}
