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

#region Namespaces
using System;
using System.ComponentModel;
using System.Drawing;

using TickZoom.Api;
using TickZoom.Common;

#endregion

namespace TickZoom.StarterTest
{
	public class TestSimpleStrategy : Strategy
	{
		
		public TestSimpleStrategy() {
			Performance.GraphTrades = true;
			Performance.Equity.GraphEquity = true;
		}
		
		public override void OnInitialize()
		{
		}
		
		public override bool OnProcessTick(Tick tick)
		{
			return true;
		}
		
		public override bool OnIntervalClose()
		{
			// Example log message.
			//Log.WriteLine( "close: " + Ticks[0] + " " + Minutes.Close[0] + " " + Minutes.Time[0]);
			
			if( Bars.Close[0] > Bars.High[1]) {
				Orders.Enter.ActiveNow.BuyMarket();
			}
			if( Bars.Close[0] < Bars.Low[1]) {
				Orders.Enter.ActiveNow.SellMarket();
			}
			return true;
		}
		
		public override string OnGetOptimizeHeader(System.Collections.Generic.Dictionary<string, object> optimizeValues)
		{
			return "DailyCount,DailyWinRate,DailyProfitFactor," + base.OnGetOptimizeHeader(optimizeValues);
		}
		
		public override string OnGetOptimizeResult(System.Collections.Generic.Dictionary<string, object> optimizeValues)
		{
			EquityStats stats = Performance.Equity.CalculateStatistics();
			return stats.Daily.Count + "," + stats.Daily.WinRate + "," + stats.Daily.ProfitFactor + "," + base.OnGetOptimizeResult(optimizeValues);
		}
		
	}
}