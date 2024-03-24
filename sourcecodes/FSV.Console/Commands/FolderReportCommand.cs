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

namespace FSV.Console.Commands
{
    using System.Collections.Generic;
    using Abstractions;
    using Microsoft.Extensions.CommandLineUtils;
    using Properties;

    public class FolderReportCommand : ICommand
    {
        private IEnumerable<CommandArgument> arguments;
        private IEnumerable<CommandOption> options;

        public FolderReportCommand()
        {
            this.DirectoryArgument = new DirectoryArgument("[path]", Resources.ArgumentPathDescriptionText);
        }

        public DirectoryArgument DirectoryArgument { get; }

        public string CommandName => "fr";

        public string CommandDescription => Resources.FolderReportDescription;

        public string HelpText => "-h|-?|--help";

        public IEnumerable<CommandArgument> Arguments => this.arguments ??= this.CreateArguments();

        public IEnumerable<CommandOption> Options => this.options ??= this.CreateOptions();

        public CommandOption OptionExportType { get; } = new("-e|--export <fileType>", CommandOptionType.SingleValue) { Description = Resources.OptionExportDescriptionText };

        public CommandOption OptionExportPath { get; } = new("-p|--path <fileName>", CommandOptionType.SingleValue) { Description = Resources.OptionExportPathDescriptionText };

        private IEnumerable<CommandOption> CreateOptions()
        {
            return new[] { this.OptionExportType, this.OptionExportPath };
        }

        private IEnumerable<CommandArgument> CreateArguments()
        {
            return new[] { this.DirectoryArgument };
        }
    }
}