using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;

namespace Nickel;

internal record ProgramRunInfo(
	ParseResult LaunchArgs,
	Settings Settings,
	DirectoryInfo ModStorageDirectory,
	List<LogEntry.Local> EarlyLogs
);
