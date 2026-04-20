using Ganss.Xss;

namespace Souq.Services.Implementations
{
    public static class InputSanitizer
    {
        private static readonly HtmlSanitizer _sanitizer = new ();

        public static string Sanitize(string? input)
        {
            if(string.IsNullOrWhiteSpace(input))
                return string.Empty;


            return _sanitizer.Sanitize(input);
        }

        public static string? SanitizeNullable(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            return _sanitizer.Sanitize(input);
        }
    }
}
