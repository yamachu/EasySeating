using System;
using System.Linq;
using System.Text;

namespace EasySeating
{
    public static class Utils
    {
        public static int GetRequiredWidth(int number) => (int)Math.Floor(Math.Log10(number)) + 1;

        public static string GetNumberedSquare(int requiredSeats, int startIdx = 1, int numberWidth = -1)
        {
            if (numberWidth == -1)
            {
                numberWidth = GetRequiredWidth(requiredSeats + startIdx - 1);
            }

            if (GetRequiredWidth(requiredSeats + startIdx - 1) > numberWidth)
            {
                throw new ArgumentException($"numberWidth is too small");
            }

            var sb = new StringBuilder();

            var innerBar = String.Concat(Enumerable.Repeat("━", numberWidth));
            var innerBarUpper = String.Concat(innerBar, "┳");
            var innerBarBottom = String.Concat(innerBar, "┻");

            // upper
            sb.Append("┏");
            sb.Append(String.Concat(Enumerable.Repeat(innerBarUpper, requiredSeats - 1)));
            sb.Append(innerBar);
            sb.AppendLine("┓");

            // middle
            sb.Append("┃");
            Enumerable.Range(startIdx, requiredSeats).ToList()
            .ForEach(i => sb.Append($"{ToZenkaku(i.ToString().PadLeft(numberWidth))}┃"));
            sb.AppendLine();

            // bottom
            sb.Append("┗");
            sb.Append(String.Concat(Enumerable.Repeat(innerBarBottom, requiredSeats - 1)));
            sb.Append(innerBar);
            sb.AppendLine("┛");

            return sb.ToString();
        }

        private static string ToZenkaku(string paddedNumber)
        {
            return new string(paddedNumber.ToCharArray().Select(c =>
            {
                if (c == ' ') return (char)'　';
                return (char)(c - '0' + '０');
            }).ToArray());
        }
    }
}