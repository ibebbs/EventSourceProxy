﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EventSourceProxy.Tests
{
	[TestFixture]
	public class EventActivityScopeTests
	{
		[Test]
		public void GetActivityIdShouldReturnEmptyGuid()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
			}
		}

		[Test]
		public void NewScopeShouldGenerateNewActivity()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope())
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreNotEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}

		[Test]
		public void ReuseScopeShouldReuseActivityId()
		{
			using (EventActivityScope scope1 = new EventActivityScope())
			{
				// make sure we don't have an outer activity, but do have an inner activity
				Assert.AreEqual(Guid.Empty, scope1.PreviousActivityId);
				Assert.AreNotEqual(Guid.Empty, scope1.ActivityId);

				using (EventActivityScope scope2 = new EventActivityScope(true))
				{
					Assert.AreEqual(scope1.ActivityId, scope2.PreviousActivityId);
					Assert.AreEqual(scope1.ActivityId, scope2.ActivityId);
				}
			}
		}
	}
}
