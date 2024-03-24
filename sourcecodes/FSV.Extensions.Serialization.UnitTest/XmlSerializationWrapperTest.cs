// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.Extensions.Serialization.UnitTest
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class XmlSerializationWrapperTest : SerializationWrapperTest
    {
        [Fact]
        public void XmlSerializationWrapper_Deserialize_integration_Test()
        {
            // Arrange
            var expected = new MutableAvenger
            {
                Name = "Iron Man",
                AlterEgo = "Tony Stark"
            };

            IServiceProvider serviceProvider = GetServiceProvider();
            var wrapperFactory = serviceProvider.GetRequiredService<Func<SerializerType, ISerializationWrapper>>();

            using MemoryStream stream = new();
            SerializeObjectAsXml(stream, expected);

            ISerializationWrapper sut = wrapperFactory(SerializerType.Xml);

            // Act
            var actual = sut.Deserialize<MutableAvenger>(stream);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void XmlSerializationWrapper_Serialize_integration_Test()
        {
            // Arrange
            var expected = new MutableAvenger
            {
                Name = "Iron Man",
                AlterEgo = "Tony Stark"
            };

            IServiceProvider serviceProvider = GetServiceProvider();
            var wrapperFactory = serviceProvider.GetRequiredService<Func<SerializerType, ISerializationWrapper>>();

            using MemoryStream stream = new();
            ISerializationWrapper sut = wrapperFactory(SerializerType.Xml);

            // Act
            sut.SerializeAsync(stream, expected);

            stream.Seek(0, SeekOrigin.Begin);
            var actual = sut.Deserialize<MutableAvenger>(stream);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        private static void SerializeObjectAsXml(Stream stream, object model)
        {
            XmlSerializer serializer = new(model.GetType());

            serializer.Serialize(stream, model);
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
        }
    }
}