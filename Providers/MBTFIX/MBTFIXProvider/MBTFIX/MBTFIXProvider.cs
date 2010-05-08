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
using System.Text;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.MBTFIX
{

	public class MBTFIXProvider : Provider, PhysicalOrderHandler
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(MBTFIXProvider));
		private static readonly bool debug = log.IsDebugEnabled;
		private static readonly bool trace = log.IsTraceEnabled;
        private readonly object readersLock = new object();
	    private readonly static object listLock = new object();
        private int nextValidId = 0;
        private Dictionary<ulong,SymbolHandler> symbolHandlers = new Dictionary<ulong,SymbolHandler>();

		public MBTFIXProvider()
		{
        }
        
        private void Initialize() {
        }
        
		Receiver receiver;
        public void Start(Receiver receiver)
        {
        	try { 
	        	log.Info("MBTFIXInterface Startup");
	        	this.receiver = (Receiver) receiver;
	        	Initialize();
				string appDataFolder = Factory.Settings["AppDataFolder"];
				if( appDataFolder == null) {
					throw new ApplicationException("Sorry, AppDataFolder must be set in the app.config file.");
				}
				string configFile = appDataFolder+@"/Providers/MBTFIXProvider.config";
				
				LoadProperties(configFile);
        	} catch( Exception ex) {
        		log.Error(ex.Message,ex);
        		throw;
        	}
        }
        
        public void Stop(Receiver receiver) {
        	
        }

        Dictionary<string, string> data;
        string configFile;
        private void LoadProperties(string configFile) {
        	this.configFile = configFile;
			data = new Dictionary<string, string>();
			if( !File.Exists(configFile) ) {
				Directory.CreateDirectory(Path.GetDirectoryName(configFile));
		        using (StreamWriter sw = new StreamWriter(configFile)) 
		        {
		            // Add some text to the file.
		            sw.WriteLine("ClientId=3712");
		            sw.WriteLine("EquityOrForex=equity");
		            sw.WriteLine("LiveOrDemo=demo");
		            sw.WriteLine("ForexDemoUserName=CHANGEME");
		            sw.WriteLine("ForexDemoPassword=CHANGEME");
		            sw.WriteLine("EquityDemoUserName=CHANGEME");
		            sw.WriteLine("EquityDemoPassword=CHANGEME");
		            sw.WriteLine("ForexLiveUserName=CHANGEME");
		            sw.WriteLine("ForexLivePassword=CHANGEME");
		            sw.WriteLine("EquityLiveUserName=CHANGEME");
		            sw.WriteLine("EquityLivePassword=CHANGEME");
		            // Arbitrary objects can also be written to the file.
		        }
			} 
			
			foreach (var row in File.ReadAllLines(configFile)) {
				string[] nameValue = row.Split('=');
				data.Add(nameValue[0].Trim(),nameValue[1].Trim());
			}
		}
        
        private string GetProperty( string name) {
        	string value;
			if( !data.TryGetValue(name,out value) ||
				value == null || value.Length == 0 || value.Contains("CHANGEME")) {
				throw new ApplicationException(name + " property must be set in " + configFile);
			}
        	return value;
        }
        
        
		private string UpperFirst(string input)
		{
			string temp = input.Substring(0, 1);
			return temp.ToUpper() + input.Remove(0, 1);
		}        
        
		public void StartSymbol(Receiver receiver, SymbolInfo symbol, StartSymbolDetail detail)
		{
			if( debug) log.Debug("StartSymbol " + symbol + ", " + detail.LastTime);
            SymbolHandler handler = GetSymbolHandler(symbol,receiver);
            receiver.OnEvent(symbol,(int)EventType.StartRealTime,null);
		}
		
		public void StopSymbol(Receiver receiver, SymbolInfo symbol)
		{
			if( debug) log.Debug("StartSymbol");
       		SymbolHandler buffer = symbolHandlers[symbol.BinaryIdentifier];
       		buffer.Stop();
			receiver.OnEvent(symbol,(int)EventType.EndRealTime,null);
		}
		
		public void PositionChange(Receiver receiver, SymbolInfo symbol, double signal, IList<LogicalOrder> orders)
		{
			int orderCount = orders==null?0:orders.Count;
			log.Info("Received PositionChange for " + symbol + " at position " + signal + " and " + orderCount + " orders.");
			if( orders != null) {
				foreach( var order in orders) {
					log.Info("Logical Order: " + order);
				}
			}
			
			LogicalOrderHandler handler = symbolHandlers[symbol.BinaryIdentifier].LogicalOrderHandler;
			handler.SetDesiredPosition(signal);
			handler.SetLogicalOrders(orders);
			
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
	            }
    		}
	    }
        
        
        private SymbolHandler GetSymbolHandler(SymbolInfo symbol, Receiver receiver) {
        	SymbolHandler symbolHandler;
        	if( symbolHandlers.TryGetValue(symbol.BinaryIdentifier,out symbolHandler)) {
        		symbolHandler.Start();
        		return symbolHandler;
        	} else {
    	    	symbolHandler = Factory.Utility.SymbolHandler(symbol,receiver);
    	    	symbolHandler.LogicalOrderHandler = Factory.Utility.LogicalOrderHandler(symbol,this);
    	    	symbolHandlers.Add(symbol.BinaryIdentifier,symbolHandler);
    	    	symbolHandler.Start();
    	    	return symbolHandler;
        	}
        }

        private void RemoveSymbolHandler(SymbolInfo symbol) {
        	if( symbolHandlers.ContainsKey(symbol.BinaryIdentifier) ) {
        		symbolHandlers.Remove(symbol.BinaryIdentifier);
        	}
        }
        
       private int GetLogicalOrderId(int physicalOrderId) {
        	int logicalOrderId;
        	if( physicalToLogicalOrderMap.TryGetValue(physicalOrderId,out logicalOrderId)) {
        		return logicalOrderId;
        	} else {
        		return 0;
        	}
        }
        Dictionary<int,int> physicalToLogicalOrderMap = new Dictionary<int, int>();
        
        
		public void OnCreateBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( trace) log.Trace( "OnCreateBrokerOrder " + physicalOrder);
			SymbolInfo symbol = physicalOrder.Symbol;
			nextValidId++;
			physicalToLogicalOrderMap.Add(nextValidId,physicalOrder.LogicalOrderId);
		}
		
		public void OnCancelBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( trace) log.Trace( "OnCancelBrokerOrder " + physicalOrder);
