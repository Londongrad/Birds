using System.Text.RegularExpressions;

namespace Birds.App
{
    public partial class App
    {
        // Compiled, culture-invariant, matches ${NAME}
        private static readonly Regex EnvPattern =
            new(@"\$\{(\w+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Replaces <c>${NAME}</c> placeholders in the input string with environment variable values.
        /// If a variable is not found, the original placeholder is left intact.
        /// </summary>
        /// <param name="raw">The input string that may contain <c>${...}</c> placeholders.</param>
        /// <returns>The input string with placeholders replaced where possible.</returns>
        /// <remarks>
        /// This method does not throw for missing variables and performs a single pass replacement.
        /// </remarks>
        internal static string ReplaceEnvPlaceholders(string raw)
        {
            if (raw is null) return string.Empty;

            return EnvPattern.Replace(raw, m =>
            {
                var key = m.Groups[1].Value;
                return Environment.GetEnvironmentVariable(key) ?? m.Value;
            });
        }
    }
}
