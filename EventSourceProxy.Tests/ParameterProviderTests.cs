﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class ParameterProviderTests : BaseLoggingTest
	{
		#region Collapsing Parameters
		public interface IHaveTraceAs
		{
			[TraceAsData]
			void TraceAsData(string p1, string p2);

			void TraceSomeParameters(string p, [TraceAsData] string p1, [TraceAsData] string p2);
		}

		[Test]
		public void InterfaceParametersCanBeCollapsedTogether()
		{
			EnableLogging<IHaveTraceAs>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<IHaveTraceAs>();
			testLog.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());
		}

		[Test]
		public void InterfaceParametersCanBeCollapsedIndividually()
		{
			EnableLogging<IHaveTraceAs>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<IHaveTraceAs>();
			testLog.TraceSomeParameters("p", "p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(2, events[0].Payload.Count);
			Assert.AreEqual("p", events[0].Payload[0].ToString());
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[1].ToString());
		}
		#endregion

		#region Proxy Collapsing
		public class HasTraceAs : IHaveTraceAs
		{
			public void TraceAsData(string p1, string p2)
			{
			}

			public void TraceSomeParameters(string p, string p1, string p2)
			{
			}
		}

		public class VirtualHasTraceAs : IHaveTraceAs
		{
			public virtual void TraceAsData(string p1, string p2)
			{
			}

			public virtual void TraceSomeParameters(string p, string p1, string p2)
			{
			}
		}

		public class VirtualHasTraceAsWithoutInterface
		{
			[TraceAsData]
			public virtual void TraceAsData(string p1, string p2)
			{
			}

			public virtual void TraceSomeParameters(string p, [TraceAsData]string p1, [TraceAsData]string p2)
			{
			}
		}

		[Test]
		public void ProxyFromClassToInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<IHaveTraceAs>(new HasTraceAs());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassToInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<IHaveTraceAs>(new VirtualHasTraceAs());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassWithoutInterfaceCanCollapseParameters()
		{
			EnableLogging<VirtualHasTraceAsWithoutInterface>();

			var proxy = TracingProxy.Create<VirtualHasTraceAsWithoutInterface>(new VirtualHasTraceAsWithoutInterface());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		[Test]
		public void ProxyFromVirtualClassWithoutInterfaceToUnrelatedInterfaceCanCollapseParameters()
		{
			EnableLogging<IHaveTraceAs>();

			var proxy = TracingProxy.Create<VirtualHasTraceAsWithoutInterface, IHaveTraceAs>(new VirtualHasTraceAsWithoutInterface());
			proxy.TraceAsData("p1", "p2");
			proxy.TraceSomeParameters("p", "p1", "p2");

			VerifyEvents();
		}

		private void EnableLogging<TLog>() where TLog : class
		{
			// create the logger and make sure it is serializing the parameters properly
			var logger = EventSourceImplementer.GetEventSource<TLog>();
			_listener.EnableEvents(logger, EventLevel.LogAlways, (EventKeywords)(-1));
		}

		private void VerifyEvents()
		{
			// look at the events again
			var events = _listener.Events.ToArray();
			Assert.AreEqual(4, events.Length);

			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());

			Assert.AreEqual(2, events[2].Payload.Count);
			Assert.AreEqual("p", events[2].Payload[0].ToString());
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[2].Payload[1].ToString());
		}
		#endregion

		#region EventSource Implementation
		public abstract class EventSourceWithCollapse : EventSource
		{
			[TraceAsData]
			public abstract void TraceAsData(string p1, string p2);
		}

		[Test]
		public void CanApplyTraceAsDataToEventSourceMethods()
		{
			EnableLogging<EventSourceWithCollapse>();

			// do some logging
			var testLog = EventSourceImplementer.GetEventSourceAs<EventSourceWithCollapse>();
			testLog.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p1\":\"p1\",\"p2\":\"p2\"}", events[0].Payload[0].ToString());
		}
		#endregion

		#region Provider Tests
		[TraceParameterProvider(typeof(MyCollapseAllTPP))]
		public interface InterfaceWithProvider
		{
			void TraceAsData(string p1, string p2);
		}

		public class MyCollapseAllTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				var mappings = new List<ParameterMapping>();
				var mapping = new ParameterMapping("data");
				mappings.Add(mapping);
				foreach (var p in methodInfo.GetParameters().Reverse())
					mapping.AddSource(p);

				return mappings.AsReadOnly();
			}
		}

		[Test]
		public void CanReplaceParameterProvider()
		{
			EnableLogging<InterfaceWithProvider>();

			var log = EventSourceImplementer.GetEventSourceAs<InterfaceWithProvider>();
			log.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(1, events[0].Payload.Count);
			Assert.AreEqual("{\"p2\":\"p2\",\"p1\":\"p1\"}", events[0].Payload[0].ToString());
		}

		[TraceParameterProvider(typeof(MyEmptyTPP))]
		public interface InterfaceWithEmptyProvider
		{
			void TraceAsData(string p1, string p2);
		}

		public class MyEmptyTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				var mappings = new List<ParameterMapping>();
				var mapping = new ParameterMapping("data");
				mappings.Add(mapping);

				// return a mapping with an empty source

				return mappings.AsReadOnly();
			}
		}

		[Test]
		public void CanReturnEmptySourceList()
		{
			EnableLogging<InterfaceWithEmptyProvider>();

			var log = EventSourceImplementer.GetEventSourceAs<InterfaceWithEmptyProvider>();
			log.TraceAsData("p1", "p2");

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(0, events[0].Payload.Count);
		}

		public interface UntaggedInterface
		{
			void TraceAsData(string p1, string p2);
		}

		[Test]
		public void CanSetDefaultProvider()
		{
			EventSourceImplementer.RegisterDefaultProvider(new MyCollapseAllTPP());
			try
			{
				EnableLogging<UntaggedInterface>();

				var log = EventSourceImplementer.GetEventSourceAs<UntaggedInterface>();
				log.TraceAsData("p1", "p2");

				// look at the events
				var events = _listener.Events.ToArray();
				Assert.AreEqual(1, events.Length);
				Assert.AreEqual(1, events[0].Payload.Count);
				Assert.AreEqual("{\"p2\":\"p2\",\"p1\":\"p1\"}", events[0].Payload[0].ToString());
			}
			finally
			{
				EventSourceImplementer.RegisterDefaultProvider((TraceParameterProvider)null);
			}
		}
		#endregion

		#region Exploding Parameters
		public class EmailChange
		{
			public string From;
			public string To;
			public DateTime When;
		}

		[TraceParameterProvider(typeof(TempTPP))]
		public interface ILogEmailChanges
		{
			void LogChange(EmailChange email);
		}

		[Test]
		public void ParametersCanBeExploded()
		{
			EnableLogging<ILogEmailChanges>();

			// do some logging
			var log = EventSourceImplementer.GetEventSourceAs<ILogEmailChanges>();
			var change = new EmailChange() { From = "me", To = "you", When = new DateTime(2010, 1, 1) };
			log.LogChange(change);

			// look at the events
			var events = _listener.Events.ToArray();
			Assert.AreEqual(1, events.Length);
			Assert.AreEqual(3, events[0].Payload.Count);
			Assert.AreEqual(change.From, events[0].Payload[0].ToString());
			Assert.AreEqual(change.To, events[0].Payload[1].ToString());
			Assert.AreEqual(change.When.ToString(), events[0].Payload[2].ToString());
		}

		class TempTPP : TraceParameterProvider
		{
			public override IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(System.Reflection.MethodInfo methodInfo)
			{
				List<ParameterMapping> mapping = new List<ParameterMapping>();

				var from = new ParameterMapping("from");
				mapping.Add(from);
				from.AddSource(methodInfo.GetParameters()[0], (EmailChange e) => e.From);

				var to = new ParameterMapping("to");
				to.AddSource(methodInfo.GetParameters()[0], (EmailChange e) => e.To);
				mapping.Add(to);

				var when = new ParameterMapping("when");
				when.AddSource(methodInfo.GetParameters()[0], (EmailChange e) => e.When);
				mapping.Add(when);

				return mapping.AsReadOnly();
			}
		}

		// TODO: test for nice exceptions when the expression input doesn't match the parameter type
		// TODO: find a way to make this code pretty (e.g. attributes, fluent config)
		#endregion
	}
}
