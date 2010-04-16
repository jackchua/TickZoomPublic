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
using NUnit.Framework;
using TickZoom.Api;
using TickZoom.Common;

namespace TickZoom.TradingFramework
{
	[TestFixture]
	public class BaseStatsTest
	{
		protected BaseStats baseStats;
		protected int maxCount = 200000;
		ExitStrategy exits;
		RandomCommon random;
		Starter  starter;
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		[TestFixtureSetUp]
		public void Setup() 
		{
			starter = new HistoricalStarter();
			random = new RandomCommon();
			ProfitLossDefault profitLossLogic = new ProfitLossDefault();
			profitLossLogic.Slippage = 0.0140;
			profitLossLogic.Commission = 0.010;
//			if( trace) log.Trace("RandomTest engine.Formula.Chain="+starter.Model.Chain.ToString());
			exits = random.ExitStrategy;
			
			exits.StopLoss = 0.050;
			exits.TargetProfit = 0;

			starter.EndCount = maxCount;
			starter.DataFolder = "TestData";
			starter.ProjectProperties.Starter.Symbols = "USD_JPY";
			starter.ProjectProperties.Starter.SymbolProperties[0].ProfitLoss = profitLossLogic;
			starter.Run(random);
			
			Constructor(random.Performance.ComboTrades);
			Assert.IsNotNull(baseStats,"MoneyManagerSupport constructor");
			Assert.Greater(random.Performance.ComboTrades.Count,0,"Need some trades to do this.");
			
//			Uncomment this loop to log the trades and compare to below
//			as to what they're supposed to be.
				
			TransactionPairs trades = random.Performance.ComboTrades;
			if( log.IsDebugEnabled) {
				for( int i=0; i<trades.Count; i++) {
					log.Debug( i + ": " + trades[i]);
				}
			}

//			0: 1,105320,2005-05-02 07:03:15.780,105340,2005-05-02 07:17:13.252,-4
//			1: 1,105290,2005-05-02 07:54:03.050,105350,2005-05-02 08:14:14.963,36
//			2: 1,105360,2005-05-02 08:52:45.785,105310,2005-05-02 09:09:21.169,-74
//			3: 1,105290,2005-05-02 09:17:29.899,105240,2005-05-02 09:20:28.225,-74
//			4: -1,105150,2005-05-02 09:39:43.181,105100,2005-05-02 09:50:51.729,26
//			5: -1,105030,2005-05-02 10:56:39.451,105080,2005-05-02 11:03:35.995,-74
//			6: 1,105200,2005-05-03 08:15:15.668,105150,2005-05-03 08:27:06.849,-74
//			7: -1,105140,2005-05-03 08:28:17.901,105150,2005-05-03 08:50:41.761,-34
//			8: 1,105150,2005-05-03 08:50:41.761,105130,2005-05-03 09:06:53.403,-44
//			9: -1,105130,2005-05-03 09:06:53.403,105130,2005-05-03 09:13:21.062,-24
//			10: 1,105130,2005-05-03 09:13:21.062,105120,2005-05-03 09:13:23.069,-34
//			11: -1,105090,2005-05-03 09:22:26.334,105090,2005-05-03 09:27:19.877,-24
//			12: 1,105090,2005-05-03 09:27:19.877,105130,2005-05-03 09:41:10.694,16
//			13: 1,105170,2005-05-03 09:46:34.495,105120,2005-05-03 10:00:54.735,-74
//			14: -1,105030,2005-05-03 10:35:11.274,105080,2005-05-03 10:40:38.072,-74
//			15: 1,105080,2005-05-03 10:41:02.126,105030,2005-05-03 10:45:26.821,-74
//			16: -1,105020,2005-05-03 10:57:16.127,105020,2005-05-03 10:59:47.711,-24
//			17: 1,105020,2005-05-03 10:59:47.711,104560,2005-05-04 07:00:08.015,-484
//			18: -1,104580,2005-05-04 07:14:00.252,104550,2005-05-04 07:35:09.332,6
//			19: -1,104570,2005-05-04 07:36:17.436,104360,2005-05-04 08:37:57.786,186
//			20: 1,104350,2005-05-04 08:55:14.192,104380,2005-05-04 08:57:09.379,6
//			21: 1,104390,2005-05-04 08:58:04.483,104360,2005-05-04 09:14:58.643,-54
//			22: -1,104360,2005-05-04 09:14:58.643,104410,2005-05-04 10:04:56.162,-74
//			23: 1,104370,2005-05-04 10:05:56.490,104350,2005-05-04 10:09:10.388,-44
//			24: -1,104330,2005-05-04 10:16:46.307,104380,2005-05-04 10:19:28.029,-74
//			25: 1,104370,2005-05-04 10:39:09.740,104320,2005-05-04 11:31:52.298,-74
//			26: -1,104310,2005-05-05 07:15:37.270,104320,2005-05-05 07:17:28.372,-34
//			27: 1,104320,2005-05-05 07:17:28.372,104300,2005-05-05 07:30:10.182,-44
//			28: -1,104300,2005-05-05 07:30:10.182,104280,2005-05-05 07:33:38.448,-4
//			29: 1,104280,2005-05-05 07:33:38.448,104350,2005-05-05 07:49:07.816,46
//			30: -1,104350,2005-05-05 07:49:07.816,104380,2005-05-05 07:58:18.351,-54
//			31: 1,104380,2005-05-05 07:58:18.351,104400,2005-05-05 08:20:17.920,-4
//			32: -1,104440,2005-05-05 08:48:45.249,104480,2005-05-05 08:53:22.514,-64
//			33: 1,104480,2005-05-05 08:53:22.514,104430,2005-05-05 08:58:35.105,-74
//			34: -1,104390,2005-05-05 09:11:35.199,104310,2005-05-05 09:23:22.693,56
//			35: -1,104350,2005-05-05 09:31:55.188,104310,2005-05-05 09:36:50.454,16
//			36: 1,104310,2005-05-05 09:36:50.454,104260,2005-05-05 09:39:34.270,-74
//			37: -1,104330,2005-05-05 09:44:25.264,104380,2005-05-05 10:12:27.183,-74
//			38: 1,104380,2005-05-05 10:17:07.033,104400,2005-05-05 10:25:06.165,-4
//			39: -1,104400,2005-05-05 10:25:06.165,104380,2005-05-05 10:38:55.157,-4
//			40: 1,104380,2005-05-05 10:38:55.157,104400,2005-05-05 10:48:58.981,-4
//			41: -1,104650,2005-05-06 07:01:39.259,104570,2005-05-06 07:28:32.159,56
//			42: 1,104570,2005-05-06 07:28:32.159,104560,2005-05-06 07:29:17.237,-34
//			43: -1,104560,2005-05-06 07:29:17.237,104610,2005-05-06 07:37:44.939,-74
//			44: 1,104630,2005-05-06 07:45:41.450,104640,2005-05-06 07:50:24.819,-14
//			45: -1,104640,2005-05-06 07:50:24.819,104690,2005-05-06 08:20:10.236,-74
//			46: 1,104650,2005-05-06 08:23:44.508,104600,2005-05-06 08:25:20.657,-74
//			47: -1,104670,2005-05-06 08:41:00.760,104670,2005-05-06 08:52:22.779,-24
//			48: 1,104670,2005-05-06 08:52:22.779,104880,2005-05-06 10:05:32.889,186
//			49: -1,104880,2005-05-06 10:05:32.889,104930,2005-05-06 10:07:50.758,-74
//			50: 1,104880,2005-05-06 10:13:17.204,104830,2005-05-06 10:17:29.806,-74
//			51: -1,105340,2005-05-09 07:05:17.394,105330,2005-05-09 07:17:59.670,-14
//			52: -1,105440,2005-05-09 07:57:01.620,105430,2005-05-09 07:58:14.761,-14
//			53: 1,105500,2005-05-09 08:05:47.007,105550,2005-05-09 08:19:47.822,26
//			54: -1,105550,2005-05-09 08:19:47.822,105550,2005-05-09 08:44:05.031,-24
//			55: 1,105550,2005-05-09 08:44:05.031,105560,2005-05-09 08:46:13.252,-14
//			56: -1,105640,2005-05-09 09:40:07.041,105650,2005-05-09 09:42:20.374,-34
//			57: 1,105640,2005-05-09 09:51:28.282,105590,2005-05-09 10:23:27.903,-74
//			58: -1,105580,2005-05-09 10:31:25.122,105450,2005-05-09 10:50:01.571,106
//			59: 1,105500,2005-05-09 10:56:53.779,105450,2005-05-09 11:01:38.771,-74
//			60: -1,105740,2005-05-10 07:11:21.000,105790,2005-05-10 07:15:37.285,-74
//			61: 1,105800,2005-05-10 07:34:23.674,105780,2005-05-10 07:39:13.942,-44
//			62: -1,105750,2005-05-10 07:46:10.319,105680,2005-05-10 07:56:54.954,46
//			63: 1,105670,2005-05-10 08:00:31.695,105650,2005-05-10 08:21:01.701,-44
//			64: -1,105650,2005-05-10 08:21:01.701,105700,2005-05-10 08:24:49.989,-74
//			65: 1,105630,2005-05-10 09:05:32.461,105640,2005-05-10 09:08:21.796,-14
//			66: -1,105690,2005-05-10 09:36:12.151,105680,2005-05-10 09:36:14.159,-14
//			67: 1,105650,2005-05-10 09:40:32.881,105600,2005-05-10 10:20:21.059,-74
//			68: -1,105320,2005-05-11 07:08:37.727,105370,2005-05-11 07:18:52.385,-74
//			69: -1,105340,2005-05-11 07:42:10.367,105390,2005-05-11 08:00:00.540,-74
//			70: -1,105310,2005-05-11 08:55:55.425,105360,2005-05-11 09:04:32.125,-74
//			71: -1,105730,2005-05-11 10:30:08.455,105780,2005-05-11 10:39:14.956,-74
//			72: 1,105790,2005-05-11 10:54:20.286,105780,2005-05-11 10:55:14.459,-34
//			73: -1,106170,2005-05-12 07:42:12.249,106170,2005-05-12 07:43:54.354,-24
//			74: 1,106170,2005-05-12 07:43:54.354,106190,2005-05-12 07:54:52.477,-4
//			75: -1,106190,2005-05-12 07:54:52.477,106240,2005-05-12 08:05:43.074,-74
//			76: 1,106200,2005-05-12 08:58:00.634,106330,2005-05-12 09:25:46.966,106
//			77: -1,106350,2005-05-12 09:25:50.990,106400,2005-05-12 09:30:06.405,-74
//			78: 1,106560,2005-05-12 09:34:38.202,106510,2005-05-12 09:37:45.404,-74
//			79: -1,106540,2005-05-12 09:41:18.723,106590,2005-05-12 09:41:35.848,-74
//			80: 1,106810,2005-05-12 09:51:16.964,106760,2005-05-12 10:00:18.694,-74
//			81: 1,106760,2005-05-12 10:18:24.240,106710,2005-05-12 10:22:12.811,-74
//			82: -1,106710,2005-05-12 10:30:24.395,106660,2005-05-12 10:45:29.119,26
//			83: -1,107150,2005-05-13 07:04:27.764,107110,2005-05-13 07:29:20.582,16
//			84: -1,107140,2005-05-13 07:41:59.288,107130,2005-05-13 07:52:12.783,-14
//			85: -1,107160,2005-05-13 08:19:21.248,107170,2005-05-13 08:22:04.536,-34
//			86: -1,107190,2005-05-13 08:27:09.344,106990,2005-05-13 09:12:49.761,176
//			87: 1,107010,2005-05-13 09:13:18.828,107050,2005-05-13 09:42:06.278,16
//			88: 1,107060,2005-05-13 10:11:03.404,107010,2005-05-13 10:16:29.550,-74
//			89: -1,107000,2005-05-13 10:19:57.488,107000,2005-05-13 10:20:08.546,-24
//			90: 1,107030,2005-05-13 10:29:35.827,106980,2005-05-13 10:48:34.986,-74
//			91: -1,107600,2005-05-16 07:15:10.967,107610,2005-05-16 07:21:01.890,-34
//			92: 1,107610,2005-05-16 07:21:01.890,107650,2005-05-16 07:28:22.086,16
//			93: 1,107690,2005-05-16 07:44:35.337,107640,2005-05-16 08:24:16.973,-74
//			94: 1,107670,2005-05-16 08:43:05.620,107620,2005-05-16 08:48:17.126,-74
//			95: 1,107570,2005-05-16 08:58:13.199,107520,2005-05-16 09:04:11.077,-74
//			96: 1,107500,2005-05-16 09:40:47.135,107550,2005-05-16 09:47:59.110,26
//			97: -1,107550,2005-05-16 09:47:59.110,107580,2005-05-16 09:53:58.018,-54
//			98: -1,107360,2005-05-16 10:27:58.643,107260,2005-05-16 10:53:31.461,76
//			99: -1,107090,2005-05-17 07:12:45.688,107140,2005-05-17 07:40:22.556,-74
//			100: 1,107160,2005-05-17 07:43:11.816,107320,2005-05-17 08:01:22.581,136
//			101: 1,107290,2005-05-17 08:14:02.953,107290,2005-05-17 08:16:20.162,-24
//			102: 1,107220,2005-05-17 08:28:27.371,107280,2005-05-17 08:40:03.957,36
//			103: -1,107280,2005-05-17 08:40:03.957,107330,2005-05-17 08:40:53.166,-74
//			104: 1,107310,2005-05-17 08:58:08.957,107260,2005-05-17 09:08:43.168,-74
//			105: -1,107220,2005-05-17 09:09:25.268,107270,2005-05-17 09:17:34.236,-74
//			106: 1,107330,2005-05-17 09:46:01.885,107280,2005-05-17 09:54:44.600,-74
//			107: -1,107310,2005-05-17 10:05:58.137,107250,2005-05-17 10:30:07.419,36
//			108: 1,107420,2005-05-18 07:07:26.938,107420,2005-05-18 07:16:12.837,-24
//			109: -1,107420,2005-05-18 07:16:12.837,107470,2005-05-18 07:27:53.753,-74
		
		}
    	
