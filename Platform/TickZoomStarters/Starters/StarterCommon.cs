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
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.Common
{
    
    public delegate void ProgressCallback(string fileName, Int64 BytesRead, Int64 TotalBytes);
    
	/// <summary>
	/// Description of Test.
	/// </summary>
	public abstract class StarterCommon : Starter
	{
		static readonly Log log = Factory.Log.GetLogger(typeof(StarterCommon));
		static readonly bool debug = log.IsDebugEnabled;
		BackgroundWorker backgroundWorker;
	    ShowChartCallback showChartCallback;
	    CreateChartCallback createChartCallback;
	    TickQueue realTimeTickQueue;
    	string dataFolder = "DataCache";
   		static Interval initialInterval = Factory.Engine.DefineInterval(BarUnit.Day,1);
   		long endCount = long.MaxValue;
   		int startCount = 0;
	    static object callBackLocker = new object();
	    ProjectProperties projectProperties = new ProjectPropertiesCommon();
	    string projectFile = Factory.Settings["AppDataFolder"] + @"\portfolio.tzproj";
		private List<Provider> tickProviders = new List<Provider>();
		
		public StarterCommon() : this(true) {
			
		}
		
		public StarterCommon(bool releaseEngineCache) {
			if( releaseEngineCache) {
				Factory.Engine.Dispose();
				Factory.Provider.Release();
			}
			string dataFolder = Factory.Settings["DataFolder"];
			if( dataFolder != null) {
				this.dataFolder = dataFolder;
			}
		}
		
		public void Release() {
			Factory.Engine.Dispose();
			Factory.Provider.Release();
		}
		
		bool CancelPending {
			get { if( backgroundWorker != null) {
					return backgroundWorker.CancellationPending;
				} else {
					return false;
				}
			}
		}
		
		public void ReportProgress( string text, Int64 current, Int64 final) {
			lock( callBackLocker) {
	    		if( backgroundWorker!= null && !backgroundWorker.CancellationPending) {
	    			backgroundWorker.ReportProgress(0, new ProgressImpl(text,current,final));
	    		}
	    	}
		}
		
		public virtual Provider[] SetupProviders(bool quietMode, bool singleLoad) {
			List<Provider> senderList = new List<Provider>();
			SymbolInfo[] symbols = ProjectProperties.Starter.SymbolProperties;
			for(int i=0; i<symbols.Length; i++) {
	    		TickReader tickReader = new TickReader();
	    		tickReader.MaxCount = EndCount;	
	    		tickReader.StartTime = ProjectProperties.Starter.StartTime;
	    		tickReader.EndTime = ProjectProperties.Starter.EndTime;
	    		tickReader.BackgroundWorker = BackgroundWorker;
	    		tickReader.LogProgress = true;
	    		tickReader.BulkFileLoad = singleLoad;
	    		tickReader.QuietMode = quietMode;
	    		try { 
		    		tickReader.Initialize(DataFolder,symbols[i].Symbol);
					senderList.Add(tickReader);
	    		} catch( System.IO.FileNotFoundException ex) {
	    			throw new ApplicationException("Error: " + ex.Message);
	    		}
			}
			return senderList.ToArray();
		}
		
		public Provider[] SetupDataProviders() {
			try {
				List<Provider> senderList = new List<Provider>();
				SymbolInfo[] symbols = ProjectProperties.Starter.SymbolProperties;
				for(int i=0; i<symbols.Length; i++) {
					Provider provider = Factory.Provider.RemoteProvider();
					senderList.Add(provider);
				}
				return senderList.ToArray();
			} catch( Exception ex) {
				log.Error("Setup failed.", ex);
				throw;
			}
		}
		
		public virtual TickQueue[] SetupTickWriters() {
			List<TickQueue> queueList = new List<TickQueue>();
			SymbolInfo[] symbols = ProjectProperties.Starter.SymbolProperties;
			for(int i=0; i<symbols.Length; i++) {
				TickWriter tickWriter = Factory.TickUtil.TickWriter(false);
	    		tickWriter.Initialize(DataFolder,symbols[i].Symbol);
				queueList.Add(tickWriter.WriteQueue);
			}
			return queueList.ToArray();
		}
	    
		public abstract void Run(ModelInterface model);
		
		private static readonly string projectFileLoaderCategory = "TickZOOM";
		private static readonly string projectFileLoaderName = "Project File";
		public virtual void Run() {
			ModelLoaderInterface loader = Plugins.Instance.GetLoader(projectFileLoaderCategory + ": " + projectFileLoaderName);
			projectProperties = ProjectPropertiesCommon.Create(new StreamReader(ProjectFile));
			Run(loader);
		}
		
		public virtual void Run(ModelLoaderInterface loader) {
			if( loader.Category == projectFileLoaderCategory && loader.Name == projectFileLoaderName ) {
				log.Notice("Loading project from " + ProjectFile);
				projectProperties = ProjectPropertiesCommon.Create(new StreamReader(ProjectFile));
			}
			loader.OnInitialize(ProjectProperties);
			loader.OnLoad(ProjectProperties);
			ModelInterface model = loader.TopModel;
			Run( model);
		}
		
		public ShowChartCallback ShowChartCallback {
			get { return showChartCallback; }
			set { showChartCallback = value; }
		}
    	
		public BackgroundWorker BackgroundWorker {
			get { return backgroundWorker; }
			set { backgroundWorker = value; }
		}

		public string DataFolder {
			get { return dataFolder; }
			set { dataFolder = value; }
		}
    	
		public long EndCount {
			get { return endCount; }
			set { endCount = value; }
		}
   		
		public int StartCount {
			get { return startCount; }
			set { startCount = value; }
		}
		
		public TickQueue RealTimeTickQueue {
			get { return realTimeTickQueue; }
			set { realTimeTickQueue = value; }
		}
		
		public ProjectProperties ProjectProperties {
			get { return projectProperties; }
			set { projectProperties = value; }
		}
   		
		public CreateChartCallback CreateChartCallback {
			get { return createChartCallback; }
			set { createChartCallback = value; }
		}
		
		public string ProjectFile {
			get { return projectFile; }
			set { projectFile = value; }
		}
		
		public Provider[] DataFeeds {
			get { return tickProviders.ToArray(); }
			set { tickProviders = new List<Provider>(value); }
		}
		
		public void AddProvider(Provider provider)
		{
			tickProviders.Add(provider);
		}
		
		public abstract void Wait();
		
	}
}
