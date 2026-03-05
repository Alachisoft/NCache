// Portions copyright (C) 2005 Nigel Sheridan-Smith
// Modified 11 January 2005 through to 13 January 2005 and beyond 
// Derived from ASN.1 lexer/parser by Vivek Gupta (11 November 2003)
//
// Portions copyright (c) 2011-2012, Lex Li
// All rights reserved.
//   
// Redistribution and use in source and binary forms, with or without modification, are 
// permitted provided that the following conditions are met:
//   
// - Redistributions of source code must retain the above copyright notice, this list 
//   of conditions and the following disclaimer.
//   
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials 
//   provided with the distribution.
//   
// - Neither the name of the <ORGANIZATION> nor the names of its contributors may be used to 
//   endorse or promote products derived from this software without specific prior written 
//   permission.
//   
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS 
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY 
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER 
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

grammar Smi; 

options
{
  language=CSharp3;
  output=AST;
  ASTLabelType=CommonTree;
}

@parser::header
{
#pragma warning disable 3001, 3003, 3005, 3009, 1591 
}

@lexer::header
{
#pragma warning disable 3001, 3003, 3005, 3009, 1591 
}

@parser::namespace { Lextm.SharpSnmpLib.Mib }
@lexer::namespace { Lextm.SharpSnmpLib.Mib }

@parser::footer
{
#pragma warning restore 3001, 3003, 3005, 3009, 1591 
}

@lexer::footer
{
#pragma warning restore 3001, 3003, 3005, 3009, 1591 
}

// Alter code generation so catch-clauses get replace with
// this action.
@rulecatch{
catch (RecognitionException) 
{
    throw;
}
}

//  Creation of ASN.1 grammar for ANTLR V2.7.1
// ===================================================
//      TOKENS FOR ASN.1 LEXER DEFINITIONS
// ===================================================

//  ASN1 Tokens 
/*
tokens {  
  ABSENT_KW;
  ABSTRACT_SYNTAX_KW;
  ALL_KW ;
  ANY_KW ;
  ARGUMENT_KW ;
  APPLICATION_KW ;
  AUTOMATIC_KW ;
  BASED_NUM_KW ;
  BEGIN_KW ;
  BIT_KW ;
  BMP_STR_KW ;
  BOOLEAN_KW ;
  BY_KW ;
  CHARACTER_KW ;
  CHOICE_KW ;
  CLASS_KW ;
  COMPONENTS_KW ;
  COMPONENT_KW ;
  CONSTRAINED_KW ;
  DEFAULT_KW ;
  DEFINED_KW ;
  DEFINITIONS_KW ;
  EMBEDDED_KW ;
  END_KW ;
  ENUMERATED_KW ;
  ERROR_KW ;
  ERRORS_KW ;
  EXCEPT_KW ;
  EXPLICIT_KW ;
  EXPORTS_KW ;
  EXTENSIBILITY_KW ;
  EXTERNAL_KW ;
  FALSE_KW ;
  FROM_KW ;
  GENERALIZED_TIME_KW ;
  GENERAL_STR_KW ;
  GRAPHIC_STR_KW ;
  IA5_STRING_KW ;
  IDENTIFIER_KW ;
  IMPLICIT_KW ;
  IMPLIED_KW ;
  IMPORTS_KW ;
  INCLUDES_KW ;
  INSTANCE_KW ;
  INTEGER_KW ;
   INTERSECTION_KW ;
   ISO646_STR_KW ;
   LINKED_KW ;
   MAX_KW ;
   MINUS_INFINITY_KW ;
   MIN_KW ;
   NULL_KW ;
   NUMERIC_STR_KW ;
   OBJECT_DESCRIPTOR_KW ;
   OBJECT_KW ;
   OCTET_KW ;
   OPERATION_KW ;
   OF_KW ;
   OID_KW ;
   OPTIONAL_KW ;
   PARAMETER_KW ;
   PATTERN_KW;
   PDV_KW ;
   PLUS_INFINITY_KW ;
   PRESENT_KW ;
   PRINTABLE_STR_KW ;
   PRIVATE_KW ;
   REAL_KW ;
   RELATIVE_KW ;
   RESULT_KW ;
   SEQUENCE_KW ;
   SET_KW ;
   SIZE_KW ;
   STRING_KW ;
   TAGS_KW ;
   TELETEX_STR_KW ;
   T61_STR_KW;
   TRUE_KW ;
   TYPE_IDENTIFIER_KW ;
   UNION_KW ;
   UNIQUE_KW ;
   UNIVERSAL_KW ;
   UNIVERSAL_STR_KW ;
   UTC_TIME_KW ;
   UTF8_STR_KW ;
   VIDEOTEX_STR_KW ;
   VISIBLE_STR_KW ;
   WITH_KW ;
}
*/
ABSENT_KW
  : 'ABSENT'
  ;

ABSTRACT_SYNTAX_KW
  : 'ABSTRACT-SYNTAX'
  ;

ALL_KW
  : 'ALL'
  ;

ANY_KW
  : 'ANY'
  ;

ARGUMENT_KW
  : 'ARGUMENT'
  ;

APPLICATION_KW
  : 'APPLICATION'
  ;

AUTOMATIC_KW
  : 'AUTOMATIC'
  ;

BASED_NUM_KW
  : 'BASEDNUM'
  ;

BEGIN_KW
  : 'BEGIN'
  ;

fragment
BIT_KW
  : 'BIT'
  ;

BMP_STR_KW
  : 'BMPString'
  ;

BOOLEAN_KW
  : 'BOOLEAN'
  ;

BY_KW
  : 'BY'
  ;

CHARACTER_KW
  : 'CHARACTER'
  ;

CHOICE_KW
  : 'CHOICE'
  ;

CLASS_KW
  : 'CLASS'
  ;

COMPONENTS_KW
  : 'COMPONENTS'
  ;

COMPONENT_KW
  : 'COMPONENT'
  ;

CONSTRAINED_KW
  : 'CONSTRAINED'
  ;

DEFAULT_KW
  : 'DEFAULT'
  ;

DEFINED_KW
  : 'DEFINED'
  ;

DEFINITIONS_KW
  : 'DEFINITIONS'
  ;

EMBEDDED_KW
  : 'EMBEDDED'
  ;

END_KW
  : 'END'
  ;

ENUMERATED_KW
  : 'ENUMERATED'
  ;

ERROR_KW
  : 'ERROR'
  ;

ERRORS_KW
  : 'ERRORS'
  ;

EXCEPT_KW
  : 'EXCEPT'
  ;

EXPLICIT_KW
  : 'EXPLICIT'
  ;

EXPORTS_KW
  : 'EXPORTS'
  ;

EXTENSIBILITY_KW
  : 'EXTENSIBILITY'
  ;

EXTERNAL_KW
  : 'EXTERNAL'
  ;

FALSE_KW
  : 'FALSE'
  ;

FROM_KW
  : 'FROM'
  ;

GENERALIZED_TIME_KW
  : 'GeneralizedTime'
  ;

GENERAL_STR_KW
  : 'GeneralString'
  ;

GRAPHIC_STR_KW
  : 'GraphicString'
  ;

IA5_STRING_KW
  : 'IA5String'
  ;

IDENTIFIER_KW
  : 'IDENTIFIER'
  ;

IMPLICIT_KW
  : 'IMPLICIT'
  ;

IMPLIED_KW
  : 'IMPLIED'
  ;

IMPORTS_KW
  : 'IMPORTS'
  ;

