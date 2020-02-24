using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models
{
    public interface ICopy<T>
    {
        T Copy();
    }
}
