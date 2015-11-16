using System;

namespace SharpWave {
	
	public delegate TReturn Func<TReturn>();
	public delegate TReturn Func<T1, TReturn>( T1 arg1 );
	public delegate TReturn Func<T1, T2, TReturn>( T1 arg1, T2 arg2 );	
	public delegate TReturn Func<T1, T2, T3, TReturn>( T1 arg1, T2 arg2, T3 arg3 );
	
	public delegate void Action<T1>( T1 arg1 );
	public delegate void Action<T1, T2>( T1 arg1, T2 arg2 );
	public delegate void Action<T1, T2, T3>( T1 arg1, T2 arg2, T3 arg3 );
	
	public class Tuple<TKey, TValue> {
		public TKey Key;
		public TValue Value;
	}
	
	public static class Tuple {
		
		public static Tuple<TKey, TValue> Create<TKey, TValue>( TKey key, TValue value ) {
			Tuple<TKey, TValue> elem = new Tuple<TKey, TValue>();
			elem.Key = key;
			elem.Value = value;
			return elem;
		}
	}
}
