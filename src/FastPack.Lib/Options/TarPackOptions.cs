using System;
using FastPack.Lib.Hashing;

namespace FastPack.Lib.Options;

public class TarPackOptions :  FilterOptions , IOptions
{
	private bool _showProgress = Environment.UserInteractive && !Console.IsOutputRedirected;
	
	public string InputDirectoryPath { get; set; }
	public string OutputFilePath { get; set; }
	public int? MaxDegreeOfParallelism { get; set; }
	public bool QuietMode { get; set; }
	
	public HashAlgorithm HashAlgorithm { get; set; } = HashAlgorithm.XXHash;
	
	public bool ShowProgress
	{
		get => _showProgress;
		set => _showProgress = value && Environment.UserInteractive && !Console.IsOutputRedirected;
	}
}