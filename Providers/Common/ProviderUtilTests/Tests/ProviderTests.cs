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
using System.Diagnostics;
using System.IO;
using System.Threading;

using NUnit.Framework;
using TickZoom.Api;

namespace TickZoom.Test
{
	public abstract class ProviderTests
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(TimeAndSales));
		private static readonly bool debug = log.IsDebugEnabled;		
		private List<LogicalOrder> orders = new List<LogicalOrder>();
		protected SymbolInfo symbol;
		public enum TickTest {
			TimeAndSales,
			Level1
		}
		
		public ProviderTests() {
			string providerAssembly = Factory.Settings["ProviderAssembly"];
			if( !string.IsNullOrEmpty(providerAssembly)) {
				SetProviderAssembly( providerAssembly);
			}
			SyncTicks.Enabled = true;
		}
			
		[TestFixtureSetUp]
		public void Init()
		{
			string appData = Factory.Settings["AppDataFolder"];
			File.Delete( Factory.Log.LogFolder + @"\" + providerAssembly+"Tests.log");
			File.Delete( Factory.Log.LogFolder + @"\" + providerAssembly+".log");
			
		}
		
		public void SetSymbol( string symbolString) {
			symbol = Factory.Symbol.LookupSymbol(symbolString);
		}
		
		private string providerAssembly = "TickZoomProviderMock";
		
		public void SetProviderAssembly( string providerAssembly) {
			this.providerAssembly = providerAssembly;	
		}
		
		public abstract Provider ProviderFactory();
		
		public Provider CreateProvider(bool inProcessFlag) {
			Provider provider;
			if( inProcessFlag) {
				provider = ProviderFactory();
			} else {
				provider = Factory.Provider.ProviderProcess("127.0.0.1",6492,providerAssembly+".exe");
			}
			return provider;
		}
		
		[TearDown]
		public virtual void TearDown() {
			int start = Environment.TickCount;
			int elapsed = 0;
			if( Factory.Parallel.Tasks.Length > 0) {
				log.Warn("Found " + Factory.Parallel.Tasks.Length + " Parallel tasks still running...");
			}
			while( elapsed < 10000 && Factory.Parallel.Tasks.Length > 0) {
				Thread.Sleep(1000);
				elapsed = Environment.TickCount - start;
			}
			if( Factory.Parallel.Tasks.Length > 0) {
				log.Error("These tasks still running after " + elapsed + "ms.");
				log.Error(Factory.Parallel.GetStats());
			}
			Assert.AreEqual(0,Factory.Parallel.Tasks.Length,"running tasks");
		}
		
		[Test]
		public void DemoConnectionTest() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = CreateProvider(true)) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				if(debug) log.Debug("===DemoConnectionTest===");
				if(debug) log.Debug("===StartSymbol===");
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
				if(debug) log.Debug("===VerifyFeed===");
		  		long count = verify.Verify(2,assertTick,symbol,25);
		  		Assert.GreaterOrEqual(count,2,"tick count");
		  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
		  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			}
		}
		
		Action<TickIO, TickIO, ulong> assertTick;
			
		[Test]		
		public void DemoStopSymbolTest() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = CreateProvider(true)) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				log.Info("===DemoStopSymbolTest===");
				if(debug) log.Debug("===StartSymbol===");
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
				if(debug) log.Debug("===VerifyFeed===");
		  		long count = verify.Verify(2,assertTick,symbol,25000);
		  		Assert.GreaterOrEqual(count,2,"tick count");
				if(debug) log.Debug("===StopSymbol===");
		  		provider.SendEvent(verify,symbol,(int)EventType.StopSymbol,null);
		  		
		  		// Wait for it to switch out of real time or historical mode.
		  		var expectedState = ReceiverState.Ready;
		  		var actualState = verify.VerifyState(expectedState,symbol,25000);
		  		Assert.AreEqual(expectedState,actualState,"after receiving a StopSymbol event, if your provider plugin was sending ticks then it must return either respond with an EndHistorical or EndRealTime event. If it has already sent one of those prior to the StopSymbol, then no reponse is required.");
		  		
		  		count = verify.Verify(0,assertTick,symbol,5);
		  		Assert.AreEqual(0,count,"your provider plugin must not send any more ticks after receiving a StopSymbol event.");
		  		
		  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
		  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			}
		}
	
		[Test]
		public void DemoReConnectionTest() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = CreateProvider(true)) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
		  		long count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
	  			provider.SendEvent(verify,null,(int)EventType.Terminate,null);
	  			
		  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
		  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			}
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = CreateProvider(true)) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
	  			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  			long count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
	  			
		  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
		  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			}
		}
	
		[Test]
		public void TestSeperateProcess() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = CreateProvider(false)) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
				if(debug) log.Debug("===VerifyFeed===");
	  			long count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
	  			Process[] processes = Process.GetProcessesByName(providerAssembly);
	  			Assert.AreEqual(1,processes.Length,"Number of provider service processes.");
	  			
		  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
		  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			}
		}

		private void CreateEntry( Provider provider, VerifyFeed verify, OrderType orderType, double desiredPositions, double actualPosition) {
			CreateOrder(provider, verify, TradeDirection.Entry,orderType,desiredPositions,actualPosition);
		}
		private void CreateExit( Provider provider, VerifyFeed verify, OrderType orderType, double desiredPositions, double actualPosition) {
			CreateOrder(provider, verify, TradeDirection.Exit,orderType,desiredPositions,actualPosition);
		}
		
		private void CreateOrder( Provider provider, VerifyFeed verify, TradeDirection tradeDirection, OrderType orderType, double desiredPositions, double actualPosition) {
  			List<LogicalOrder> list = new List<LogicalOrder>();
  			LogicalOrder order = Factory.Engine.LogicalOrder(symbol,null);
  			order.TradeDirection = tradeDirection;
  			order.Type = orderType;
  			order.Positions = desiredPositions;
  			order.IsActive = true;
  			list.Add(order);
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,actualPosition,list));
		}
		
		[Test]
		public void TestMarketOrder() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = ProviderFactory()) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				int secondsDelay = 25;
				if(debug) log.Debug("===DemoConnectionTest===");
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  			long count = verify.Verify(1,assertTick,symbol,25);
	  			double desiredPosition = 2;
	  			log.Warn("Sending 1");
	  			CreateEntry(provider,verify,OrderType.BuyMarket,desiredPosition,0);
	  			double actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
	  			Assert.AreEqual(desiredPosition,actualPosition,"position");
	
	  			desiredPosition = 0;
	  			log.Warn("Sending 2");
	  			CreateExit(provider,verify,OrderType.SellMarket,desiredPosition,actualPosition);
	  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
	  			Assert.AreEqual(desiredPosition,actualPosition,"position");
	
	  			desiredPosition = 2;
	  			log.Warn("Sending 3");
	  			CreateEntry(provider,verify,OrderType.BuyMarket,desiredPosition,actualPosition);
	  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
	  			Assert.AreEqual(desiredPosition,actualPosition,"position");
	
	  			desiredPosition = 2;
	  			log.Warn("Sending 4");
	  			CreateEntry(provider,verify,OrderType.BuyMarket,desiredPosition,actualPosition);
	  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
	  			Assert.AreEqual(desiredPosition,actualPosition,"position");
	  			
	  			desiredPosition = 0;
	  			log.Warn("Sending 5");
	  			CreateExit(provider,verify,OrderType.SellMarket,desiredPosition,actualPosition);
	  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
	  			Assert.AreEqual(desiredPosition,actualPosition,"position");
			}
		}
		
		[Test]
		public void TestSignalChanges() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = ProviderFactory()) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				int secondsDelay = 25;
				if(debug) log.Debug("===DemoConnectionTest===");
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  			double expectedPosition = 5;
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
	  			double position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
	  			Assert.AreEqual(expectedPosition,position,"position");
	  			expectedPosition = 0;
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
	  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
	  			Assert.AreEqual(expectedPosition,position,"position");
	
	  			expectedPosition = 5;
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
	  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
	  			Assert.AreEqual(expectedPosition,position,"position");
	
	  			expectedPosition = 5;
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
	  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
	  			Assert.AreEqual(expectedPosition,position,"position");
			}
		}
		
		[Test]
		public void TestLogicalOrders() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = ProviderFactory()) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
	  			long count = verify.Verify(1,assertTick,symbol,25);
				CreateLogicalEntry(OrderType.BuyLimit,15.12,3);
				CreateLogicalEntry(OrderType.SellLimit,34.12,3);
				CreateLogicalExit(OrderType.SellLimit,40.12);
				CreateLogicalExit(OrderType.SellStop,5.12);
				CreateLogicalExit(OrderType.BuyLimit,10.12);
				CreateLogicalExit(OrderType.BuyStop,45.12);
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
	  			count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
	  			Thread.Sleep(2000);
			}
		}
		
		[Test]
		public void TestSpecificLogicalOrder() {
			using( VerifyFeed verify = Factory.Utility.VerifyFeed())
			using( Provider provider = ProviderFactory()) {
				provider.SendEvent(verify,null,(int)EventType.Connect,null);
				provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
	  			long count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
				CreateLogicalEntry(OrderType.BuyLimit,503.72,4);
	  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
	  			count = verify.Verify(2,assertTick,symbol,25);
	  			Assert.GreaterOrEqual(count,2,"tick count");
	  			Thread.Sleep(2000);
			}
		}
		
		public void AssertLevel1( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsTrue(tick.IsQuote || tick.IsTrade);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
//	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
//	        	Assert.Greater(tick.AskLevel(0),0);
        	}
        	if( tick.IsTrade) {
	        	Assert.Greater(tick.Price,0);
    	    	Assert.Greater(tick.Size,0);
        	}
    		Assert.IsTrue(tick.Time>=lastTick.Time,"tick.Time > lastTick.Time");
    		Assert.AreEqual(symbol,tick.lSymbol);
		}
		
		public void AssertTimeAndSales( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsFalse(tick.IsQuote);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
	        	Assert.Greater(tick.AskLevel(0),0);
        	}
        	Assert.IsTrue(tick.IsTrade);
        	if( tick.IsTrade) {
	        	Assert.Greater(tick.Price,0);
    	    	Assert.Greater(tick.Size,0);
        	}
    		Assert.IsTrue(tick.Time>=lastTick.Time,"tick.Time > lastTick.Time");
    		Assert.AreEqual(symbol,tick.lSymbol);
		}
		
		public void SetTickTest( TickTest test) {
			switch( test) {
				case TickTest.Level1:
					assertTick = AssertLevel1;
					break;
				case TickTest.TimeAndSales:
					assertTick = AssertTimeAndSales;
					break;
			}
		}
		
		public LogicalOrder CreateLogicalEntry(OrderType type, double price, int size) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Entry;
			logical.Type = type;
			logical.Price = price;
			logical.Positions = size;
			orders.Add(logical);
			return logical;
		}
		
		public LogicalOrder CreateLogicalExit(OrderType type, double price) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Exit;
			logical.Type = type;
			logical.Price = price;
			orders.Add(logical);
			return logical;
		}
		
		public static Log Log {
			get { return log; }
		}
	}
}
