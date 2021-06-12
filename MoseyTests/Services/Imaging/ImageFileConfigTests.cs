using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using AutoFixture;
using NUnit.Framework;

namespace Mosey.Services.Imaging.Tests
{
    [TestFixture]
    public class ImageFileConfigTests
    {
        Fixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void CloneTest()
        {
            var original = _fixture.Create<ImageFileConfig>();
            var clone = original.Clone();

            // Not a deep clone
            original.Should().NotBeSameAs(clone);
            // Properties and fields should be the same
            clone.Should().BeEquivalentTo(original);
        }
    }
}