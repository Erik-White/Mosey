using System;
using System.IO;
using Microsoft.Scripting.Hosting;

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
    public class ExternalInstance : IExternalInstance
    {
        private string _Name;
        private ScriptEngine scriptEngine;
        private ScriptScope scriptScope;
        private ScriptSource scriptSource;
        private CompiledCode scriptCompiled;
        private object classObject;

        public string Name { get { return _Name; } }

        public ExternalInstance(ScriptEngine engine, string codeSource, string className)
        {
            if (!string.IsNullOrEmpty(className))
            {
                _Name = className;
            }
            else
            {
                throw new ArgumentException("The name for the class to be created must not be empty", nameof(className));
            }

            scriptEngine = engine;
            scriptScope = scriptEngine.CreateScope();

            // Determine if code is passed as a string or located in a file
            try
            {
                if(File.Exists(codeSource))
                {
                    scriptEngine.CreateScriptSourceFromFile(codeSource);
                }
                else
                {
                    scriptEngine.CreateScriptSourceFromString(codeSource, Microsoft.Scripting.SourceCodeKind.Statements);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Unable to parse source code: {0}", codeSource), ex);
            }

            // Compile and execute the code
            scriptCompiled = scriptSource.Compile();
            scriptCompiled.Execute(scriptScope);

            // Create an object from the external instance class
            classObject = engine.Operations.Invoke(scriptScope.GetVariable(_Name));
        }

        public void SetVariable(string variable, dynamic value)
        {
            scriptScope.SetVariable(variable, value);
        }

        public dynamic GetVariable(string variable)
        {
            return scriptScope.GetVariable(variable);
        }

        public void ExecuteMethod(string method, params dynamic[] arguments)
        {
            scriptEngine.Operations.InvokeMember(classObject, method, arguments);
        }

        public dynamic ExecuteFunction(string method, params dynamic[] arguments)
        {
            return scriptEngine.Operations.InvokeMember(classObject, method, arguments);
        }
        public override string ToString()
        {
            return Name;
        }
        /*
         var var1,var2=...
ScriptEngine engine = Python.CreateEngine();
ScriptScope scriptScope = engine.CreateScope();
engine.ExecuteFile(@"C:\test.py", scriptScope);
dynamic testFunction = scriptScope.GetVariable("test_func");
var result = testFunction(var1,var2);

*/
    }
}
