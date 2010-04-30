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
	    int tasksRemaining;
	    
	    public OptimizeStarter() {
	    }
	    
		public override void Run(ModelInterface model)
		{
			throw new MustUseLoaderException("Must set ModelLoader instead of Model for Optimization");
		}
			
		List<TickEngine> engineIterations;
		public override void Run(ModelLoaderInterface loader)
		{
    		try {
    			if( loader.OptimizeOutput == null) {
		    		Directory.CreateDirectory( Path.GetDirectoryName(FileName));
		    		File.Delete(FileName);
    			}
    		} catch( Exception ex) {
    			log.Error("Error while creating directory and deleting '" + FileName + "'.",ex);
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
			
			WriteEngineResults(loader,engineIterations);

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
			
			if( !SetOptimizeValues(loader)) {
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
	
			engine.ReportWriter.OptimizeValues = OptimizeValues;
			
			totalTasks++;
			engine.OptimizePass = totalTasks;
			engine.QueueTask();
			engineIterations.Add(engine);
		}

	}
}
