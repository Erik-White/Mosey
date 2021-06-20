using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mosey.Models
{
    /// <summary>
    /// Convenience methods for raising <see cref="PropertyChanged"/> events when property values are set.
    /// </summary>
    public abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Propagates changes to property values to observers such as <see cref="ObservableItemsCollection{T}"/>.
        /// <see cref="SetField{T}(ref T, T, string)"/> must be used in property setters.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles <see cref="PropertyChanged"/> events and automatically locates the calling property name.
        /// </summary>
        /// <param name="propertyName">The name of the property that has been updated</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Raise <see cref="PropertyChanged"/> events when setting a property's value.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="field">The property backing field</param>
        /// <param name="value">The property value</param>
        /// <param name="propertyName">The property name reference</param>
        /// <returns><see langword="true"/> on completion</returns>
        /// <example>
        /// <code>
        /// public int Property
        /// {
        ///     get { return _property; }
        ///     set { SetField(ref _property, value); }
        /// }
        ///</code>
        ///</example>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
