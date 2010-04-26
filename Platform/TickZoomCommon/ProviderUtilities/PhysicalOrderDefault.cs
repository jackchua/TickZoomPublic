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
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.Common
{
	public struct PhysicalOrderDefault : PhysicalOrder {
		private SymbolInfo symbol;
		private OrderType type;
		private double price;
		private double size;
		private int logicalOrderId;
		private object brokerOrder;
		
		public override string ToString()
		{
			return type + " at " + price + " for " + size;
		}
		
		public PhysicalOrderDefault(SymbolInfo symbol, LogicalOrder logical, double size, object brokerOrder) {
			this.symbol = symbol;
			this.type = logical.Type;
			this.price = logical.Price;
			this.size = size;
			this.logicalOrderId = logical.Id;
			this.brokerOrder = brokerOrder;
		}
		
		public PhysicalOrderDefault(SymbolInfo symbol, OrderType type, double price, double size, int logicalOrderId, object brokerOrder) {
			this.symbol = symbol;
			this.type = type;
			this.price = price;
			this.size = size;
			this.logicalOrderId = logicalOrderId;
			this.brokerOrder = brokerOrder;
		}
	
		public OrderType Type {
			get { return type; }
		}
		
		public double Price {
			get { return price; }
		}
		
		public double Size {
			get { return size; }
		}
		
		public object BrokerOrder {
			get { return brokerOrder; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
		}
		
		public int LogicalOrderId {
			get { return logicalOrderId; }
		}
	}
}