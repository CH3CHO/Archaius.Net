using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using log4net;

namespace Archaius.Dynamic
{
    /// <summary>
    /// A cached configuration property value that is automatically
    /// updated when the config is changed.
    /// The object is fully thread safe, and access is very fast.
    /// (In fact, testing indicates that using a DynamicProperty is faster
    /// than fetching an environment variable.)
    /// 
    /// This class is intended for those situations where the value of
    /// a property is fetched many times, and the value may be
    /// changed on-the-fly.
    /// If the property is being read only once, "normal" access methods
    /// should be used.
    /// If the property value is fixed, consider just caching the value
    /// in a variable.
    /// 
    /// Fetching the cached value is synchronized only on this property,
    /// so contention should be negligible.
    /// If even that level of overhead is too much for you,
    /// you should (a) think real hard about what you are doing, and
    /// (b) just cache the property value in a variable and be done
    /// with it.
    /// 
    /// <b>IMPORTANT NOTE</b> 
    /// DynamicProperty objects are not subject to normal garbage collection.
    /// They should be used only as a static value that lives for the
    /// lifetime of the program.
    /// </summary>
    public class DynamicProperty
    {
        #region [Static Fields]
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string[] m_TrueValues = {"true", "t", "yes", "y", "on"};
        private static readonly string[] m_FalseValues = {"false", "f", "no", "n", "off"};

        private static volatile IDynamicPropertySupport m_DynamicPropertySupportImpl;

        /// <summary>
        /// Cache update is handled by a single configuration listener,
        /// with a static collection holding all defined DynamicProperty objects.
        /// It is assumed that DynamicProperty objects are static and never
        /// subject to gc, so holding them in the collection does not cause
        /// a memory leak.
        /// </summary>
        /// <returns></returns>
        private static readonly ConcurrentDictionary<string, DynamicProperty> m_AllProperties = new ConcurrentDictionary<string, DynamicProperty>();
        #endregion

        #region [Private Fields]
        private readonly object m_ObjectLock = new object(); // synchs caches and updates
        private readonly string m_PropName;
        private string m_StringValue;
        private DateTime m_ChangedTime;

        private readonly CachedValue<bool> m_CachedBooleanValue;
        private readonly CachedValue<string> m_CachedStringValue;
        private readonly CachedValue<int> m_CachedIntegerValue;
        private readonly CachedValue<long> m_CachedLongValue;
        private readonly CachedValue<float> m_CachedFloatValue;
        private readonly CachedValue<double> m_CachedDoubleValue;
        private readonly CachedValue<Type> m_CachedTypeValue;
        #endregion

        #region [Factory Methods]
        /// <summary>
        /// Gets the DynamicProperty for a given property name.
        /// This may be a previously constructed object, or an object constructed on-demand to satisfy the request.
        /// </summary>
        /// <param name="propName">the name of the property</param>
        /// <returns>a DynamicProperty object that holds the cached value of the configuration property named <see cref="propName"/></returns>
        public static DynamicProperty GetInstance(string propName)
        {
            // This is to ensure that a configuration source is registered with DynamicProperty
            if (m_DynamicPropertySupportImpl == null)
            {
                DynamicPropertyFactory.GetInstance();
            }
            DynamicProperty prop;
            if (!m_AllProperties.TryGetValue(propName, out prop))
            {
                prop = m_AllProperties.GetOrAdd(propName, name => new DynamicProperty(propName));
            }
            return prop;
        }
        #endregion

        #region [Constructors]
        protected DynamicProperty()
        {
            m_CachedBooleanValue = new DelegateCachedValue<bool>(this, rep =>
                                                                       {
                                                                           if (
                                                                               m_TrueValues.Any(
                                                                                   v => string.Equals(rep, v, StringComparison.OrdinalIgnoreCase)))
                                                                           {
                                                                               return true;
                                                                           }
                                                                           if (
                                                                               m_FalseValues.Any(
                                                                                   v => string.Equals(rep, v, StringComparison.OrdinalIgnoreCase)))
                                                                           {
                                                                               return false;
                                                                           }
                                                                           throw new ArgumentException();
                                                                       });
            m_CachedStringValue = new DelegateCachedValue<string>(this, rep => rep);
            m_CachedIntegerValue = new DelegateCachedValue<int>(this, int.Parse);
            m_CachedLongValue = new DelegateCachedValue<long>(this, long.Parse);
            m_CachedFloatValue = new DelegateCachedValue<float>(this, float.Parse);
            m_CachedDoubleValue = new DelegateCachedValue<double>(this, double.Parse);
            m_CachedTypeValue = new DelegateCachedValue<Type>(this, Type.GetType);
        }

