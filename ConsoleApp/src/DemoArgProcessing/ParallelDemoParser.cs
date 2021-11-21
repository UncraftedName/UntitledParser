using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DemoParser.Parser;

namespace ConsoleApp.DemoArgProcessing {

	public interface ICurrentParseInfo {
		public string DisplayPath {get;}
		public IProgressBar ProgressBar {get;}
	}
	

	public interface IParallelDemoParser : IDisposable {
		public ICurrentParseInfo GetCurrentParseInfo();
		public (SourceDemo demo, Exception? exception) GetNext();
		public bool NextReady();
		public bool HasNext();
	}
	
	
	// queues jobs in the thread pool, this seems to be faster than manually using locks and having a specific number of threads
	public class ThreadPoolDemoParser : IParallelDemoParser {

		private class ParseInfo : ICurrentParseInfo {
			
			public readonly ManualResetEventSlim ResetEvent;
			public IProgressBar ProgressBar {get;}
			public readonly FileInfo FileInfo;
			public string DisplayPath {get;}
			public SourceDemo Demo;
			public Exception? Exception;
			

			public ParseInfo(ManualResetEventSlim resetEvent, IProgressBar progressBar, FileInfo fileInfo, string displayPath) {
				ResetEvent = resetEvent;
				ProgressBar = progressBar;
				FileInfo = fileInfo;
				DisplayPath = displayPath;
				Demo = null!;
				Exception = null;
			}
		}
		

		private readonly ParseInfo[] _parseInfos;
		private int _parseIdx;
		private volatile bool _disposed;
		
		
		public ThreadPoolDemoParser(IImmutableList<(FileInfo demoPath, string displayPath)> paths) {
			_parseInfos = new ParseInfo[paths.Count];
			for (int i = 0; i < paths.Count; i++) {
				ThreadPool.QueueUserWorkItem(state => {
					ParseInfo info = (ParseInfo)state;
					if (!_disposed) {
						info.Demo = new SourceDemo(info.FileInfo, info.ProgressBar);
						try {
							info.Demo.Parse();
						} catch (Exception e) {
							info.Exception = e;
						}
					}
					info.ResetEvent.Set();
				}, _parseInfos[i] = new ParseInfo(new ManualResetEventSlim(), new ProgressBar(false), paths[i].demoPath, paths[i].displayPath));
			}
			_parseIdx = 0;
		}


		public ICurrentParseInfo GetCurrentParseInfo() {
			return _parseInfos[_parseIdx];
		}


		public (SourceDemo demo, Exception? exception) GetNext() {
			Debug.Assert(HasNext());
			ParseInfo info = _parseInfos[_parseIdx];
			info.ResetEvent.Wait();
			info.ResetEvent.Dispose();
			var ret = (info.Demo, info.Exception);
			_parseInfos[_parseIdx++] = null!; // allow garbage collection TODO fix that something doesn't get collected for large amounts of demos
			return ret;
		}


		public bool NextReady() {
			return _parseInfos[_parseIdx].ResetEvent.IsSet;
		}


		public bool HasNext() {
			return _parseIdx < _parseInfos.Length;
		}


		public void Dispose() {
			// the least we can do is not parse more demos, should probably cancel ongoing parses though
			_disposed = true;
		}
	}
}