INCLUDES_KW
  : 'INCLUDES'
  ;

INSTANCE_KW
  : 'INSTANCE'
  ;

INTEGER_KW
  : 'INTEGER'
  ;

INTERSECTION_KW
  : 'INTERSECTION'
  ;

ISO646_STR_KW
  : 'ISO646String'
  ;

LINKED_KW
  : 'LINKED'
  ;

MACRO_KW
  : 'MACRO'
  ;

MAX_KW
  : 'MAX'
  ;

MINUS_INFINITY_KW
  : 'MINUSINFINITY'
  ;

MIN_KW
  : 'MIN'
  ;

NULL_KW
  : 'NULL'
  ;

NUMERIC_STR_KW
  : 'NumericString'
  ;

OBJECT_DESCRIPTOR_KW
  : 'ObjectDescriptor'
  ;

OBJECT_KW
  : 'OBJECT'
  ;

OCTET_KW
  : 'OCTET'
  ;

OPERATION_KW
  : 'OPERATION'
  ;

OF_KW
  : 'OF'
  ;

OID_KW
  : 'OID'
  ;

OPTIONAL_KW
  : 'OPTIONAL'
  ;

PARAMETER_KW
  : 'PARAMETER'
  ;

PATTERN_KW
  : 'PATTERN'
  ; 

PDV_KW
  : 'PDV'
  ;

PIBDEFINITIONS_KW
  : 'PIB-DEFINITIONS'
  ;

PLUS_INFINITY_KW
  : 'PLUSINFINITY'
  ;

PRESENT_KW
  : 'PRESENT'
  ;

PRINTABLE_STR_KW
  : 'PrintableString'
  ;

PRIVATE_KW
  : 'PRIVATE'
  ;

REAL_KW
  : 'REAL'
  ;

RELATIVE_KW
  : 'RELATIVE'
  ;

RESULT_KW
  : 'RESULT'
  ;

SEQUENCE_KW
  : 'SEQUENCE'
  ;

SET_KW
  : 'SET'
  ;

SIZE_KW
  : 'SIZE'
  ;

STRING_KW
  : 'STRING'
  ;

TAGS_KW
  : 'TAGS'
  ;

TELETEX_STR_KW
  : 'TeletexString'
  ;
  
T61_STR_KW
  : 'T61String'
  ;

TRUE_KW
  : 'TRUE'
  ;

TYPE_IDENTIFIER_KW
  : 'TYPE-IDENTIFIER'
  ;

UNION_KW
  : 'UNION'
  ;

UNIQUE_KW
  : 'UNIQUE'
  ;

UNIVERSAL_KW
  : 'UNIVERSAL'
  ;

UNIVERSAL_STR_KW
  : 'UniversalString'
  ;

UTC_TIME_KW
  : 'UTCTime'
  ;

UTF8_STR_KW
  : 'UTF8String'
  ;

VIDEOTEX_STR_KW
  : 'VideotexString'
  ;

VISIBLE_STR_KW
  : 'VisibleString'
  ;

WITH_KW
  : 'WITH'
  ;
  
// Operators

ASSIGN_OP:  '::=';
BAR:    '|';
COLON:    ':';
COMMA:    ',';
DOT:    '.';
DOTDOT:   '..';
DOTDOTDOT
  : '...';
// ELLIPSIS:  '...';
EXCLAMATION:  '!';
INTERSECTION: '^';
LESS:   '<';
L_BRACE:  '{';
L_BRACKET:  '[';
L_PAREN:  '(';
MINUS:    '-';
PLUS:   '+';
R_BRACE:  '}';
R_BRACKET:  ']';
R_PAREN:  ')';
SEMI:   ';';
SINGLE_QUOTE: '\'';
CHARB:    '\'B';
CHARH:    '\'H';

// Whitespace -- ignored

fragment
NEWLINE :
    ('\r\n') => '\r\n'    // DOS
  | '\r'      // Macintosh
  | '\n'    // Unix 
  ;

WS      
      : ( ' ' | '\t' | '\f' | NEWLINE )+
  { Skip(); }
  ;
  
COMMENT 
 : '--' ({!(input.LA(1) == '-' && input.LA(2) == '-')}?=> ~('\r' | '\n'))* ('\r'? '\n' | '--')
 { Skip(); }
 ;

NUMBER  : ('0'..'9')+ ;

fragment
HDIG    : ( :('0'..'9') )
      | ('A'..'F')
      | ('a'..'f')
      ;

/* NSS 13/1/05: Added '_' as acceptable character - required for some PIBs */

NAME  : ('a'..'z' | 'A'..'Z') ( 'a'..'z' | 'A'..'Z' |'-' | '_' | '0'..'9' )*  ; 

// Unable to resolve a string like 010101 followed by 'H
//B_STRING  :   SINGLE_QUOTE ({LA(3)!='B'}? BDIG)+  BDIG SINGLE_QUOTE 'B';
//H_STRING  :   SINGLE_QUOTE ({LA(3)!='H'}? HDIG)+  HDIG SINGLE_QUOTE 'H';

//B_OR_H_STRING
//  : (
//    :(B_STRING)=>B_STRING
//    | H_STRING)
//  ;
  
/* Changed by NSS 13/1/05 - upper case *or* lower case 'B' and 'H'; zero or more digits */
B_STRING  :   SINGLE_QUOTE (('0'|'1'))* SINGLE_QUOTE ('B' | 'b') ;

H_STRING  :   SINGLE_QUOTE (HDIG)* SINGLE_QUOTE ('H' | 'h') ; 
  
C_STRING  :   '"' (options {greedy=false;}
                             : NEWLINE 
                             | ~('\r' | '\n')
                            )* 
                        '"' ;


//*************************************************************************
//**********    PARSER DEFINITIONS
//*************************************************************************

statement returns [MibDocument result = new MibDocument()]
    : (mod=module_definition { $result.Add(mod.result); })* EOF
  ;
  
// Grammar Definitions

/* NSS 13/1/05: Added 'PIB-DEFINITIONS' for SPPI */
module_definition returns [MibModule result]
    : name=NAME (obj_id_comp_lst)? (PIBDEFINITIONS_KW | DEFINITIONS_KW) 
    ( (EXPLICIT_KW | IMPLICIT_KW | AUTOMATIC_KW) TAGS_KW )? 
    (EXTENSIBILITY_KW IMPLIED_KW)?
    ASSIGN_OP BEGIN_KW mod=module_body END_KW 
    {
        $result = $mod.result; 
        $result.Name = $name.text;
        WantModuleName($name);
    }
    ;

module_body returns [MibModule result = new MibModule()]
    :
  (ex=exports { $result.Exports = $ex.result; })? 
  (im=imports { $result.Imports = $im.result; })? 
  (a=assignment { $result.Add($a.result); })* ;

/* NSS 15/1/05: Added syntactic predicate */
obj_id_comp_lst returns [IdComponentList result = new IdComponentList()]
    : L_BRACE ((NAME (NAME|NUMBER)) => dv=defined_value { $result.DefinedValue = $dv.result; })?
  (oid=obj_id_component { $result.Add($oid.result); })+ 
  R_BRACE
  ;
//obj_id_comp_lst: L_BRACE (defined_value)? (obj_id_component)+ R_BRACE;

fragment defined_value returns [DefinedValue result = new DefinedValue()]
    : (mod=NAME DOT 
    { 
      $result.Module = $mod.text; 
      WantModuleName($mod);
    })? 
    v=NAME 
    { 
      $result.Value = $v.text; 
      WantCamelCase($v);
    }
  ;

