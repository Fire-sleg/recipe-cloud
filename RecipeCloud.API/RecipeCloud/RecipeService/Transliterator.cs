using System.Text.RegularExpressions;
using System.Text;

namespace RecipeService
{
    public class Transliterator
    {
        private static readonly Dictionary<char, string> _ukrainianToLatin = new Dictionary<char, string>
    {
        {'а', "a"},  {'б', "b"},  {'в', "v"},  {'г', "h"},  {'ґ', "g"},  {'д', "d"},
        {'е', "e"},  {'є', "ye"}, {'ж', "zh"}, {'з', "z"},  {'и', "y"},  {'і', "i"},
        {'ї', "yi"}, {'й', "i"},  {'к', "k"},  {'л', "l"},  {'м', "m"},  {'н', "n"},
        {'о', "o"},  {'п', "p"},  {'р', "r"},  {'с', "s"},  {'т', "t"},  {'у', "u"},
        {'ф', "f"},  {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "shch"},
        {'ь', ""},   {'ю', "yu"}, {'я', "ya"}, {'\'', ""}
    };

        public static string Transliterate(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            var words = input.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int w = 0; w < words.Length; w++)
            {
                var word = words[w].ToLower();
                for (int i = 0; i < word.Length; i++)
                {
                    char c = word[i];
                    if (_ukrainianToLatin.TryGetValue(c, out string latinChar))
                    {
                        result.Append(latinChar);
                    }
                    else if (char.IsDigit(c) || c == '.')
                    {
                        result.Append(c);
                    }
                    else if (char.IsLetter(c))
                    {
                        result.Append(c); 
                    }
                }

                if (w < words.Length - 1)
                {
                    result.Append("-");
                }
            }

            return result.ToString();
        }

        public static string TransliterateUrl(string input)
        {
            string transliterated = Transliterate(input);
            transliterated = Regex.Replace(transliterated, @"[^\w\s-]", "");
            transliterated = Regex.Replace(transliterated, @"\s+", "-");
            return transliterated.Trim('-', ' ').ToLower();
        }
    }
}
