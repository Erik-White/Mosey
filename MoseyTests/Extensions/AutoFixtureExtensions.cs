using System;
using System.Collections.Generic;
using System.Text;
using AutoFixture;
using AutoFixture.Kernel;

namespace MoseyTests
{
    public static class AutoFixtureExtensions
    {   
        /// <summary>
        /// Specifies that the fixture should use the greediest constructor available
        /// </summary>
        /// <remarks>
        /// AutoFixture uses the least greedy constructor by default
        /// </remarks>
        /// <typeparam name="T">The type to modify with this <see cref="IFixture"/></typeparam>
        /// <param name="fixture">An <see cref="IFixture"/> instance</param>
        public static void SetGreedyConstructor<T>(this IFixture fixture)
        {
            fixture.Customize<T>(c => c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
        }

        /// <summary>
        /// Create and register mocked instance of <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// The mock is registered as the underlying type, <typeparamref name="T"/>.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixture">The fixture to register the mocked instance with</param>
        /// <returns>The mocked <typeparamref name="T"/> instance</returns>
        public static Moq.Mock<T> FreezeMoq<T>(this IFixture fixture) where T : class
        {
            var td = new Moq.Mock<T>();
            fixture.Register<T>(() => td.Object);

            return td;
        }
    }
}