/* NSS 14/1/05: Checked against X.680 */
obj_id_component returns [IdComponent result = new IdComponent()]
    : (num1=NUMBER { $result.Number = long.Parse($num1.text); }
    | name=NAME 
    { 
      $result.Name = $name.text; 
      WantCamelCase($name);
    })
  (L_PAREN num2=NUMBER R_PAREN { $result.Number = long.Parse($num2.text); })?
  ;

tag_default returns [TagDefault result]
    : EXPLICIT_KW { $result = TagDefault.Explicit; }
  | IMPLICIT_KW { $result = TagDefault.Implicit; }
  | AUTOMATIC_KW { $result = TagDefault.Automatic; }
  ;

exports returns [Exports result = new Exports()]
    : EXPORTS_KW (
      (sym=symbol_list { $result.Add($sym.text); })? 
    | ALL_KW { $result.AllExported = true; } 
    ) SEMI;

imports returns [Imports result = new Imports()]
    : IMPORTS_KW (sym=symbols_from_module { $result.Add($sym.result); })* SEMI ;

/* NSS 14/1/05: Shouldn't need syntactic predicate */
assignment returns [IConstruct result]
    : 
  u=NAME ASSIGN_OP t=type 
  { 
    $result = $t.result; 
    $result.Name = $u.text;
    WantTypeName($u);
  }
    | l=NAME t2=type ASSIGN_OP v=value 
  { 
    $result = new ValueAssignment($t2.result, $v.result);
    $result.Name = $l.text;
    WantObjectName($l);
  }
    | name=symbol MACRO_KW ASSIGN_OP BEGIN_KW (~(END_KW))* END_KW 
  { 
    $result = new Macro($name.text); 
    WantMacroName(name.Start);
  }
  ;

symbol_list returns [IList<string> result = new List<string>()]
    : sym=symbol { $result.Add($sym.text); }
   (COMMA sym2=symbol { $result.Add($sym2.text); })* ;

symbols_from_module returns [Import result = new Import()]
    : syms=symbol_list { $result.Symbols = $syms.result; }
    FROM_KW mod=NAME 
    { 
      $result.Module = $mod.text;
      WantModuleName($mod);
    }
                        ( obj_id_comp_lst 
                          | (defined_value) => defined_value 
                        )? ;

symbol: NAME | macroName | 'BITS';

macroName: OPERATION_KW | ERROR_KW  | 'BIND' | 'UNBIND' 
         | 'APPLICATION-SERVICE-ELEMENT' | 'APPLICATION-CONTEXT' | 'EXTENSION' 
         | 'EXTENSIONS' | 'EXTENSION-ATTRIBUTE' | 'TOKEN' | 'TOKEN-DATA' 
   | 'SECURITY-CATEGORY' | 'OBJECT' | 'PORT' | 'REFINE' | 'ABSTRACT-BIND' 
   | 'ABSTRACT-UNBIND' | 'ABSTRACT-OPERATION' | 'ABSTRACT-ERROR' 
   | 'ALGORITHM' | 'ENCRYPTED' | 'SIGNED' | 'SIGNATURE' | 'PROTECTED' 
   | smi_macros;

type returns [ISmiType result]
    : 
  b=built_in_type { $result = $b.result; }
  | d=defined_type { $result = $d.result; }
  | s=selection_type { $result = $s.result; }
  | m=macros_type { $result = $m.result; }
  | sm=smi_type { $result = $sm.result; }
  ;

value returns [ISmiValue result]
    : (TRUE_KW) => TRUE_KW { $result = new TrueLiteralValue(); }
     | (FALSE_KW) => FALSE_KW { $result = new FalseLiteralValue(); }
     | (NULL_KW) => NULL_KW { $result = new NullLiteralValue(); }
     | (C_STRING) => s=C_STRING { $result = new LiteralValue($s.text); }
     | (full_qualified_value) => fqv=full_qualified_value { $result = $fqv.result; }
	 | (namedbit) => nb=namedbit { $result = $nb.result; }
     | (defined_value) => dv=defined_value { $result = $dv.result; }
     | (signed_number) => sn=signed_number { $result = $sn.result; }
     | (choice_value) => cv=choice_value { $result = $cv.result; }
     | (sequence_value) => sv=sequence_value { $result = $sv.result; }
     | (sequenceof_value) => sov=sequenceof_value { $result = $sov.result; }
     | (cstr_value) => csv=cstr_value { $result = $csv.result; }
     | (obj_id_comp_lst) => oid=obj_id_comp_lst { $result = $oid.result; }
     | (PLUS_INFINITY_KW) => PLUS_INFINITY_KW { $result = new PlusInfinityLiteralValue(); }
     | (MINUS_INFINITY_KW) => MINUS_INFINITY_KW { $result = new MinusInfinityLiteralValue(); }
     | (symbol) => name=symbol { $result = new LiteralValue($name.text); }
     ;

built_in_type returns [ISmiType result]
    : a=any_type { $result = $a.result; }
    | bs=bit_string_type { $result = $bs.result; }
    | BOOLEAN_KW { $result = new BooleanType(); }
    | cs=character_str_type { $result = $cs.result; }
    | c=choice_type { $result = $c.result; }
    | em=embedded_type EMBEDDED_KW PDV_KW { $result = $em.result; }
    | en=enum_type { $result = $en.result; }
    | EXTERNAL_KW { $result = new ExternalType(); }
  | i=integer_type { $result = $i.result; }
  | NULL_KW { $result = new NullType(); }
  | oid=object_identifier_type { $result = $oid.result; }
  | oct=octetString_type { $result = $oct.result; }
  | REAL_KW { $result = new RealType(); }
  | 'RELATIVE-OID' { $result = new RelativeIdType(); }
  | se=sequence_type { $result = $se.result; }
  | so=sequenceof_type { $result = $so.result; }
  | s=set_type { $result = $s.result; }
  | s1=setof_type { $result = $s1.result; }
  | t=tagged_type { $result = $t.result; }
  ;

defined_type returns [DefinedType result = new DefinedType()]
    : (mod=NAME 
    { 
      $result.Module = $mod.text; 
      WantPascalCase($mod);
    } DOT )? 
    name=NAME 
    { 
      $result.Name = $name.text; 
      WantPascalCase($name);
    }
  (c=constraint { $result.Constraint = $c.result; })? ;

selection_type returns [SelectionType result]
    : name=NAME LESS t=type 
    { 
      $result = new SelectionType($name.text, $t.result); 
      WantCamelCase($name);
    }
  ;

any_type returns [AnyType result = new AnyType()]
    : ANY_KW (DEFINED_KW BY_KW def=NAME 
    { 
      $result.DefinedById = $def.text;
      WantCamelCase($def); 
    })? 
    ;

/* NSS 15/1/2005: Added syntactic predicate */
bit_string_type returns [BitStringType result = new BitStringType()]
    : BIT_KW STRING_KW ((L_BRACE namedNumber) => n=namedNumber_list { $result.NamedNumberList = $n.result; })? 
  (c=constraint { $result.Constraint = $c.result; })? 
  ;

character_str_type returns [CharacterStringType result = new CharacterStringType()]
    : CHARACTER_KW STRING_KW 
   | cs=character_set { $result.CharacterSet = $cs.result; }
   (c=constraint { $result.Constraint = $c.result; })? 
   ;

