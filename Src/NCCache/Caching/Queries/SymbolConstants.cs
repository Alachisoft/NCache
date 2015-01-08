// Copyright (c) 2015 Alachisoft
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
// limitations under the License.
namespace Alachisoft.NCache.Caching.Queries
{
    enum SymbolConstants : int
    {
        SYMBOL_EOF               = 0  , // (EOF)
        SYMBOL_ERROR             = 1  , // (Error)
        SYMBOL_WHITESPACE        = 2  , // (Whitespace)
        SYMBOL_MINUS             = 3  , // '-'
        SYMBOL_EXCLAMEQ          = 4  , // '!='
        SYMBOL_DOLLARTEXTDOLLAR  = 5  , // '$Text$'
        SYMBOL_LPARAN            = 6  , // '('
        SYMBOL_RPARAN            = 7  , // ')'
        SYMBOL_TIMES             = 8  , // '*'
        SYMBOL_COMMA             = 9  , // ','
        SYMBOL_DOT               = 10 , // '.'
        SYMBOL_QUESTION          = 11 , // '?'
        SYMBOL_LT                = 12 , // '<'
        SYMBOL_LTEQ              = 13 , // '<='
        SYMBOL_LTGT              = 14 , // '<>'
        SYMBOL_EQ                = 15 , // '='
        SYMBOL_EQEQ              = 16 , // '=='
        SYMBOL_GT                = 17 , // '>'
        SYMBOL_GTEQ              = 18 , // '>='
        SYMBOL_AND               = 19 , // AND
        SYMBOL_AVGLPARAN         = 20 , // 'AVG('
        SYMBOL_COUNTLPARAN       = 21 , // 'COUNT('
        SYMBOL_DATETIME          = 22 , // DateTime
        SYMBOL_DELETE            = 23 , // DELETE
        SYMBOL_FALSE             = 24 , // false
        SYMBOL_IDENTIFIER        = 25 , // Identifier
        SYMBOL_IN                = 26 , // IN
        SYMBOL_INTEGERLITERAL    = 27 , // IntegerLiteral
        SYMBOL_IS                = 28 , // IS
        SYMBOL_KEYWORD           = 29 , // Keyword
        SYMBOL_LIKE              = 30 , // LIKE
        SYMBOL_MAXLPARAN         = 31 , // 'MAX('
        SYMBOL_MINLPARAN         = 32 , // 'MIN('
        SYMBOL_NOT               = 33 , // NOT
        SYMBOL_NOW               = 34 , // now
        SYMBOL_NULL              = 35 , // NULL
        SYMBOL_OR                = 36 , // OR
        SYMBOL_REALLITERAL       = 37 , // RealLiteral
        SYMBOL_SELECT            = 38 , // SELECT
        SYMBOL_STRINGLITERAL     = 39 , // StringLiteral
        SYMBOL_SUMLPARAN         = 40 , // 'SUM('
        SYMBOL_TRUE              = 41 , // true
        SYMBOL_WHERE             = 42 , // WHERE
        SYMBOL_AGGREGATEFUNCTION = 43 , // <AggregateFunction>
        SYMBOL_ANDEXPR           = 44 , // <AndExpr>
        SYMBOL_ATRRIB            = 45 , // <Atrrib>
        SYMBOL_AVERAGEFUNCTION   = 46 , // <AverageFunction>
        SYMBOL_COMPAREEXPR       = 47 , // <CompareExpr>
        SYMBOL_COUNTFUNCTION     = 48 , // <CountFunction>
        SYMBOL_DATE              = 49 , // <Date>
        SYMBOL_DATELIST          = 50 , // <DateList>
        SYMBOL_DELETEPARAMS      = 51 , // <DeleteParams>
        SYMBOL_EXPRESSION        = 52 , // <Expression>
        SYMBOL_INLIST            = 53 , // <InList>
        SYMBOL_LISTTYPE          = 54 , // <ListType>
        SYMBOL_MAXFUNCTION       = 55 , // <MaxFunction>
        SYMBOL_MINFUNCTION       = 56 , // <MinFunction>
        SYMBOL_NUMLITERAL        = 57 , // <NumLiteral>
        SYMBOL_NUMLITERALLIST    = 58 , // <NumLiteralList>
        SYMBOL_OBJECTATTRIBUTE   = 59 , // <ObjectAttribute>
        SYMBOL_OBJECTTYPE        = 60 , // <ObjectType>
        SYMBOL_OBJECTVALUE       = 61 , // <ObjectValue>
        SYMBOL_OREXPR            = 62 , // <OrExpr>
        SYMBOL_PROPERTY          = 63 , // <Property>
        SYMBOL_QUERY             = 64 , // <Query>
        SYMBOL_STRLITERAL        = 65 , // <StrLiteral>
        SYMBOL_STRLITERALLIST    = 66 , // <StrLiteralList>
        SYMBOL_SUMFUNCTION       = 67 , // <SumFunction>
        SYMBOL_TYPEPLUSATTRIBUTE = 68 , // <TypePlusAttribute>
        SYMBOL_UNARYEXPR         = 69 , // <UnaryExpr>
        SYMBOL_VALUE             = 70   // <Value>
    };
}
