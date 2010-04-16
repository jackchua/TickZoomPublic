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
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using TickZoom.Api;

namespace TickZoom.Common
{
	public class SymbolHandlerDefault : SymbolHandler {
		private TickIO tickIO = Factory.TickUtil.TickIO();
		private Receiver receiver;
		private SymbolInfo symbol;
		private bool isTradeInitialized = false;
		private bool isQuoteInitialized = false;
		private double position;
		public int bidSize;
		public double bid;
		public int askSize;
		public double ask;
		public int lastSize;
		public double last;
        private LogicalOrderHandler logicalOrderHandler;
        private bool isRunning = false;
		public void Start()
		{
			isRunning = true;
		}
		
		public void Stop()
		{
			isRunning = false;
		}
        
		public SymbolHandlerDefault(SymbolInfo symbol, Receiver receiver) {
        	this.symbol = symbol;
			this.receiver = receiver;
		}
		
		public void SendQuote() {
			if( isQuoteInitialized) {
				if( isRunning && symbol.QuoteType == QuoteType.Level1) {
					tickIO.Initialize();
					tickIO.SetSymbol(symbol.BinaryIdentifier);
					tickIO.SetTime(TimeStamp.UtcNow);
					tickIO.SetQuote(Bid,Ask,(ushort)BidSize,(ushort)AskSize);
					TickBinary binary = tickIO.Extract();
					receiver.OnEvent(symbol,(int)EventType.Tick,binary);
				}
			} else {
				VerifyQuote();
			}
		}
        
        public void AddPosition( double position) {
        	this.position += position;
        }
	        	
        public void SetPosition( double position) {
        	if( this.position != position) {
	        	this.position = position;
//	        	LogicalFillBinary fill = new LogicalFillBinary(position,tickIO.Bid,tickIO.Time,0);
//	        	receiver.OnEvent(symbol,(int)EventType.LogicalFill,fill);
        	}
        }
        
		private void VerifyQuote() {
			if(BidSize > 0 && Bid > 0 && AskSize > 0 && Ask > 0) {
				isQuoteInitialized = true;
			}
		}
        
		private void VerifyTrade() {
			if(LastSize > 0 & Last > 0) {
				isTradeInitialized = true;
			}
		}
        
		public void SendTimeAndSales() {
			if( !isRunning || symbol.TimeAndSales != TimeAndSales.ActualTrades ) {
				return;
			}
			if( !isTradeInitialized ) {
				VerifyTrade();
				return;
			}
			if( symbol.QuoteType == QuoteType.Level1 && !isQuoteInitialized) {
				VerifyQuote();
				return;
			}
			if( symbol.TimeAndSales == TimeAndSales.ActualTrades) {
				tickIO.Initialize();
				tickIO.SetSymbol(symbol.BinaryIdentifier);
				tickIO.SetTime(TimeStamp.UtcNow);
				tickIO.SetTrade(Last,LastSize);
				if( symbol.QuoteType == QuoteType.Level1) {
					tickIO.SetQuote(Bid,Ask,(ushort)BidSize,(ushort)AskSize);
				}
				TickBinary binary = tickIO.Extract();
				receiver.OnEvent(symbol,(int)EventType.Tick,binary);
			}
		}
        
		public LogicalOrderHandler LogicalOrderHandler {
			get { return logicalOrderHandler; }
			set { logicalOrderHandler = value; }
		}
		
		public double Position {
			get { return position; }
		}
		
		public int LastSize {
			get { return lastSize; }
			set { lastSize = value; }
		}
		
		public int BidSize {
			get { return bidSize; }
			set { bidSize = value; }
		}
		
		public double Bid {
			get { return bid; }
			set { bid = value; }
		}
		
		public int AskSize {
			get { return askSize; }
			set { askSize = value; }
		}
		
		public double Ask {
			get { return ask; }
			set { ask = value; }
		}
		
		public double Last {
			get { return last; }
			set { last = value; }
		}
		
		public bool IsRunning {
			get { return isRunning; }
		}
	}
}
