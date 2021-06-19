using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.NUnit3;

namespace Mosey.Tests.AutoData
{
    public class CollectionSizeAttribute : CustomizeAttribute
    {
        private readonly int _size;

        public CollectionSizeAttribute(int size)
        {
            _size = size;
        }

        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            var objectType = parameter.ParameterType.GetGenericArguments()[0];

            var isTypeCompatible =
                parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition().MakeGenericType(objectType).IsAssignableFrom(typeof(List<>).MakeGenericType(objectType))
            ;
            if (!isTypeCompatible)
            {
                throw new InvalidOperationException($"{nameof(CollectionSizeAttribute)} specified for type incompatible with List: {parameter.ParameterType} {parameter.Name}");
            }

            var customizationType = typeof(CollectionSizeCustomization<>).MakeGenericType(objectType);
            return (ICustomization)Activator.CreateInstance(customizationType, parameter, _size);
        }

        public class CollectionSizeCustomization<T> : ICustomization
        {
            private readonly ParameterInfo _parameter;
            private readonly int _repeatCount;

            public CollectionSizeCustomization(ParameterInfo parameter, int repeatCount)
            {
                _parameter = parameter;
                _repeatCount = repeatCount;
            }

            public void Customize(IFixture fixture)
            {
                fixture.Customizations.Add(new FilteringSpecimenBuilder(
                    new FixedBuilder(fixture.CreateMany<T>(_repeatCount).ToList()),
                    new EqualRequestSpecification(_parameter)
                ));
            }
        }
    }
}
