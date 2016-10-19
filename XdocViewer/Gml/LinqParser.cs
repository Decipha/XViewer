using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Diagnostics;

namespace Evolution.X.Utility.Dynamic
{
    #region Parser Router

    /// <summary>
    /// uses compiled expressions to quickly access the Parse method of any type (if there is one)
    /// eg: <see cref="int.Parse"/>, <see cref="DateTime.Parse"/> etc
    ///
    /// provides a fast method of parsing strings to specific Type values (assuming they have a Parse method)- also provides a mechanism to provide bespoke parse methods for any type, via the RegisterParser method.
    /// this could be to provide a parse method for a new Type, or to override the default parse behaviour for a given type.
    /// by default the system adds a custom boolean parser that accepts, "1", "Yes", "Y", "True" etc.
    /// also it adds a byte[] parser to return byte arrays from base64 strings.
    /// 
    /// </summary>
    public class LinqParser
    {

        #region Fields

        /// <summary>
        /// dictionary of compile delegates stored against the type;
        /// </summary>
        private Dictionary<Type, Delegate> _parserCache = new Dictionary<Type, Delegate>();

        #endregion Fields

        #region Constructor

        /// <summary>
        /// constructor is protected to support the singleton pattern.
        /// </summary>
        protected LinqParser()
        {
            // some default parse methods registered by default:
            RegisterParser<byte[]>(ParseBytes);
            RegisterParser<bool>(ParseBoolExtended);
        }

        #endregion Constructor

        #region Custom Parsers

        /// <summary>
        /// parses a string to an array of bytes (UU-DECODE)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static byte[] ParseBytes(string s)
        {
            return Convert.FromBase64String(s);
        }

        /// <summary>
        /// parse a "1","Y","true" for true, or "0","N","False" for false, anything else generates an exception.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool ParseBoolStrict(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ApplicationException("Invalid Boolean Value (empty string)");

            switch (s.ToUpper())
            {
                case "TRUE":
                case "1":
                case "-1":
                case "Y":
                    return true;

                case "0":
                case "N":
                case "FALSE":
                    return false;
            }
            throw new ApplicationException("Invalid Boolean Value '" + s + "'");
        }