        /// <summary>
        /// Create a new DynamicProperty with a given property name.
        /// </summary>
        /// <param name="propName">the name of the property</param>
        private DynamicProperty(string propName) : this()
        {
            m_PropName = propName;
            UpdateValue();
        }
        #endregion

        #region [Properties]
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name
        {
            get
            {
                return m_PropName;
            }
        }

        /// <summary>
        /// Gets the time when the property value was last set/changed.
        /// </summary>
        public DateTime ChangedTime
        {
            get
            {
                lock (m_ObjectLock)
                {
                    return m_ChangedTime;
                }
            }
        }
        #endregion

        #region [Events]
        public event EventHandler PropertyChanged;

        protected internal EventHandler[] PropertyChangedHandlers
        {
            get
            {
                return PropertyChanged != null ? PropertyChanged.GetInvocationList().OfType<EventHandler>().ToArray() : new EventHandler[0];
            }
        }
        #endregion

        #region [Initialization]
        internal static void Initialize(IDynamicPropertySupport config)
        {
            lock (typeof(DynamicProperty))
            {
                m_DynamicPropertySupportImpl = config;
                config.ConfigurationChanged += OnConfigurationChanged;
                UpdateAllProperties();
            }
        }

        internal static void RegisterWithDynamicPropertySupport(IDynamicPropertySupport config)
        {
            Initialize(config);
        }
        #endregion

        #region [Event Handlers]
        private static void OnConfigurationChanged(object sender, ConfigurationEventArgs args)
        {
            switch (args.Type)
            {
                case ConcurrentCompositeConfiguration.ConfigurationSourceChanged:
                    UpdateAllProperties();
                    break;
                case ConfigurationEventType.AddProperty:
                    OnAddPropertyEvent(sender, args);
                    break;
                case ConfigurationEventType.SetProperty:
                    OnSetPropertyEvent(sender, args);
                    break;
                case ConfigurationEventType.ClearProperty:
                    OnClearPropertyEvent(sender, args);
                    break;
                case ConfigurationEventType.Clear:
                    OnClearEvent(sender, args);
                    break;
            }
        }

        private static void OnAddPropertyEvent(object sender, ConfigurationEventArgs args)
        {
            if (!args.BeforeOperation)
            {
                UpdateProperty(args.Name, args.Value);
            }
            else
            {
                //Validate(args.Name, args.Value);
            }
        }

        private static void OnSetPropertyEvent(object sender, ConfigurationEventArgs args)
        {
            if (!args.BeforeOperation)
            {
                UpdateProperty(args.Name, args.Value);
            }
            else
            {
                //Validate(args.Name, args.Value);
            }
        }

        private static void OnClearPropertyEvent(object sender, ConfigurationEventArgs args)
        {
            if (!args.BeforeOperation)
            {
                UpdateProperty(args.Name, args.Value);
            }
            else
            {
                //Validate(args.Name, args.Value);
            }
        }

        private static void OnClearEvent(object sender, ConfigurationEventArgs args)
        {
            if (!args.BeforeOperation)
            {
                UpdateAllProperties();
            }
        }
        #endregion