		public virtual void Constructor(TransactionPairs trades)
		{
			baseStats = new BaseStats(trades);
		}
		
		[Test]
		public void Count() {
			Assert.AreEqual(baseStats.Count,random.Performance.ComboTrades.Count,"Count of Trades");
			Assert.AreEqual(186,baseStats.Count,"Count of Trades");
		}
		
		[Test]
		public void ProfitLoss() {
//			for( int i = 0; i< manager.Trades.Count; i++) {
//				TickConsole.WriteLine(i + ": " + manager.Trades[i]);
//			}
			Assert.AreEqual(-4.7340,Math.Round(baseStats.ProfitLoss,4),"Profit Loss");
		}

		[Test]
		public void Average() {
			Assert.AreEqual(-0.0255,Math.Round(baseStats.Average,4),"Average Trade");
		}
		
		[Test]
		public void Variance() {
			Assert.AreEqual(0.0056,Math.Round(baseStats.Variance,4),"Variance");
		}
		
		[Test]
		public void DownsideRisk() {
			Assert.AreEqual(0.4839,Math.Round(baseStats.DownsideRisk,4),"DownsideRisk");
		}
		
		[Test]
		public void SortinoRatio() {
			Assert.AreEqual(-0.0227,Math.Round(baseStats.SortinoRatio,4),"SortinoRatio");
		}
		
