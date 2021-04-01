//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

using Alachisoft.NCache.Storage.Util;
using Alachisoft.NCache.Storage.Mmf;
using Alachisoft.NCache.Storage.Interop;

namespace Alachisoft.NCache.Storage.Mmf
{
    internal class ViewManager
	{
		class ViewIDComparer : IComparer
		{
			int IComparer.Compare(object x, object y)
			{
				return ((View)x).ID.CompareTo(((View)y).ID);
			}
		}

		private readonly uint RESERVED = (uint)SysUtil.AllignViewSize(0);

		private MmfFile		_mmf;
		private ArrayList	_viewsOpen;
		private ArrayList	_viewsClosed;

		private uint		_maxViews;
		private uint		_viewSize;

		/// <summary>
		/// Initializes the ViewManager.
		/// </summary>
		/// <param name="mapper"></param>
		public ViewManager(MmfFile mmf, uint viewSize)
		{
			_mmf = mmf;
			_viewSize = viewSize;
			_viewsOpen = new ArrayList();
			_viewsClosed = new ArrayList();
		}

		/// <summary>
		/// 
		/// </summary>
		public int ViewCount
		{
			get { return _viewsClosed.Count + _viewsOpen.Count; }
		}

		/// <summary>
		/// Initializes the ViewManager.
		/// </summary>
		/// <param name="mapper"></param>
		public void CreateInitialViews(uint initial)
		{
			_maxViews = initial;
			ulong maxSize = _mmf.MaxLength;
			int totalViews = (int)(maxSize / _viewSize);

			if (maxSize % _viewSize > 0)
			{
				totalViews ++;
				_mmf.SetMaxLength((ulong)(totalViews * _viewSize));
			}

			for (uint i = 0; i < totalViews; i++)
			{
				View view = new View(_mmf, i, _viewSize);
				OpenView(view);
				CloseView(view);
			}

			ReOpenViews();
		}

