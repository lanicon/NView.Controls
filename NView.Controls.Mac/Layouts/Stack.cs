﻿using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;

#if __IOS__
using UIKit;
using NativeView = UIKit.UIView;
#else
using AppKit;
using NativeView = AppKit.NSView;
#endif

namespace NView.Controls
{
	/// <summary>
	/// Cross platform Stack layout for NView. Stacks can be horizontal or vertical.
	/// </summary>
	[Preserve]
	public class Stack : IView
	{
		NativeView nativeView;

		class Child
		{
			public IView View;
			public StackLayout Layout;
			public NativeView NativeView;
		}
		readonly List<Child> children = new List<Child> ();

		/// <summary>
		/// The orientation of the <see cref="Stack"/>.
		/// </summary>
		public StackOrientation Orientation { get; set; }

		/// <summary>
		/// Add a child view to this stack with the given layout.
		/// </summary>
		/// <param name="child">The child view.</param>
		/// <param name="layout">The layout to associate with the child.</param>
		public void AddChild (IView child, StackLayout layout)
		{			
			children.Add (new Child {
				View = child,
				Layout = layout,
			});
			if (nativeView != null) {
				SetStackConstraints ();
			}
		}

		void CreateNativeViewsForChildren ()
		{
			if (nativeView == null)
				return;

			foreach (var c in children) {
				if (c.NativeView != null)
					continue;
				c.NativeView = c.View.CreateBoundNativeView ();
				c.NativeView.TranslatesAutoresizingMaskIntoConstraints = false;
				nativeView.AddSubview (c.NativeView);
			}
		}

		NSLayoutConstraint[] constraints;

		void SetStackConstraints ()
		{
			if (nativeView == null)
				return;

			CreateNativeViewsForChildren ();

			var horizontal = Orientation == StackOrientation.Horizontal;

			var lefts = horizontal ?
				children.Where (x => x.Layout.HorizontalAlignment == HorizontalAlignment.Left).ToList () :
				children.Where (x => x.Layout.VerticalAlignment == VerticalAlignment.Top).ToList ();

			var centers = horizontal ?
				children.Where (x => x.Layout.HorizontalAlignment == HorizontalAlignment.Center).ToList () :
				children.Where (x => x.Layout.VerticalAlignment == VerticalAlignment.Center).ToList ();

			var rights = horizontal ?
				children.Where (x => x.Layout.HorizontalAlignment == HorizontalAlignment.Right).ToList () :
				children.Where (x => x.Layout.VerticalAlignment == VerticalAlignment.Bottom).ToList ();
			
			var newConstraints = new List<NSLayoutConstraint> ();

			//
			// Align the major axis
			//
			var leftAttr = horizontal ? NSLayoutAttribute.Left : NSLayoutAttribute.Top;
			var rightAttr = horizontal ? NSLayoutAttribute.Right : NSLayoutAttribute.Bottom;
			//var centerAttr = horizontal ? NSLayoutAttribute.CenterX : NSLayoutAttribute.CenterY;

			Action<NativeView, NSLayoutAttribute, NativeView, NSLayoutAttribute> eq = (v1, a1, v2, a2) => {
				newConstraints.Add (NSLayoutConstraint.Create (v1, a1, NSLayoutRelation.Equal, v2, a2, 1, 0));
			};

			Child prev = null;

			if (lefts.Count > 0) {

				var first = lefts.First ();
				eq (nativeView, leftAttr, first.NativeView, leftAttr);
				prev = first;
				foreach (var c in lefts.Skip (1)) {
					eq (prev.NativeView, rightAttr, c.NativeView, leftAttr);
					prev = c;
				}

			}

			Child subs = null;

			if (rights.Count > 0) {
				var last = rights.Last ();
				eq (nativeView, rightAttr, last.NativeView, rightAttr);
				subs = last;
				foreach (var c in rights.Take (rights.Count - 1).Reverse ()) {
					eq (subs.NativeView, leftAttr, c.NativeView, rightAttr);
					subs = c;
				}
			}

			if (centers.Count > 0) {
				throw new NotImplementedException ("Center alignment not yet supported");
			}

			//
			// Align the minor axis
			//
			foreach (var c in children) {
				var a = NSLayoutAttribute.Left;
				if (horizontal) {
					switch (c.Layout.VerticalAlignment) {
					case VerticalAlignment.Top:
						a = NSLayoutAttribute.Top;
						break;
					case VerticalAlignment.Center:
						a = NSLayoutAttribute.CenterY;
						break;
					default:
						a = NSLayoutAttribute.Bottom;
						break;
					}
				} else {
					switch (c.Layout.HorizontalAlignment) {
					case HorizontalAlignment.Left:
						a = NSLayoutAttribute.Left;
						break;
					case HorizontalAlignment.Center:
						a = NSLayoutAttribute.CenterX;
						break;
					default:
						a = NSLayoutAttribute.Right;
						break;
					}
				}
				eq (nativeView, a, c.NativeView, a);
			}

			// Swap out the old, put in the new
			if (constraints != null) {
				nativeView.RemoveConstraints (constraints);
			}
			constraints = newConstraints.ToArray ();
			nativeView.AddConstraints (constraints);
		}

		#region IView implementation

		/// <inheritdoc/>
		public void BindToNative (object nativeView, BindOptions options = BindOptions.None)
		{
			if (nativeView == null)
				throw new ArgumentNullException ("nativeView");
			
			UnbindFromNative ();

			this.nativeView = ViewHelpers.GetView<NativeView> (nativeView);

			SetStackConstraints ();
		}

		/// <inheritdoc/>
		public void UnbindFromNative ()
		{
			if (nativeView == null)
				return;
			foreach (var c in children) {
				c.View.UnbindFromNative ();
				c.NativeView = null;
			}
			nativeView = null;
		}

		/// <inheritdoc/>
		public object CreateNative (object context = null)
		{
			return new NativeView ();
		}

		#endregion
	}
}

