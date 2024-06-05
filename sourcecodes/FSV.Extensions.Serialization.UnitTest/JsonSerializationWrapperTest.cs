// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Sch�fer, Matthias Friedrich, and Ritesh Gite
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
    using System.Text;
    using System.Threading.Tasks;
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Xunit;

    public class JsonSerializationWrapperTest : SerializationWrapperTest
    {
        [Fact]
        public void JsonSerializationWrapper_Deserialize_integration_Test()
        {
            // Arrange
            ImmutableAvenger expected = new("Iron Man", "Tony Stark");

            IServiceProvider serviceProvider = GetServiceProvider();
            var wrapperFactory = serviceProvider.GetRequiredService<Func<SerializerType, ISerializationWrapper>>();

            ISerializationWrapper sut = wrapperFactory(SerializerType.Json);

            using var stream = new MemoryStream();
            SerializeObjectAsJson(stream, expected);

            // Act
            var model = sut.Deserialize<ImmutableAvenger>(stream);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(expected, model);
        }

        [Fact]
        public async Task JsonSerializationWrapper_Serialize_integration_Test()
        {
            // Arrange
            ImmutableAvenger expected = new("Iron Man", "Tony Stark");

            IServiceProvider serviceProvider = GetServiceProvider();
            var wrapperFactory = serviceProvider.GetRequiredService<Func<SerializerType, ISerializationWrapper>>();

            ISerializationWrapper sut = wrapperFactory(SerializerType.Json);

            using var stream = new MemoryStream();

            // Act
            await sut.SerializeAsync(stream, expected);
            stream.Seek(0, SeekOrigin.Begin);
            var model = sut.Deserialize<ImmutableAvenger>(stream);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(expected, model);
        }

        private static void SerializeObjectAsJson(Stream target, object model)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            string json = JsonConvert.SerializeObject(model);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            target.Write(bytes, 0, bytes.Length);
            target.Seek(0, SeekOrigin.Begin);
        }
    }
}