        /// <summary>
        /// parses the string: anything recognized as true (eg, "Y", "Yes", "1","True", "t" etc) is returned as true, anything else is false.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool ParseBoolExtended(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            else
            {
                switch (s.ToLower())
                {
                    case "y":
                    case "yes":
                    case "true":
                    case "t":
                    case "1":
                        return true;

                    default:
                        return false;
                }
            }
        }

        #endregion

        #region Private Method(s)

        /// <summary>
        /// this is the internal Parse method.
        /// uses a build-on-access cache of compiled expressions. Whenever a new type is used, it's methods are queried by linq for a "Parse" method.
        /// if one is found, a <see cref="MethodCallExpression"/> is created to access the Parse method, and this is compiled to a <see cref="Func&lt;string,T&gt;"/> using a <see cref="LambdaExpression"/> and stored in cache;
        ///
        /// The string value is then passed in to the compiled delegate and the result returned;
        /// The end result is as just about as fast as if the <see cref="DateTime.Parse"/>/<see cref="int.Parse"/> etc, methods were called directly.
        /// </summary>
        /// <typeparam name="T">
        /// the type of object to parse to
        /// </typeparam>
        /// <param name="value">
        /// the string value to parse;
        /// </param>
        /// <returns>
        /// the correct value
        /// </returns>
        /// <exception cref="InvalidCastException">if the data cannot be parsed, cast or converted</exception>
        private T Parse<T>(string value)
        {
            // when creating lambda expressions, types must match exactly, hence the generic parameter on the delegate;
            Func<string, T> parser = null;

            // the cache is for multiple, different types, so it stores an abstract delegate type:
            Delegate cacheItem = null;

            // get the requested generic type:
            Type t = typeof(T); bool fromCache = false;

            // try to find an existing delegate in the cache:
            if (!_parserCache.TryGetValue(t, out cacheItem))
            {
                // not found - need to create the parser delegate;
                // use linq to find the Parse method (if any)
                var parseMethod = (from method in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                                  where method.IsStatic 
                                     && method.Name.Equals("Parse", StringComparison.OrdinalIgnoreCase) 
                                     && method.GetParameters().Count() == 1
                                 select method).FirstOrDefault();

                // was a method returned:
                if (parseMethod != null)
                {
                    // if 'method' is not null, then a static Parse(string s) method has been found on the type requested;
                    try
                    {
                        // the method has a single string parameter called "s"
                        var s = Expression.Parameter(typeof(string), "stringValue");

                        // create the method-call expression to call the T.Parse() method:
                        var c = Expression.Call(parseMethod, s);

                        // create the lambda expression that will invoke the call expression:
                        var l = Expression.Lambda<Func<string,T>>(c, s);

                        // compile the lambda expression and return a delegate to access the code:
                        parser = l.Compile();

                        // store it in cache:
                        _parserCache[t] = parser;
                    }
                    catch (Exception lambdaExpressionException)
                    {
                        // write to the console the reason why the lambda expression could not be created:
                        Console.WriteLine(lambdaExpressionException.ToString());
                    }
                }
                else
                {
                    // maybe the type has a constructor with a single string argument?
                    // that might do it.
                    var ctor = (from ct in t.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                               where ct.GetParameters().Count() == 1
                                  && ct.GetParameters().First().ParameterType.Equals(typeof(string))
                              select ct).FirstOrDefault();
                    
                    // was a relevent constructor found?
                    if (ctor != null)
                    {
                        try
                        {
                            // define the string parameter:
                            var s = Expression.Parameter(typeof(string), "stringValue");

                            // define the call to the constructor:
                            var c = Expression.New(ctor, s);

                            // create the lambda expression:
                            var l = Expression.Lambda<Func<string, T>>(c, s);

                            // compile the expression to the delegate signature:
                            parser = l.Compile();

                            // add to the parser cache:
                            _parserCache[t] = parser;
                        }
                        catch (Exception lambdaExpressionException)
                        {
                            // write to the console the reason why the lambda expression could not be created:
                            Console.WriteLine(lambdaExpressionException.ToString());
                        }
                    }
                }
            }
            else
            {
                // cache hit;
                fromCache = true;

                // convert the cache-item to the generic parser type:
                parser = cacheItem as Func<string, T>;
               
            }

            // did we get a parser?
            if (parser != null)
            {
                // invoke the parse method:
                T parsed = parser.Invoke(value);

                // return the result:
                return parsed;
            }
            else
            {
                if (fromCache)
                {
                    try
                    {
                        // try to use dynamic invoke;
                        object result = cacheItem.DynamicInvoke(value);
                        if (result is T)
                        {
                            return (T)result;
                        }
                    }
                    catch
                    {

                        // ignore exception;
                        Debug.Print("Unable to dynamic invoke item found in parser cache. Fallback to Convert...");
                    }
                }
            }

            // attempt to do the conversion using Convert as a fallback:
            return (T)Convert.ChangeType(value, t);
        }

        /// <summary>
        /// this is the internal Parse method.
        /// uses a build-on-access cache of compiled expressions. Whenever a new type is used, it's methods are queried by linq for a "Parse" method.
        /// if one is found, a <see cref="MethodCallExpression"/> is created to access the Parse method, and this is compiled to a <see cref="Func&lt;string,T&gt;"/> using a <see cref="LambdaExpression"/> and stored in cache;
        ///
        /// The string value is then passed in to the compiled delegate and the result returned;
        /// The end result is as just about as fast as if the <see cref="DateTime.Parse"/>/<see cref="int.Parse"/> etc, methods were called directly.
        /// </summary>
        /// <param name="t">
        /// the type of object to parse to
        /// </typeparam>
        /// <param name="value">
        /// the string value to parse;
        /// </param>
        /// <returns>
        /// the value parsed to the type of t.
        /// </returns>
        /// <exception cref="InvalidCastException">if the data cannot be parsed, cast or converted</exception>
        private object Parse(string value, Type t)
        {
            // don't bother converting string-string.
            if (t.Equals(typeof(string)))
                return value;

            // when creating lambda expressions, types must match exactly, hence the generic parameter on the delegate;
            Delegate parser = null;

            // try to find an existing delegate in the cache:
            if (!_parserCache.TryGetValue(t, out parser))
            {
                // not found - need to create the parser delegate;
                // use linq to find the Parse method (if any)
                var method = (from member in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                             where member.IsStatic && member.Name.Equals("Parse") && member.GetParameters().Count() == 1
                            select member).FirstOrDefault();

                // was a method returned:
                if (method != null)
                {
                    // if 'method' is not null, then a static Parse(string s) method has been found on the type requested;
                    try
                    {
                        // the method has a single string parameter called "s"
                        var s = Expression.Parameter(typeof(string), "stringValue");

                        // create the method-call expression to call the T.Parse() method:
                        var call = Expression.Call(method, s);

                        // create the lambda expression that will invoke the call expression:
                        var lambda = Expression.Lambda(call, s);

                        // compile the lambda expression and return a delegate to access the code:
                        parser = lambda.Compile();  

                        // store it in cache:
                        _parserCache[t] = parser;
                    }
                    catch (Exception lambdaExpressionException)
                    {
                        // write to the console the reason why the lambda expression could not be created:
                        Console.WriteLine(lambdaExpressionException.ToString());
                    }
                }
                else
                {
                    // maybe the type has a constructor with a single string argument?
                    // that might be just as good.
                    var ctor = (from ct in t.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                               where ct.GetParameters().Count() == 1 && ct.GetParameters().First().ParameterType.Equals(typeof(string))
                              select ct).FirstOrDefault();

                    if (ctor != null)
                    {
                        try
                        {
                            // define the string parameter:
                            var sParam = Expression.Parameter(typeof(String), "stringValue");

                            // define the call to the constructor:
                            var creator = Expression.New(ctor, sParam);

                            // create the lambda expression:
                            var lambda = Expression.Lambda(creator, sParam);

                            // compile the expression to the delegate signature:
                            parser = lambda.Compile();

                            // add to the parser cache:
                            _parserCache[t] = parser;
                        }
                        catch (Exception lambdaExpressionException)
                        {
                            // write to the console the reason why the lambda expression could not be created:
                            Console.WriteLine(lambdaExpressionException.ToString());
                        }
                    }
                }
            }

            // did we get a parser?
            if (parser != null)
            {
                // invoke the parse method:
                object parsed = parser.DynamicInvoke(value);

                // return the result:
                return parsed;
            }

            // attempt to do the conversion using Convert as a fallback:
            return Convert.ChangeType(value, t);
        }

        #endregion Private Method(s)

        #region Public Method(s)

        /// <summary>
        /// registers a parse method against the type.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="method"></param>
        public void RegisterParser(Type t, Delegate method)
        {
            _parserCache[t] = method;
        }

        /// <summary>
        /// registers a generic parse method against the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        public void RegisterParser<T>(Func<string, T> method)
        {
            _parserCache[typeof(T)] = method;
        }
        
        /// <summary>
        /// presents a TryParse method with generics for any type that has a static Parse(string s) method, eg <see cref="DateTime.Parse"/> <see cref="Int32.Parse"/> <see cref="double.Parse"/>
        /// </summary>
        /// <typeparam name="T">the target data type</typeparam>
        /// <param name="value">the string value to parse</param>
        /// <param name="result">the output parsed value</param>
        /// <returns>true: parse successful, false: parse failed</returns>
        public bool TryParse<T>(string value, out T result)
        {
            try
            {
                result = Parse<T>(value);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// non-generic try-parse method, for when the type won't be known until runtime.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="t"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryParse(string value, Type t, out object result)
        {
            try
            {
                result = Parse(value, t);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        #endregion Public Method(s)

        #region Singleton

        /// <summary>
        /// the default instance of the parser; this is a singleton instance to ensure the caching operations
        /// arent wasted.
        /// </summary>
        private static LinqParser _def = null;

        /// <summary>
        /// the locking object.
        /// </summary>
        private static object _locker = new object();

        /// <summary>
        /// thread-safe, singleton instance of the parser;
        /// </summary>
        public static LinqParser Default
        {
            get
            {
                lock (_locker)
                {
                    if (_def == null)
                        _def = new LinqParser();
                }
                return _def;
            }
        }

        #endregion Singleton
    }

    #endregion Parser Router

    public static class LinqParserExtensions
    {
        public static bool TryParse<T>(this string value, out T result)
        {
            return LinqParser.Default.TryParse<T>(value, out result);
        }

        public static bool TryParse(this string value, Type t, out object result)
        {
            return LinqParser.Default.TryParse(value, t, out result);
        }

        public static T Parse<T>(this string value)
        {
            T result;
            if (LinqParser.Default.TryParse<T>(value, out result))
            {
                return result;
            }
            else
            {
                throw new ApplicationException($"Cannot parse {value} as {typeof(T).Name}");
            }
        }

        public static object Parse(this string value, Type asType)
        {
            object result;
            if (LinqParser.Default.TryParse(value, asType, out result))
            {
                return result;
            }
            else
            {
                throw new ApplicationException($"Cannot parse {value} as {asType.Name}");
            }
        }
    }
}