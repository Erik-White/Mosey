using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models
{
    public interface IFactory<T>
    {
        public T Create();
    }
}
