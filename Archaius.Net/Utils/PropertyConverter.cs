using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Archaius.Utils
{
    public static class PropertyConverter
    {
        #region [Constants]
        /// <summary>
        /// Constant for the list delimiter as char.
        /// </summary>
        public const char ListEscapeChar = '\\';

        /// <summary>
        /// Constant for the list delimiter escaping character as string.
        /// </summary>
        public const string ListEscape = "\\";

        /// <summary>
        /// Constant for the prefix of hex numbers.
        /// </summary>
        private const string HexPrefix = "0x";

        /// <summary>
        /// Constant for the radix of hex numbers.
        /// </summary>
        private const int HexRadix = 16;

        /// <summary>
        ///  Constant for the prefix of binary numbers.
        /// </summary>
        private const string BinaryPrefix = "0b";

        /// <summary>
        /// Constant for the radix of binary numbers.
        /// </summary>
        private const int BinaryRadix = 2;
        #endregion

        /// <summary>
        /// Convert the specified object into a bool.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToBoolean(object obj, out bool value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToBoolean(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = false;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a byte.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToByte(object obj, out byte value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToByte(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a short.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToShort(object obj, out short value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToInt16(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into an int.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToInt(object obj, out int value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToInt32(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a long.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToLong(object obj, out long value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToInt64(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a decimal.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToDecimal(object obj, out decimal value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToDecimal(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a float.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToFloat(object obj, out float value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToSingle(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a double.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <param name="value">The converted value</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static bool ToDouble(object obj, out double value)
        {
            if (obj is IConvertible)
            {
                try
                {
                    value = Convert.ToDouble(obj);
                    return true;
                }
                catch
                {
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Convert the specified object into a string.
        /// </summary>
        /// <param name="obj">The value to convert</param>
        /// <returns>Whether the conversion succeeds or not.</returns>
        public static string ToString(object obj)
        {
            return obj != null ? obj.ToString() : null;
        }

        /// <summary>
        /// Returns a collection with all values contained in the specified object.
        /// This method is used for instance by the <see cref="IConfiguration.AddProperty"/>
        /// implementation of the default configurations to gather all values of the
        /// property to add. Depending on the type of the passed in object the
        /// following things happen:
        /// <ul>
        /// <li>Strings are checked for delimiter characters and split if necessary.</li>
        /// <li>For objects implementing the <see cref="IEnumerable"/> interface, the 
        /// corresponding <see cref="IEnumerable"/> is obtained, and contained elements
        /// are added to the resulting collection.</li>
        /// <li>Arrays are treated as <see cref="IEnumerable"/> objects.</li>
        /// <li>All other types are directly inserted.</li>
        /// <li>Recursive combinations are supported, e.g. a collection containing
        /// an array that contains strings: The resulting collection will only
        ///  contain primitive objects (hence the name &quot;flatten&quot;).</li>
        /// </ul>
        /// </summary>
        /// <param name="value">The value to be processed</param>
        /// <param name="delimiter">The delimiter for string values</param>
        /// <returns>a "flat" collection containing all primitive values of
        ///   the passed in object</returns>
        public static ICollection Flatten(object value, char delimiter)
        {
            var result = new List<Object>();
            if (value is string)
            {
                var s = (string)value;
                if (s.IndexOf(delimiter) > 0)
                {
                    result.AddRange(Split(s, delimiter));
                }
                else
                {
                    result.Add(value);
                }
            }
            else if (value is IEnumerable)
            {
                foreach (var elem in (IEnumerable)value)
                {
                    result.AddRange(Flatten(elem, delimiter).Cast<object>());
                }
            }
            else if (value != null)
            {
                result.Add(value);
            }
            return result;
        }

        /// <summary>
        /// Split a string on the specified delimiter.
        /// </summary>
        /// <param name="s">The string to split</param>
        /// <param name="delimiter">The delimiter</param>
        /// <param name="trim">A flag whether the single elements should be trimmed</param>
        /// <returns></returns>
        public static IList<string> Split(string s, char delimiter, bool trim = false)
        {
            if (s == null)
            {
                return new string[0];
            }

            var tokens = new List<string>();
            var token = new StringBuilder();
            var inEscape = false;
            for (int i = 0, len = s.Length; i < len; ++i)
            {
                var c = s[i];
                if (inEscape)
                {
                    // Last character was the escape marker.
                    // Can current character be escaped?
                    if (c != delimiter && c != ListEscapeChar)
                    {
                        // No, also add escape character
                        token.Append(ListEscapeChar);
                    }
                    token.Append(c);
                    inEscape = false;
                }
                else
                {
                    if (c == delimiter)
                    {
                        // Found a list delimiter -> add token and reset the buffer
                        tokens.Add(trim ? token.ToString().Trim() : token.ToString());
                        token.Clear();
                    }
                    else if (c == ListEscapeChar)
                    {
                        // Eventually escape next character
                        inEscape = true;
                    }
                    else
                    {
                        token.Append(c);
                    }
                }
            }

            // Trailing delimiter?
            if (inEscape)
            {
                token.Append(ListEscapeChar);
            }
            // Add last token
            tokens.Add(trim ? token.ToString().Trim() : token.ToString());

            return tokens;
        }
    }
}