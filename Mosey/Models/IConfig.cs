using System;

namespace Mosey.Models
{
    public interface IConfigGroup<T> : ICopy<T> where T : IConfigGroup<T> { }

    public interface IConfig : ICloneable { }
}
