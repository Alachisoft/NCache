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
// limitations under the License.


namespace Alachisoft.NCache.Caching.Queries
{


    enum SymbolConstants : int
    {
        SYMBOL_EOF                   = 0  , // (EOF)
        SYMBOL_ERROR                 = 1  , // (Error)
        SYMBOL_WHITESPACE            = 2  , // (Whitespace)
        SYMBOL_MINUS                 = 3  , // '-'
        SYMBOL_EXCLAMEQ              = 4  , // '!='
        SYMBOL_DOLLARTEXTDOLLAR      = 5  , // '$Text$'
        SYMBOL_LPARAN                = 6  , // '('
        SYMBOL_RPARAN                = 7  , // ')'
        SYMBOL_TIMES                 = 8  , // '*'
        SYMBOL_COMMA                 = 9  , // ','
        SYMBOL_DOT                   = 10 , // '.'
        SYMBOL_QUESTION              = 11 , // '?'
        SYMBOL_LT                    = 12 , // '<'
        SYMBOL_LTEQ                  = 13 , // '<='
        SYMBOL_LTGT                  = 14 , // '<>'
        SYMBOL_EQ                    = 15 , // '='
        SYMBOL_EQEQ                  = 16 , // '=='
        SYMBOL_GT                    = 17 , // '>'
        SYMBOL_GTEQ                  = 18 , // '>='
        SYMBOL_AND                   = 19 , // AND
        SYMBOL_ASC                   = 20 , // ASC
        SYMBOL_AVGLPARAN             = 21 , // 'AVG('
        SYMBOL_COUNTLPARAN           = 22 , // 'COUNT('
        SYMBOL_DATETIME              = 23 , // DateTime
        SYMBOL_DELETE                = 24 , // DELETE
        SYMBOL_DESC                  = 25 , // DESC
        SYMBOL_FALSE                 = 26 , // false
        SYMBOL_GROUPBY               = 27 , // 'GROUP BY'
        SYMBOL_IDENTIFIER            = 28 , // Identifier
        SYMBOL_IN                    = 29 , // IN
        SYMBOL_INTEGERLITERAL        = 30 , // IntegerLiteral
        SYMBOL_IS                    = 31 , // IS
        SYMBOL_KEYWORD               = 32 , // Keyword
        SYMBOL_LIKE                  = 33 , // LIKE
        SYMBOL_MAXLPARAN             = 34 , // 'MAX('
        SYMBOL_MINLPARAN             = 35 , // 'MIN('
        SYMBOL_NOT                   = 36 , // NOT
        SYMBOL_NOW                   = 37 , // now
        SYMBOL_NULL                  = 38 , // NULL
        SYMBOL_OR                    = 39 , // OR
        SYMBOL_ORDERBY               = 40 , // 'ORDER BY'
        SYMBOL_REALLITERAL           = 41 , // RealLiteral
        SYMBOL_SELECT                = 42 , // SELECT
        SYMBOL_STRINGLITERAL         = 43 , // StringLiteral
        SYMBOL_SUMLPARAN             = 44 , // 'SUM('
        SYMBOL_TAG                   = 45 , // Tag
        SYMBOL_TRUE                  = 46 , // true
        SYMBOL_WHERE                 = 47 , // WHERE
        SYMBOL_AGGREGATEFUNCTION     = 48 , // <AggregateFunction>
        SYMBOL_AGGREGATEFUNCTIONLIST = 49 , // <AggregateFunctionList>
        SYMBOL_ANDEXPR               = 50 , // <AndExpr>
        SYMBOL_ATRRIB                = 51 , // <Atrrib>
        SYMBOL_AVERAGEFUNCTION       = 52 , // <AverageFunction>
        SYMBOL_COMPAREEXPR           = 53 , // <CompareExpr>
        SYMBOL_COUNTFUNCTION         = 54 , // <CountFunction>
        SYMBOL_DATE                  = 55 , // <Date>
        SYMBOL_DATELIST              = 56 , // <DateList>
        SYMBOL_DELETEPARAMS          = 57 , // <DeleteParams>
        SYMBOL_EXPRESSION            = 58 , // <Expression>
        SYMBOL_GROUPBYVALUELIST      = 59 , // <GroupByValueList>
        SYMBOL_INLIST                = 60 , // <InList>
        SYMBOL_LISTTYPE              = 61 , // <ListType>
        SYMBOL_MAXFUNCTION           = 62 , // <MaxFunction>
        SYMBOL_MINFUNCTION           = 63 , // <MinFunction>
        SYMBOL_NUMLITERAL            = 64 , // <NumLiteral>
        SYMBOL_NUMLITERALLIST        = 65 , // <NumLiteralList>
        SYMBOL_OBJECTATTRIBUTE       = 66 , // <ObjectAttribute>
        SYMBOL_OBJECTATTRIBUTELIST   = 67 , // <ObjectAttributeList>
        SYMBOL_OBJECTTYPE            = 68 , // <ObjectType>
        SYMBOL_OBJECTVALUE           = 69 , // <ObjectValue>
        SYMBOL_ORDER                 = 70 , // <Order>
        SYMBOL_ORDERARGUMENT         = 71 , // <OrderArgument>
        SYMBOL_ORDERBYVALUELIST      = 72 , // <OrderByValueList>
        SYMBOL_OREXPR                = 73 , // <OrExpr>
        SYMBOL_PROPERTY              = 74 , // <Property>
        SYMBOL_QUERY                 = 75 , // <Query>
        SYMBOL_SELECTQUERY           = 76 , // <SelectQuery>
        SYMBOL_STRLITERAL            = 77 , // <StrLiteral>
        SYMBOL_STRLITERALLIST        = 78 , // <StrLiteralList>
        SYMBOL_SUMFUNCTION           = 79 , // <SumFunction>
        SYMBOL_TYPEPLUSATTRIBUTE     = 80 , // <TypePlusAttribute>
        SYMBOL_UNARYEXPR             = 81 , // <UnaryExpr>
        SYMBOL_VALUE                 = 82   // <Value>
    };
}
