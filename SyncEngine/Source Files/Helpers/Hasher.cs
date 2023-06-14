using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class Hasher
	{
		public static int Hash(string value)
		{
			int num = 352654597;
			int num2 = num;

			for (int i = 0; i < value.Length; i += 4)
			{
				int ptr0 = value[i] << 16;
				if (i + 1 < value.Length)
					ptr0 |= value[i + 1];

				num = (num << 5) + num + (num >> 27) ^ ptr0;

				if (i + 2 < value.Length)
				{
					int ptr1 = value[i + 2] << 16;
					if (i + 3 < value.Length)
						ptr1 |= value[i + 3];
					num2 = (num2 << 5) + num2 + (num2 >> 27) ^ ptr1;
				}
			}

			return num + num2 * 1566083941;
		}
	}
}