choice_type returns [ChoiceType result]
    : CHOICE_KW L_BRACE e=elementType_list R_BRACE { $result = new ChoiceType($e.result); }
  ;

embedded_type returns [EmbeddedType result = new EmbeddedType()]
    : EMBEDDED_KW PDV_KW 
  ;

enum_type returns [EnumType result]
    : ENUMERATED_KW n=namedNumber_list { $result = new EnumType($n.result); }
  ;

/* NSS 15/1/05: Added syntactic predicate */
integer_type returns [IntegerType result = new IntegerType()]
    : INTEGER_KW ((L_BRACE namedNumber) => n=namedNumber_list { $result.NamedNumberList = $n.result; }
  | c=constraint { $result.Constraint = $c.result; })? 
  ;

//integer_type: INTEGER_KW (namedNumber_list | constraint)? ;

object_identifier_type returns [ObjectIdentifierType result = new ObjectIdentifierType()]
    : OBJECT_KW IDENTIFIER_KW 
  ;

octetString_type returns [OctetStringType result = new OctetStringType()]
    : OCTET_KW STRING_KW (c=constraint { $result.Constraint = $c.result; })? 
  ;

sequence_type returns [SequenceType result = new SequenceType()]
    : SEQUENCE_KW L_BRACE (e=elementType_list { $result.ElementTypeList = $e.result; })? R_BRACE 
  ;

sequenceof_type returns [SequenceOfType result = new SequenceOfType()]
    : SEQUENCE_KW (L_PAREN SIZE_KW c=constraint { $result.Constraint = $c.result; } R_PAREN)?
  OF_KW t=type { $result.Subtype = $t.result; }
  ;

set_type returns [SetType result = new SetType()]
    : SET_KW L_BRACE (e=elementType_list { $result.ElementTypeList = $e.result; })? R_BRACE
  ;

setof_type returns [SetOfType result = new SetOfType()]
    : SET_KW (SIZE_KW c=constraint { $result.Constraint = $c.result; })?
  OF_KW t=type { $result.Subtype = $t.result; }
  ;

tagged_type returns [TaggedType result = new TaggedType()]
    : t=tag { $result.Tag = $t.result; }
  (td=tag_default { $result.TagDefault = $td.result; })? 
  ty=type { $result.Subtype = $ty.result; }
  ;

namedNumber_list returns [IList<ISmiValue> result = new List<ISmiValue>()]
    : L_BRACE num1=namedNumber { $result.Add($num1.result); }
  (COMMA num2=namedNumber { $result.Add($num2.result); })* R_BRACE
  ;

constraint returns [Constraint result = new Constraint()]
    : L_PAREN (es=element_set_specs { $result.ElementSetSpecs = $es.result; })? 
  (ex=exception_spec { $result.ExceptionSpec = $ex.result; })? R_PAREN
  ;

character_set returns [CharacterSet result]
    : BMP_STR_KW { $result = CharacterSet.Bmp; }
  | GENERALIZED_TIME_KW { $result = CharacterSet.GeneralizedTime; }
  | GENERAL_STR_KW { $result = CharacterSet.General; }
  | GRAPHIC_STR_KW { $result = CharacterSet.Graphic; }
    | IA5_STRING_KW { $result = CharacterSet.IA5; }
  | ISO646_STR_KW { $result = CharacterSet.ISO646; }
  | NUMERIC_STR_KW { $result = CharacterSet.Numeric; }
  | PRINTABLE_STR_KW { $result = CharacterSet.Printable; }
    | TELETEX_STR_KW { $result = CharacterSet.TeleTex; }
  | T61_STR_KW { $result = CharacterSet.T61; }
  | UNIVERSAL_STR_KW { $result = CharacterSet.Universal; }
  | UTF8_STR_KW { $result = CharacterSet.UTF8; }
    | UTC_TIME_KW { $result = CharacterSet.UTCTime; }
  | VIDEOTEX_STR_KW { $result = CharacterSet.VideoTex; }
  | VISIBLE_STR_KW { $result = CharacterSet.Visible; }
  ;

elementType_list returns [IList<ISmiType> result = new List<ISmiType>()]
    : t1=elementType { $result.Add($t1.result); } 
  (COMMA ((CHOICE_KW) => c=choice_type { $result.Add($c.result); }
  | t2=elementType { $result.Add($t2.result); }))*
  ;

tag returns [Tag result = new Tag()]
    : L_BRACKET (c=clazz { $result.TagType = $c.text; })? 
  cl=class_NUMBER R_BRACKET { $result.TagNumber = $cl.result; }
  ;

clazz: UNIVERSAL_KW | APPLICATION_KW | PRIVATE_KW;

/* NSS 14/1/05: 'defined_value' not 'LOWER' */
class_NUMBER returns [ClassNumber result]
    : num=NUMBER { $result = new ClassNumber($num.text); }
  | dv=defined_value { $result = new ClassNumber($dv.result); }
  ;

/* NSS 15/1/05: Added syntactic predicates; removed 'SEMI' */
operation_macro returns [OperationMacro result = new OperationMacro()]
    : OPERATION_KW (ARGUMENT_KW ((NAME) => l1=NAME 
    { 
      $result.ArgumentIdentifier = $l1.text; 
      WantCamelCase($l1);
    })? 
  t1=type { $result.ArgumentType = $t1.result; })? 
    ( (RESULT_KW) => RESULT_KW 
        ((NAME) => ((NAME) => l2=NAME 
        { 
          $result.ResultIdentifier = $l2.text; 
          WantCamelCase($l2);
        })? 
    t2=type { $result.ResultType = $t2.result; })? 
  )?
    ( (ERRORS_KW) => ERRORS_KW L_BRACE (o=operation_errorlist { $result.ErrorList = $o.result; })? R_BRACE )? 
    ( (LINKED_KW) => LINKED_KW L_BRACE (l=linkedOp_list { $result.LinkedOperationList = $l.result; })? R_BRACE )? 
  ;

/* NSS 15/1/05: Added syntactic predicate */
error_macro returns [ErrorMacro result = new ErrorMacro()]
    : ERROR_KW ( PARAMETER_KW ((NAME) => id=NAME 
    { 
      $result.Identifier = $id.text; 
      WantCamelCase($id);
    })? 
  t=type { $result.Subtype = $t.result; })? ;

/* SMI processing - a bunch of macros defined in RFC 1155 and RFC 2578 for SMI v1 and v2 respectively */
/* Adapted/added by Nigel Sheridan-Smith 12 January 2005 */

macros_type returns [ISmiType result]
    : om=operation_macro { $result = $om.result; }
  | em=error_macro { $result = $em.result; }
  | otm=objecttype_macro { $result = $otm.result; }
  | mm=moduleidentity_macro { $result = $mm.result; }
    | oim=objectidentity_macro { $result = $oim.result; }
  | ntm=notificationtype_macro { $result = $ntm.result; }
  | tcm=textualconvention_macro { $result = $tcm.result; }
    | ogm=objectgroup_macro { $result = $ogm.result; }
  | ngm=notificationgroup_macro { $result = $ngm.result; }
  | mcm=modulecompliance_macro { $result = $mcm.result; }
    | acm=agentcapabilities_macro { $result = $acm.result; }
  | ttm=traptype_macro { $result = $ttm.result; };

smi_macros: 'OBJECT-TYPE' | 'MODULE-IDENTITY' | 'OBJECT-IDENTITY' | 'NOTIFICATION-TYPE' 
         | 'TEXTUAL-CONVENTION' | 'OBJECT-GROUP' | 'NOTIFICATION-GROUP' | 'MODULE-COMPLIANCE' 
         | 'AGENT-CAPABILITIES' | 'TRAP-TYPE';

