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
using System.Threading;

using TickZoom.Api;

namespace TickZoom.Common
{
	public class VerifyFeedDefault : Receiver, VerifyFeed, IDisposable
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(VerifyFeed));
		private static readonly bool debug = log.IsDebugEnabled;
		private TickQueue tickQueue = Factory.TickUtil.TickQueue(typeof(VerifyFeed));
		private volatile bool isRealTime = false;
		private SimpleLock syncTicks;
		private volatile ReceiverState receiverState = ReceiverState.Ready;
		private Task task;
		private static object taskLocker = new object();

		public TickQueue TickQueue {
			get { return tickQueue; }
		}

		public ReceiverState OnGetReceiverState(SymbolInfo symbol)
		{
			return receiverState;
		}

		public VerifyFeedDefault()
		{
			tickQueue.StartEnqueue = Start;
		}

		public void Start()
		{
		}
		
		public long VerifyEvent(Action<SymbolInfo,int,object> assertTick, SymbolInfo symbol, int timeout)
		{
			return VerifyEvent(1, assertTick, symbol, timeout);
		}
		
		public long Verify(Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			return Verify(2, assertTick, symbol, timeout);
		}
		TickIO lastTick = Factory.TickUtil.TickIO();
		
		int countLog = 0;
		TickBinary tickBinary = new TickBinary();
		TickIO tickIO = Factory.TickUtil.TickIO();
		public long Verify(int expectedCount, Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (!tickQueue.CanDequeue)
					Thread.Sleep(100);
				if (tickQueue.CanDequeue) {
					if (HandleTick(expectedCount, assertTick, symbol)) {
						break;
					}
				}
			}
			return count;
		}
		
		public ReceiverState VerifyState(ReceiverState expectedState, SymbolInfo symbol, int timeout) {
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (tickQueue.CanDequeue) {
					try { 
						tickQueue.Dequeue(ref binary);
					} catch (QueueException ex) {
						if( HandleQueueException(ex)) {
							break;
						}
					}
				} else {
					Thread.Sleep(10);
				}
				if( receiverState == expectedState) {
					break;
				}
			}
			return receiverState;
		}
		
		public long VerifyEvent(int expectedCount, Action<SymbolInfo,int,object> assertEvent, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyEvent");
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (!tickQueue.CanDequeue)
					Thread.Sleep(100);
				if (tickQueue.CanDequeue) {
					if (HandleEvent(expectedCount, assertEvent, symbol)) {
						break;
					}
				}
			}
			return count;
		}
		
		public double VerifyPosition(double expectedPosition, SymbolInfo symbol, int timeout)
		{
			if (debug)
				log.Debug("VerifyFeed");
			int startTime = Environment.TickCount;
			count = 0;
			double position;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (tickQueue.CanDequeue) {
					try { 
						tickQueue.Dequeue(ref binary);
					} catch (QueueException ex) {
						if( HandleQueueException(ex)) {
							break;
						}
					}
				} else {
					Thread.Sleep(10);
				}
				if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
					if( position == expectedPosition) {
						return expectedPosition;
					}
				}
			}
			if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
				return position;
			} else {
				throw new ApplicationException("Position was never set via call back.");
			}
		}

		private bool HandleTick(int expectedCount, Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol)
		{
			try {
				tickQueue.Dequeue(ref tickBinary);
				tickIO.Inject(tickBinary);
				if (debug && countLog < 5) {
					log.Debug("Received a tick " + tickIO);
					countLog++;
				}
				startTime = Environment.TickCount;
				count++;
				if (count > 0) {
					assertTick(tickIO, lastTick, symbol.BinaryIdentifier);
				}
				lastTick.Copy(tickIO);
				syncTicks.Unlock();
				if (count >= expectedCount)
					return true;
			} catch (QueueException ex) {
				return( HandleQueueException(ex));
			}
			return false;
		}

		private bool HandleEvent(int expectedCount, Action<SymbolInfo,int,object> assertEvent, SymbolInfo symbol)
		{
			try {
				// Remove ticks just so as to get to the event we want to see.
				tickQueue.Dequeue(ref tickBinary);
				if (customEventType> 0) {
					assertEvent(customEventSymbol,customEventType,customEventDetail);
					count++;
				} else {
					Thread.Sleep(10);
				}
				if (count >= expectedCount)
					return true;
			} catch (QueueException ex) {
				return HandleQueueException(ex);
			}
			return false;
		}
		
		private bool HandleQueueException( QueueException ex) {
			log.Notice("QueueException: " + ex.EntryType);
			switch (ex.EntryType) {
				case EventType.StartHistorical:
					receiverState = ReceiverState.Historical;
					isRealTime = false;
					break;
				case EventType.StartRealTime:
					receiverState = ReceiverState.RealTime;
					isRealTime = true;
					break;
				case EventType.EndHistorical:
					receiverState = ReceiverState.Ready;
					break;
				case EventType.EndRealTime:
					receiverState = ReceiverState.Ready;
					isRealTime = false;
					break;
				case EventType.Terminate:
					receiverState = ReceiverState.Stop;
					isRealTime = false;
					return true;
				default:
					throw new ApplicationException("Unexpected QueueException: " + ex.EntryType);
			}
			return false;
		}
		
		volatile int count = 0;
		int startTime;
		public void StartTimeTheFeed()
		{
			startTime = Environment.TickCount;
			count = 0;
			countLog = 0;
			task = Factory.Parallel.Loop(this, TimeTheFeedTask);
		}

		public int EndTimeTheFeed(int expectedTickCount, int timeoutSeconds)
		{
			int end = startTime + timeoutSeconds * 1000;
			while( count < expectedTickCount && Environment.TickCount < end) {
				Thread.Sleep(100);
			}
			log.Notice("Last tick received: " + tickIO.ToPosition());
			Factory.TickUtil.TickQueue("Stats").LogStats();
			Dispose();
			return count;
		}

		public Yield TimeTheFeedTask()
		{
			lock(taskLocker) {
				if( isDisposed) {
					return null;
				}
				try {
					if (!tickQueue.CanDequeue)
						return null;
					tickQueue.Dequeue(ref tickBinary);
#if DEBUG					
					if( count % 10 == 0) {
#else
					if( count % 10 == 0) {
#endif
						Thread.Sleep(1);
					}
					tickIO.Inject(tickBinary);
					if (debug && count < 5) {
						log.Debug("Received a tick " + tickIO);
						countLog++;
					}
					if( count == 0) {
						log.Notice("First tick received: " + tickIO.ToPosition());
					}
					count++;
					if (count % 1000000 == 0) {
						log.Notice("Read " + count + " ticks");
					}
					return TimeTheFeedTask;
				} catch (QueueException ex) {
					HandleQueueException(ex);
				}
				return null;
			}
		}

		public void OnRealTime(SymbolInfo symbol)
		{
			isRealTime = true;
		}

		public void OnHistorical(SymbolInfo symbol)
		{
			receiverState = ReceiverState.Historical;
		}

		public void OnSend(ref TickBinary o)
		{
			try {
				tickQueue.EnQueue(ref o);
			} catch (QueueException) {
				// Queue already terminated.
			}
		}

		Dictionary<ulong, double> actualPositions = new Dictionary<ulong, double>();

		public double GetPosition(SymbolInfo symbol)
		{
			return actualPositions[symbol.BinaryIdentifier];
		}

		public void OnPositionChange(SymbolInfo symbol, LogicalFillBinary fill)
		{
			log.Info("Got Logical Fill of " + symbol + " at " + fill.Price + " for " + fill.Position);
			actualPositions[symbol.BinaryIdentifier] = fill.Position;
		}

		public void OnStop()
		{
			Dispose();
		}

		public void OnError(string error)
		{
			log.Error(error);
			Dispose();
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
	    		lock( taskLocker) {
		            isDisposed = true;   
		            if (disposing) {
		            	if( task != null) {
			            	task.Stop();
			            	task.Join();
							tickQueue.Terminate();
		            	}
		            }
		            task = null;
		            // Leave tickQueue set so any extraneous
		            // events will see the queue is already terminated.
//		            tickQueue = null;
	    		}
    		}
	    }
	    
		public void OnEndHistorical(SymbolInfo symbol)
		{
			try {
				tickQueue.EnQueue(EventType.EndHistorical, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
		}

		public void OnEndRealTime(SymbolInfo symbol)
		{
			try {
				tickQueue.EnQueue(EventType.EndRealTime, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
		}
		
		public bool IsRealTime {
			get { return isRealTime; }
		}
		
		volatile SymbolInfo customEventSymbol;
		volatile int customEventType;
		volatile object customEventDetail;
		public void OnCustomEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			customEventSymbol = symbol;
			customEventType = eventType;
			customEventDetail = eventDetail;
		}
		
		public bool OnEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			if( isDisposed) return false;
			try {
				switch( (EventType) eventType) {
					case EventType.Tick:
						TickBinary binary = (TickBinary) eventDetail;
						OnSend(ref binary);
						break;
					case EventType.EndHistorical:
						OnEndHistorical(symbol);
						break;
					case EventType.StartRealTime:
						OnRealTime(symbol);
						break;
					case EventType.StartHistorical:
						OnHistorical(symbol);
						break;
					case EventType.EndRealTime:
						OnEndRealTime(symbol);
						break;
					case EventType.Error:
						OnError((string)eventDetail);
						break;
					case EventType.LogicalFill:
						OnPositionChange(symbol,(LogicalFillBinary)eventDetail);
						break;
					case EventType.Terminate:
						OnStop();
			    		break;
					case EventType.Initialize:
					case EventType.Open:
					case EventType.Close:
					case EventType.PositionChange:
			    		throw new ApplicationException("Unexpected EventType: " + eventType);
					default:
			    		OnCustomEvent(symbol,eventType,eventDetail);
			    		break;
				}
				return true;
			} catch( QueueException) {
				log.Warn("Already terminated.");
			}
			return false;
		}
		
		public TickIO LastTick {
			get { return lastTick; }
		}
	}
}
