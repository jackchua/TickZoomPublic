#region Copyright
/*
 * Copyright 2008 M. Wayne Walter
 * Software: TickZoom Trading Platform
 * User: Wayne Walter
 * 
 * You can use and modify this software under the terms of the
 * TickZOOM General Public License Version 1.0 or (at your option)
 * any later version.
 * 
 * Businesses are restricted to 30 days of use.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * TickZOOM General Public License for more details.
 *
 * You should have received a copy of the TickZOOM General Public
 * License along with this program.  If not, see
 * 
 * 
 *
 * User: Wayne Walter
 * Date: 5/25/2009
 * Time: 3:36 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using NUnit.Framework;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;
using ZedGraph;

namespace Loaders
{
	[TestFixture]
	public class ExampleReversalTradeOnlyTest : StrategyTest
	{
		#region SetupTest
		Log log = Factory.Log.GetLogger(typeof(ExampleReversalTest));
		protected Strategy strategy;
		public ExampleReversalTradeOnlyTest() {
			Symbols = "/ESH0";
		}
		
		[TestFixtureSetUp]
		public override void RunStrategy() {
			base.RunStrategy();
			try {
				Starter starter = CreateStarter();
				
				// Set run properties as in the GUI.
				starter.ProjectProperties.Starter.StartTime = new TimeStamp("1800/1/1");
	    		starter.ProjectProperties.Starter.EndTime = new TimeStamp("2010/2/17");
	    		starter.DataFolder = "TestData";
	    		starter.ProjectProperties.Starter.Symbols = Symbols;
				starter.ProjectProperties.Starter.IntervalDefault = Intervals.Minute1;
				starter.ProjectProperties.Engine.RealtimeOutput = false;
				
	    		starter.CreateChartCallback = new CreateChartCallback(HistoricalCreateChart);
	    		starter.ShowChartCallback = new ShowChartCallback(HistoricalShowChart);
	    		
	    		// Run the loader.
				ExampleReversalLoader loader = new ExampleReversalLoader();
	    		starter.Run(loader);
	
	    		// Get the stategy
	    		strategy = loader.TopModel as ExampleReversalStrategy;
			} catch( Exception ex) {
				log.Error( "Setup failed.", ex);
				throw;
			}
		}
		#endregion
		
		[Test]
		public void VerifyCurrentEquity() {
			Assert.AreEqual( Math.Round(9975.00,2),Math.Round(strategy.Performance.Equity.CurrentEquity,2),"current equity");
		}
		[Test]
		public void VerifyOpenEquity() {
			Assert.AreEqual( Math.Round(12.5000,2),Math.Round(strategy.Performance.Equity.OpenEquity,2),"open equity");
		}
		[Test]
		public void VerifyClosedEquity() {
			Assert.AreEqual( Math.Round(9962.50,2),Math.Round(strategy.Performance.Equity.ClosedEquity,2),"open equity");
		}
		[Test]
		public void VerifyStartingEquity() {
			Assert.AreEqual( 10000,strategy.Performance.Equity.StartingEquity,"starting equity");
		}
		[Test]
		public void CompareTradeCount() {
			Assert.AreEqual( 2,strategy.Performance.ComboTrades.Count, "trade count");
		}
		
		[Test]
		public void BuyStopSellStopTest() {
			VerifyPair( strategy, 0, "2010-02-16 16:52:00.060", 1061.00d,
			                 "2010-02-16 16:54:00.183",1061.75d);
	
		}
	
		[Test]
		public void LastTradeTest() {
			VerifyPair( strategy, strategy.Performance.ComboTrades.Count-1, "2010-02-16 16:54:00.183", 1061.75d,
			                 "2010-02-16 16:59:56.140",1062.0d);
		}
		
		[Test]
		public void SellLimitBuyLimitTest() {
			VerifyPair( strategy, 1, "2010-02-16 16:54:00.183", 1061.75,
			                 "2010-02-16 16:59:56.140", 1062.0d);
		}
		
		[Test]
		public void VerifyBarData() {
			Bars days = strategy.Data.Get(Intervals.Minute1);
			Assert.AreEqual( 11, days.BarCount);
		}
		
		[Test]
		public void VerifyChartData() {
			Assert.AreEqual(1,ChartCount);
			ChartControl chart = GetChart(0);
	     		GraphPane pane = chart.DataGraph.MasterPane.PaneList[0];
	    		Assert.IsNotNull(pane.CurveList);
	    		Assert.Greater(pane.CurveList.Count,0);
	    		Assert.AreEqual(11,pane.CurveList[0].Points.Count);
		}
		
		[Test]
		public void CompareBars() {
			CompareChart(strategy,GetChart(strategy.SymbolDefault));
		}
	}
}