/* NSS 12-13/1/05: SMI types - some of these are standard 'textual conventions' which can replace BITS */
/* NSS 13/1/05: Added 'LOWER' since some PIBs can't handle it */
smi_type returns [ISmiType result]
    : 'BITS' { $result = new BitsType(); }
	| name=NAME { $result = new UnknownType($name.text); }
    ; 

/* Possible SMI types??? - IpAddress Counter32 TimeTicks Opaque Counter64 Unsigned32 */


/* SMI v2: Sub-typing - defined in RFC 1902 section 7.1 and appendix C and RFC 1904 */
smi_subtyping: L_PAREN subtype_range (BAR subtype_range)* R_PAREN
             | L_PAREN 'SIZE' L_PAREN subtype_range (BAR subtype_range)* R_PAREN R_PAREN;
subtype_range: subtype_value (DOTDOT subtype_value)? ;
subtype_value: (MINUS)? NUMBER | B_STRING | H_STRING | MAX_KW;

/* SMI v1/2 and SPPI: Object-type macro */
objecttype_macro returns [ObjectTypeMacro result = new ObjectTypeMacro()]
    : 'OBJECT-TYPE' 'SYNTAX' 
                    ( (smi_type L_BRACE) => t1=smi_type nb1=namedbits 
          {  
              $result.Syntax = $t1.result;
            $result.NamedBits = $nb1.result;
          }
                     | (smi_type) => t2=smi_type (smi_subtyping)? { $result.Syntax = $t2.result; }
                     | t3=type { $result.Syntax = $t3.result; }
                    ) 
                  ('UNITS' u1=C_STRING { $result.Units = $u1.text; })? 
                  ( ('MAX-ACCESS' { $result.AccessType = AccessType.MaxAccess; }
           | 'ACCESS' { $result.AccessType = AccessType.Access; }) 
           ma=objecttype_macro_accesstypes { $result.MibAccess = $ma.result; }
                   | 'PIB-ACCESS' pa=objecttype_macro_pibaccess { $result.PibAccess = $pa.result; })?              /* Only in SPPI; Optional */
                  ('PIB-REFERENCES' L_BRACE v1=value R_BRACE { $result.PibReference = $v1.result; })?                 /* Only in SPPI */
                  ('PIB-TAG' L_BRACE v2=value R_BRACE { $result.PibTag = $v2.result; })?                        /* Only in SPPI */
                  'STATUS' s=objecttype_macro_statustypes { $result.Status = $s.result; }
                  ( ('DESCRIPTION') => 'DESCRIPTION' c1=C_STRING { $result.Description = $c1.text; })?                               /* Optional only for SMIv1 */
                  ('INSTALL-ERRORS' L_BRACE e1=objecttype_macro_error { $result.InstallErrors.Add($e1.result); }
          (COMMA e2=objecttype_macro_error { $result.InstallErrors.Add($e2.result); })* R_BRACE)?    /* Only in SPPI */
      ( 'REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? 
      ( (~('PIB-INDEX')) => 'INDEX' i3=objecttype_macro_index { $result.MibIndex = $i3.result; }
                    | 'AUGMENTS' i4=objecttype_macro_augments { $result.MibArguments = $i4.result; }
                    | 'PIB-INDEX' L_BRACE v3=value R_BRACE { $result.PibIndex = $v3.result; }                    /* Only in SPPI */
                    | 'EXTENDS' L_BRACE v4=value R_BRACE { $result.PibExtends = $v4.result; }                       /* Only in SPPI */
                  )? 
                  ( 'INDEX' i5=objecttype_macro_index { $result.MibIndex = $i5.result; })?                       /* Only in SPPI - replicated from above */
                  ( 'UNIQUENESS' L_BRACE (v5=value { $result.UniquenessValues.Add($v5.result); }
          (COMMA v6=value { $result.UniquenessValues.Add($v6.result); })* )? R_BRACE)?                      /* Only in SPPI */
      ( ('DEFVAL') => 'DEFVAL' L_BRACE 
                    ( (L_BRACE NAME (COMMA | R_BRACE)) => b=objecttype_macro_bitsvalue { $result.DefaultValueBits = $b.result; }
                       | v7=value { $result.DefaultValue = $v7.result; }) 
                    R_BRACE )? ;

fragment objecttype_macro_accesstypes returns [Access result]
    : l=NAME {if (l.Text == ("read-only")) $result = Access.ReadOnly;
             else if (l.Text == ("read-write")) $result = Access.ReadWrite;
               else if (l.Text == ("write-only")) $result = Access.WriteOnly;
         else if (l.Text == ("read-create")) $result = Access.ReadCreate; 
               else if (l.Text == ("not-accessible")) $result = Access.NotAccessible;
         else if (l.Text == ("accessible-for-notify")) $result = Access.AccessibleForNotify;
               else {throw new SemanticException(l);}}
    ;

fragment objecttype_macro_pibaccess returns [PibAccess result]
    : l=NAME {if (l.Text == ("install")) $result = PibAccess.Install; 
               else if (l.Text == ("notify")) $result = PibAccess.Notify;
               else if (l.Text == ("install-notify")) $result = PibAccess.InstallNotify;
               else if (l.Text == ("report-only")) $result = PibAccess.ReportOnly;
               else {throw new SemanticException(l);}}
  ;

fragment objecttype_macro_statustypes returns [EntityStatus result]
    : l=NAME {if (l.Text == ("mandatory")) $result = EntityStatus.Mandatory; 
               else if (l.Text == ("optional")) $result = EntityStatus.Optional; 
               else if (l.Text == ("obsolete")) $result = EntityStatus.Obsolete;
               else if (l.Text == ("current")) $result = EntityStatus.Current;
               else if (l.Text == ("deprecated")) $result = EntityStatus.Deprecated;
               else {throw new SemanticException(l);}}
  ;


// 'typeorvaluelist' in original ASN.1 grammar between braces
objecttype_macro_index returns [IList<ISmiValue> result = new List<ISmiValue>()]
    : L_BRACE t=objecttype_macro_indextype { $result.Add($t.result); } 
  (COMMA t2=objecttype_macro_indextype { $result.Add($t2.result); })* R_BRACE
  ;       

objecttype_macro_indextype returns [ISmiValue result]
    : ('IMPLIED')? v=value { $result = $v.result; }
  ;

objecttype_macro_augments returns [ISmiValue result]
    : L_BRACE v=value R_BRACE { $result = $v.result; }
  ;  

/* NSS 13/1/05: Added LOWER *and* UPPER for a PIB */
namedbits returns [IList<NamedBit> result = new List<NamedBit>()]
    : L_BRACE n=namedbit { $result.Add($n.result); } 
  (COMMA n2=namedbit { $result.Add($n2.result); })* R_BRACE
  ;   

objecttype_macro_bitsvalue returns [IList<string> result = new List<string>()]
    : L_BRACE l=NAME 
    { 
      $result.Add($l.text); 
      WantBitName($l);
    } 
    (COMMA l2=NAME 
    { 
      $result.Add($l2.text); 
      WantBitName($l2);
    })* R_BRACE
  ;     

objecttype_macro_error returns [NamedBit result] 
    : n=namedbit { $result = $n.result; }
  ;

/* SMI v2 and SPPI: Module-identity macro */
moduleidentity_macro returns [ModuleIdentityMacro result = new ModuleIdentityMacro()]
    : 'MODULE-IDENTITY' ('SUBJECT-CATEGORIES' L_BRACE c=moduleidentity_macro_categories R_BRACE { $result.Categories = $c.result; })? /* Only in SPPI */
    'LAST-UPDATED' c1=C_STRING { $result.LastUpdate = $c1.text; }
  'ORGANIZATION' c2=C_STRING { $result.Organization = $c2.text; }
  'CONTACT-INFO' c3=C_STRING { $result.ContactInfo = $c3.text; }
    'DESCRIPTION' c4=C_STRING { $result.Description = $c4.text; }
  (m=moduleidentity_macro_revision { $result.Revisions.Add($m.result); })* ;

moduleidentity_macro_revision returns [Revision result]
    : 'REVISION' c1=C_STRING 'DESCRIPTION' c2=C_STRING { $result = new Revision($c1.text, $c2.text); }
  ; 

moduleidentity_macro_categories returns [Categories result = new Categories()]
    : l=NAME {if (l.Text ==  ("all")) $result.AllCategories = true;
           else { throw new SemanticException(l); }
      } 
    | m1=moduleidentity_macro_categoryid { $result.CategoryIds.Add($m1.result); } 
  (COMMA m2=moduleidentity_macro_categoryid { $result.CategoryIds.Add($m2.result); })*
  ;

moduleidentity_macro_categoryid returns [NamedBit result]
    : n=namedbit { $result = $n.result; }
  ;
 
/* SMI v2 and SPPI: Object-identity macro */
objectidentity_macro returns [ObjectIdentityMacro result]
    : 'OBJECT-IDENTITY' 'STATUS' s=status 
  'DESCRIPTION' c1=C_STRING { $result = new ObjectIdentityMacro($s.result, $c1.text); } 
  ('REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? ;

/* SMI v2: Notification-type macro */
notificationtype_macro returns [NotificationTypeMacro result = new NotificationTypeMacro()]
    : 'NOTIFICATION-TYPE' ('OBJECTS' L_BRACE v1=value { $result.Objects.Add($v1.result); } 
    (COMMA v2=value { $result.Objects.Add($v2.result); })* R_BRACE)? 
    'STATUS' s=status { $result.Status = $s.result; }
    'DESCRIPTION' c1=C_STRING { $result.Description = $c1.text; }
    ('REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? 
    ;

/* SMI v2 and SPPI: Textual convention */
textualconvention_macro returns [TextualConventionMacro result = new TextualConventionMacro()]
    : 'TEXTUAL-CONVENTION' ('DISPLAY-HINT' c1=C_STRING { $result.DisplayHint = $c1.text; })?
    'STATUS' s=status { $result.Status = $s.result; }
    'DESCRIPTION' c2=C_STRING { $result.Description = $c2.text; }
    ('REFERENCE' c3=C_STRING { $result.Reference = $c3.text; })? 
    'SYNTAX' ( (smi_type L_BRACE) => s2=smi_type { $result.Syntax = $s2.result; }
  L_BRACE nb1=namedbit { $result.SyntaxNamedBits.Add($nb1.result); }
            (COMMA nb2=namedbit { $result.SyntaxNamedBits.Add($nb2.result); })* R_BRACE 
      | t=type)
  ;

namedbit returns [NamedBit result]
    : name=NAME 
    { 
      $result = new NamedBit($name.text); 
      WantBitName($name);
    }
  L_PAREN (MINUS { $result.Minus = true; })? 
  num=NUMBER R_PAREN { $result.Number = long.Parse($num.text); }
  ;

/* SMI v2 and SPPI: Object group */
objectgroup_macro returns [ObjectGroupMacro result]
    : 'OBJECT-GROUP' 'OBJECTS' L_BRACE v1=value { $result = new ObjectGroupMacro($v1.result); }
  (COMMA v2=value { $result.Objects.Add($v2.result); })* R_BRACE 
    'STATUS' s=status { $result.Status = $s.result; }
  'DESCRIPTION' c1=C_STRING { $result.Description = $c1.text; }
  ('REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? 
  ;

/* SMI v2: Notification group*/
notificationgroup_macro returns [NotificationGroupMacro result]
    : 'NOTIFICATION-GROUP' 'NOTIFICATIONS' L_BRACE v1=value { $result = new NotificationGroupMacro($v1.result);}
  (COMMA v2=value { $result.Notifications.Add($v2.result); })* R_BRACE 
    'STATUS' s=status { $result.Status = $s.result; }
  'DESCRIPTION' c1=C_STRING { $result.Description = $c1.text; }
  ('REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? 
  ;

/* SMI v2 and SPPI: Module compliance */
modulecompliance_macro returns [ModuleComplianceMacro result]
    : 'MODULE-COMPLIANCE' 'STATUS' s=status
    'DESCRIPTION' c1=C_STRING { $result = new ModuleComplianceMacro($s.result, $c1.text); }
  ('REFERENCE' c2=C_STRING { $result.Reference = $c2.text; })? 
  (m=modulecompliance_macro_module { $result.Modules.Add($m.result); })+ 
  ;

status returns [EntityStatus result]
    : l=NAME {if (l.Text == ("current")) $result = EntityStatus.Current;
               else if (l.Text == ("deprecated")) $result = EntityStatus.Deprecated;
               else if (l.Text == ("obsolete")) $result = EntityStatus.Obsolete;
               else {throw new SemanticException(l);}}
    ;

modulecompliance_macro_module returns [ModuleCompliance result = new ModuleCompliance()]
    : 'MODULE' ((NAME) => name=NAME 
    { 
      $result.Name = $name.text; 
      WantModuleName($name);
    }
  ((value) => v1=value { $result.Value = $v1.result; })? )? 
    ('MANDATORY-GROUPS' L_BRACE v2=value { $result.MandatoryGroups.Add($v2.result); }
  (COMMA v3=value { $result.MandarotyGroups.Add($v3.result); })* R_BRACE)?
    (c=modulecompliance_macro_compliance { $result.Compliances.Add($c.result); })* 
  ;

modulecompliance_macro_compliance returns [Compliance result]
    : 'GROUP' v1=value 'DESCRIPTION' c1=C_STRING { $result = new GroupCompliance($v1.result, $c1.text); }
    | 'OBJECT' v2=value { $result = new ObjectCompliance($v2.result); } 
    ('SYNTAX' s=syntax { $result.Syntax = $s.result; })?
    ('WRITE-SYNTAX' s2=syntax { $result.WriteSyntax = $s2.result; })?     /* Only in SMI v2 */
    ('MIN-ACCESS' a=modulecompliance_macro_access { $result.MinAccess = $a.result; })? 
    ('PIB-MIN-ACCESS' a2=modulecompliance_macro_pibaccess { $result.PibMinAccess = $a2.result; })?    /* Only in SPPI */
    'DESCRIPTION' c2=C_STRING { $result.Description = $c2.text; }
  ;

modulecompliance_macro_access returns [Access result]
    : l=NAME {if (l.Text == ("not-accessible")) $result = Access.NotAccessible;
               else if (l.Text == ("accessible-for-notify")) $result = Access.AccessibleForNotify;
               else if (l.Text == ("read-only")) $result = Access.ReadOnly; 
               else if (l.Text == ("read-write")) $result = Access.ReadWrite;
               else if (l.Text == ("read-create")) $result = Access.ReadCreate;
               else {throw new SemanticException(l);}}
    ;

modulecompliance_macro_pibaccess returns [PibAccess result]
    : l=NAME {if (l.Text == ("not-accessible")) $result = PibAccess.NotAccessible;
               else if (l.Text == ("install")) $result = PibAccess.Install;
               else if (l.Text == ("notify")) $result = PibAccess.Notify;
               else if (l.Text == ("install-notify")) $result = PibAccess.InstallNotify;
               else if (l.Text == ("report-only")) $result = PibAccess.ReportOnly;
               else {throw new SemanticException(l);}}
  ;

/* SMI v2: Agent capabilities */
agentcapabilities_macro returns [AgentCapabilitiesMacro result]
    : 'AGENT-CAPABILITIES' 'PRODUCT-RELEASE' c1=C_STRING { $result = new AgentCapabilitiesMacro($c1.text); }
  'STATUS' s=agentcapabilities_macro_status { $result.Status = $s.result; }
  'DESCRIPTION' c2=C_STRING { $result.Description = $c2.text; }
  ('REFERENCE' c3=C_STRING)? { $result.Reference = $c3.text; }
  (m=agentcapabilities_macro_module { $result.Modules.Add($m.result); })*
  ;

agentcapabilities_macro_status returns [EntityStatus result]
    : l=NAME {if (l.Text == ("current")) $result = EntityStatus.Current;
               else if (l.Text == ("obsolete")) $result =EntityStatus.Obsolete; 
               else {throw new SemanticException(l);}};

agentcapabilities_macro_module returns [AgentCapabilitiesModule result]
    : 'SUPPORTS' name=NAME 
    { 
      $result = new AgentCapabilitiesModule($name.text); 
      WantModuleName($name);
    }
  (v1=value)? { $result.Value = $v1.result; }
    'INCLUDES' L_BRACE v2=value { $result.Includes.Add($v2.result); }
  (COMMA v3=value)* R_BRACE { $result.Includes.Add($v3.result); }
  (va=agentcapabilities_macro_variation { $result.Variations.Add($va.result); })*
  ;

agentcapabilities_macro_variation returns [Variantion result]
    : 'VARIATION' v1=value { $result = new Variantion($v1.result); }
  ('SYNTAX' s1=syntax)? { $result.Syntax = $s1.result; }
  ('WRITE-SYNTAX' s2=syntax)? { $result.WriteSyntax = $s2.result; }
  ('ACCESS' a1=agentcapabilities_macro_access)? { $result.Access = $a1.result; }
    ('CREATION-REQUIRES' L_BRACE v2=value { $result.CreationRequires.Add($v2.result); }
  (COMMA v3=value)* R_BRACE)? { $result.CreationRequires.Add($v3.result); }
    ('DEFVAL' L_BRACE ((L_BRACE (NAME | COMMA | R_BRACE)) => L_BRACE (l1=NAME 
    { 
      $result.DefaultValueIdentifiers.Add($l1.text); 
      WantCamelCase($l1);
    })?  
    (COMMA l2=NAME)* 
    { 
      $result.DefaultValueIdentifiers.Add($l2.text); 
      WantCamelCase($l2);
    }
  | v4=value) R_BRACE)? { $result.DefaultValue = $v4.result; }
    'DESCRIPTION' c1=C_STRING { $result.Description = $c1.text; }
  ;

syntax returns [Syntax result = new Syntax()]
    : (smi_type L_BRACE) => st=smi_type { $result.Subtype = $st.result; }
  L_BRACE nb1=namedbit { $result.SubtypeNamedBits.Add($nb1.result); }
  (COMMA nb2=namedbit { $result.SubtypeNamedBits.Add($nb2.result); })* R_BRACE
    | (smi_type) => st2=smi_type (smi_subtyping)? { $result.Subtype = $st2.result; }
    | t=type { $result.Subtype=$t.result; }
  ;

agentcapabilities_macro_access returns [Access result]
    : l=NAME
    {if (l.Text == ("not-implemented")) $result = Access.NotImplemented;
     else if (l.Text == ("accessible-for-notify")) $result = Access.AccessibleForNotify;
     else if (l.Text == ("read-only")) $result = Access.ReadOnly;
     else if (l.Text == ("read-write")) $result = Access.ReadWrite;
     else if (l.Text == ("read-create")) $result = Access.ReadCreate;
     else if (l.Text == ("write-only")) $result = Access.WriteOnly;
     else {throw new SemanticException(l);}}
  ;

/* SMI v1: Trap types */
traptype_macro returns [TrapTypeMacro result = new TrapTypeMacro()]
    : 'TRAP-TYPE' 'ENTERPRISE' v=value { $result.Enterprise = $v.result; }
  ('VARIABLES' L_BRACE v2=value { $result.Variables.Add($v2.result); }
  (COMMA v3=value { $result.Variables.Add($v3.result); })* R_BRACE)? 
    (('DESCRIPTION') => 'DESCRIPTION' v4=value { $result.Description = $v4.result; })? 
  ('REFERENCE' v5=value { $result.Reference = $v5.result; })? ;

operation_errorlist returns [IList<TypeOrValue> result]
    : t=typeorvaluelist { $result = $t.result; }
  ;

linkedOp_list returns [IList<TypeOrValue> result]
    : t=typeorvaluelist { $result = $t.result; }
  ;

typeorvalue returns [TypeOrValue result]
    : (type) => t=type { $result = new TypeOrValue($t.result); } 
  | v=value { $result = new TypeOrValue($v.result); };

// ERROR HERE in ASN.1 grammar? '*' was only applied to 'typeorvalue'
typeorvaluelist returns [IList<TypeOrValue> result = new List<TypeOrValue>()]
    : t1=typeorvalue { $result.Add($t1.result); } 
  (COMMA t2=typeorvalue { $result.Add($t2.result); })* 
  ;

/* NSS 15/1/05: Added syntactic predicate */
elementType returns [ElementType result]
    : t=elementType_tagged { $result = $t.result; }
    | COMPONENTS_KW OF_KW t4=type { $result = new ComponentsOfElementType($t4.result); }
  ;

elementType_tagged returns [TaggedElementType result]
    : name=NAME 
    { 
      $result = new TaggedElementType($name.text);
      WantCamelCase($name);
    }
  ((L_BRACKET (NUMBER|NAME)) => t1=tag { $result.Tag = $t1.result; })? 
    (t2=tag_default { $result.TagDefault = $t2.result; })? 
  t3=type {  $result.Subtype = $t3.result; }
  (OPTIONAL_KW { $result.Optional = true; } 
  | DEFAULT_KW { $result.Default = true; }
  v=value { $result.Value = $v.result; })? 
  ;

namedNumber returns [NamedNumber result]
    : name=NAME L_PAREN (sn=signed_number 
    { 
      $result = new NamedNumber($name.text, $sn.result); 
      WantNumberName($name);
    } 
    | dv=defined_value 
    { 
      $result = new NamedNumber($name.text, $dv.result); 
      WantNumberName($name);
    }) R_PAREN
  ;

signed_number returns [NumberLiteralValue result]
    : MINUS num=NUMBER { $result = new NumberLiteralValue(-1 * long.Parse($num.text)); }
  | num2=NUMBER { $result = new NumberLiteralValue(ulong.Parse($num2.text)); }
  ;

element_set_specs returns [ElementSetRange result = new ElementSetRange()]
    : left=element_set_spec { $result.LeftElement = $left.result; } 
   (COMMA DOTDOTDOT { $result.ContainsEllipsis = true; })? 
   (COMMA right=element_set_spec { $result.RightElement = $right.result; })? 
   ;

exception_spec returns [ExceptionSpec result]
     : EXCLAMATION 
                ( (signed_number) => sn=signed_number { $result = new ExceptionSpec($sn.result); }
                  | (defined_value) => dv=defined_value { $result = new ExceptionSpec($dv.result); }
                  | t=type COLON v=value { $result = new ExceptionSpec($t.result, $v.result); }
                );

element_set_spec returns [ConstraintElement result]
     : n=element_set_spec_normal { $result = $n.result; }
     | ALL_KW EXCEPT_KW c=constraint_elements { $result = new AllExceptConstraintElement($c.result); }
   ;

element_set_spec_normal returns [NormalConstraintElement result = new NormalConstraintElement()]
    : left=intersections 
   {  
     $result.Add(new NormalConstraintElement($left.result));
   } 
   ( ( BAR | UNION_KW ) right=intersections { $result.Add(new NormalConstraintElement($right.result)); })* 
   ;

intersections returns [ConstraintElement result = new NormalConstraintElement()]
     : c1=constraint_elements_except 
   {
       $result.Add($c1.result); 
     }   
     ( (INTERSECTION | INTERSECTION_KW) 
   c2=constraint_elements_except { $result.Add($c2.result); }
   )* ;

constraint_elements_except returns [ConstraintElement result]
     : c=constraint_elements { $result = $c.result; }
   (EXCEPT_KW c2=constraint_elements { $result.Element = $c2.result; })?;

constraint_elements returns [ConstraintElement result]
    : (value_range) => vr=value_range { $result = new ValueRangeConstraintElement($vr.result); }
    | (value) => v=value { $result = new ValueConstraintElement($v.result); }
    | SIZE_KW c=constraint { $result = new SizeConstraintElement($c.result); }
    | FROM_KW c2=constraint { $result = new FromConstraintElement($c2.result); }
    | L_PAREN e=element_set_spec R_PAREN { $result = new ElementSetConstraintElement($e.result); }
    | i=constraint_elements_includes { $result = $i.result; }
    | PATTERN_KW v2=value { $result = new PatternConstraintElement($v2.result); } 
    | WITH_KW 
      (COMPONENT_KW co1=constraint { $result = new WithComponentConstraintElement($co1.result); } 
    | cs=constraint_elements_components { $result = $cs.result; })
  ;

constraint_elements_includes returns [IncludeTypeConstraintElement result = new IncludeTypeConstraintElement()]
    : (INCLUDES_KW { $result.Includes = true; })? 
  t=type { $result.ConstraintType = $t.result; }
  ;

constraint_elements_components returns [WithComponentsConstraintElement result = new WithComponentsConstraintElement()]
    : COMPONENTS_KW L_BRACE (DOTDOTDOT COMMA { $result.Ellipsis = true; })? 
    tcl=type_constraint_list R_BRACE { $result.TypeConstraintList = $tcl.result; }
    ;

value_range returns [ValueRange result = new ValueRange()]
    : (lower=value { $result.LowerValue = $lower.result; }
  | MIN_KW { $result.MinValue = true; }) 
  (LESS { $result.LessThan = true; })? DOTDOT 
  (LESS { $result.GreaterThan = true; })? 
  (upper=value { $result.UpperValue = $upper.result; } | 
  MAX_KW { $result.MaxValue = true; }) 
  ;

type_constraint_list returns [IList<ConstraintElement> result = new List<ConstraintElement>()]
    : nc1=named_constraint { $result.Add($nc1.result); } 
  (COMMA nc2=named_constraint { $result.Add($nc2.result); })* 
  ;

named_constraint returns [NamedConstraintElement result]
    : name=NAME 
    { 
      $result = new NamedConstraintElement($name.text); 
      WantConstraintName($name);
    }
  (c=constraint { $result.Constraint = $c.result; })? 
  (PRESENT_KW { $result.Present = true; }
  | ABSENT_KW { $result.Absent = true; }
  | OPTIONAL_KW { $result.Optinal = true; })? 
  ;

choice_value returns [ChoiceValue result = new ChoiceValue()]
    : name=NAME 
    { 
      $result.Name = $name.text; 
      WantChoiceName($name);
    } 
  (COLON { $result.ContainsColon = true; })? 
  v=value { $result.Value = $v.result; }
  ;

sequence_value returns [SequenceValue result = new SequenceValue()]
    : L_BRACE (nv1=named_value { $result.Add($nv1.result); })? 
  (COMMA nv2=named_value { $result.Add($nv2.result); })* 
  R_BRACE
  ;

sequenceof_value returns [SequenceOfValue result = new SequenceOfValue()]
    : L_BRACE (v1=value { $result.Add($v1.result); })? 
  (COMMA v2=value { $result.Add($v2.result); })* 
  R_BRACE
  ;

cstr_value returns [ISmiValue result]
    : (H_STRING) => h=H_STRING 
  { 
      $result = new HexLiteralValue($h.text); 
    }
    | (B_STRING) => b=B_STRING
  {
      $result = new BinaryLiteralValue($b.text);
  } 
    | L_BRACE 
            ( (id_list) => id=id_list { $result = $id.result; }
              | (char_defs_list) => ch=char_defs_list { $result = $ch.result; }
              | tu=tuple_or_quad { $result = $tu.result; }        
            ) R_BRACE;

id_list returns [IdListValue result = new IdListValue()]
    : name1=NAME 
    { 
      $result.Add($name1.text); 
      WantIdName($name1);
    }
    (COMMA name2=NAME 
    { 
      $result.Add($name2.text); 
      WantIdName($name2);
    })* 
  ;

char_defs_list returns [CharDefinitionListValue result = new CharDefinitionListValue()]
    : ch1=char_defs { $result.Add($ch1.result); }
  (COMMA ch2=char_defs { $result.Add($ch2.result); })* 
  ;

//ERROR: no R_BRACE required here
tuple_or_quad returns [ISmiValue result]
    : tu=tuple_number { $result = $tu.result; }
  | qu=quad_number { $result = $qu.result; }
  ;

tuple_number returns [TupleValue result]
    : sn1=signed_number COMMA sn2=signed_number { $result = new TupleValue($sn1.result, $sn2.result); }
  ;

quad_number returns [QuadValue result]
    : sn1=signed_number COMMA sn2=signed_number COMMA sn3=signed_number COMMA sn4=signed_number
  { $result = new QuadValue($sn1.result, $sn2.result, $sn3.result, $sn4.result); }
  ;

char_defs returns [CharDefinition result]
    : 
  cs=C_STRING { $result = new CharDefinition($cs.text); }
    | L_BRACE tu=tuple_or_quad R_BRACE { $result = new CharDefinition($tu.result); }
    | dv=defined_value { $result = new CharDefinition($dv.result); };
//char_defs: C_STRING 
//         | L_BRACE signed_number COMMA signed_number ( R_BRACE | COMMA signed_number COMMA signed_number R_BRACE ) 
//         | defined_value;

named_value returns [NamedValue result]
    : name=NAME v=value 
    { 
      $result = new NamedValue($name.text, $v.result); 
      WantCamelCase($name);
    }
  ;

full_qualified_value returns [FullQualifiedValue result = new FullQualifiedValue()]
  : (part=NAME { $result.Add($part.text); } DOT)+ part2=NAME { $result.Add($part2.text); }
  ;