using System;
using System.Collections.Generic;
using System.Text;

namespace BiDirectionalSync
{
	public class ColumnChange
	{
		public ColumnChange()
		{
			ChangedOn = DateTimeOffset.Now;
		}
		public string Column { get; set; }
		public DateTimeOffset ChangedOn { get; private set; }
	}
}
