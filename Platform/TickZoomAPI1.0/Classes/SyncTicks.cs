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
 * Date: 8/9/2009
 * Time: 1:12 AM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace TickZoom.Api
{
	/// <summary>
	/// This class is only used during unit tests for assisting
	/// in simulating a live trading environment when sending limit,
	/// stop, and other orders to the broker.
	/// </summary>
	public static class SyncTicks {
		private static int mockTradeCount = 0;
		private static bool enabled = false;
		private static Dictionary<ulong,SimpleLock> tickSyncs;
		private static object locker = new object();
		
		public static Dictionary<ulong, SimpleLock> TickSyncs {
			get { 
				if( tickSyncs == null) {
					lock( locker) {
						if( tickSyncs == null) {
							tickSyncs = new Dictionary<ulong,SimpleLock>();
						}
					}
				}
				return tickSyncs;
			}
		}
		
		public static SimpleLock GetTickSync(ulong symbolBinaryId) {
			SimpleLock tickSync;
			lock( locker) {
				if( TickSyncs.TryGetValue(symbolBinaryId,out tickSync)) {
				   	return tickSync;
				} else {
					tickSync = new SimpleLock();
					TickSyncs.Add(symbolBinaryId,tickSync);
					return tickSync;
				}
			}
		}
		
		public static bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}
		
		public static int MockTradeCount {
			get { return mockTradeCount; }
			set { mockTradeCount = value; }
		}
	}
}
