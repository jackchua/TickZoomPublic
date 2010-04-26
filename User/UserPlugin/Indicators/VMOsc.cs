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
using System.Drawing;
using TickZoom.Api;


namespace TickZoom.Common
{
	/// <summary>
	/// Variable Length Mean Oscillator
	/// </summary>
	public class VMOsc : IndicatorCommon
	{
		VMean vmean = VMean();
		
		public VMOsc()
		{
			Name = "VMOsc";
			Drawing.Color = Color.Green;
			Drawing.PaneType = PaneType.Secondary;
			Drawing.IsVisible = true;
		}
		
		public override void OnInitialize()
		{
			AddIndicator(vmean);
		}
	
		public override void Update() {
			double middle = (Bars.High[0] + Bars.Low[0])/2;
			this[0] = vmean[0] - middle;
		}
	}
}
