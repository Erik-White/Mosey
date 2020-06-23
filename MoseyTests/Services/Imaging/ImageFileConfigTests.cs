using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using AutoFixture;

namespace Mosey.Services.Imaging.Tests
{
    [TestClass()]
    public class ImageFileConfigTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void SetUp()
        {
            _fixture = new Fixture();
        }

        [TestMethod()]
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