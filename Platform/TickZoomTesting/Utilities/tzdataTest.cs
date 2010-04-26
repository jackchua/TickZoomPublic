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
using System.Configuration;
using NUnit.Framework;
using System.IO;
using TickZoom.Api;
using tzdata;

namespace TickZoom.Utilities
{
	[TestFixture]
	public class tzdataTest
	{
		[Test]
		public void TestFilter()
		{
	       	string storageFolder = Factory.Settings["AppDataFolder"];
	       	if( storageFolder == null) {
	       		throw new ApplicationException( "Must set AppDataFolder property in app.config");
	       	}
			string[] args = {
				storageFolder + @"\TestData\Daily4Ticks_Tick.tck",
				storageFolder + @"\TestData\Daily4Sim_Tick.tck",
			};
			Filter filter = new Filter(args);
		}
		
		[Test]
		public void TestMigrate()
		{
	       	string storageFolder = Factory.Settings["AppDataFolder"];
	       	if( storageFolder == null) {
	       		throw new ApplicationException( "Must set AppDataFolder property in app.config");
	       	}
	       	string origFile = storageFolder + @"\TestData\Migrate_Tick.tck";
	       	string tempFile = origFile + ".temp";
	       	string backupFile = origFile + ".back";
	       	File.Delete( backupFile);
	       	File.Delete( origFile);
	       	File.Copy(storageFolder + @"\TestData\USD_JPY_Tick.tck", origFile);
	       	
	       	string[] args = { "USD/JPY", storageFolder + @"\TestData\Migrate_Tick.tck" };
	       	
	       	Migrate migrate = new Migrate(args);
			Assert.IsTrue( File.Exists( origFile));
			Assert.IsTrue( File.Exists( backupFile));
			Assert.IsFalse( File.Exists( tempFile));
		}
	}
}
