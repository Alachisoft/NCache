// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

namespace Alachisoft.NCache.Runtime.Dependencies
{
    public enum SqlCmpOptions
    {
        // Summary:
        //     Specifies the default option settings for System.Data.SqlTypes.SqlString
        //     comparisons.
        None = 0,
        //
        // Summary:
        //     Specifies that System.Data.SqlTypes.SqlString comparisons must ignore case.
        IgnoreCase = 1,
        //
        // Summary:
        //     Specifies that System.Data.SqlTypes.SqlString comparisons must ignore nonspace
        //     combining characters, such as diacritics. The Unicode Standard defines combining
        //     characters as characters that are combined with base characters to produce
        //     a new character. Non-space combining characters do not use character space
        //     by themselves when rendered. For more information about non-space combining
        //     characters, see the Unicode Standard at http://www.unicode.org.
        IgnoreNonSpace = 2,
        //
        // Summary:
        //     Specifies that System.Data.SqlTypes.SqlString comparisons must ignore the
        //     Kana type. Kana type refers to Japanese hiragana and katakana characters
        //     that represent phonetic sounds in the Japanese language. Hiragana is used
        //     for native Japanese expressions and words, while katakana is used for words
        //     borrowed from other languages, such as "computer" or "Internet". A phonetic
        //     sound can be expressed in both hiragana and katakana. If this value is selected,
        //     the hiragana character for one sound is considered equal to the katakana
        //     character for the same sound.
        IgnoreKanaType = 8,
        //
        // Summary:
        //     Specifies that System.Data.SqlTypes.SqlString comparisons must ignore the
        //     character width. For example, Japanese katakana characters can be written
        //     as full-width or half-width and, if this value is selected, the katakana
        //     characters written as full-width are considered equal to the same characters
        //     written in half-width.
        IgnoreWidth = 16,
        //
        // Summary:
        //     Performs a binary sort.
        BinarySort2 = 16384,
        //
        // Summary:
        //     Specifies that sorts should be based on a characters numeric value instead
        //     of its alphabetical value.
        BinarySort = 32768,
    }
}