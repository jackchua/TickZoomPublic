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
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

#if POSTSHARPX
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace TickZoom.Api
{
	/// <summary>
	/// Description of DiagramAttribute.
	/// </summary>
#region Attibutes
	[MulticastAttributeUsage( MulticastTargets.Method | MulticastTargets.InstanceConstructor, 
                          AllowMultiple=true,
                          TargetMemberAttributes = 
                            MulticastAttributes.NonAbstract | 
                            MulticastAttributes.Managed)]
#endregion
	[Serializable]
	public class DiagramAttribute : OnMethodBoundaryAspect
	{
#region Fields
		[ThreadStatic]
		private static Stack<string> stack;
		private bool isDisabled = false;
		private string methodSignature;
		private bool isConstructor = false;
		private bool verified = false;
		private bool isThread = false;
		private object staticObject = new object();
		private Type objectType;
		private MethodBase method;
#endregion		
		public DiagramAttribute() {
		}
		
		[PostSharp.Extensibility.CompileTimeSemanticAttribute()]
		public override void CompileTimeInitialize(System.Reflection.MethodBase method)
		{
			base.CompileTimeInitialize(method);
			objectType = method.DeclaringType;
			this.method = method;
			if( method.Name == ".ctor" ) {
			 	isConstructor = true;
			}
			if( method.Name == "Set" && method.DeclaringType.Name == "StubServiceConnection") {
				isDisabled = true;				
			}
			if( method.Name == "NameThread" && method.DeclaringType.Name == "StubServiceConnection") {
				isDisabled = true;				
			}
			if( method.Name.StartsWith("set_") ||
			   method.Name.StartsWith("get_")) {
				isDisabled = true;	
			}
		}
		
		private class InstanceTagProperties {
			public bool IsInternalCall = false;
		}
		
	    public sealed override void OnEntry( MethodExecutionEventArgs eventArgs )
	    {
	    	if( Diagram.IsDebug) {
		    	try {
			    	if( isDisabled) {
			    		return;
			    	} else {
		    			bool isConstructorChain = false;
				    	object instance = eventArgs.Instance;
				    	string callee;
				    	if( instance == null) {
				    		MethodBase method = new StackFrame(1).GetMethod();
				    		callee = Diagram.GetTypeName(method.DeclaringType);
				    	} else {
				    		bool isNew;
				    		callee = Diagram.GetInstanceName(instance,out isNew);
				    		if( !isNew && isConstructor) {
			    				isConstructorChain = true;
				    		}
				    		if( Stack.Count>0 && callee == Stack.Peek() ) {
								if( Diagram.IsTrace) Diagram.Log.Trace("==> " + callee + "\n");
					    		Stack.Push(callee);
				    			return;
				    		}
				    	}
				    	string caller;
			    		if( Stack.Count > 0) {
					    	caller = Stack.Peek();
					    	if( Diagram.IsTrace) {
					    		MethodBase method = new StackFrame(2).GetMethod();
					    		string callerType = Diagram.GetTypeName(method.DeclaringType);
					    		if( caller.StartsWith(callerType)) {
					    			if( Diagram.IsTrace) Diagram.Log.Trace("-- Matches Type--");
					    		} else {
					    			Diagram.Log.Error("ERROR: stack instance, " + caller + " mismatches type, " + callerType + ".");
					    		}
					    	}
		    			} else {
				    		MethodBase method = new StackFrame(2).GetMethod();
				    		caller = Diagram.GetTypeName(method.DeclaringType);
							if( Diagram.IsTrace) Diagram.Log.Trace("==> " + caller + "\n");
				    		Stack.Push(caller);
		    			}
	   					Call(caller, callee, isConstructorChain);
			    	}
		    	} catch( Exception ex) {
		    		Diagram.Log.Error("Diagram Aspect failures",ex);
		    	}
	    	}
	    }
	    
		public void Call(string caller, string callee, bool isConstructorChain) {
			if( !verified) {
	    		if( caller == "StackBuilderSink") {
					isDisabled = true;
				}
	    		if( caller == "ThreadHelper") {
					SwitchToThread();
				}
			}
			verified = true;
			if( isThread) {
				Diagram.Log.Debug(callee + " ==> " + callee + " " + MethodSignature);
			} else if( isConstructor) {
				if( !isConstructorChain) {
					if( Diagram.IsDebug) Diagram.Log.Debug(caller + " (!) " + callee);
				}
			} else {
				if( Diagram.IsDebug) Diagram.Log.Debug(caller + " ==> " + callee + " " + MethodSignature);
			}
			if( Diagram.IsTrace) Diagram.Log.Trace("==> " + callee + "\n");
			Stack.Push(callee);
		}	   
	    
	    public void SwitchToThread() {
	    	isThread = true;
	    }
	    
	    public sealed override void OnExit( MethodExecutionEventArgs eventArgs )
	    {
	    	if( Diagram.IsDebug) {
		    	try {
			    	if( isDisabled ) return;
			    	if( Stack.Count > 0) {
				    	string callee = Stack.Pop();
				    	if( Diagram.IsTrace) Diagram.Log.Trace("<== " + callee + "\n");
				    	if( Stack.Count > 0) {
					    	string caller = Stack.Peek();
					    	if( caller == callee) {
					    		return;
					    	}
					    	if( !isConstructor) {
				 				Return(caller,callee);
					    	}
				    	}
			    	}
		    	} catch( Exception ex) {
		    		Diagram.Log.Error("Diagram Aspect failures",ex);
		    	}
	    	}
	    }
	    
		public override void OnException(MethodExecutionEventArgs eventArgs)
		{
			if( Stack.Count > 0) {
				string callee = Stack.Peek();
				Diagram.Log.Debug(callee + " (X) " + callee);
			}
		}

		public void Return(object caller, object callee) {
			if( isThread) {
				Diagram.Log.Debug(callee + " <== " + callee + " " + MethodSignature);
			} else {
				Diagram.Log.Debug(caller + " <== " + callee + " " + MethodSignature);
			}
		}
	    
	    private string GetSignature(MethodBase method) {
			ParameterInfo[] parameters = method.GetParameters();
			StringBuilder builder = new StringBuilder();
			builder.Append(method.Name);
			builder.Append("(");
			for( int i=0; i<parameters.Length; i++) {
				if( i!=0) builder.Append(",");
				ParameterInfo parameter = parameters[i];
				Type type = parameter.ParameterType;
				builder.Append(Diagram.GetTypeName(type));
			}
			builder.Append(")");
			return builder.ToString();
		}	    

		
		public static Stack<string> Stack {
			get {
				if( stack == null) {
					 stack = new Stack<string>();
				}			
				return stack;
			}
		}
		
	    private object locker = new object();
		public string MethodSignature {
			get { if( methodSignature == null) {
	    			lock( locker) {
	    				if( methodSignature == null) {
							methodSignature = GetSignature(method);
	    				}
	    			}
				}
				return methodSignature;
			}
		}
	}
}
#endif