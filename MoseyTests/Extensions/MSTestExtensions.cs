using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MoseyTests.Extensions
{
    public static class MSTestExtensions
    {
        /// <summary>
        /// Assert that an object's properties have been set and are not default values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToInspect">An object instance to verify</param>
        /// <param name="getters"></param>
        public static void AssertAllPropertiesAreNotDefault<T>(this T objectToInspect, params Expression<Func<T, object>>[] getters)
        {
            var defaultProperties = getters.Where(f => f.Compile()(objectToInspect).Equals(default(T)));

            if (defaultProperties.Any())
            {
                var commaSeparatedPropertiesNames = string.Join(", ", defaultProperties.Select(GetName));
                Assert.Fail("Expected properties not to have default values: " + commaSeparatedPropertiesNames);
            }
        }

        /// <summary>
        /// A property name as a string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp"></param>
        /// <returns>A property name as a string</returns>
        public static string GetName<T>(Expression<Func<T, object>> exp)
        {
            // Return type is an object, so type cast expression will be added to value types
            if (!(exp.Body is MemberExpression body))
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