        #region [Accessors]
        /// <summary>
        /// Gets the current value of the property as a string.
        /// </summary>
        /// <returns>the current property value, or null if there is none</returns>
        public string GetString()
        {
            return m_CachedStringValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as a string.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined</param>
        /// <returns>the current property value, or the default value there is none</returns>
        public string GetString(string defaultValue)
        {
            return m_CachedStringValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as a bool.
        /// A property string value of "true", "yes", "on", "t" or "y" produces true.
        /// A property string value of "false", "no", "off", "f" or "b" produces false.
        /// (The value comparison ignores case.)
        /// Any other value will result in an exception.
        /// </summary>
        /// <returns>the current property value, or false if there is none.</returns>
        public bool GetBoolean()
        {
            return m_CachedBooleanValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as a bool.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not of the proper format</param>
        /// <returns>
        /// the current property value, or the default value if there is none or the property is not of the proper format
        /// </returns>
        public bool GetBoolean(bool defaultValue)
        {
            return m_CachedBooleanValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as an integer.
        /// </summary>
        /// <returns>the current property value, or 0 if there is none.</returns>
        public int GetInteger()
        {
            return m_CachedIntegerValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as an integer.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not of the proper format</param>
        /// <returns>the current property value, or the default value if there is none or the property is not of the proper format</returns>
        public int GetInteger(int defaultValue)
        {
            return m_CachedIntegerValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as a float.
        /// </summary>
        /// <returns>the current property value, or 0.0f if there is none.</returns>
        public float GetFloat()
        {
            return m_CachedFloatValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as a float.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not of the proper format</param>
        /// <returns>the current property value, or the default value if there is none or the property is not of the proper format</returns>
        public float GetFloat(float defaultValue)
        {
            return m_CachedFloatValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as a long.
        /// </summary>
        /// <returns></returns>
        public long GetLong()
        {
            return m_CachedLongValue.GetValue();
        }

        /// <summary>
        ///  Gets the current value of the property as a long.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not of the proper format</param>
        /// <returns>the current property value, or the default value if there is none or the property is not of the proper format</returns>
        public long GetLong(long defaultValue)
        {
            return m_CachedLongValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as a double.
        /// </summary>
        /// <returns>the current property value, or 0.0 if there is none.</returns>
        public double GetDouble()
        {
            return m_CachedDoubleValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as a double.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not of the proper format</param>
        /// <returns>the current property value, or the default value if there is none or the property is not of the proper format</returns>
        public double GetDouble(double defaultValue)
        {
            return m_CachedDoubleValue.GetValue(defaultValue);
        }

        /// <summary>
        /// Gets the current value of the property as a Type.
        /// </summary>
        /// <returns>the current property value, or null if there is none.</returns>
        public Type GetNamedType()
        {
            return m_CachedTypeValue.GetValue();
        }

        /// <summary>
        /// Gets the current value of the property as a Type.
        /// </summary>
        /// <param name="defaultValue">the value to return if the property is not defined, or is not the name of a Class</param>
        /// <returns>the current property value, or the default value if there is none or the property is not of the proper format</returns>
        public Type GetNamedType(Type defaultValue)
        {
            return m_CachedTypeValue.GetValue(defaultValue);
        }
        #endregion

        #region [Value Updaters]
        private void RaisePropertyChangedEvent()
        {
            var eventHandler = PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Update the value of the property. The new value shall be fetched through the underlying dynamic property support layer.
        /// </summary>
        /// <returns>return true iff the value actually changed</returns>
        private bool UpdateValue()
        {
            string newValue;
            try
            {
                if (m_DynamicPropertySupportImpl == null)
                {
                    return false;
                }
                newValue = m_DynamicPropertySupportImpl.GetString(m_PropName);
            }
            catch (Exception e)
            {
                m_Log.Error("Unable to update property: " + m_PropName, e);
                return false;
            }
            return UpdateValue(newValue);
        }

        /// <summary>
        /// Update the value of the property.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns>Return true iff the value actually changed</returns>
        private bool UpdateValue(object newValue)
        {
            var nv = newValue != null ? newValue.ToString() : null;
            lock (m_ObjectLock)
            {
                if (nv == m_StringValue)
                {
                    return false;
                }
                m_StringValue = nv;
                m_CachedStringValue.Flush();
                m_CachedBooleanValue.Flush();
                m_CachedIntegerValue.Flush();
                m_CachedFloatValue.Flush();
                m_CachedTypeValue.Flush();
                m_CachedDoubleValue.Flush();
                m_CachedLongValue.Flush();
                m_ChangedTime = DateTime.Now;
                return true;
            }
        }

        /// <summary>
        /// Update the value of the given property.
        /// </summary>
        /// <returns>Return true iff the value actually changed</returns>
        private static bool UpdateProperty(string propName, object value)
        {
            DynamicProperty prop;
            if (m_AllProperties.TryGetValue(propName, out prop) && prop.UpdateValue(value))
            {
                prop.RaisePropertyChangedEvent();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update the value of all properties.
        /// </summary>
        /// <returns>Return true iff _some_ value actually changed</returns>
        private static bool UpdateAllProperties()
        {
            var changed = false;
            foreach (var prop in m_AllProperties.Values)
            {
                if (prop.UpdateValue())
                {
                    prop.RaisePropertyChangedEvent();
                    changed = true;
                }
            }
            return changed;
        }
        #endregion

        #region [CachedValue Inner Classes]
        /// <summary>
        /// A cached value of a particular type.
        /// </summary>
        /// <typeparam name="T">the type of the cached value</typeparam>
        private abstract class CachedValue<T>
        {
            private readonly DynamicProperty m_Property;
            private volatile bool m_IsCached;
            private volatile ArgumentException m_Exception;
            private T m_Value;

            public CachedValue(DynamicProperty property)
            {
                m_Property = property;
                Flush();
            }

            /// <summary>
            /// Flushes the cached value.
            /// Must be called with the object lock variable held by this thread.
            /// </summary>
            public void Flush()
            {
                // NOTE: is only called from updateValue(Object) which holds the lock
                // assert(Thread.holdsLock(lock));
                m_IsCached = false;
                m_Exception = null;
                m_Value = default(T);
            }

            /// <summary>
            /// Gets the cached value.
            /// If the value has not yet been parsed from the string value, parse it now.
            /// </summary>
            /// <returns>the parsed value, or null if there was no string value</returns>
            public T GetValue()
            {
                // Not quite double-check locking -- since isCached is marked as volatile
                if (!m_IsCached)
                {
                    lock (m_Property.m_ObjectLock)
                    {
                        try
                        {
                            m_Value = (m_Property.m_StringValue == null) ? default(T) : Parse(m_Property.m_StringValue);
                            m_Exception = null;
                        }
                        catch (Exception e)
                        {
                            m_Value = default(T);
                            m_Exception = new ArgumentException("Failed to parse the property value.", e);
                        }
                        finally
                        {
                            m_IsCached = true;
                        }
                    }
                }
                if (m_Exception != null)
                {
                    throw m_Exception;
                }
                return m_Value;
            }

            /// <summary>
            ///  Gets the cached value.
            /// If the value has not yet been parsed from the string value,
            /// parse it now.
            /// If there is no string value, or there was a parse error,
            /// returns the given default value.
            /// </summary>
            /// <param name="defaultValue">defaultValue the value to return if there was a problem</param>
            /// <returns>the parsed value, or the default if there was no string value or a problem during parse</returns>
            public T GetValue(T defaultValue)
            {
                try
                {
                    T result = GetValue();
                    return Equals(result, default(T)) ? defaultValue : result;
                }
                catch (ArgumentException e)
                {
                    return defaultValue;
                }
            }

            public override string ToString()
            {
                if (!m_IsCached)
                {
                    return "{Not cached}";
                }
                if (m_Exception != null)
                {
                    return string.Concat("{Exception: ", m_Exception, "}");
                }
                {
                    return string.Concat("{Value: ", m_Value, "}");
                }
            }

            /// <summary>
            /// Parse a string, converting it to an object of the value type.
            /// </summary>
            /// <param name="rep">the string representation to parse</param>
            /// <returns>the parsed value</returns>
            protected abstract T Parse(string rep);
        }

        private class DelegateCachedValue<T> : CachedValue<T>
        {
            private readonly Func<string, T> m_ParseFunc;

            public DelegateCachedValue(DynamicProperty property, Func<string, T> parseFunc) : base(property)
            {
                m_ParseFunc = parseFunc;
            }

            protected override T Parse(string rep)
            {
                return m_ParseFunc(rep);
            }
        }
        #endregion
    }
}