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
using NUnit.Framework;
using TickZoom.Api;
using TickZoom.Common;

namespace TickZoom.StarterTest
{
	[TestFixture]
	public class ProjectStarterTest
	{
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		List<ChartThread> chartThreads = new List<ChartThread>();
	    public delegate void ShowChartDelegate(ChartControl chart);
		[SetUp]
		public void TestSetup() {
		}
		
		[TearDown]
		public void TearDown() {
		}
		
		[Test]
		public void TestHistorical()
		{
			Starter starter = new HistoricalStarter();
			starter.ProjectFile = @"..\..\Platform\TickZoomTesting\Startup\singleOptimize.tzproj";
			// No Charts for these tests.
    		starter.DataFolder = "TestData";
    		starter.Run();
		}
		
		[Test]
		public void TestOptimize()
		{
			Starter starter = new OptimizeStarter();
			starter.ProjectFile = @"..\..\Platform\TickZoomTesting\Startup\singleOptimize.tzproj";
    		starter.DataFolder = "TestData";
    		starter.Run();	
		}
		
		[Test]
		public void TestGenetic()
		{
			Starter starter = new GeneticStarter();
			starter.ProjectFile = @"..\..\Platform\TickZoomTesting\Startup\singleOptimize.tzproj";
     		starter.DataFolder = "TestData";
    		starter.Run();
		}
	    
	}
}
