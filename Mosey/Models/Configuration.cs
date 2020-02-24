using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models
{
    public interface IConfigGroup<T> : ICopy<T> where T : IConfigGroup<T> { }

    public interface IConfig : ICloneable { }
}
