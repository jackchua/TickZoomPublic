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
using System.Runtime.InteropServices;

using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.Common
{
	/// <summary>
	/// Description of Test.
	/// </summary>
	public class OptimizeStarter : StarterCommon
	{
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		int totalTasks=0;
		ModelLoaderInterface loader;
	    string storageFolder;
	    int tasksRemaining;
	    string fileName;
	    
	    public OptimizeStarter() {
    		storageFolder = Factory.Settings["AppDataFolder"];
   			if( storageFolder == null) {
       			throw new ApplicationException( "Must set AppDataFolder property in app.config");
   			}
	    }
	    
		public override void Run(ModelInterface model)
		{
			throw new MustUseLoaderException("Must set ModelLoader instead of Model for Optimization");
		}
			
		List<TickEngine> engineIterations;
		public override void Run(ModelLoaderInterface loader)
		{
    		fileName = storageFolder + @"\Statistics\optimizeResults.csv";
    		try {
    			if( loader.OptimizeOutput == null) {
		    		Directory.CreateDirectory( Path.GetDirectoryName(fileName));
		    		File.Delete(fileName);
    			}
    		} catch( Exception ex) {
    			log.Error("Error while creating directory and deleting '" + fileName + "'.",ex);
    			return;
    		}
			this.loader = loader;
			this.loader.QuietMode = true;
			int startMillis = Environment.TickCount;
			engineIterations = new List<TickEngine>();
			
			loader.OnInitialize(ProjectProperties);
			
			totalTasks = 0;

			try {
				RecursiveOptimize(0);
			} catch( ApplicationException ex) {
				log.Error(ex.Message);
				return;
			}
			
			tasksRemaining = totalTasks;

			ReportProgress( "Optimizing...", 0, totalTasks);

			GetEngineResults();
			
			WriteEngineResults();

			engineIterations.Clear();

			ReportProgress( "Optimizing Complete", totalTasks-tasksRemaining, totalTasks);

			int elapsedMillis = Environment.TickCount - startMillis;
			log.Notice("Finished optimizing in " + elapsedMillis + "ms.");
		}
		
		public override void Wait() {
			// finishes during Run()
		}
		
		private void GetEngineResults() {
			for( int i=0; i<engineIterations.Count; i++) {
				TickEngine engine = engineIterations[i];
				engine.WaitTask();
		        --tasksRemaining;
				ReportProgress( "Optimizing...", totalTasks-tasksRemaining, totalTasks);
			}
		}

		private void WriteEngineResults() {
    		try {
    			if( loader.OptimizeOutput == null) {
					loader.OptimizeOutput = new FileStream(fileName,FileMode.Append);
    			}
    			StreamWriter fwriter = new StreamWriter(loader.OptimizeOutput);
				TickEngine engine = engineIterations[0];
				string header = engine.OptimizeHeader;
				if( header != null && header.Length > 0) {
					fwriter.WriteLine(header);
				}
				for( int i=0; i<engineIterations.Count; i++) {
					engine = engineIterations[i];
					string stats = engine.OptimizeResult;
					if( stats != null && stats.Length > 0) {
						fwriter.WriteLine(stats);
					}
				}
	       		fwriter.Close();
    		} catch( Exception ex) {
    			log.Error("ERROR: Problem writing optimizer results.", ex);
    		}
		}

		private bool CancelPending {
			get { if( BackgroundWorker != null) {
					return BackgroundWorker.CancellationPending;
				} else {
					return false;
				}
			}
		}
		
		private void RecursiveOptimize(int index) {
			if( index < loader.Variables.Count) {
				// Loop through a specific optimization variable.
				for( double i = loader.Variables[index].Start;
				    i <= loader.Variables[index].End;
				    i = Math.Round(i+loader.Variables[index].Increment,9)) {
//				    i += loader.Variables[index].Increment) {
					loader.Variables[index].Value = i.ToString();
					RecursiveOptimize(index+1);
				}
			} else {
				ProcessHistorical();
			}
		}

		public void ProcessHistorical() {
	    	loader.OnClear();
			loader.OnLoad(ProjectProperties);
			
			// Then set them for logging separately to optimization reports.
			Dictionary<string,object> optimizeValues = new Dictionary<string,object>();
			for( int i = 0; i < loader.Variables.Count; i++) {
				optimizeValues[loader.Variables[i].Name] = loader.Variables[i].Value;
			}
			
			if( !SetOptimizeValues(optimizeValues)) {
				throw new ApplicationException("Error, setting optimize variables.");
			}
	    			
			TickEngine engine = Factory.Engine.TickEngine;
			engine.Model = loader.TopModel;
			engine.SymbolInfo = ProjectProperties.Starter.SymbolProperties;
			
			engine.IntervalDefault = ProjectProperties.Starter.IntervalDefault;
			engine.EnableTickFilter = ProjectProperties.Engine.EnableTickFilter;
			
			engine.Providers = SetupProviders(true,false);
			engine.BackgroundWorker = BackgroundWorker;
			engine.RunMode = RunMode.Historical;
			engine.StartCount = StartCount;
			engine.EndCount = EndCount;
			engine.StartTime = ProjectProperties.Starter.StartTime;
			engine.EndTime = ProjectProperties.Starter.EndTime;
	
			if(CancelPending) return;
			
			engine.BackgroundWorker = BackgroundWorker;
			engine.QuietMode = true;
	
			if(CancelPending) return;
			
			engine.ReportWriter.OptimizeValues = optimizeValues;
			
			totalTasks++;
			engine.OptimizePass = totalTasks;
			engine.QueueTask();
			engineIterations.Add(engine);
		}
		
	    internal bool SetOptimizeValues(Dictionary<string,object> optimizeValues) {
	    	bool retVal = true;
	    	foreach( KeyValuePair<string, object> kvp in optimizeValues) {
	    		string[] namePairs = kvp.Key.Split('.');
	    		if( namePairs.Length < 2) {
	    			log.Error("Sorry, the optimizer variable '" + kvp.Key + "' was not found.");
	    			retVal = false;
	    			continue;
	    		}
	    		string strategyName = namePairs[0];
	    		StrategyInterface strategy = null;
	    		foreach( StrategyInterface tempStrategy in GetStrategyList(loader.TopModel.Chain.Tail)) {
	    			if( tempStrategy.Name == strategyName) {
	    				strategy = tempStrategy;
	    				break;
	    			}
	    		}
	    		if( strategy == null) {
	    			log.Error("Sorry, the optimizer variable '" + kvp.Key + "' was not found.");
	    			retVal = false;
	    			continue;
	    		}
    			string propertyName = namePairs[1];
				PropertyDescriptorCollection props = TypeDescriptor.GetProperties(strategy);
				PropertyDescriptor property = null;
				for( int i = 0; i < props.Count; i++) {
					PropertyDescriptor tempProperty = props[i];
					if( tempProperty.Name == propertyName) {
						property = tempProperty;
						break;
					}
		    	}
				if( property == null) {
	    			log.Error("Sorry, the optimizer variable '" + kvp.Key + "' was not found.");
	    			retVal = false;
	    			continue;
				}
				if( namePairs.Length == 2) {
					if( !SetProperty(strategy,property,kvp.Value)) {
						log.Error("Sorry, the value '" + kvp.Value + "' isn't valid for optimizer variable '" + kvp.Key + "'.");
		    			retVal = false;
		    			continue;
					}
				} else if( namePairs.Length == 3) {
					StrategyInterceptor strategySupport = property.GetValue(strategy) as StrategyInterceptor;
					if( strategySupport == null) {
		    			log.Error("Sorry, the optimizer variable '" + kvp.Key + "' was not found.");
		    			retVal = false;
		    			continue;
					}
					string strategySupportName = namePairs[1];
					propertyName = namePairs[2];
					props = TypeDescriptor.GetProperties(strategySupport);
					property = null;
					for( int i = 0; i < props.Count; i++) {
						PropertyDescriptor tempProperty = props[i];
						if( tempProperty.Name == propertyName) {
							property = tempProperty;
							break;
						}
			    	}
					if( property == null) {
		    			log.Error("Sorry, the optimizer variable '" + kvp.Key + "' was not found.");
		    			retVal = false;
		    			continue;
					}
					if( !SetProperty(strategySupport,property,kvp.Value)) {
						log.Error("Sorry, the value '" + kvp.Value + "' isn't valid for optimizer variable '" + kvp.Key + "'.");
		    			retVal = false;
		    			continue;
					}
				}
	    	}
	    	return retVal;
	    }

		private bool SetProperty( object target, PropertyDescriptor property, object value) {
			Type type = property.PropertyType;
			TypeConverter convert = TypeDescriptor.GetConverter(type);
	        if (!convert.IsValid(value)) {
				return false;
	        }
			object convertedValue = convert.ConvertFrom(value);
			property.SetValue(target,convertedValue);
			return true;
		}
		
   		internal IEnumerable<StrategyInterface> GetStrategyList(Chain chain) {
   			StrategyInterface currentStrategy = chain.Model as StrategyInterface;
   			if( currentStrategy != null) {
   				yield return currentStrategy;
   			}
   			foreach( Chain tempChain in chain.Dependencies) {
   				foreach( StrategyInterface strategy in GetStrategyList(tempChain.Tail)) {
   					yield return strategy;
   				}
   			}
   			if( chain.Previous.Model != null) {
   				foreach( StrategyInterface strategy in GetStrategyList(chain.Previous)) {
   					yield return strategy;
   				}
   			}
   		}
		
	}
}
