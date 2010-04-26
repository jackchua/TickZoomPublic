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
	public class TransactionPairsPages {
		PagePool<TransactionPairsPage> pagePool = new PagePool<TransactionPairsPage>();
		int pagesCount = 0;
		private PageStore pageStore = PageStore.Instance;
		Dictionary<int,long> offsets = new Dictionary<int,long>();
		List<TransactionPairsPage> unwrittenPages = new List<TransactionPairsPage>();
		public TransactionPairsPages() {
			
		}
		public void TryRelease(TransactionPairsPage page) {
			if( page != null && !unwrittenPages.Contains(page)) {
				pagePool.Free(page);
			}
		}
		public TransactionPairsPage GetPage(int pageNumber) {
			if( pageNumber >= pagesCount) {
				foreach( var unwrittenPage in unwrittenPages) {
					if( unwrittenPage.PageNumber == pageNumber) {
						return unwrittenPage;
					}
				}
				throw new ApplicationException("Page number " + pageNumber + " was out side number of pages: " + pagesCount + " and not found in unwritten page list.");
			}
			long offset = offsets[pageNumber];
			TransactionPairsPage page = pagePool.Create();
			int pageSize = pageStore.GetPageSize(offset);
			page.SetPageSize(pageSize);
			page.PageNumber = pageNumber;
			pageStore.Read(offset,page.Buffer,0,page.Buffer.Length);
			return page;
		}
		
		public TransactionPairsPage CreatePage(int pageNumber, int capacity) {
			if( pageNumber < pagesCount) {
				throw new ApplicationException("Page number " + pageNumber + " already exists.");
			}
			TransactionPairsPage page = pagePool.Create();
			page.SetCapacity(capacity);
			page.PageNumber = pageNumber;
			unwrittenPages.Add(page);
			return page;
		}
		
		public void WritePage(TransactionPairsPage page) {
			// Go to end of file.
			long offset = pageStore.Write(page.Buffer,0,page.Buffer.Length);
			offsets.Add(page.PageNumber,offset);
			pagesCount ++;
			unwrittenPages.Remove(page);
			pagePool.Free(page);
		}
	}
}
