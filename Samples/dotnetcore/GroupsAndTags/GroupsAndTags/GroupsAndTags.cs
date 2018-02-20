// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// A sample program that demonstrates how to utilize the Groups, Tags and NamedTags feature
    /// in NCache.
    /// 
    /// Requirements:
    ///     1. A running NCache cache
    ///     2. Connection attributes in app.config
    /// </summary>
	public class GroupsAndTags
	{

		public static void Main(string[] args)
		{
            try
            {
                Alachisoft.NCache.Samples.Groups.Run();

                Alachisoft.NCache.Samples.Tags.Run();

                Alachisoft.NCache.Samples.NamedTags.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
		}
	}
}