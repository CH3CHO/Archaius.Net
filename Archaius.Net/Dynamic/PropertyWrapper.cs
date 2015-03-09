using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace Archaius.Dynamic
{
    public abstract class PropertyWrapper<V> : Property<V>
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly IDictionary<Type, object> m_SubClassesWithNoCallback = new Dictionary<Type, object>();
        private static readonly object m_DummyValue = new object();

        protected readonly DynamicProperty m_Property;
        protected readonly V m_DefaultValue;
        private readonly IList<EventHandler> m_PropertyChangedHandlers = new List<EventHandler>();

        /// <summary>
        /// By default, a subclass of PropertyWrapper will automatically register {@link #propertyChanged()} as a callback
        /// for property value change. This method provide a way for a subclass to avoid this overhead if it is not interested
        /// to get callback.
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterSubClassWithNoCallback(Type type)
        {
            m_SubClassesWithNoCallback[type] = m_DummyValue;
        }

        static PropertyWrapper()
        {
            RegisterSubClassWithNoCallback(typeof(DynamicIntProperty));
            RegisterSubClassWithNoCallback(typeof(DynamicStringProperty));
            RegisterSubClassWithNoCallback(typeof(DynamicBooleanProperty));
            RegisterSubClassWithNoCallback(typeof(DynamicFloatProperty));
            RegisterSubClassWithNoCallback(typeof(DynamicLongProperty));
            RegisterSubClassWithNoCallback(typeof(DynamicDoubleProperty));
        }

        protected PropertyWrapper(string propName, V defaultValue)
        {
            m_Property = DynamicProperty.GetInstance(propName);
            m_DefaultValue = defaultValue;
            var type = GetType();
            // This checks whether this constructor is called by a class that
            // extends the immediate sub classes (IntProperty, etc.) of PropertyWrapper.
            // If yes, it is very likely that OnPropertyChanged() is overriden
            // in the sub class and we need to register the callback.
            // Otherwise, we know that OnPropertyChanged() does nothing in 
            // immediate subclasses and we can avoid registering the callback, which
            // has the cost of modifying the event.
            if (!m_SubClassesWithNoCallback.ContainsKey(type))
            {
                PropertyChanged += (o, args) => OnPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return m_Property.Name;
            }
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public abstract V Value
        {
            get;
        }

        /// <summary>
        /// Gets the default property value specified at creation time
        /// </summary>
        public V DefaultValue
        {
            get
            {
                return m_DefaultValue;
            }
        }

        /// <summary>
        ///  Gets the time when the property was last set/changed.
        /// </summary>
        public DateTime ChangedTime
        {
            get
            {
                return m_Property.ChangedTime;
            }
        }

        internal DynamicProperty Property
        {
            get
            {
                return m_Property;
            }
        }

        public event EventHandler PropertyChanged
        {
            add
            {
                m_Property.PropertyChanged += value;
                m_PropertyChangedHandlers.Add(value);
            }
            remove
            {
                m_Property.PropertyChanged -= value;
                m_PropertyChangedHandlers.Remove(value);
            }
        }

        /// <summary>
        /// Remove all the handlers registered to OnChanged event
        /// </summary>
        public void ClearPropertyChangedHandlers()
        {
            foreach (var propertyChangedHandler in m_PropertyChangedHandlers)
            {
                m_Property.PropertyChanged -= propertyChangedHandler;
            }
            m_PropertyChangedHandlers.Clear();
        }

        /// <summary>
        /// Called when the property value is updated.
        /// The default does nothing.
        /// Subclasses are free to override this if desired.
        /// </summary>
        protected virtual void OnPropertyChanged()
        {
            OnPropertyChanged(Value);
        }

        /// <summary>
        /// Called when the property value is updated.
        /// The default does nothing.
        /// Subclasses are free to override this if desired.
        /// </summary>
        protected void OnPropertyChanged(V newValue)
        {
            // By default, do nothing
        }

        public override string ToString()
        {
            return string.Format("DynamicProperty: {{name={0}, current value={1}}}", Name,
                                 m_Property.GetString(m_DefaultValue != null ? m_DefaultValue.ToString() : null));
        }
    }
}