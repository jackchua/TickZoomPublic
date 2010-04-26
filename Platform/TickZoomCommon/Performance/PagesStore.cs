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

using TickZoom.Api;

namespace TickZoom.Common
{
	public class PageStore {
		FileStream fs;
		byte[] sizeBuffer = new byte[sizeof(int)];
		
		public PageStore() {
			string fileName = "TradeFile.dat";
   			fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
		}
		public int GetPageSize(long pageOffset) {
			lock( locker) {
				fs.Seek(pageOffset, SeekOrigin.Begin);
				fs.Read(sizeBuffer,0,sizeBuffer.Length);
				return BitConverter.ToInt32(sizeBuffer,0);
			}
		}
		public void Read(long pageOffset, byte[] buffer, int offset, int length) {
			lock( locker) {
				fs.Seek(pageOffset+sizeof(int), SeekOrigin.Begin);
				fs.Read(buffer,offset,length);
			}
		}
		public long Write(byte[] buffer, int offset, int length) {
			lock( locker) {
				long pageOffset = fs.Seek(0, SeekOrigin.End);
				byte[] lengthBytes = BitConverter.GetBytes(length);
				fs.Write(lengthBytes,0,lengthBytes.Length);
				fs.Write(buffer,offset,length);
				return pageOffset;
			}
		}
		private static PageStore instance;
		private static object locker = new object();
		public static PageStore Instance {
			get {
				if( instance == null) {
					lock( locker) {
						if( instance == null) {
							instance = new PageStore();
						}
					}
				}
				return instance;
			}
		}
	}
	

	
	internal class PagePool<T> where T : new()
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(PagePool<>));
		private Stack<T> stack = new Stack<T>();
	    private object locker = new object(); 
	
	    public T Create()
	    {
	        lock (locker)
	        {
	            if (stack.Count == 0)
	            {
	            	return new T();
	            }
	            else
	            {
	            	return stack.Pop();
	            }
	        }
	    }
	
	    public void Free(T item)
	    {
	        lock (locker)
	        {
            	stack.Push(item);
	        }
	    }
	}
}
