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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using TickZoom.Api;
using TickZoom.Common;

namespace TickZoom.Common
{
	public class SymbolLibrary 
	{
		Dictionary<string,SymbolProperties> symbolMap;
		Dictionary<ulong,SymbolProperties> universalMap;
		public SymbolLibrary() {
			symbolMap = new Dictionary<string, SymbolProperties>();
			SymbolDictionary dictionary = SymbolDictionary.Create("universal",SymbolDictionary.UniversalDictionary);
			IEnumerable<SymbolProperties> enumer = dictionary;
			foreach( SymbolProperties symbolProperties in dictionary) {
				symbolMap[symbolProperties.Symbol] = symbolProperties;
			}
			dictionary = SymbolDictionary.Create("user",SymbolDictionary.UserDictionary);
			foreach( SymbolProperties symbolProperties in dictionary) {
				symbolMap[symbolProperties.Symbol] = symbolProperties;
			}
			ulong universalIdentifier = 1;
			universalMap = new Dictionary<ulong, SymbolProperties>();
			foreach( var kvp in symbolMap) {
				kvp.Value.BinaryIdentifier = universalIdentifier;
				universalMap.Add(universalIdentifier,kvp.Value);
				universalIdentifier ++;
			}
		}
		
		public SymbolProperties GetSymbolProperties(string symbol) {
			SymbolProperties symbolProperties;
			if( symbolMap.TryGetValue(symbol.Trim(),out symbolProperties)) {
				return symbolProperties;
			} else {
				throw new ApplicationException( "Sorry, symbol " + symbol + " was not found in any symbol dictionary.");
			}
		}
		
		public SymbolInfo LookupSymbol(string symbol) {
			return GetSymbolProperties(symbol);
		}
	
		public SymbolInfo LookupSymbol(ulong universalIdentifier) {
			SymbolProperties symbolProperties;
			if( universalMap.TryGetValue(universalIdentifier,out symbolProperties)) {
				return symbolProperties;
			} else {
				throw new ApplicationException( "Sorry, universal id " + universalIdentifier + " was not found in any symbol dictionary.");
			}
		}
	}
}
