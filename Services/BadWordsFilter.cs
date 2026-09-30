using System;
using System.Linq;

namespace AdoreFlowerShop.Services
{
    internal static class BadWordsFilter
    {
        private static readonly string[] BadWords = {
            "хуй", "хуя", "хуё", "пизд", "бля", "бляд", "ебат", "ебал", "ёба",
            "гандон", "гнида", "говно", "дерьмо", "долбоеб", "мудак", "пидор",
            "срака", "срать", "сука", "тварь", "ублюдок", "херня", "чмо",
            "fuck", "shit", "bitch", "asshole", "bastard", "damn", "crap",
            "cock", "dick", "pussy", "whore", "slut"
        };

        public static bool ContainsBadWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lower = text.ToLowerInvariant();
            return BadWords.Any(word => lower.Contains(word, StringComparison.OrdinalIgnoreCase));
        }
    }
}
