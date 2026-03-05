








 




using System.Collections;
#pragma warning disable

namespace System
{
#region Conversions
	
	/// <summary>
	/// Static class containing extension methods For IEnumberable that convert a list
	/// to a tuple. Same as calling Tuple.ToTuple(enumerable), but shorter
	/// </summary>
	public static class TupleConversions
	{

        /// <summary>
        /// Creates a tuple of length 1 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 1 elements.</param>
        /// <returns>Tuple of length 1 that contains values from the enumerable.</returns>
		public static Tuple<T1> ToTuple<T1>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 2 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 2 elements.</param>
        /// <returns>Tuple of length 2 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2> ToTuple<T1, T2>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 3 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 3 elements.</param>
        /// <returns>Tuple of length 3 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3> ToTuple<T1, T2, T3>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 4 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 4 elements.</param>
        /// <returns>Tuple of length 4 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4> ToTuple<T1, T2, T3, T4>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 5 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 5 elements.</param>
        /// <returns>Tuple of length 5 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5> ToTuple<T1, T2, T3, T4, T5>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 6 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5, T6&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <typeparam name="T6">Type of the new Item6. Enumerable must have an object of the same type at position 6</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 6 elements.</param>
        /// <returns>Tuple of length 6 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5, T6> ToTuple<T1, T2, T3, T4, T5, T6>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5, T6>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 7 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5, T6, T7&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <typeparam name="T6">Type of the new Item6. Enumerable must have an object of the same type at position 6</typeparam>

        /// <typeparam name="T7">Type of the new Item7. Enumerable must have an object of the same type at position 7</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 7 elements.</param>
        /// <returns>Tuple of length 7 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple<T1, T2, T3, T4, T5, T6, T7>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5, T6, T7>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 8 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5, T6, T7, T8&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <typeparam name="T6">Type of the new Item6. Enumerable must have an object of the same type at position 6</typeparam>

        /// <typeparam name="T7">Type of the new Item7. Enumerable must have an object of the same type at position 7</typeparam>

        /// <typeparam name="T8">Type of the new Item8. Enumerable must have an object of the same type at position 8</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 8 elements.</param>
        /// <returns>Tuple of length 8 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5, T6, T7, T8>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 9 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5, T6, T7, T8, T9&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <typeparam name="T6">Type of the new Item6. Enumerable must have an object of the same type at position 6</typeparam>

        /// <typeparam name="T7">Type of the new Item7. Enumerable must have an object of the same type at position 7</typeparam>

        /// <typeparam name="T8">Type of the new Item8. Enumerable must have an object of the same type at position 8</typeparam>

        /// <typeparam name="T9">Type of the new Item9. Enumerable must have an object of the same type at position 9</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 9 elements.</param>
        /// <returns>Tuple of length 9 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(pList);
		}
		

        /// <summary>
        /// Creates a tuple of length 10 by taking values from the enumerable called on / passed in.
		/// Same as calling Tuple.ToTuple&lt;T1, T2, T3, T4, T5, T6, T7, T8, T9, T10&gt;(pList)
        /// </summary>

        /// <typeparam name="T1">Type of the new Item1. Enumerable must have an object of the same type at position 1</typeparam>

        /// <typeparam name="T2">Type of the new Item2. Enumerable must have an object of the same type at position 2</typeparam>

        /// <typeparam name="T3">Type of the new Item3. Enumerable must have an object of the same type at position 3</typeparam>

        /// <typeparam name="T4">Type of the new Item4. Enumerable must have an object of the same type at position 4</typeparam>

        /// <typeparam name="T5">Type of the new Item5. Enumerable must have an object of the same type at position 5</typeparam>

        /// <typeparam name="T6">Type of the new Item6. Enumerable must have an object of the same type at position 6</typeparam>

        /// <typeparam name="T7">Type of the new Item7. Enumerable must have an object of the same type at position 7</typeparam>

        /// <typeparam name="T8">Type of the new Item8. Enumerable must have an object of the same type at position 8</typeparam>

        /// <typeparam name="T9">Type of the new Item9. Enumerable must have an object of the same type at position 9</typeparam>

        /// <typeparam name="T10">Type of the new Item10. Enumerable must have an object of the same type at position 10</typeparam>

        /// <param name="pList">The Enumerable form which to take the new tuple elements. Should have at least 10 elements.</param>
        /// <returns>Tuple of length 10 that contains values from the enumerable.</returns>
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEnumerable pList)
		{
			return Tuple.ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(pList);
		}
		

	}

#endregion
}

#pragma warning enable