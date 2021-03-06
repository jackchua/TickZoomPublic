﻿#region Copyright
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

#define FOREX
using System;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;
using TickZoom.Api;
using TickZoom.MBTFIX;
using TickZoom.Test;

namespace Test
{
	[TestFixture]
	public class QuotesAPI 
	{
		public QuotesAPI()
		{
		}

		private string Hash(string password) {
			SHA256 hash = new SHA256Managed();
			ASCIIEncoding encoding = new ASCIIEncoding();
			byte[] hashBytes = hash.ComputeHash(encoding.GetBytes(password));
			return Convert.ToBase64String(hashBytes);
		}
		
		[Test]
		public void StockQuotePOSTTest() {
			PostSubmitter post=new PostSubmitter();
			post.Url="https://www.mbtrading.com/secure/getquoteserverxml.asp";
			post.PostItems.Add("username","DEMOXJRX");
			post.PostItems.Add("password",Hash("1clock2bird"));
			string message=post.Post();
		}
		
		[Test]
		public void ForexQuotePOSTTest() {
			PostSubmitter post=new PostSubmitter();
			post.Url="https://www.mbtrading.com/secure/getquoteserverxml.asp";
			post.PostItems.Add("username","DEMOYZPS");
			post.PostItems.Add("password",Hash("1step2wax"));
			string message=post.Post();
		}
	}
}
