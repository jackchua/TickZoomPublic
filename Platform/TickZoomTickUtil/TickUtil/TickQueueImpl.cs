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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.TickUtil
{
	public class TickQueueImpl : FastQueueImpl<QueueItem>, TickQueue
	{
	    public TickQueueImpl(string name) : base(name) {
	    	
	    }
	    
	    public TickQueueImpl(string name, int size) : base(name, size) {
	    	
	    }
	    
	    public void EnQueue(ref TickBinary o)
	    {
        	QueueItem item = new QueueItem();
    		item.EventType = 0;
    		item.Tick = o;
    		while( !EnQueueStruct(ref item)) {
    			Factory.Parallel.Yield();
    		}
	    }
	    
	    public void EnQueue(EventType entryType, SymbolInfo symbol)
	    {
        	QueueItem item = new QueueItem();
	    	item.EventType = (int) entryType;
	    	if( symbol != null) {
	    		item.EventChange.Symbol = symbol.BinaryIdentifier;
	    	}
	    	while( !EnQueueStruct(ref item)) {
	    		Factory.Parallel.Yield();
	    	}
	    }
	    
	    public void EnQueue(EventType entryType, string error)
	    {
        	QueueItem item = new QueueItem();
	    	item.EventType = (int) entryType;
//	    	item.ErrorEvent.Message = error;
	    	while( !EnQueueStruct(ref item)) {
	    		Factory.Parallel.Yield();
	    	}
	    }
	    
	    public void Dequeue(ref TickBinary tick)
	    {
        	QueueItem item = new QueueItem();
	    	while( !DequeueStruct(ref item)) {
        		Factory.Parallel.Yield();
	    	}
	    	// If not a tick
	    	if( item.EventType != 0) {
	    		string symbol;
	    		if( item.Tick.Symbol != 0) {
	    			symbol = item.Tick.Symbol.ToSymbol();
	    		} else {
	    			symbol = "";
	    		}
	    		throw new QueueException( (EventType) item.EventType, symbol);
	    	} else {
	    		tick = item.Tick;
	    	}
	    }
	    
	    public void LogStats() {
	    	FastQueueImpl<QueueItem>.LogAllStatistics();
	    }


    }
	
}
