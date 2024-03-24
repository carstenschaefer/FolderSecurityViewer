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

namespace FSV.Extensions.Serialization.Wrappers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Abstractions;

    internal sealed class XmlSerializationWrapper : ISerializationWrapper
    {
        private readonly object syncObject = new();

        public T Deserialize<T>(Stream stream) where T : class
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (stream.Length == 0)
            {
                throw new ArgumentException("Stream cannot be empty.", nameof(stream));
            }

            using XmlTextReader xmlReader = new(stream);

            XmlSerializer xmlSerializer = new(typeof(T));

            if (!xmlSerializer.CanDeserialize(xmlReader))
            {
                throw new InvalidOperationException($"Cannot deserialize file into {nameof(T)}.");
            }

            lock (this.syncObject)
            {
                return (T)xmlSerializer.Deserialize(xmlReader);
            }
        }

        public async Task SerializeAsync<T>(Stream stream, T instance)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            XmlSerializer xmlSerializer = new(typeof(T));
            lock (this.syncObject)
            {
                xmlSerializer.Serialize(stream, instance);
            }

            await stream.FlushAsync();
        }
    }
}