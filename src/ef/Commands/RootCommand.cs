﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.DotNet.Cli.CommandLine;
using static Microsoft.EntityFrameworkCore.Tools.AnsiConstants;
using Microsoft.EntityFrameworkCore.Tools.Properties;

namespace Microsoft.EntityFrameworkCore.Tools.Commands
{
    internal class RootCommand : HelpCommandBase
    {
        public override void Configure(CommandLineApplication command)
        {
            command.FullName = Resources.EFFullName;

            // NB: Update ShouldHelp in dotnet-ef when adding new command groups
            command.Command("database", new DatabaseCommand().Configure);
            command.Command("dbcontext", new DbContextCommand().Configure);
            command.Command("migrations", new MigrationsCommand().Configure);

            command.VersionOption("--version", GetVersion);

            base.Configure(command);
        }

        protected override int Execute()
        {
            Reporter.WriteInformation(
                string.Join(
                    Environment.NewLine,
                    string.Empty,
                    Reporter.Colorize(@"                     _/\__       ", s => s.Insert(21, Bold + Gray)),
                    Reporter.Colorize(@"               ---==/    \\      ", s => s.Insert(20, Bold + Gray)),
                    Reporter.Colorize(@"         ___  ___   |.    \|\    ", s => s.Insert(26, Bold).Insert(21, Dark).Insert(20, Bold + Gray).Insert(9, Dark + Magenta)),
                    Reporter.Colorize(@"        | __|| __|  |  )   \\\   ", s => s.Insert(20, Bold + Gray).Insert(8, Dark + Magenta)),
                    Reporter.Colorize(@"        | _| | _|   \_/ |  //|\\ ", s => s.Insert(20, Bold + Gray).Insert(8, Dark + Magenta)),
                    Reporter.Colorize(@"        |___||_|       /   \\\/\\", s => s.Insert(33, Reset).Insert(23, Bold + Gray).Insert(8, Dark + Magenta)),
                    string.Empty));

            return base.Execute();
        }

        private static string GetVersion()
            => typeof(RootCommand).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
    }
}
