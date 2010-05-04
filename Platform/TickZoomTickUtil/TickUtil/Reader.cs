#region Copyright
/*
 * Software: TickZoom Trading Platform
 * Copyright 2009 M. Wayne Walter
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * Business use restricted to 30 days except as otherwise stated in
 * in your Service Level Agreement (SLA).
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
 * or write to Free Software Foundation, Inc., 51 Franklin Street,
 * Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using TickZoom.Api;

namespace TickZoom.TickUtil
{
	public abstract class Reader<Y>	where Y : ReadWritable<TickBinary>, new()
	{
		BackgroundWorker backgroundWorker;
		long maxCount = long.MaxValue;
		SymbolInfo symbol = null;
		ulong lSymbol = 0;
        static readonly Log log = Factory.Log.GetLogger("TickZoom.TickUtil.Reader<" + typeof(TickBinary).Name + ">");
		static readonly bool debug = log.IsDebugEnabled;
		static readonly bool trace = log.IsDebugEnabled;
		bool quietMode = false;
		long progressDivisor = 1;
		private Elapsed sessionStart = new Elapsed(8,0,0);
		private Elapsed sessionEnd = new Elapsed(12,0,0);
		bool excludeSunday = true;
		string fileName = null;
		bool logProgress = false;
		bool bulkFileLoad = false;
	    long length;
	    private Receiver receiver;
		Task fileReaderTask;
		private static List<Reader<Y>> readerList = new List<Reader<Y>>();
	    private object taskLocker = new object();
		private volatile bool isDisposed = false;
		string storageFolder;
		MemoryStream memory;
		byte[] buffer;
		
		public Reader()
		{
      		storageFolder = Factory.Settings["AppDataFolder"];
       		if( storageFolder == null) {
       			throw new ApplicationException( "Must set AppDataFolder property in app.config");
       		}
			readerList.Add(this);
			memory = new MemoryStream();
			memory.SetLength(TickImpl.minTickSize);
			buffer = memory.GetBuffer();
		}
		
		bool CancelPending {
			get { return backgroundWorker != null && backgroundWorker.CancellationPending; }
		}
		
		[Obsolete("Pass symbol string instead of SymbolInfo",true)]
		public void Initialize(string _folder, SymbolInfo symbolInfo) {
			symbol = symbolInfo;
			lSymbol = symbolInfo.BinaryIdentifier;
			fileName = storageFolder + "\\" + _folder + "\\" + symbolInfo.Symbol.Replace("/","") + "_Tick" + ".tck";
			Initialize(_folder,symbolInfo.Symbol);
		}
		
		public void Initialize(string folderOrfile, string _symbol) {
			symbol = Factory.Symbol.LookupSymbol(_symbol);
			lSymbol = symbol.BinaryIdentifier;
			string filePath = storageFolder + "\\" + folderOrfile;
			if( Directory.Exists( filePath)) {
				fileName = storageFolder + "\\" + folderOrfile + "\\" + _symbol.StripInvalidPathChars() + "_Tick" + ".tck";
			} else if( File.Exists( folderOrfile)) {
			    fileName = folderOrfile;
			} else {
				throw new ApplicationException("Requires either a file or folder to read data. Tried both " + folderOrfile + " and " + filePath);
			}
		}
		
		[Obsolete("Use Initialize( folderOrfile, symbol) instead.",true)]
		public void Initialize(string fileName) {
			this.fileName = fileName;
			if(debug) log.Debug("File Name = " + fileName);
			if(debug) log.Debug("Setting start method on reader queue.");
			string baseName = Path.GetFileNameWithoutExtension(fileName);
			if( symbol == null) {
				symbol = Factory.Symbol.LookupSymbol(baseName.Replace("_Tick",""));
				lSymbol = symbol.BinaryIdentifier;
			}
   			Directory.CreateDirectory( Path.GetDirectoryName(fileName));
		}
		
		public Y GetLastTick() {
			Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); 
			length = stream.Length;
			dataIn = new BinaryReader(stream,Encoding.Unicode); 
			position = 0;
			try { 
		    	while( position < length && !CancelPending) {
					if( !TryReadTick(length)) {
						break;
					}
				}
			} catch( ObjectDisposedException) {
				// Only partial tick was read at the end of the file.
				// Another writer must not have completed.
				log.Warn("ObjectDisposedException returned from tickIO.FromReader(). Incomplete last tick. Ignoring.");
			}
			return tickIO;
		}

        public void Start(Receiver receiver)
        {
		    this.receiver = receiver;
			if(debug) log.Debug("Start called.");
			StartupTask();
			fileReaderTask = Factory.Parallel.IO.Loop(this,FileReader);
		}
        
        public void Stop(Receiver receiver)
        {
            if (debug) log.Debug("Stop(" + receiver + ")");
//			if( receiver != null) {
//            	if( !receiver.CanReceive(symbol)) {
//            		log.Warn("Can't receive Terminate message");
//            	}
//				receiver.OnEvent(null,(int)EventType.Terminate,null);
//			}
            Dispose();
        }
		
		public abstract bool IsAtStart(TickBinary tick);
        public abstract bool IsAtEnd(TickBinary tick);
		
		public bool LogTicks = false;
		
		void LogInfo(string logMsg) {
		    if( !quietMode) {
		    		log.Notice(logMsg);
			} else {
		    		log.Debug(logMsg);
			}
		}
        static Dictionary<SymbolInfo, byte[]> fileBytesDict = new Dictionary<SymbolInfo, byte[]>();
		static object fileLocker = new object();
	    long position = 0;
	    BinaryReader dataIn = null; 
		Y tickIO = new Y();
		
        TickBinary tick = new TickBinary();
		bool isDataRead = false;
		bool isFirstTick = true;
		long nextUpdate = 0;
		int count = 0;
		int start;
		private bool StartupTask() {
		    for( int retry=0; retry<3; retry++) {
			    try { 
				    position = 0;
				    if( !quietMode) {
			    		LogInfo("Reading from file: " + fileName);
				    }
		    		
	    			Directory.CreateDirectory( Path.GetDirectoryName(fileName));
	
	    			Stream stream;
	    			if( bulkFileLoad) {
	   					byte[] filebytes;
		    			lock( fileLocker) {
		   					if( !fileBytesDict.TryGetValue(symbol,out filebytes)) {
					    	 	filebytes = File.ReadAllBytes(fileName);
			    			 	fileBytesDict[symbol] = filebytes;
		    				}
		    			}	
						length = filebytes.Length;
		    			stream = new MemoryStream(filebytes);
		    		} else {
						stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); 
						length = stream.Length;
		    		}
		    			
					dataIn = new BinaryReader(stream,Encoding.Unicode); 
		    		
		     		progressDivisor = length/20;
		     		if( !quietMode || debug) {
		    			if(debug) log.Debug("Starting to read data.");
		    			log.Indent();
		     		}
					start = Environment.TickCount;
					return true;
		    	} catch ( Exception ex) {
		    		ExceptionHandler(ex);
			    }
		    	Factory.Parallel.Sleep(1000);
		    }
		    return false;
		}
	
		private void ExceptionHandler(Exception e) {
    		if( e is CollectionTerminatedException) {
				log.Warn( "Reader queue was terminated.");
	   		} else if( e is ThreadAbortException) {
				//	
			} else if( e is FileNotFoundException) {
	    		log.Error( "ERROR: " + e.Message);
	    	} else {
	    		log.Error( "ERROR: " + e);
	    	}
			if( dataIn != null) {
				isDisposed = true;
				dataIn.Close();
				dataIn = null;
			}
		}
	
		byte dataVersion;
		
		public byte DataVersion {
			get { return dataVersion; }
		}
		
		private bool TryReadTick(long length) {
			int size = dataIn.ReadByte();
			if( position + size > length) {
				return false;
			}
			position++;
			// Check for old style prior to version 8 where
			// single byte version # was first.
			if( size < 8) {
				position += tickIO.FromReader((byte)size,dataIn);
			} else {
				size--; // Subtract the size byte.
				int count = 0;
				while(count < size) {
					count += dataIn.Read(buffer, count, size-count);
				}
				memory.Position = 0;
				position += count;
				tickIO.FromReader(memory);
			}
   			tickIO.SetSymbol(lSymbol);
   			return true;
		}
		
		private Yield FileReader() {
			lock( taskLocker) {
				if( isDisposed ) return Yield.Terminate;
				try {
		    		if( position < length && !CancelPending
					   && TryReadTick(length)) {
						
		    			if( dataVersion == 0) {
		    				dataVersion = tickIO.DataVersion;			
		    			}
		    			tick = tickIO.Extract();
						isDataRead = true;
		    			
						if( position > nextUpdate) {
							try {
					    		progressCallback("Loading bytes...", position, length);
							} catch( Exception ex) {
								log.Debug( "Exception on progressCallback: " + ex.Message);
							}
					    	nextUpdate = position + progressDivisor;
						}
						
		    			if( maxCount > 0 && count > maxCount) {
							if(debug) log.Debug("Ending data read because count reached " + maxCount + " ticks.");
							return Yield.DidWork.Invoke(SendFinish);
		    			}
						
						if( IsAtEnd(tick)) {
							return Yield.DidWork.Invoke(SendFinish);
		    			}
		    
		    			if( IsAtStart(tick)) {
		    				count++;
		    				if( debug && count<5) {
		    					log.Debug("Read a tick " + tickIO);
		    				} else if( trace) {
		    					log.Trace("Read a tick " + tickIO);
		    				}
		    				tick.Symbol = symbol.BinaryIdentifier;
		    				
			    			if( isFirstTick) {
			    				isFirstTick = false;
			    				return Yield.DidWork.Invoke(StartEvent);
			    			}
							
		    				return Yield.DidWork.Invoke(TickEvent);
						}
						
					} else {
						return Yield.DidWork.Invoke(SendFinish);
					}
				} catch( ObjectDisposedException) {
					return Yield.DidWork.Invoke(SendFinish);
				}
   				return Yield.DidWork.Repeat;
			}
		}
		
		private Yield StartEvent() {
			if( !receiver.OnEvent(symbol,(int)EventType.StartHistorical,null)) {
				return Yield.NoWork.Repeat;
			} else {
				if( !quietMode) {
					LogInfo("Starting loading for " + symbol + " from " + tickIO.ToPosition());
				}
				return Yield.DidWork.Invoke(TickEvent);
			}
		}
		
		private Yield TickEvent() {
			if( !receiver.OnEvent(symbol,(int)EventType.Tick,tick)) {
				return Yield.NoWork.Repeat;
			} else {
				return Yield.DidWork.Return;
			}
		}
		
		private Yield SendFinish() {
			if( !receiver.OnEvent(symbol,(int)EventType.EndHistorical,null)) {
				return Yield.NoWork.Repeat;
			} else {
				return Yield.DidWork.Invoke(FinishTask);
			}
		}
		
		private Yield FinishTask() {
			try {
				if( !quietMode && isDataRead ) {
					LogInfo("Processing ended for " + symbol + " at " + tickIO.ToPosition());
	    		}
				int end = Environment.TickCount;
				if( !quietMode) {
		    		LogInfo( "Processed " + count + " ticks in " + (end-start)  + " ms.");
				}
				try {
		    		progressCallback("Processing complete.", length, length);
				} catch( Exception ex) {
					log.Debug( "Exception on progressCallback: " + ex.Message);
				}
				if( debug) log.Debug("calling receiver.OnEvent(symbol,(int)EventType.EndHistorical)");
    		} catch ( ThreadAbortException) {
		
    		} catch ( FileNotFoundException ex) {
    			log.Error( "ERROR: " + ex.Message);
    		} catch ( Exception ex) {
    			log.Error( "ERROR: " + ex);
    		} finally {
				isDisposed = true;
				fileReaderTask.Stop();
				if( dataIn != null) {
					dataIn.Close();
				}
    		}
			return Yield.Terminate;
		}
		
	    public void Dispose() 
	    {
	        Dispose(true);
	        GC.SuppressFinalize(this);      
	    }
	
	    protected virtual void Dispose(bool disposing)
	    {
       		if( !isDisposed) {
				isDisposed = true;
	    		lock( taskLocker) {
					if( fileReaderTask != null) {
						fileReaderTask.Stop();
						fileReaderTask.Join();
					}
					if( dataIn != null) {
						dataIn.Close();
					}
					readerList.Remove(this);
	    		}
    		}
	    }
		
		public static void CloseAll() {
			for( int i=0; i<readerList.Count; i++) {
				readerList[i].Dispose();
			}
			readerList.Clear();
		}
		
		void progressCallback( string text, Int64 current, Int64 final) {
			if( !quietMode) {
				if( backgroundWorker != null && !backgroundWorker.CancellationPending &&
				    backgroundWorker.WorkerReportsProgress) {
					backgroundWorker.ReportProgress(0, (object) new ProgressImpl(text,current,final));
				}
			}
		}
		
		public BackgroundWorker BackgroundWorker {
			get { return backgroundWorker; }
			set { backgroundWorker = value; }
		}
		
		public Elapsed SessionStart {
			get { return sessionStart; }
			set { sessionStart = value; }
		}
		
		public Elapsed SessionEnd {
			get { return sessionEnd; }
			set { sessionEnd = value; }
		}
		
		public bool ExcludeSunday {
			get { return excludeSunday; }
			set { excludeSunday = value; }
		}
		
		public string FileName {
			get { return fileName; }
		}
		    
		public SymbolInfo Symbol {
			get { return symbol; }
		}
		
		public bool LogProgress {
			get { return logProgress; }
			set { logProgress = value; }
		}
		   		
		public long MaxCount {
			get { return maxCount; }
			set { maxCount = value; }
		}
		
		public bool QuietMode {
			get { return quietMode; }
			set { quietMode = value; }
		}
		 		
		public bool BulkFileLoad {
			get { return bulkFileLoad; }
			set { bulkFileLoad = value; }
		}
	
		public Y LastTick {
			get { return tickIO; }
		}
	}
}
