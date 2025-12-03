using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastPack.Lib.Hashing;
using FastPack.Lib.Logging;
using FastPack.Lib.ManifestManagement;
using FastPack.Lib.Options;

namespace FastPack.Lib.Actions;

public class TarPackAction : IAction
{
	private ILogger Logger { get; }
	private TarPackOptions Options { get; }
	
	internal IHashProviderFactory HashProviderFactory { get; set; } = new HashProviderFactory();
	
	private IFilter Filter { get; set; } = new Filter();
	
	public TarPackAction(ILogger logger, TarPackOptions options)
	{
		Logger = logger;
		Options = options;
	}
	
	public async Task<int> Run()
	{
		string outputDir = Path.GetDirectoryName(Options.OutputFilePath);
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir!);

		Stopwatch overallStopWatch = Stopwatch.StartNew();
		Stopwatch currentStopwatch = Stopwatch.StartNew();

		await Logger.InfoLine($"Using {Options.MaxDegreeOfParallelism} of {Environment.ProcessorCount} logical cores.");
		await Logger.StartTextProgress("Determining files and directories ...");
		FileSystemInfo[] fileSystemInfos = new DirectoryInfo(Options.InputDirectoryPath).GetFileSystemInfos("*", SearchOption.AllDirectories);
		await Logger.FinishTextProgress($"Found files in {currentStopwatch.Elapsed}...");
		currentStopwatch.Restart();

		await Logger.StartTextProgress("Filtering files and directories ...");
		List<FileSystemInfo> filteredFileSystemInfos = Filter.Apply(fileSystemInfos, Options, Path.DirectorySeparatorChar, e => Path.GetRelativePath(Options.InputDirectoryPath, e.FullName), e => e is DirectoryInfo);
		await Logger.FinishTextProgress($"Filtered files and directories in {currentStopwatch.Elapsed}.");
		currentStopwatch.Restart();
		
		List<FileInfo> filteredFileInfos = filteredFileSystemInfos.OfType<FileInfo>().ToList();
		List<DirectoryInfo> filteredDirectoryInfos = filteredFileSystemInfos.OfType<DirectoryInfo>().ToList();
		
		// Calculate stream hashes and unique files...
		ConcurrentDictionary<string, string> dict = new();
		IHashProvider hashProvider = HashProviderFactory.GetHashProvider(Options.HashAlgorithm);
		int processedHashes = 0;

		await Logger.StartTextProgress($"Determining hashes of {filteredFileInfos.Count} files ...");
		IProgress<int> hashProgress = new Progress<int>(current => ShowProgress(current, filteredFileInfos.Count, "Hashing progress: ").Wait());
		await Parallel.ForEachAsync(filteredFileInfos, new ParallelOptions { MaxDegreeOfParallelism = Options.MaxDegreeOfParallelism!.Value }, async (file, _) =>
		{
			dict[file.FullName] = await CryptoUtil.CalculateFileHash(hashProvider, file.FullName);

			if (!Options.ShowProgress)
				return;

			Interlocked.Increment(ref processedHashes);
			hashProgress.Report(processedHashes);
		});
		await Logger.FinishTextProgress($"Determined hashes in {currentStopwatch.Elapsed}.");

		
		await using var outputFileStream = new FileStream(Options.OutputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize, Constants.OpenFileStreamsAsync);
		await using var tarWriter = new System.Formats.Tar.TarWriter(outputFileStream);
		
		
		throw new System.NotImplementedException();
	}
	
	private async Task ShowProgress(int current, int total, string prefixText)
	{
		double percentage = (double)current / total * 100;
		await Logger.ReportTextProgress(percentage, prefixText);
	}
}