		/// <summary>
		/// Maps the view of the file into the memory.
		/// </summary>
		/// <param name="viewId">Id of the view to be mapped</param>
		public View OpenView(View view)
		{
			if (view == null || view.IsOpen) return view;

			try
			{
				bool scavengeMem = _viewsOpen.Count >= _maxViews;
				do 
				{
					// If there isnt enough mem or more than required views are open
					// we need to close some views.
					if (scavengeMem)
					{
						View v = GetLeastUsedOpenView();
						CloseView(v);
					}
					try
					{
						view.Open();
					}
					catch (OutOfMemoryException)
					{
					}
					scavengeMem = !view.IsOpen;
				}
				while (scavengeMem && _viewsOpen.Count > 0);

				// View still not open so bail out.
				if (scavengeMem) return null;

				if (!view.HasValidHeader) view.Format();
				_viewsClosed.Remove(view);
				_viewsOpen.Add(view);
				return view;
			}
			catch (Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Maps the view of the file into the memory.
		/// </summary>
		/// <param name="viewId">Id of the view to be mapped</param>
		public View CloseView(View view)
		{
			if (view == null || !view.IsOpen) return view;

			try
			{
				view.Close();
				_viewsOpen.Remove(view);
				_viewsClosed.Add(view);
				return view;
			}
			catch (Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Unmaps all the views of the file mapped into memory of process. 
		/// </summary>
		public void CloseAllViews()
		{
			for (int i = _viewsOpen.Count - 1; i >= 0; i--)
			{
				CloseView((View)_viewsOpen[i]);
			}
			_viewsClosed.Sort(new ViewIDComparer());
		}

		/// <summary>
		/// Unmaps all the views of the file mapped into memory of process. 
		/// </summary>
		public void ClearAllViews()
		{
			CloseAllViews();
			for (int i = _viewsClosed.Count - 1; i >= 0; i--)
			{
				View v = OpenView((View)_viewsClosed[i]);
				v.Format();
				if (i >= _maxViews)
					CloseView(v);
			}
		}

		/// <summary>
		/// Initializes the ViewManager.
		/// </summary>
		/// <param name="mapper"></param>
		public void ExtendViewsBucket(int numViews)
		{
			if (numViews < 1) return;

			uint viewCount = (uint)(_viewsClosed.Count + _viewsOpen.Count);
            try
            {
                for (uint i = 0; i < numViews; i++)
                {
                    View view = new View(_mmf, viewCount + i, _viewSize);
                    OpenView(view);
                }
            }
            catch (Exception e)
            {
                Trace.error("MmfStorage.ExtendViewsBucket" + "Error:", e.ToString());
            }
		}

		/// <summary>
		/// Initializes the ViewManager.
		/// </summary>
		/// <param name="mapper"></param>
		public void ShrinkViewsBucket(int numViews)
		{
			if (numViews < 1) return;

			uint viewCount = (uint)(_viewsClosed.Count + _viewsOpen.Count);

			CloseAllViews();

			for (uint i = 0; i < numViews; i++)
			{
				_viewsClosed.RemoveAt(_viewsClosed.Count - 1);
				if (_viewsClosed.Count < 2) break;
			}
			_viewsClosed.TrimToSize();

			ReOpenViews();
		}

		/// <summary>
		/// Initializes the ViewManager.
		/// </summary>
		/// <param name="mapper"></param>
		public void ReOpenViews()
		{
			_viewsClosed.Sort(new ViewIDComparer());
			for (uint i = 0; i < _maxViews && i < _viewsClosed.Count; i++)
			{
				OpenView((View)_viewsClosed[0]);
			}
		}

		/// <summary>
		/// Gets the view, if not mapped then it mapps the view.
		/// </summary>
		/// <param name="id">view to be mapped</param>
		/// <returns></returns>
		private View GetFreeView()
		{
			if(_viewsClosed.Count > 0)
				return (View)_viewsClosed[0];
			return null;
		}

		/// <summary>
		/// Gets the view, if not mapped then it mapps the view.
		/// </summary>
		/// <param name="id">view to be mapped</param>
		/// <returns></returns>
		public View GetViewByID(uint viewID)
		{
			for (int i = 0; i < _viewsOpen.Count; i++)
			{
				View v = (View)_viewsOpen[i];
				if (v.ID == viewID)
					return v;
			}
			for (int i = 0; i < _viewsClosed.Count; i++)
			{
				View v = (View)_viewsClosed[i];
				if (v.ID == viewID)
					return v;
			}
			return null;
		}

		/// <summary>
		/// Gets the view, if not mapped then it mapps the view.
		/// </summary>
		/// <param name="id">view to be mapped</param>
		/// <returns></returns>
		private View GetLeastUsedFreeView()
		{
			View minUsed = null;
			for (int i = 0; i < _viewsClosed.Count; i++)
			{
				View v = (View)_viewsClosed[i];
				if (minUsed == null)
				{
					minUsed = v;
					continue;
				}
				if (v.Usage < minUsed.Usage)
					minUsed = v;
			}
			return minUsed;
		}

		/// <summary>
		/// Gets the view, if not mapped then it mapps the view.
		/// </summary>
		/// <param name="id">view to be mapped</param>
		/// <returns></returns>
		private View GetLeastUsedOpenView()
		{
			View minUsed = null;
			for (int i = 0; i < _viewsOpen.Count; i++)
			{
				View v = (View)_viewsOpen[i];
				if (minUsed == null)
				{
					minUsed = v;
					continue;
				}
				if (v.Usage < minUsed.Usage)
					minUsed = v;
			}
			return minUsed;
		}

		/// <summary>
		/// Gets the view, if not mapped then it mapps the view.
		/// </summary>
		/// <param name="id">view to be mapped</param>
		/// <returns></returns>
		public View GetMatchingView(uint memRequirements)
		{
			for (int i = 0; i < _viewsOpen.Count; i++)
			{
				View v = (View)_viewsOpen[i];
				if (v.FreeSpace >= memRequirements)
					return v;
			}
			for (int i = 0; i < _viewsClosed.Count; i++)
			{
				View v = (View)_viewsClosed[i];
				if (v.FreeSpace >= memRequirements)
					return OpenView(v);
			}
			return null;
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder(1024);
			b.Append("Views Open:\r\n");
			for(int i=0; i <_viewsOpen.Count; i++)
				b.Append(_viewsOpen[i]);
			b.Append("Views Closed:\r\n");
			for (int i = 0; i < _viewsClosed.Count; i++)
				b.Append(_viewsClosed[i]);
			return b.ToString();
		}
	}

}
