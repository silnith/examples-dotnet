using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CDBPopulator
{
    public record LevelOfDetail(int Level)
    {
        public static Regex LevelPattern
        {
            get;
        } = new("^L(?<negated>C?)(?<level>\\d{2})$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        public static LevelOfDetail FromRegexMatch(string negated, string level)
        {
            return FromRegexMatch(negated, int.Parse(level, CultureInfo.InvariantCulture));
        }

        public static LevelOfDetail FromRegexMatch(string negated, int level)
        {
            if (string.IsNullOrEmpty(negated))
            {
                return new(level);
            }
            else
            {
                return new(-level);
            }
        }

        public static bool TryFromCode(string code, [NotNullWhen(true)] out LevelOfDetail? levelOfDetail)
        {
            Match match = LevelPattern.Match(code);
            if (match.Success)
            {
                var level = int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture);
                var negated = match.Groups["negated"].Value;
                if (string.IsNullOrEmpty(negated))
                {
                    levelOfDetail = new(level);
                }
                else
                {
                    levelOfDetail = new(-level);
                }
                return true;
            }
            else
            {
                levelOfDetail = null;
                return false;
            }
        }

        public string Code
        {
            get
            {
                if (Level < 0)
                {
                    return $"LC{Level:D2}";
                }
                else
                {
                    return $"L{Level:D2}";
                }
            }
        }
    }
}
