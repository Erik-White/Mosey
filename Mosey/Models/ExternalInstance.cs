using System;

namespace Mosey.Models
{
    public interface IExternalInstance
    {
        string Name { get; }
        void SetVariable(string variable, dynamic value);
        dynamic GetVariable(string variable);
        void ExecuteMethod(string method, params dynamic[] arguments);
        dynamic ExecuteFunction(string function, params dynamic[] arguments);
    }
}
