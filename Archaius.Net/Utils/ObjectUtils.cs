using System;
using System.Collections;
using System.Collections.Generic;

namespace Archaius.Utils
{
    public static class ObjectUtils
    {
        public static bool AreEqual(object o1, object o2)
        {
            if (o1 == null)
            {
                return o2 == null;
            }
            if (o2 == null)
            {
                return false;
            }
            if (o1 is Array)
            {
                return o2 is Array && AreEqual(o1 as Array, o2 as Array);
            }
            if (o1 is IDictionary<string, object>)
            {
                return o2 is IDictionary<string, object> &&
                       AreEqual(o1 as IDictionary<string, object>, o2 as IDictionary<string, object>);
            }
            if (o1 is IEnumerable)
            {
                return o2 is IEnumerable && AreEqual(o1 as IEnumerable, o2 as IEnumerable);
            }
            return o1.Equals(o2);
        }

        public static bool AreEqual(IDictionary<string, object> d1, IDictionary<string, object> d2)
        {
            if (d1 == d2)
            {
                return true;
            }
            if (d1 == null || d2 == null)
            {
                return false;
            }
            if (d1.Count != d2.Count)
            {
                return false;
            }
            foreach (var kv in d1)
            {
                object o;
                if (!d2.TryGetValue(kv.Key, out o))
                {
                    return false;
                }
                if (!AreEqual(o, kv.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreEqual(Array a1, Array a2)
        {
            if (a1 == a2)
            {
                return true;
            }
            if (a1 == null || a2 == null)
            {
                return false;
            }
            if (a1.Length != a2.Length)
            {
                return false;
            }
            for (int i = 0; i < a1.Length; i++)
            {
                if (!AreEqual(a1.GetValue(i), a2.GetValue(i)))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreEqual(IEnumerable first, IEnumerable second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }

            if (first is IDictionary<string, object> && second is IDictionary<string, object>)
            {
                return AreEqual((IDictionary<string, object>)first, (IDictionary<string, object>)second);
            }

            IEnumerator it1 = first.GetEnumerator(), it2 = second.GetEnumerator();
            bool hasNext1 = it1.MoveNext(), hasNext2 = it2.MoveNext();
            while (hasNext1 && hasNext2)
            {
                var innerEnum1 = it1.Current as IEnumerable;
                var innerEnum2 = it2.Current as IEnumerable;
                if (innerEnum1 != null && innerEnum2 != null)
                {
                    if (!Equals(innerEnum1, innerEnum2))
                    {
                        return false;
                    }
                }
                else if (innerEnum1 == null ^ innerEnum2 == null)
                {
                    return false;
                }
                else if (!AreEqual(it1.Current, it2.Current))
                {
                    return false;
                }
                hasNext1 = it1.MoveNext();
                hasNext2 = it2.MoveNext();
            }
            return hasNext1 == hasNext2;
        }
    }
}