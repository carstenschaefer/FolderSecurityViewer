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

namespace FSV.ActiveDirectoryServices.TestAbstractionLayer
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using AdServices;
    using AdServices.AbstractionLayer.Abstractions;
    using Models;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ActiveDirectoryYamlAbstractionService : IActiveDirectoryAbstractionService
    {
        private static readonly INamingConvention DefaultNamingConvention = CamelCaseNamingConvention.Instance;
        private readonly ForestModel forest;

        public ActiveDirectoryYamlAbstractionService(ForestModel forest)
        {
            this.forest = forest ?? throw new ArgumentNullException(nameof(forest));
        }

        public IForest GetCurrentForest()
        {
            return this.forest;
        }

        /// <summary>
        ///     Saves the underlying <see cref="ForestModel" /> to the specified yaml file.
        /// </summary>
        /// <param name="yamlFile">The destination yaml file.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Save(string yamlFile)
        {
            if (string.IsNullOrWhiteSpace(yamlFile))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(yamlFile));
            }

            ISerializer serializer = new SerializerBuilder().WithNamingConvention(DefaultNamingConvention).Build();
            string yaml = serializer.Serialize(this.forest);
            File.WriteAllText(yamlFile, yaml);
        }

        public static IActiveDirectoryAbstractionService Create(string yamlFile)
        {
            if (string.IsNullOrWhiteSpace(yamlFile))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(yamlFile));
            }

            if (File.Exists(yamlFile) == false)
            {
                throw new FileNotFoundException("The specified forest definition file could not be found.", yamlFile);
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(DefaultNamingConvention)
                .Build();

            string yaml = File.ReadAllText(yamlFile);
            var forest = deserializer.Deserialize<ForestModel>(yaml);

            return new ActiveDirectoryYamlAbstractionService(forest);
        }
    }
}