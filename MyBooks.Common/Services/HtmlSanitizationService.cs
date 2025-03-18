using Ganss.Xss;
using System.Text.RegularExpressions;

namespace MyBooks.Common.Services
{
    public class HtmlSanitizationService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizationService()
        {
            _sanitizer = new HtmlSanitizer();

            // Allow only safe formatting tags
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");

            // Remove potentially dangerous JavaScript attributes
            _sanitizer.AllowedAttributes.Remove("onmouseover");
            _sanitizer.AllowedAttributes.Remove("onclick");
        }

        public string Sanitize(string input, bool allowEmail = false)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Step 1: Apply HTML sanitization
            string sanitized = _sanitizer.Sanitize(input).Trim();

            // Step 2: Remove unsafe characters (for general text fields)
            if (allowEmail)
            {
                //allow @ in an email
                sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_\-.\s@]", "");
            } else
            {
                sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_\-.\s]", "");
            }                

            return sanitized;
        }
    }
}
