using System;

namespace Game.ChartManagement
{
    public class StringHelper
    {
        public static string GetShortestFirstSegment(string input, string[] separators)
        {
            if (string.IsNullOrEmpty(input) || separators == null || separators.Length == 0) return input;

            string shortestSegment = null;

            foreach (var sep in separators)
            {
                if (string.IsNullOrEmpty(sep)) continue;

                if (!input.Contains(sep)) continue;

                var parts = input.Split(new[] { sep }, StringSplitOptions.None);

                var firstSegment = parts[0];

                if (shortestSegment == null || firstSegment.Length < shortestSegment.Length)
                    shortestSegment = firstSegment;
            }

            return shortestSegment ?? input;
        }
    }
}