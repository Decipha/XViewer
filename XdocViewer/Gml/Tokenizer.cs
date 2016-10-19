using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Evolution.X.Utility.ExpressionParser
{
    /// <summary>
    /// breaks the expression into individual string tokens.
    /// </summary>
    public class StringTokenizer : IEnumerable<String>
    {
        /// <summary>
        /// characters to split on
        /// </summary>
        private char[] seperators = "/=%*+-><!&|^, []'\"#.".ToCharArray();

        /// <summary>
        /// characters that indicate a literal token:
        /// </summary>
        private char[] literals = ":'\"[]#".ToCharArray();

        /// <summary>
        /// characters that are returned as a single token:
        /// </summary>
        private char[] singles = ",().".ToCharArray();

        /// <summary>
        /// the expression being tokenized.
        /// </summary>
        private string _expression = null;

        /// <summary>
        /// queue of characters
        /// </summary>
        private Queue<char> _input = new Queue<char>();

        /// <summary>
        /// constructor; protected;
        /// </summary>
        /// <param name="expression"></param>
        protected StringTokenizer(string expression)
        {
            _expression = expression;
        }

        /// <summary>
        /// determines if the character is a single-character token.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool IsSingleCharToken(char c)
        {
            return singles.Contains(c);
        }

        /// <summary>
        /// determines if the character is a literal indicator (eg, ", ', # [, ] etc...)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool IsLiteralIndicator(char c)
        {
            return literals.Contains(c);
        }

        /// <summary>
        /// determines if the character is a seperator.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool IsSeperator(char c)
        {
            return seperators.Contains(c);
        }

        /// <summary>
        /// break the string into an enumeration of string tokens.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Tokenize()
        {
            // input characters onto the queue:
            QueueCharacters();

            // the current token will be kept in this string buffer:
            var current = new StringBuilder();

            // while there are characters remaining:
            while (_input.Count > 0)
            {
                // dequeue the next character:
                char c = _input.Dequeue();

                // is this a single character token?
                if (IsSingleCharToken(c))
                {
                    if (current.Length > 0)
                    {
                        // handle numeric literals with floating points:
                        if (c == '.' && current.ToString().IsNumericLiteral())
                        {
                            // continue appending to the current numeric literal token.
                            current.Append(c);
                            continue;
                        }
                        else
                        {
                            // return the current buffer:
                            yield return current.ToString();
                        }

                        // clear the buffer
                        current.Clear();
                    }

                    // return the single character as a string token
                    yield return c.ToString();

                    // continue to next token
                    continue;
                }

                //
                if (IsLiteralIndicator(c))
                {
                    // yeild anything in the current buffer as a token:
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }

                    yield return ConsumeLiteral(c);
                    continue;
                }
                else
                {
                    if (IsSeperator(c))
                    {
                        if (current.Length > 0)
                        {

                                yield return current.ToString();
                                current.Clear();
                            
                        }
                        yield return c.ToString();
                        continue;
                    }
                }
                if (char.IsWhiteSpace(c))
                {
                    yield return c.ToString();
                }
                else
                { 
                    current.Append(c);
                }
            }
            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }

        /// <summary>
        /// load the characters from the expression onto the input queue.
        /// </summary>
        private void QueueCharacters()
        {
            _input.Clear();
            foreach (var c in _expression)
                _input.Enqueue(c);
        }

        /// <summary>
        /// consume a literal delineated by the specified character.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private string ConsumeLiteral(char c)
        {
            var buffer = new StringBuilder().Append(c);

            // get the compliment character:
            char comp = c.GetCompliment();

            // consume characters from the input queue until the compliment character is found.
            while (_input.Count > 0)
            {
                // dequeue the next character
                char d = _input.Dequeue();
                // if this is the compliment character
                if (d == comp)
                {
                    // is the next character also the same?
                    if (_input.Count > 0 && _input.Peek() == d)
                    {
                        // this is a double delimiter (escaped), discard the next character and treat the current as a literal.
                        _input.Dequeue();
                    }
                    else
                        // reached the end of the literal
                        break;
                }
                // append the character to the literal
                buffer.Append(d);
            }

            // we want the token to keep it's delimiters
            buffer.Append(comp);

            // return the buffer as a string:
            return buffer.ToString().Trim();
        }

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return Tokenize().GetEnumerator();
        }

        #endregion IEnumerable<string> Members

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Tokenize().GetEnumerator();
        }

        #endregion IEnumerable Members

        /// <summary>
        /// splits the specified string into tokens and returns an array.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string[] Split(string expression)
        {
            return new StringTokenizer(expression).ToArray();
        }

        public static void Test()
        {
            StringTokenizer st = new StringTokenizer(File.ReadAllText(@"C:\TEMP\0009258.xml"));
            List<string> tokens = new List<string>();
            foreach (var token in st)
            {
                tokens.Add(token);
                Console.WriteLine(token);
            }
            Console.Write(tokens.Count);
        }
    }


}


