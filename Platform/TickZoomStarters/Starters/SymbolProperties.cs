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
 * Date: 3/18/2009
 * Time: 6:52 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using TickZoom.Api;

namespace TickZoom.Common
{
	[Serializable]
	public class SymbolProperties : PropertiesBase, ISymbolProperties
	{
		private Elapsed sessionStart = new Elapsed( 8, 0, 0);
		private Elapsed sessionEnd = new Elapsed( 16, 30, 0);
		private bool simulateTicks;
		private string symbol;
		private double minimumTick;
		private double fullPointValue;
		private int level2LotSize = 1;
		private double level2Increment;
		private int level2LotSizeMinimum;
		private ulong binaryIdentifier;
		private SymbolInfo universalSymbol;
		private int chartGroup;
		private QuoteType quoteType = QuoteType.Level1;
		private TimeAndSales timeAndSales = TimeAndSales.ActualTrades;
		private string displayTimeZone;
		private string timeZone;
		private bool useSyntheticMarkets = true;
		private bool useSyntheticLimits = true;
		private bool useSyntheticStops = true;
		InstrumentType instrumentType = InstrumentType.None;
		private ProfitLoss profitLoss = new ProfitLossDefault();
		
		public TimeAndSales TimeAndSales {
			get { return timeAndSales; }
			set { timeAndSales = value; }
		}

		public int ChartGroup {
			get { return chartGroup; }
			set { chartGroup = value; }
		}
		
		public int Level2LotSizeMinimum {
			get { return level2LotSizeMinimum; }
			set { level2LotSizeMinimum = value; }
		}
		
	    public SymbolProperties Copy()
	    {
	    	SymbolProperties result;
	    	
	        using (var memory = new MemoryStream())
	        {
	            var formatter = new BinaryFormatter();
	            formatter.Serialize(memory, this);
	            memory.Position = 0;
	
	            result = (SymbolProperties)formatter.Deserialize(memory);
	            memory.Close();
	        }
	
	        return result;
	    }
	    
		public override string ToString()
		{
			return symbol == null ? "empty" : symbol;
		}
		
		[Obsolete("Please create your data with the IsSimulateTicks flag set to true instead of this property.",true)]
		public bool SimulateTicks {
			get { return simulateTicks; }
			set { simulateTicks = value; }
		}
		
		public Elapsed SessionEnd {
			get { return sessionEnd; }
			set { sessionEnd = value; }
		}
		
		public Elapsed SessionStart {
			get { return sessionStart; }
			set { sessionStart = value; }
		}
		
		public double MinimumTick {
			get { return minimumTick; }
			set { minimumTick = value; }
		}
		
		public double FullPointValue {
			get { return fullPointValue; }
			set { fullPointValue = value; }
		}
	
		public string Symbol {
			get { return symbol; }
			set { symbol = value; }
		}
		
		public int Level2LotSize {
			get { return level2LotSize; }
			set { level2LotSize = value; }
		}
		
		public double Level2Increment {
			get { return level2Increment; }
			set { level2Increment = value; }
		}
		
		public SymbolInfo UniversalSymbol {
			get { return universalSymbol; }
			set { universalSymbol = value; }
		}
		
		public ulong BinaryIdentifier {
			get { return binaryIdentifier; }
			set { binaryIdentifier = value; }
		}
		
		public override bool Equals(object obj)
		{
			return ((SymbolInfo)obj).BinaryIdentifier == binaryIdentifier;
		}
	
		public override int GetHashCode()
		{
			return binaryIdentifier.GetHashCode();
		}
		
		public QuoteType QuoteType {
			get { return quoteType; }
			set { quoteType = value; }
		}
		
		public string DisplayTimeZone {
			get { return displayTimeZone; }
			set { displayTimeZone = value; }
		}

		public string TimeZone {
			get { return timeZone; }
			set { timeZone = value; }
		}
		
		public bool UseSyntheticMarkets {
			get { return useSyntheticMarkets; }
			set { useSyntheticMarkets = value; }
		}
		
		public bool UseSyntheticLimits {
			get { return useSyntheticLimits; }
			set { useSyntheticLimits = value; }
		}
		
		public bool UseSyntheticStops {
			get { return useSyntheticStops; }
			set { useSyntheticStops = value; }
		}
		
		public InstrumentType InstrumentType {
			get { return instrumentType; }
			set { instrumentType = value; }
		}
		
		public ProfitLoss ProfitLoss {
			get { return profitLoss; }
			set { profitLoss = value; }
		}
	}
}
