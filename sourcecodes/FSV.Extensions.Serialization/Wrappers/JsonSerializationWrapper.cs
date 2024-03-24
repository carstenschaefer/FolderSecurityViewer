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
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Abstractions;
    using Newtonsoft.Json;

    internal sealed class JsonSerializationWrapper : ISerializationWrapper
    {
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

            try
            {
                const bool leaveOpen = true;
                using StreamReader streamReader = new(stream, Encoding.UTF8, false, 4096, leaveOpen);
                string json = streamReader.ReadToEnd();

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonReaderException ex)
            {
                throw new SerializationException("Failed to deserialize stream as it does not have JSON content. See inner exception for more details.", ex);
            }
            catch (JsonSerializationException ex)
            {
                throw new SerializationException("Failed to deserialize stream as it has a malformed JSON content. See inner exception for more details.", ex);
            }
            catch (IOException ex)
            {
                throw new SerializationException("Failed to deserialize stream. See inner exception for more details.", ex);
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

            try
            {
                string json = JsonConvert.SerializeObject(instance);

                const bool leaveOpen = true;
                using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen);

                await streamWriter.WriteAsync(json);
                await streamWriter.FlushAsync();
            }
            catch (JsonWriterException ex)
            {
                throw new SerializationException("Failed to write object into JSON. See inner exception for more details.", ex);
            }
            catch (IOException ex)
            {
                throw new SerializationException("Failed to write JSON into the stream. See inner exception for more details.", ex);
            }
        }
    }
}