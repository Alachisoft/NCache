//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

namespace Alachisoft.NCache.SocketServer.Resp.PubSub
{
    /// <summary>
    /// Redis glob matching, as used by PSUBSCRIBE and PUBSUB CHANNELS.
    /// <para>
    /// Written out rather than translated into a regular expression: the escaping rules differ in
    /// enough corners (a '[' that never closes, a trailing backslash) that a translation tends to
    /// throw where Redis simply fails to match. This is the same recursive backtracking shape
    /// Redis itself uses, and it cannot throw on any input.
    /// </para>
    /// </summary>
    internal static class RespGlob
    {
        public static bool IsMatch(string pattern, string subject)
        {
            if (pattern == null || subject == null) return false;
            return Match(pattern, 0, subject, 0);
        }

        private static bool Match(string p, int pi, string s, int si)
        {
            while (pi < p.Length)
            {
                switch (p[pi])
                {
                    case '*':
                        // Collapse runs of '*', then try every split point.
                        while (pi + 1 < p.Length && p[pi + 1] == '*') pi++;
                        if (pi + 1 == p.Length) return true;      // trailing star matches the rest
                        for (int k = si; k <= s.Length; k++)
                            if (Match(p, pi + 1, s, k)) return true;
                        return false;

                    case '?':
                        if (si >= s.Length) return false;
                        si++; pi++;
                        break;

                    case '[':
                        {
                            if (si >= s.Length) return false;

                            int close = p.IndexOf(']', pi + 1);
                            if (close < 0)
                            {
                                // Unterminated class: Redis treats '[' as a literal.
                                if (s[si] != '[') return false;
                                si++; pi++;
                                break;
                            }

                            int start = pi + 1;
                            bool negate = start < close && p[start] == '^';
                            if (negate) start++;

                            bool hit = false;
                            for (int k = start; k < close; k++)
                            {
                                // a-z style range
                                if (k + 2 < close && p[k + 1] == '-')
                                {
                                    char lo = p[k], hi = p[k + 2];
                                    if (lo > hi) { char t = lo; lo = hi; hi = t; }
                                    if (s[si] >= lo && s[si] <= hi) hit = true;
                                    k += 2;
                                }
                                else if (p[k] == s[si]) hit = true;
                            }

                            if (hit == negate) return false;
                            si++; pi = close + 1;
                            break;
                        }

                    case '\\':
                        // Escape: the next character is literal. A trailing backslash is itself.
                        if (pi + 1 < p.Length) pi++;
                        if (si >= s.Length || s[si] != p[pi]) return false;
                        si++; pi++;
                        break;

                    default:
                        if (si >= s.Length || s[si] != p[pi]) return false;
                        si++; pi++;
                        break;
                }
            }

            return si == s.Length;
        }
    }
}
