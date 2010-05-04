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
using System.IO;
using System.Threading;

using TickZoom.Api;
using TickZoom.TickUtil;

namespace tzdata
{
	/// <summary>
	/// Description of Query.
	/// </summary>
	public class Query
	{
		public Query(string[] args)
		{
			if( args.Length != 2) {
				Console.Write("Query Usage:");
				Console.Write("tzdata query <symbol> <file>");
				return;
			}
			string symbol = args[0];
			string file = args[1];
			ReadFile(file,symbol);
		}
		
		public void ReadFile(string filePath, string symbol) {
			TickReader reader = new TickReader();
			reader.Initialize(filePath,symbol);
			TickQueue queue = reader.ReadQueue;
			TickImpl firstTick = new TickImpl();
			TickImpl lastTick = new TickImpl();
			TickImpl prevTick = new TickImpl();
			long count = 0;
			long dups = 0;
			TickIO tickIO = new TickImpl();
			TickBinary tickBinary = new TickBinary();
			queue.Dequeue(ref tickBinary);
			tickIO.Inject(tickBinary);
			count++;
			firstTick.Copy( tickIO);
			prevTick.Copy( tickIO);
			try {
				while(true) {
					while( !queue.TryDequeue(ref tickBinary)) {
						Thread.Sleep(1);
					}
					tickIO.Inject(tickBinary);
					count++;
					if( tickIO.Bid == prevTick.Bid && tickIO.Ask == prevTick.Ask) {
						dups++;
					}
					prevTick.Copy(tickIO);
				}
			} catch( CollectionTerminatedException) {
				
			}
			lastTick.Copy( tickIO);
			Console.WriteLine(reader.Symbol + ": " + count + " ticks from " + firstTick.Time + " to " + lastTick.Time + " " + dups + " duplicates");
			TickReader.CloseAll();
		}
	}
}