//			Order order = physicalOrder.BrokerOrder as Order;
//			if( order != null) {
//				client.CancelOrder( order.OrderId);
//				if(debug) log.Debug("Cancel Order: " + physicalOrder.Symbol.Symbol + " " + OrderToString(order));
//			} else {
//				throw new ApplicationException("BrokerOrder property want's an Order object.");
//			}
		}
		
		public void OnChangeBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( trace) log.Trace( "OnChangeBrokerOrder " + physicalOrder);
//			Order order = physicalOrder.BrokerOrder as Order;
//			if( order != null) {
//				if(debug) log.Debug("Change Order (Cancel/Replace): " + physicalOrder.Symbol.Symbol + " " + OrderToString(order));
//				OnCancelBrokerOrder(physicalOrder);
//				OnCreateBrokerOrder(physicalOrder);
//			} else {
//				throw new ApplicationException("BrokerOrder property want's an Order object.");
//			}
		}
		
		public void SendEvent( Receiver receiver, SymbolInfo symbol, int eventType, object eventDetail) {
			switch( (EventType) eventType) {
				case EventType.Connect:
					Start(receiver);
					break;
				case EventType.Disconnect:
					Stop(receiver);
					break;
				case EventType.StartSymbol:
					StartSymbol(receiver, symbol, (StartSymbolDetail) eventDetail);
					break;
				case EventType.StopSymbol:
					StopSymbol(receiver,symbol);
					break;
				case EventType.PositionChange:
					PositionChangeDetail positionChange = (PositionChangeDetail) eventDetail;
					PositionChange(receiver,symbol,positionChange.Position,positionChange.Orders);
					break;
				case EventType.Terminate:
					Dispose();
					break; 
				default:
					throw new ApplicationException("Unexpected event type: " + (EventType) eventType);
			}
		}
	}
}
