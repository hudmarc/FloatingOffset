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
    }
}
