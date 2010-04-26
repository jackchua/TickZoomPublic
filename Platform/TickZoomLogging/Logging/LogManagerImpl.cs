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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

using log4net.Core;
using TickZoom.Api;
using log4net.Filter;

namespace TickZoom.Logging
{
	public class LogManagerImpl : LogManager {
		Dictionary<string,LogImpl> map = new Dictionary<string,LogImpl>();
		public void Configure() {
			AppDomain domain = AppDomain.CurrentDomain;
			Console.WriteLine(domain.FriendlyName+": TickZoom.Logging.LogManagerImpl: Configuring log4net");
			log4net.Config.XmlConfigurator.Configure();
		}
		public string LogFolder {
			get {
                // get the log directory
                string logDirectory = Factory.Settings["AppDataFolder"];
				string uniqueFolder = Environment.CurrentDirectory;
				uniqueFolder = uniqueFolder.Replace(Path.DirectorySeparatorChar,'_');
				uniqueFolder = uniqueFolder.Replace(":","");
				logDirectory = logDirectory +
					Path.DirectorySeparatorChar +
					uniqueFolder + 
					Path.DirectorySeparatorChar +
					"Logs";
				return logDirectory;
			}
		}
		public Log GetLogger(Type type) {
			LogImpl log;
			if( map.TryGetValue(type.FullName, out log)) {
				return log;
			} else {
				ILogger logger = LoggerManager.GetLogger(Assembly.GetCallingAssembly(),type);
				log = new LogImpl(logger);
				map[type.FullName] = log;
			}
			return log;
		}
		public Log GetLogger(string name) {
			LogImpl log;
			if( map.TryGetValue(name, out log)) {
				return log;
			} else {
				ILogger logger = LoggerManager.GetLogger(Assembly.GetCallingAssembly(),name);
				log = new LogImpl(logger);
				map[name] = log;
			}
			return log;
		}
	}
}
