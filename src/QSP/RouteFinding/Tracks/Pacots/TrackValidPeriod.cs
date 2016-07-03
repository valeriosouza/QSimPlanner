﻿using static QSP.LibraryExtension.Strings;

namespace QSP.RouteFinding.Tracks.Pacots
{
    public static class TrackValidPeriod
    {
        private static readonly char[] DelimiterWords =
               { ' ', '\n', '\r', '\t' };

        /// <summary>
        /// Use the given string to to find StartTime and EndTime. 
        /// The given string must contain 2 substrings of form "11061200UTC". 
        /// Otherwise both StartTime and EndTime will be empty strings.
        /// </summary>
        public static ValidPeriod GetValidPeriod(string item)
        {
            int index = 0;
            var start = FindTimeStamp(item, ref index);
            string end = null;

            if (start != null)
            {
                end = FindTimeStamp(item, ref index);

                // If end cannot be found, set both start and end to null. 
                if (end == null)
                {
                    start = null;
                }
            }

            // Convert any null to empty string.
            SetEmptyIfNull(ref start);
            SetEmptyIfNull(ref end);
            return new ValidPeriod(start, end);
        }

        private static string FindTimeStamp(string item, ref int index)
        {
            //returning string format: 11061200UTC
            int currentIndex = index;
            int matchCount = 0;
            const int AllMatchLen = 11;

            while (currentIndex >= 0 && currentIndex < item.Length)
            {
                if (CurrentCharMatch(matchCount, item[currentIndex]))
                {

                    if (matchCount == AllMatchLen - 1)
                    {
                        index = currentIndex + 1;
                        return item.Substring(currentIndex - AllMatchLen + 1, AllMatchLen);
                    }
                    else
                    {
                        currentIndex++;
                        matchCount++;
                    }
                }
                else
                {
                    //char does not match
                    if (currentIndex + 1 < item.Length)
                    {
                        currentIndex = item.IndexOfAny(DelimiterWords, currentIndex + 1) + 1;
                        matchCount = 0;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        private static bool CurrentCharMatch(int matchCount, char currentChar)
        {
            if (matchCount < 8)
            {
                return (currentChar >= '0' && currentChar <= '9');
            }
            else
            {
                return (matchCount == 8 && currentChar == 'U') ||
                       (matchCount == 9 && currentChar == 'T') ||
                       (matchCount == 10 && currentChar == 'C');
            }            
        }

        public struct ValidPeriod
        {
            public string Start { get; private set; }
            public string End { get; private set; }

            public ValidPeriod(string Start, string End)
            {
                this.Start = Start;
                this.End = End;
            }
        }
    }
}
