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
using NUnit.Framework;
using TickZoom.Api;
using TickZoom.Common;

namespace TickZoom.StarterTest
{
	[TestFixture]
	public class ProjectLoadTest
	{
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	    public delegate void ShowChartDelegate(ChartControl chart);
		[SetUp]
		public void TestSetup() {
		}
		
		[TearDown]
		public void TearDown() {
		}
		
		[Test]
		public void TestEmpty()
		{
			string fileName = @"..\..\Platform\TickZoomTesting\Startup\empty.tzproj";
			ProjectProperties props = ProjectPropertiesCommon.Create(new StreamReader(fileName));

			Assert.IsNotNull( props.Starter);
			Assert.IsNotNull( props.Chart);
			Assert.IsNotNull( props.Engine);
			Assert.IsNotNull( props.Model);
			Assert.AreEqual( 0, props.Model.GetPropertyKeys().Length,0);
			Assert.AreEqual( 5, props.Model.GetModelKeys().Length,5);
		}
	    
		[Test]
		public void TestSimpleSingle()
		{
			string fileName = @"..\..\Platform\TickZoomTesting\Startup\singleSimple.tzproj";
			ProjectProperties props = ProjectPropertiesCommon.Create(new StreamReader(fileName));

			Assert.IsNotNull( props.Starter);
			Assert.IsNotNull( props.Chart);
			Assert.IsNotNull( props.Engine);
			Assert.IsNotNull( props.Model);
			Assert.AreEqual( 0, props.Model.GetPropertyKeys().Length);
			Assert.AreEqual( 2, props.Model.GetModelKeys().Length);
		}
	}
}