		[Test]
		public void SharpeRatio() {
			Assert.AreEqual(-182.4362,Math.Round(baseStats.SharpeRatio,4),"SharpeRatio");
		}
		
		[Test]
		public void ModifiedSharpe() {
			Assert.AreEqual(-0.3407,Math.Round(baseStats.ModifiedSharpe,4),"ModifiedSharpe");
		}
		
		[Test]
		public void Volatility() {
			Assert.AreEqual(0.0001,Math.Round(baseStats.Volatility,4),"Volatility");
		}
		
		[Test]
		public void AnnualReturn() {
			Assert.AreEqual(-0.0010,Math.Round(baseStats.AnnualReturn,4),"AnnualReturn");
		}
		
		[Test]
		public void StandardDeviation() {
			Assert.AreEqual(0.0747,Math.Round(baseStats.StandardDeviation,4),"StandardDeviation");
		}
		
		[Test]
		public void Name() {
			string expected = "Wayne";
			baseStats.Name = expected;
			Assert.AreEqual(expected,baseStats.Name,"Name");
		}
		
		[Test]
		public void Trades() {
			TransactionPairs trades = baseStats.Trades;
			Assert.AreEqual(trades,baseStats.Trades,"Trades (list)");
		}
		
		public virtual void ToStringTest() {
			baseStats.Name = "Test Strategy";
		}
		[Test]
		public virtual void ToStringTestRunner() {
			ToStringTest();
		}
	}
}
