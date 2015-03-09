using System;

namespace Archaius
{
    /// <summary>
    /// Base interface for Archaius properties. Provides common methods across all property implementations.
    /// </summary>
    /// <typeparam name="T">The value type of the property</typeparam>
    public interface Property<out T>
    {
        /// <summary>
        /// Gets the name of the property
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        T Value
        {
            get;
        }

        /// <summary>
        /// Gets the default property value specified at creation time
        /// </summary>
        T DefaultValue
        {
            get;
        }

        /// <summary>
        /// Gets the time when the property was last set/changed.
        /// </summary>
        DateTime ChangedTime
        {
            get;
        }

        /// <summary>
        /// An event which will be triggered when the value of the property is changed.
        /// </summary>
        event EventHandler PropertyChanged;

        /// <summary>
        /// Remove all the handlers registered to PropertyChanged event
        /// </summary>
        void ClearPropertyChangedHandlers();
    }
}