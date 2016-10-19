using Evolution.X.Utility.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evolution.X.Utility.ExpressionParser
{
    /// <summary>
    /// extension methods
    /// </summary>
    public static class ExpressionExtensionMethods
    {
        /// <summary>
        /// enumerates the types available in the assembly (and refs)
        /// </summary>
        /// <param name="startingAssembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> Types(this Assembly startingAssembly)
        {
            foreach (var type in startingAssembly.GetTypes())
                yield return type;

            foreach (var reference in startingAssembly.GetReferencedAssemblies())
            {
                var sub = Assembly.Load(reference);
                foreach (var type in sub.GetTypes())
                    yield return type;
            }
        }

        /// <summary>
        /// yeilds an enumeration of enumerations.
        /// takes the sequence of items of <typeparamref name="T"/> and groups them together between the seperator; 
        /// eg: where (X) is the seperator:
        /// {1,2,3,4,X,5,6,7,8}
        /// returns:
        ///     {1,2,3,4}
        ///     {5,6,7,8}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">an enumeration of items of type <typeparamref name="T"/></param>
        /// <param name="seperator">the item to seperate on, must be equatable to <typeparamref name="T"/></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Seperate<T>(this IEnumerable<T> items, IEquatable<T> seperator)
        {
            var list = new List<T>();

            // enumerate the items
            foreach (T item in items)
            {
                // whenever we encounter a seperator, yeild the current list and reset it
                if (seperator.Equals(item))
                {
                    // ignore empty lists;
                    if (list.Count > 0)
                    {
                        yield return list;
                        list = new List<T>();
                    }
                }
                else
                {
                    list.Add(item);
                }
            }

            // don't assume there is a seperator at the end: yield the remaining items (if any)
            if (list.Count > 0)
                yield return list;
        }

        /// <summary>
        /// filters the elements of the input enumerable by type;
        /// </summary>
        /// <typeparam name="I">
        /// input element type</typeparam>
        /// <typeparam name="O">
        /// output element type
        /// </typeparam>
        /// <param name="input">
        /// </param>
        /// <returns></returns>
        public static IEnumerable<O> TypeFilter<I, O>(this IEnumerable<I> input) where O : class
        {
            return (from x in input let y = x as O where y != null select y);
        }

        /// <summary>
        /// determines if the token is a literal representation of a number.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsNumericLiteral(this string token)
        {
            Type t;
            return IsNumeric(token, out t);
        }

        /// <summary>
        /// determines if the string is numeric and if so, what the closest type was. utilizes trailing character as a type inference hint:
        /// <list type="bullet">
        ///     <item>trailing b or B = byte</item>
        ///     <item>trailing i or I = Integer (32 bit)</item>
        ///     <item>trailing l or L = Integer (64 bit) (long)</item>
        ///     <item>trailing d or D = Double Precision Floating Point number (double)</item>
        ///     <item>trailing f or F = Single Precision Floating Point number (float)</item>
        /// </list>
        /// </summary>
        /// <param name="token"></param>
        /// <param name="bestNumericType"></param>
        /// <returns></returns>
        public static bool IsNumeric(string token, out Type bestNumericType)
        {
            byte b;
            int i;
            long l;
            float f;
            double d;

            // get the last character:
            char last = token[token.Length - 1];

            // look for a type inference guide:
            switch (last)
            {
                case 'b':
                case 'B':
                    bestNumericType = typeof(byte);
                    return byte.TryParse(token, out b);

                case 'i':
                    bestNumericType = typeof(int);
                    return int.TryParse(token, out i);

                case 'l':
                case 'L':
                    bestNumericType = typeof(long);
                    return long.TryParse(token, out l);

                case 'd':
                case 'D':
                    bestNumericType = typeof(double);
                    return double.TryParse(token, out d);

                case 'F':
                case 'f':
                    bestNumericType = typeof(float);
                    return float.TryParse(token, out f);

                default:

                    if (int.TryParse(token, out i))
                    {
                        bestNumericType = typeof(int);
                        return true;
                    }
                    if (float.TryParse(token, out f))
                    {
                        bestNumericType = typeof(float);
                        return true;
                    }
                    if (double.TryParse(token, out d))
                    {
                        bestNumericType = typeof(double);
                        return true;
                    }
                    break;
            }

            bestNumericType = typeof(object);

            return false;
        }

        /// <summary>
        /// returns an object that is the result of parsing the specified token to the best type.
        /// </summary>
        /// <param name="token">the string value to parse to a number</param>
        /// <param name="ignoreError">
        /// if false, this method will throw an argument exception if <paramref name="token"/> cannot be parsed to a number
        /// </param>
        /// <returns></returns>
        public static object ParseNumeric(this string token, bool ignoreError = true)
        {
            Type type;
            if (IsNumeric(token, out type))
            {
                if (!Char.IsNumber(token, token.Length - 1))
                    return token.Substring(0, token.Length - 1).Parse(type);
                else
                    return token.Parse(type);
            }
            if (!ignoreError)
            {
                throw new ArgumentException(nameof(token), $"'{token}' is not a number");
            }
            else
                return 0;
        }

        /// <summary>
        /// is the character an open punctuation mark
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsOpen(this char c)
        {
            return "{[(<".Contains(c);
        }

        /// <summary>
        /// is the character close punctuation
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsClose(this char c)
        {
            return ">}])".Contains(c);
        }

        /// <summary>
        /// is the character one that is used to delimit literal values?
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsLiteralDelimiter(this char c, out DelimiterType dt)
        {
            if (IsDelimiter(c, out dt))
            {
                switch (dt)
                {
                    case DelimiterType.Text:
                    case DelimiterType.Date:
                    case DelimiterType.FieldOrColumn:
                    case DelimiterType.Block:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// determines if a specified character <paramref name="c"/> is considered a delimiter, and what type of delimiter it is.
        /// dates are delimited by "#", text is delimited with " or ', fields/properties are delimited by [], blocks delimited by {}
        /// </summary>
        /// <param name="c">
        /// the character to test
        /// </param>
        /// <param name="dt">
        /// outputs the delimiter type, <see cref="DelimiterType"/>
        /// </param>
        /// <returns></returns>
        public static bool IsDelimiter(this char c, out DelimiterType dt)
        {
            if (c.IsBlockDelimiter())
            {
                dt = DelimiterType.Block;
                return true;
            }
            if (c.IsColumnDelimiter())
            {
                dt = DelimiterType.FieldOrColumn;
                return true;
            }
            if (c.IsDateDelimiter())
            {
                dt = DelimiterType.Date;
                return true;
            }
            if (c.IsParamDelimiter())
            {
                dt = DelimiterType.Parameter;
                return true;
            }
            if (c.IsRowDelimiter())
            {
                dt = DelimiterType.Row;
                return true;
            }
            if (c.IsTextDelimiter())
            {
                dt = DelimiterType.Text;
                return true;
            }
            if (Char.IsWhiteSpace(c))
            {
                dt = DelimiterType.Whitespace;
                return true;
            }

            dt = DelimiterType.None;
            return false;
        }

        /// <summary>
        /// gets the character's delimiter type.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static DelimiterType GetDelimiterType(this char c)
        {
            DelimiterType dt;
            if (c.IsDelimiter(out dt))
                return dt;
            else
                return DelimiterType.None;
        }

        /// <summary>
        /// returns the complimenting character; eg "{" returns "}", "(" returns ")", if the input character has no compliment, the return value will be the input value.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static char GetCompliment(this char c)
        {
            switch (c)
            {
                case '{':
                    return '}';
                case '[':
                    return ']';
                case '(':
                    return ')';
                case '<':
                    return '>';

                case '}':
                    return '{';
                case ']':
                    return '[';
                case ')':
                    return '(';
                case '>':
                    return '<';

                case ':':
                    return ' ';

                default:
                    return c;
            }
        }

        /// <summary>
        /// determines if the character is classified as a date-delimiter.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsDateDelimiter(this char c)
        {
            return "#".Contains(c);
        }

        /// <summary>
        /// determines if the character is classified as a column delimiter.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsColumnDelimiter(this char c)
        {
            return "[]".Contains(c);
        }

        /// <summary>
        /// determines if the character is classified as a block delimiter.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsBlockDelimiter(this char c)
        {
            return "{}".Contains(c);
        }

        /// <summary>
        /// is this a text-delimiter.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsTextDelimiter(this char c)
        {
            return "'\"".Contains(c);
        }

        /// <summary>
        /// is this a row-delimiter?
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsRowDelimiter(this char c)
        {
            return "\r\n".Contains(c);
        }

        /// <summary>
        /// is this a parameter/list delimiter?
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsParamDelimiter(this char c)
        {
            return ",".Contains(c);
        }

        /// <summary>
        /// is the specified character considered punctuation?
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsPunctuation(this char c)
        {
            if (char.IsWhiteSpace(c))
                return true;
            if (c.IsOpen())
                return true;
            if (c.IsClose())
                return true;
            if (c.IsTextDelimiter())
                return true;
            if (c.IsParamDelimiter() || c.IsRowDelimiter())
                return true;

            return false;
        }

        /// <summary>
        /// is the delimiter type for a literal?
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsForLiteral(this DelimiterType dt)
        {
            switch (dt)
            {
                case DelimiterType.Date:
                case DelimiterType.Text:
                case DelimiterType.FieldOrColumn:
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// possible types of delimiter.
    /// </summary>
    public enum DelimiterType
    {
        /// <summary>
        /// not a delimiter
        /// </summary>
        None,

        /// <summary>
        /// indicates whitespace;
        /// </summary>
        Whitespace,

        /// <summary>
        /// a delimiter that indicates literal text
        /// </summary>
        Text,

        /// <summary>
        /// a delimiter that indicates literal date
        /// </summary>
        Date,

        /// <summary>
        /// a delimiter that indicates a field or property/ database column (eg [ or ])
        /// </summary>
        FieldOrColumn,

        /// <summary>
        /// a delimiter that ends a row. (eg, Cr/Lf)
        /// </summary>
        Row,

        /// <summary>
        /// a delimiter that ends a block (eg, ";");
        /// </summary>
        Block,

        /// <summary>
        /// a delimiter that seperates parameters of a function or method (typically a comma)
        /// </summary>
        Parameter
    }
}