﻿#region Copyright
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
using TickZoom.TickUtil;

namespace Loaders
{
	[TestFixture]
	public class ExampleDualStrategyTest : StrategyTest
	{
		#region SetupTest
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		ExampleSimpleStrategy exampleSimple;
		ExampleOrderStrategy fourTicksStrategy;
		Portfolio portfolio;
		
		public ExampleDualStrategyTest() {
			Symbols = "Daily4Sim";
		}
		
		[TestFixtureSetUp]
		public override void RunStrategy() {
			base.RunStrategy();
			try {
				Starter starter = CreateStarter();
				
				// Set run properties as in the GUI.
				starter.ProjectProperties.Starter.StartTime = new TimeStamp(1800,1,1);
	    		starter.ProjectProperties.Starter.EndTime = new TimeStamp(1990,1,1);
	    		starter.DataFolder = "TestData";
	    		starter.ProjectProperties.Starter.Symbols = Symbols;
				starter.ProjectProperties.Starter.IntervalDefault = Intervals.Day1;
				starter.ProjectProperties.Engine.RealtimeOutput = false;
				
				// Set up chart
		    	starter.CreateChartCallback = new CreateChartCallback(HistoricalCreateChart);
	    		starter.ShowChartCallback = null;
	
				// Run the loader.
				ModelLoaderCommon loader = new ExampleDualStrategyLoader();
	    		starter.Run(loader);
	
	 			ShowChartCallback showChartCallback = new ShowChartCallback(HistoricalShowChart);
	 			showChartCallback();
	 
	 			// Get the stategy
	    		portfolio = loader.TopModel as Portfolio;
	    		fourTicksStrategy = portfolio.Strategies[0] as ExampleOrderStrategy;
	    		exampleSimple = portfolio.Strategies[1] as ExampleSimpleStrategy;
	    		LoadTrades();
			} catch( Exception ex) {
				log.Error("Setup error.",ex);
				throw;
			}
		}
		#endregion
		
		[Test]
		public void CheckPortfolio() {
			double expected = exampleSimple.Performance.Equity.CurrentEquity;
			expected -= exampleSimple.Performance.Equity.StartingEquity;
			expected += fourTicksStrategy.Performance.Equity.CurrentEquity;
			expected -= fourTicksStrategy.Performance.Equity.StartingEquity;
			double portfolioTotal = portfolio.Performance.Equity.CurrentEquity;
			portfolioTotal -= portfolio.Performance.Equity.StartingEquity;
			Assert.AreEqual(expected, portfolioTotal);
			Assert.AreEqual(-297800, portfolioTotal);
		}
		
		[Test]
		public void CheckPortfolioClosedEquity() {
			double expected = exampleSimple.Performance.Equity.ClosedEquity;
			expected -= exampleSimple.Performance.Equity.StartingEquity;
			expected += fourTicksStrategy.Performance.Equity.ClosedEquity;
			expected -= fourTicksStrategy.Performance.Equity.StartingEquity;
			double portfolioTotal = portfolio.Performance.Equity.ClosedEquity;
			portfolioTotal -= portfolio.Performance.Equity.StartingEquity;
			Assert.AreEqual(expected, portfolioTotal);
			Assert.AreEqual(-296100, portfolioTotal);
		}
		
		[Test]
		public void CheckPortfolioOpenEquity() {
			double expected = exampleSimple.Performance.Equity.OpenEquity;
			expected += fourTicksStrategy.Performance.Equity.OpenEquity;
			Assert.AreEqual(expected, portfolio.Performance.Equity.OpenEquity);
			Assert.AreEqual(-1700, portfolio.Performance.Equity.OpenEquity);
		}
		
		[Test]
		public void VerifyTradeCount() {
			TransactionPairs exampleSimpleRTs = exampleSimple.Performance.ComboTrades;
			TransactionPairs fullTicksRTs = fourTicksStrategy.Performance.ComboTrades;
			Assert.AreEqual(472,fullTicksRTs.Count, "trade count");
			Assert.AreEqual(378,exampleSimpleRTs.Count, "trade count");
		}
		
		[Test]
		public void CompareBars0() {
			CompareChart(fourTicksStrategy,GetChart(fourTicksStrategy.SymbolDefault));
		}
		
		[Test]
		public void CompareBars1() {
			CompareChart(exampleSimple,GetChart(exampleSimple.SymbolDefault));
		}
		
		[Test]
		public void VerifyStrategy1Trades() {
			VerifyTrades(fourTicksStrategy);
		}
	
		[Test]
		public void VerifyStrategy2Trades() {
			VerifyTrades(exampleSimple);
		}
		
		[Test]
		public void VerifyStrategy1TradeCount() {
			VerifyTradeCount(fourTicksStrategy);
		}
		
		[Test]
		public void VerifyStrategy2TradeCount() {
			VerifyTradeCount(exampleSimple);
		}
	}
}