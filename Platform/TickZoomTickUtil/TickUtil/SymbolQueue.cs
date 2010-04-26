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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TickZoom;
using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.TickUtil
{
	public class SymbolQueue : Receiver {
		static readonly Log log = Factory.Log.GetLogger(typeof(SymbolQueue));
		static readonly bool debug = log.IsDebugEnabled;
		private SymbolInfo symbol;
		private Provider provider;
		private TickQueue tickQueue;
		private TimeStamp startTime;
		public TickBinary NextTick;
		private ReceiverState receiverState = ReceiverState.Ready;
		
		public ReceiverState OnGetReceiverState(SymbolInfo symbol) {
			return receiverState;
		}
		
		public SymbolQueue(SymbolInfo symbol, Provider provider, TimeStamp startTime) {
			this.symbol = symbol;
			this.provider = provider;
			this.tickQueue = Factory.TickUtil.TickQueue(typeof(SymbolQueue));
			this.tickQueue.StartEnqueue = Start;
			this.startTime = startTime;
			NextTick = new TickBinary();
			NextTick.Symbol = symbol.BinaryIdentifier;
			provider.SendEvent(this,null,(int)EventType.Connect,null);
		}
		
		private void Start() {
			provider.SendEvent(this,symbol,(int)EventType.StartSymbol,startTime);
		}
		
		public Provider Provider {
			get { return provider; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
		}
		
		public void OnStart()
		{
		}
		
		public bool CanReceive(SymbolInfo symbol) {
			return tickQueue.CanEnqueue;
		}
		
		public void OnEvent(SymbolInfo symbo, int eventType, object eventDetail) {
	    	if( isDisposed) return;
			try {
				switch( (EventType) eventType) {
					case EventType.Tick:
						TickBinary binary = (TickBinary) eventDetail;
						tickQueue.EnQueue(ref binary);
						break;
					case EventType.EndHistorical:
						tickQueue.EnQueue(EventType.EndHistorical, symbol);
						break;
					case EventType.StartRealTime:
						tickQueue.EnQueue(EventType.StartRealTime, symbol);
						break;
					case EventType.EndRealTime:
						tickQueue.EnQueue(EventType.EndRealTime, symbol);
						break;
					case EventType.Error:
			    		tickQueue.EnQueue(EventType.Error, symbol);
			    		break;
					case EventType.Terminate:
			    		tickQueue.EnQueue(EventType.Terminate, symbol);
			    		break;
					case EventType.LogicalFill:
					case EventType.StartHistorical:
					case EventType.Initialize:
					case EventType.Open:
					case EventType.Close:
					case EventType.PositionChange:
					default:
						break;
				}
			} catch( QueueException) {
				log.Warn("Already terminated.");
			}
		}
		
	    public void Receive(ref TickBinary tick) {
			tickQueue.Dequeue(ref tick);
		}
		
		public void OnRealTime(SymbolInfo symbol1) {
		}
		
		public void OnHistorical(SymbolInfo symbol1) {
			tickQueue.EnQueue(EventType.StartHistorical, symbol1);
		}
		
		public void OnEndHistorical(SymbolInfo symbol1)
		{
			tickQueue.EnQueue(EventType.EndHistorical, symbol);
		}
		
		public void OnEndRealTime(SymbolInfo symbol1)
		{
			tickQueue.EnQueue(EventType.EndRealTime, symbol);
		}
		
		public void OnError(string error)
		{
			tickQueue.EnQueue(EventType.Error, error);
		}
		
 		private volatile bool isDisposed = false;
	    public void Dispose() 
	    {
	        Dispose(true);
	        GC.SuppressFinalize(this);      
	    }
	
	    protected virtual void Dispose(bool disposing)
	    {
       		if( !isDisposed) {
	            isDisposed = true;   
	            if (disposing) {
	            	tickQueue.Terminate();
	            }
    		}
	    }
		
	}
}

